using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Discord.Models;

namespace Discord.Net;

internal enum Opcode
{
    Dispatch,
    Heartbeat,
    Identify,
    PresenceUpdate,
    VoiceStateUpdate,
    Resume = 6,
    Reconnect,
    RequestGuildMembers,
    InvalidSession,
    Hello,
    HeartbeatAck
}

public sealed class Gateway
{
    #region Events
    
    # region Messages
    
    /// <summary>
    /// Dispatched when a message is sent. Requires <see cref="Intents.GuildMessages"/> and or <see cref="Intents.DmMessages"/>.
    /// </summary>
    public event EventHandler<Message>? OnMessageCreate;
    
    #endregion
    
    #endregion
    
    internal ClientWebSocket _webSocket = new();
    internal Intents _intents;
    internal string? _sessionId;
    internal string? _resumeGatewayUrl;
    
    private const int GatewayVersion = 10;
    private Bot _bot;
    private readonly string _token;
    private ulong? _lastSequence;
    private CancellationTokenSource _cts = new();
    private Task _heartbeatTask;
    private Task _receiveTask;
    private int _heartbeatInterval;
    private bool _heartbeatResponse;

    internal Gateway(Bot bot, string token, Intents intents)
    {
        _bot = bot;
        _token = token;
        _intents = intents;
        _heartbeatResponse = false;
    }

    // Connects to the Discord Gateway and starts processing events.
    internal async Task ConnectAsync(Opcode type)
    {
        if (!_cts.IsCancellationRequested)
            await _cts.CancelAsync();
        
        List<Opcode> validTypes = [Opcode.Identify, Opcode.Resume];
        if (!validTypes.Contains(type))
            throw new ArgumentException($"Opcode type {type} is invalid in this context");
        
        if (_webSocket.State == WebSocketState.Open)
            await _webSocket.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, _cts.Token);
        
        _cts = new CancellationTokenSource();
        _webSocket = new ClientWebSocket();
        
        if (_heartbeatInterval > 0)
            _webSocket.Options.KeepAliveInterval = TimeSpan.FromMilliseconds(_heartbeatInterval);

        // Connect to the gateway
        string wssUrl = await _bot._rest.GetGatewayAsync();
        var uri = new Uri(_resumeGatewayUrl ?? wssUrl + $"/?v={GatewayVersion}&encoding=json");
        await _webSocket.ConnectAsync(uri, _cts.Token);
        Dev.Log("New connection initiated");

        // Receive the HELLO event
        var helloPayload = await ReceivePayloadAsync();
        
        if (helloPayload is null)
            throw new GatewayException("Unexpected null payload disconnect");
        if (helloPayload.Op != (int)Opcode.Hello)
            throw new DiscordException("Expected Hello event");

        _heartbeatInterval = helloPayload.Data["heartbeat_interval"]!.Value<int>();
        Dev.Log($"Received Opcode HELLO, heartbeat interval: {_heartbeatInterval}ms");


        // Send Identify or Resume
        if (type == Opcode.Resume)
            await SendResumeAsync();
        else
        {
            ResetCoreValues();
            await SendIdentifyAsync();
        }

        // Start the heartbeat/gateway receive tasks
        _heartbeatTask = Task.Run(HeartbeatLoopAsync, _cts.Token);
        _receiveTask = Task.Run(ProcessReceiveAsync, _cts.Token);
        await _receiveTask.ContinueWith(task =>
        {
            if (task.IsFaulted)
                throw task.Exception;
        });
    }

    // Used when the end user wants to disconnect from the gateway, otherwise not used internally
    internal async Task UserDisconnectAsync(bool instant)
    {
        ResetCoreValues();
        await _webSocket.CloseAsync(
            instant ? WebSocketCloseStatus.NormalClosure : WebSocketCloseStatus.Empty,
            string.Empty, 
            _cts.Token);
        await _cts.CancelAsync();
    }

    // Resets values associated with the gateway that would indicate a new connection
    private void ResetCoreValues()
    {
        _sessionId = null;
        _resumeGatewayUrl = null;
        _lastSequence = null;
    }

    // Main event processing loop
    private async Task ProcessReceiveAsync()
    {
        int? closeCode = -1;
        while (_webSocket.State == WebSocketState.Open)
        {
            var payload = await ReceivePayloadAsync();
            if (payload is null) // WebSocket closed
            {
                if (_webSocket.CloseStatus != null) // According to Discord, sometimes the connection can close with no close code
                    closeCode = (int)_webSocket.CloseStatus;
                Dev.Log($"WebSocket closed by Discord with error code {closeCode}");
                break;
            }
            await ProcessPayloadAsync(payload);
        }

        switch (closeCode)
        {
            case -1:
                Dev.Log("WebSocket closed with no close code - resuming");
                await ConnectAsync(Opcode.Reconnect);
                break;
            case 4000:
                Dev.Log("Unknown error/Discord wasn't sure what went wrong - reconnecting", ConsoleColor.Red);
                await ConnectAsync(Opcode.Reconnect);
                break;
            case 4001:
                throw new UnknownOpcodeException("An invalid Gateway opcode or an invalid payload for an opcode was sent");
            case 4002:
                throw new DecodeErrorException("An invalid payload was sent.");
            case 4003:
                Dev.Log("A payload prior to identifying was sent, or this session has been invalidated - starting a new session", ConsoleColor.Red);
                await ConnectAsync(Opcode.Identify);
                break;
            case 4004:
                throw new AuthenticationFailedException("The account token sent with your identify payload is incorrect");
            case 4005:
                throw new AlreadyAuthenticatedException("More than one identify payload was sent");
            case 4007:
                Dev.Log("The sequence sent when resuming the session was invalid - starting a new session", ConsoleColor.Red);
                await ConnectAsync(Opcode.Identify);
                break;
            case 4008:
                Dev.Log("Payloads are being sent too quickly - reconnecting",  ConsoleColor.Red);
                await ConnectAsync(Opcode.Reconnect);
                break;
            case 4009:
                Dev.Log("Session timed out - starting a new session", ConsoleColor.Red);
                await ConnectAsync(Opcode.Identify);
                break;
            case 4010:
                throw new InvalidShardException("An invalid shard was sent when identifying");
            case 4011:
                throw new ShardingRequiredException("The session would have handled too many guilds - you are required to shard your connection in order to connect");
            case 4012:
                throw new InvalidApiVersionException("An invalid version for the gateway was sent");
            case 4013:
                throw new InvalidIntentsException("An invalid intent for a Gateway Intent was sent");
            case 4014:
                throw new DisallowedIntentsException(
                    "A disallowed intent for a Gateway Intent was sent. An intent may have been specified that you have not enabled or are not approved for.");
        }
    }

    /// Sends the Resume payload to resume a previous session.
    private async Task SendResumeAsync()
    {
        var resume = new
        {
            op = (int)Opcode.Resume,
            d = new
            {
                token = _token,
                session_id = _sessionId,
                seq = _lastSequence
            }
        };
        await SendPayloadAsync(resume);
        Dev.Log("RESUME payload sent");
    }

    // Sends the Identify payload to authenticate with the Gateway.
    private async Task SendIdentifyAsync()
    {
        const string lib = "discord.cs";
        var identify = new
        {
            op = (int)Opcode.Identify,
            d = new
            {
                intents = (int)_intents,
                token = _token,
                properties = new
                {
                    os = Environment.OSVersion.ToString(),
                    browser = lib,
                    device = lib
                },
            }
        };
        
        await SendPayloadAsync(identify);
        Dev.Log("Opcode IDENTIFY payload sent");
    }
    
    // Receives a payload from the WebSocket connection
    private async Task<GatewayPayload?> ReceivePayloadAsync()
    {
        List<byte> allBytes = [];
        ArraySegment<byte> buffer = new(new byte[4096]);
        WebSocketReceiveResult result;

        // ReceiveAsync() needs to be called multiple times to complete the message. What would happen was if it was only
        // called once, an error would occur because JsonConvert.DeserializeObject could not work as intended due to
        // it only receiving a portion of the JSON response. This insures the entire response is added together (as bytes)
        // until the entire response is received
        do
        {
            result = await _webSocket.ReceiveAsync(buffer, _cts.Token);
            if (result.CloseStatus is not null) continue;
            for (var i = 0; i < result.Count; i++)
                allBytes.Add(buffer.Array![i]);
        } while (!result.EndOfMessage);
        

        if (result.MessageType == WebSocketMessageType.Close)
            return null;
        
        var json = Encoding.UTF8.GetString(allBytes.ToArray(), 0, allBytes.Count);
        return JsonConvert.DeserializeObject<GatewayPayload>(json);
    }

    // Runs the heartbeat loop to keep the connection alive
    private async Task HeartbeatLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            await Task.Delay(_heartbeatInterval, _cts.Token);
            if (_webSocket.State == WebSocketState.Open)
            {
                await SendHeartbeatAsync();
            }
        }
    }

    // Sends a Heartbeat payload to the Gateway
    private async Task SendHeartbeatAsync()
    {
        var heartbeat = new
        {
            op = (int)Opcode.Heartbeat,
            d = _lastSequence
        };
        await SendPayloadAsync(heartbeat);
        Dev.Log("Heartbeat sent");

        // Discord documentation:
        // If a client does not receive a heartbeat ACK between its attempts at sending heartbeats, this may be due to
        // a failed or "zombied" connection. The client should immediately terminate the connection with any close code
        // besides 1000 or 1001, then reconnect and attempt to Resume.
        _heartbeatResponse = false;
        
        // Wait for heartbeat ACK to be sent by Discord.
        await Task.Delay(1500,  _cts.Token);
        
        if (!_heartbeatResponse)
        {
            Dev.Log("Heartbeat timed out");
            await ConnectAsync(Opcode.Resume);
        }
    }

    // Sends a payload over the WebSocket connection.
    private async Task SendPayloadAsync(object payload)
    {
        var json = JsonConvert.SerializeObject(payload);
        var buffer = Encoding.UTF8.GetBytes(json);
        await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cts.Token);
    }
    
    // Converts the Discord JSON payload into an object in this library.
    internal static T Deserialize<T>(object? payload)
    {
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(payload))!;
    }

    // Processes incoming Gateway payloads.
    private async Task ProcessPayloadAsync(GatewayPayload payload)
    {
        // Update sequence number if present.
        if (payload.Sequence.HasValue)
            _lastSequence = payload.Sequence.Value;

        switch (payload.Op)
        {
            case 0: // Dispatch (this contains every common event)
                switch (payload.EventName)
                {
                    case "READY":
                        var readyData = (JObject)payload.Data;
                        _sessionId = readyData["session_id"]!.ToString();
                        _resumeGatewayUrl = payload.Data["resume_gateway_url"] + $"/?v={GatewayVersion}&encoding=json";
                        Dev.Log($"READY received, session ID: {_sessionId}");
                        break;
                    case "RESUMED":
                        Dev.Log("Gateway resumed (from dispatch)", ConsoleColor.Green);
                        break;
                    case "MESSAGE_CREATE":
                        var messageCreated = Deserialize<Message>(payload.Data);
                        OnMessageCreate?.Invoke(this, messageCreated);
                        break;
                }
                break;

            case 1: // Heartbeat request
                Dev.Log("Received heartbeat request");
                await SendHeartbeatAsync();
                break;
            
            case 6: // Resume
                Dev.Log("Gateway resumed", ConsoleColor.Green);
                break;
            
            case 7: // Reconnect
                Dev.Log("Reconnected requested - attempting reconnect with RESUME", ConsoleColor.Yellow);
                await ConnectAsync(Opcode.Resume);
                break;

            case 9: // Invalid Session
                var resumable = payload.Data.Value<bool>();
                Dev.Log($"Invalid session received from dispatch, resumable: {resumable}", ConsoleColor.Red);
                if (resumable)
                    await ConnectAsync(Opcode.Resume);
                else
                    await ConnectAsync(Opcode.Identify);
                break;

            case 11: // Heartbeat ACK
                Dev.Log("Heartbeat ACK received");
                _heartbeatResponse = true;
                break;

            default:
                Dev.Log($"Unhandled opcode: {payload.Op}");
                break;
        }
    }
}

// Represents a Gateway payload.
internal record GatewayPayload
{
    [JsonProperty("op")]
    internal int Op { get; set; }

    [JsonProperty("d")]
    internal JToken Data { get; set; }

    [JsonProperty("t")]
    internal string EventName { get; set; }

    [JsonProperty("s")]
    internal ulong? Sequence { get; set; }
}

internal static class Dev
{
    internal static void Log(string message, ConsoleColor color = ConsoleColor.White, bool timestamp = true)
    {
        if (Environment.GetEnvironmentVariable("##set_logging##") is null) return;
        var m = timestamp ? $"[{DateTime.Now:MM-dd-yyyy HH:mm:ss.fff}] {message}" : message;
        Console.WriteLine(m, color);
        Console.ResetColor();
    }
}

