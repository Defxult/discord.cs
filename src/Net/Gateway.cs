using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Discord.Models;
using Discord.Utility;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

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

public record Shard
{
    public int Id { get; init; }
    public int TotalShards { get; internal set; }
    public string? Nickname;

    public Shard(int id, string? nickname = null)
    {
        Id = id;
        Nickname = nickname;
    }
}

public sealed class DiscordGatewayClient
{
    #region Events
    
    # region Messages
    
    /// <summary>
    /// Dispatched when a message is sent. Requires <see cref="Intents.GuildMessages"/> and or <see cref="Intents.DmMessages"/>.
    /// </summary>
    public event EventHandler<Message>? OnMessageCreate;
    
    #endregion
    
    #endregion
    
    internal string? _sessionId;
    internal string? _resumeGatewayUrl;
    internal const string UriParameters = "/?v=10&encoding=json";
    internal ClientWebSocket _ws;
    internal bool _userTerminated;
    
    private Bot _bot;
    private Intents _intents;
    private readonly string _token;
    private ulong? _lastSequence;
    private CancellationTokenSource _cts;
    private Task _heartbeatTask;
    private Task _receiveTask;
    private int _heartbeatInterval;
    private bool _heartbeatResponse;
    private bool _identifyRequired;

    internal DiscordGatewayClient(Bot bot, string token, Intents intents)
    {
        _bot = bot;
        _token = token;
        _intents = intents;
        _heartbeatInterval = 30_000;
        _heartbeatResponse = false;
        _identifyRequired = true;
        _cts = new CancellationTokenSource();
        _ws = new ClientWebSocket();
        _userTerminated = false;
    }

    // Gracefully closes the WebSocket connection and sets a new WebSocket object and CancellationTokenSource.
    private async Task RefreshWebSocket()
    {
        try
        {
            if (_ws.State == WebSocketState.Open)
            {
                await _ws.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, _cts.Token);
                Dev.Log("[GW] Client WebSocket Closed");
            }
        }
        catch (Exception e)
        {
            Dev.Log($"[ERROR, RefreshWebSocket] {e.Message}");
        }
        finally
        {
            await _cts.CancelAsync();
            _ws.Dispose();
            _ws = new ClientWebSocket();
            _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);
            _cts = new CancellationTokenSource();
        }
    }

    // Connects to the Discord Gateway and starts processing events.
    internal async Task ConnectAsync(bool resume)
    {
        await RefreshWebSocket();
        if (resume)
        {
            Dev.Log("[GW] Connecting with RESUME");
            await _ws.ConnectAsync(new Uri(_resumeGatewayUrl + UriParameters), _cts.Token);
            await SendResumeAsync();
        }
        else
        {
            string wss = await _bot._rest.GetGatewayAsync();
            //await _bot._rest.GetGatewayBotAsync();
            Dev.Log("[GW] Connecting with IDENTIFY");
            await _ws.ConnectAsync(new Uri(wss + UriParameters), _cts.Token);
        }

        // Start the heartbeat/gateway receive tasks.
        _heartbeatTask = Task.Run(HeartbeatLoopAsync, _cts.Token);
        _receiveTask = Task.Run(ReceiveAsync).ContinueWith(task =>
        {
            if (task.IsFaulted)
                throw task.Exception.InnerException!;
        }, _cts.Token);;
    }

    // Used when the end user wants to disconnect from gateway, otherwise not used internally
    // internal async Task UserDisconnectAsync(bool instant)
    // {
    //     ResetCoreValues();
    //     await _ws.CloseAsync(
    //         instant ? WebSocketCloseStatus.NormalClosure : WebSocketCloseStatus.Empty,
    //         string.Empty, 
    //         _cts.Token);
    //     await _cts.CancelAsync();
    // }

    // Resets values associated with the gateway that would indicate a new connection.
    private void ResetCoreValues()
    {
        _sessionId = null;
        _resumeGatewayUrl = null;
        _lastSequence = null;
        _identifyRequired = true;
    }

    // Keeps the connection alive with Discords required heartbeats.
    private async Task HeartbeatLoopAsync()
    {
        Dev.Log("[GW] Heartbeat loop started");
        while (true)
        {
            await Task.Delay(_heartbeatInterval, _cts.Token);
            if (_ws.State == WebSocketState.Open)
                await SendHeartbeatAsync();
            else
            {
                Dev.Log($"[GW] Heartbeat loop terminated due to connection state ({_ws.State})");
                break;
            }
        }
    }

    // Main event processing loop.
    private async Task ReceiveAsync()
    {
        var closeCode = -1;
        while (_ws.State == WebSocketState.Open)
        {
            GatewayPayload? payload = await ConvertPayloadAsync();
            if (payload is not null) // AKA isn't closed
                await HandleDiscordEventAsync(payload);
            else
            {
                // According to Discord, sometimes the connection can close with no close code.
                if (_ws.CloseStatus is { } status)
                    closeCode = (int)status;
            }
        }
        
        // Identifies which types of disconnects are resumable.
        // https://discord.com/developers/docs/topics/opcodes-and-status-codes#gateway-gateway-close-event-codes
        switch (closeCode)
        {
            case -1:
                Dev.Log("[GW] WebSocket closed with no close code - resuming session");
                await ConnectAsync(true);
                break;
            case 4000:
                Dev.Log("[GW] Unknown error/Discord wasn't sure what went wrong - resuming session", ConsoleColor.Yellow);
                await ConnectAsync(true);
                break;
            case 4001:
                throw new UnknownOpcodeException(
                    "An invalid Gateway opcode or an invalid payload for an opcode was sent");
            case 4002:
                throw new DecodeErrorException("An invalid payload was sent");
            case 4003:
                Dev.Log(
                    "A payload prior to identifying was sent, or this session has been invalidated - starting a new session",
                    ConsoleColor.Red);
                await ConnectAsync(false);
                break;
            case 4004:
                throw new AuthenticationFailedException(
                    "The account token sent with your identify payload is incorrect");
            case 4005:
                throw new AlreadyAuthenticatedException("More than one identify payload was sent");
            case 4007:
                Dev.Log("The sequence sent when resuming the session was invalid - starting a new session",
                    ConsoleColor.Red);
                await ConnectAsync(false);
                break;
            case 4008:
                Dev.Log("Payloads are being sent too quickly - resuming session", ConsoleColor.Red);
                await ConnectAsync(true);
                break;
            case 4009:
                Dev.Log("Session timed out - starting new session", ConsoleColor.Red);
                await ConnectAsync(false);
                break;
            case 4010:
                throw new InvalidShardException("An invalid shard was sent when identifying");
            case 4011:
                throw new ShardingRequiredException(
                    "The session would have handled too many guilds - you are required to shard your connection in order to connect");
            case 4012:
                throw new InvalidApiVersionException("An invalid version for the gateway was sent");
            case 4013:
                throw new InvalidIntentsException("An invalid intent for a Gateway Intent was sent");
            case 4014:
                throw new DisallowedIntentsException(
                    "A disallowed intent for a Gateway Intent was sent. An intent may have been specified that you have not enabled or are not approved for");
            default:
                Dev.Log($"[GW] WebSocket closed with unhandled close code ({closeCode}:WS state {_ws.State}) - attempting resume");
                await ConnectAsync(true);
                break;
        }
    }

    // Sends the Resume payload to continue a previous session.
    private async Task SendResumeAsync()
    {
        var resume = new
        {
            op = Opcode.Resume,
            d = new
            {
                token = _token,
                session_id = _sessionId,
                seq = _lastSequence
            }
        };
        await SendJsonAsync(resume);
        Dev.Log("[GW] RESUME payload sent");
    }

    // Sends the Identify payload to authenticate with the gateway.
    private async Task SendIdentifyAsync()
    {
        const string lib = "discord.cs";
        var identify = new
        {
            op = Opcode.Identify,
            shard = new[] { _bot.ShardId, 1 }, // TODO
            d = new
            {
                intents = _intents,
                token = _token,
                properties = new
                {
                    os = Environment.OSVersion.ToString(),
                    browser = lib,
                    device = lib
                },
            }
        };
        await SendJsonAsync(identify);
        Dev.Log("[GW] IDENTIFY payload sent");
    }
    
    // Converts the Discord JSON payload into an object in this library.
    private async Task SendJsonAsync(object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var seg = Encoding.UTF8.GetBytes(json);
        await _ws.SendAsync(seg, WebSocketMessageType.Text, true, _cts.Token);
    }
    
    // Generates the payload into a single payload object which contains things such as the event name, its data, etc.
    private async Task<GatewayPayload?> ConvertPayloadAsync()
    {
        try
        {
            var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
            var builder = new ArrayBufferWriter<byte>();
            WebSocketReceiveResult? result;
            do
            {
                result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                    return null;
                builder.Write(new ReadOnlySpan<byte>(buffer, 0, result.Count));
            } while (!result.EndOfMessage);

            var data = builder.WrittenSpan.ToArray();
            var json = Encoding.UTF8.GetString(data);
            var doc = JsonDocument.Parse(json);
            return GatewayPayload.FromJson(doc.RootElement);
        }
        catch (Exception ex)
        {
            Dev.Log($"[GW ERROR] - {ex.Message}", ConsoleColor.Red);
            return null;
        }
    }
    
    // Sends a Heartbeat payload to the gateway.
    private async Task SendHeartbeatAsync()
    {
        var heartbeat = new
        {
            op = Opcode.Heartbeat,
            d = _lastSequence
        };
        await SendJsonAsync(heartbeat);
        Dev.Log("[GW] HEARTBEAT payload sent");

        await VerifyHeartbeat();
        return;

        // Discord documentation:
        // If a client does not receive a heartbeat ACK between its attempts at sending heartbeats, this may be due to
        // a failed or "zombied" connection. The client should immediately terminate the connection with any close code
        // besides 1000 or 1001, then reconnect and attempt to Resume.
        async Task VerifyHeartbeat()
        {
            _heartbeatResponse = false;
            
            // Wait for heartbeat ACK to be sent by Discord. This response time can differ based on server host location.
            // For now, waiting ~2 seconds seems like enough time to believe that a possible "zombie" connection occurred.
            var timeout = TimeSpan.FromSeconds(2);
            await Task.Delay(timeout, _cts.Token);
            
            if (!_heartbeatResponse)
            {
                Dev.Log($"[GW] Heartbeat timed out ({timeout.Seconds}s) - resuming session");
                await ConnectAsync(true);
            }
        }
    }

    // Sets the most recent sequence number so the gateway can conduct a session Resume.
    private void UpdateSequence(GatewayPayload payload)
    {
        if (payload.S.HasValue)
            _lastSequence = payload.S.Value;
    }

    private static JsonElement GetElementValue(JsonElement element, string key) => 
        element.GetProperty(key);
    
    internal static T DeserializeWithNewtonsoft<T>(JsonElement element)
    {
        string json = element.GetRawText();
        //Console.WriteLine(json + "\n\n\n");
        return JsonConvert.DeserializeObject<T>(json)!;
    }
    
    // Processes incoming Gateway events/payloads.
    private async Task HandleDiscordEventAsync(GatewayPayload payload)
    {
        UpdateSequence(payload);
        switch (payload.Op)
        {
            case 0: // Dispatch (this contains every common event)
                switch (payload.T)
                {
                    case "READY":
                        _sessionId = GetElementValue(payload.D!.Value, "session_id").ToString();
                        _resumeGatewayUrl = GetElementValue(payload.D!.Value, "resume_gateway_url").ToString();
                        Dev.Log($"[GW] READY received, session ID: {_sessionId}");
                        break;
                    case "RESUMED":
                        Dev.Log("[GW] Successfully resumed", ConsoleColor.Green);
                        break;
                    case "MESSAGE_CREATE":
                        var messageCreated = DeserializeWithNewtonsoft<Message>(payload.D!.Value);
                        messageCreated.Bot = _bot;
                        // INSERT
                        _bot.CacheMessage(messageCreated);
                        OnMessageCreate?.Invoke(this, messageCreated);
                        break;
                    
                    case "GUILD_CREATE":
                        var guildCreated = DeserializeWithNewtonsoft<Guild>(payload.D!.Value);
                        guildCreated.Bot = _bot;
                        _bot._guilds.Add(guildCreated);
                        break;
                }

                break;

            case 1: // Heartbeat request
                Dev.Log("[GW] HEARTBEAT request received");
                await SendHeartbeatAsync();
                break;
            
            case 7: // Reconnect
                Dev.Log("[GW] RECONNECT request received - resuming session", ConsoleColor.Yellow);
                await ConnectAsync(true);
                break;

            case 9: // Invalid Session
                var resumable = GetElementValue(payload.D!.Value, "d").GetBoolean();
                Dev.Log($"[GW] INVALID SESSION received, resumable: {resumable}", ConsoleColor.Red);
                if (resumable)
                    await ConnectAsync(true);
                else
                {
                    ResetCoreValues();
                    await ConnectAsync(false);
                }
                break;
            
            case 10: // Hello
                if (_identifyRequired)
                {
                    _heartbeatInterval = GetElementValue(payload.D!.Value, "heartbeat_interval") .GetInt32();
                    Dev.Log($"[GW] HELLO received, heartbeat interval set ({_heartbeatInterval}ms)");
                    await SendIdentifyAsync();
                    _identifyRequired = false;
                }
                else
                    Dev.Log("[GW] HELLO received - IDENTIFY not required for RESUME (continuing)");
                break;

            case 11: // Heartbeat ACK
                Dev.Log("[GW] HEARTBEAT ACK received");
                _heartbeatResponse = true;
                break;

            default:
                Dev.Log($"Unhandled opcode: {payload.Op}");
                break;
        }
    }
}

// Represents a Gateway payload.
public sealed record GatewayPayload(int Op, JsonElement? D, ulong? S, string? T)
{
    public static GatewayPayload FromJson(JsonElement root)
    {
        int op = root.GetProperty("op").GetInt32();
        JsonElement? d = root.TryGetProperty("d", out var dVal) ? dVal : null;
        ulong? s = root.TryGetProperty("s", out var sVal) && sVal.ValueKind != JsonValueKind.Null ? sVal.GetUInt64() : null;
        string? t = root.TryGetProperty("t", out var tVal) && tVal.ValueKind != JsonValueKind.Null ? tVal.GetString() : null;
        return new GatewayPayload(op, d, s, t);
    }
}