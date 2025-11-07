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
    HeartbeatAck,
    RequestSoundboardSounds = 31
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

/// <summary>
/// Represents a client that handles all Discord gateway traffic.
/// </summary>
public sealed class DiscordGatewayClient
{
    #region EVENTS

    #region GUILD

    /// <summary>
    /// Dispatched in three different scenarios:
    /// <list type="bullet">
    ///     <item>When the bot is initially connecting, lazily loading all available guilds. Guilds that are unavailable
    ///     due to an outage will send a <see cref="OnGuildDelete"/> event.
    ///     </item>
    ///     <item>When a guild becomes available again to the bot.</item>
    ///     <item>When the bot joins a new guild.</item>
    /// </list>
    /// </summary>
    /// <remarks>Requires <see cref="Intents.Guilds"/>.</remarks>
    public event EventHandler<Guild>? OnGuildCreate;
    
    /// <summary>
    /// Dispatched when a guild becomes or was already unavailable due to an outage, or when the bot leaves or is removed
    /// from a guild. If <c>unavailable</c> is <c>null</c>, the bot was removed from the guild.
    /// </summary>
    public event EventHandler<(ulong id, bool? unavailable)>? OnGuildDelete;

    #endregion
    
    # region MESSAGES
    
    /// <summary>
    /// Dispatched when a message is sent.
    /// </summary>
    /// <remarks>Requires <see cref="Intents.GuildMessages"/> and or <see cref="Intents.DmMessages"/>.</remarks>
    public event EventHandler<Message>? OnMessageCreate;
    
    #endregion

    #region SOUNDBOARD

    /// <summary>
    /// Dispatched when a soundboard sound is created.
    /// </summary>
    /// <remarks>Requires <see cref="Intents.GuildExpressions"/>.</remarks>
    public event EventHandler<SoundboardSound>? OnGuildSoundboardSoundCreate;
    
    /// <summary>
    /// Dispatched when a soundboard sound is updated.
    /// </summary>
    /// <remarks>Requires <see cref="Intents.GuildExpressions"/>.</remarks>
    public event EventHandler<SoundboardSound>? OnGuildSoundboardSoundUpdate;
    
    /// <summary>
    /// Dispatched when a soundboard sound is deleted.
    /// </summary>
    /// <remarks>Requires <see cref="Intents.GuildExpressions"/>.</remarks>
    public event EventHandler<(ulong guildId, ulong soundId, SoundboardSound? sound)>? OnGuildSoundboardSoundDelete;
    
    #endregion
    
    #endregion
    
    private string? _sessionId;
    private string? _resumeGatewayUrl;
    private const string UriParameters = "/?v=10&encoding=json";
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
    private bool _reconnectRequested;

    internal DiscordGatewayClient(Bot bot, Intents intents)
    {
        _bot = bot;
        _token = bot.Token!;
        _intents = intents;
        _heartbeatInterval = 30_000;
        _heartbeatResponse = false;
        _identifyRequired = true;
        _cts = new CancellationTokenSource();
        _ws = new ClientWebSocket();
        _userTerminated = false;
        _reconnectRequested = false;
    }

    // Closes the WebSocket connection and sets a new WebSocket object and CancellationTokenSource. Prior to reaching
    // this the connection should have already been gracefully closed.
    private void RefreshWebSocket()
    {
        _cts.Cancel();
        _ws = new ClientWebSocket();
        _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);
        _cts = new CancellationTokenSource();
    }

    // Connects to the Discord Gateway and starts processing events.
    internal async Task ConnectAsync(bool resume)
    {
        RefreshWebSocket();
        if (resume)
        {
            Dev.Log("[GW] Connecting with RESUME");
            await _ws.ConnectAsync(new Uri(_resumeGatewayUrl + UriParameters), _cts.Token);
            await SendResumeAsync();
        }
        else
        {
            ResetCoreValues();
            string wss = await _bot._rest.GetGatewayAsync();
            Dev.Log("[GW] Connecting with IDENTIFY");
            await _ws.ConnectAsync(new Uri(wss + UriParameters), _cts.Token);
        }

        // Start the heartbeat/gateway receive tasks.
        _heartbeatTask = Task.Run(HeartbeatLoopAsync, _cts.Token).ContinueWith(task =>
        {
            if (task.IsFaulted)
                throw task.Exception.InnerException!;
        });
        _receiveTask = Task.Run(ReceiveAsync).ContinueWith(task =>
        {
            if (task.IsFaulted)
                throw task.Exception.InnerException!;
        }, _cts.Token);;
    }

    // Disconnect from the gateway.
    internal async Task DisconnectAsync(bool instant)
    {
        await _ws.CloseAsync(instant ? WebSocketCloseStatus.NormalClosure : WebSocketCloseStatus.Empty, string.Empty,
            CancellationToken.None);
        await _cts.CancelAsync();
    }

    // Resets values associated with the gateway that would indicate a new connection.
    private void ResetCoreValues()
    {
        _sessionId = null;
        _resumeGatewayUrl = null;
        _lastSequence = null;
        _identifyRequired = true;
        _reconnectRequested = false;
        _userTerminated = false;
    }

    // Keeps the connection alive with Discords required heartbeats.
    private async Task HeartbeatLoopAsync()
    {
        Dev.Log("[GW] Starting heartbeat...");
        while (true)
        {
            try
            {
                await Task.Delay(_heartbeatInterval, _cts.Token);
                if (_ws.State == WebSocketState.Open)
                    await SendHeartbeatAsync();
                else
                {
                    Dev.Log($"[GW] Heartbeat loop >stopped< due to connection state ({_ws.State})");
                    break;
                }
            }
            catch (TaskCanceledException)
            {
                Dev.Log("[GW] Heartbeat loop >cancelled< due to cancel request");
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
            if (payload is not null) // AKA isn't closed and no error occurred.
                await HandleDiscordEventAsync(payload);
            else
            {
                Dev.Log("[GW] ConvertPayloadAsync received null response due to close/error/reconnect request");
                if (_reconnectRequested)
                {
                    _reconnectRequested = false;
                    // Don't process the close codes.
                    return;
                }
                if (_userTerminated)
                {
                    Dev.Log("[GW] Session terminated by user");
                    return;
                }
                
                // According to Discord, sometimes the connection can close with no close code. If there is a close code,
                // set it, otherwise leave it as -1 to symbolize no close code.
                if (_ws.CloseStatus is { } status)
                    closeCode = (int)status;
                break;
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
                Dev.Log("[GW] Unknown error/Discord wasn't sure what went wrong - resuming session");
                await ConnectAsync(true);
                break;
            case 4001:
                throw new UnknownOpcodeException(
                    "An invalid Gateway opcode or an invalid payload for an opcode was sent");
            case 4002:
                throw new DecodeErrorException("An invalid payload was sent");
            case 4003:
                Dev.Log(
                    "A payload prior to identifying was sent, or this session has been invalidated - starting a new session");
                await ConnectAsync(false);
                break;
            case 4004:
                throw new AuthenticationFailedException(
                    "The account token sent with your identify payload is incorrect");
            case 4005:
                throw new AlreadyAuthenticatedException("More than one identify payload was sent");
            case 4007:
                Dev.Log("The sequence sent when resuming the session was invalid - starting a new session");
                await ConnectAsync(false);
                break;
            case 4008:
                Dev.Log("Payloads are being sent too quickly - resuming session");
                await ConnectAsync(true);
                break;
            case 4009:
                Dev.Log("Session timed out - starting new session");
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
                Dev.Log(
                    $"[GW] WebSocket closed with unhandled close code ({closeCode}:WS state {_ws.State}) - attempting resume");
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
                large_threshold = 250,
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
    internal async Task SendJsonAsync(object payload)
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
                if (result.MessageType == WebSocketMessageType.Close || result.CloseStatus is not null)
                {
                    Dev.Log($"[GW] MessageType={result.MessageType}, CloseStatus={result.CloseStatus}");
                    return null;
                }
                builder.Write(new ReadOnlySpan<byte>(buffer, 0, result.Count));
            } while (!result.EndOfMessage);

            var data = builder.WrittenSpan.ToArray();
            var json = Encoding.UTF8.GetString(data);
            var doc = JsonDocument.Parse(json);
            return GatewayPayload.FromJson(doc.RootElement);
        }
        catch (Exception ex)
        {
            Dev.Log($"[GW ERROR, {ex.GetType()}]: {ex.Message}");
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
        _heartbeatResponse = false;
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
            // Wait for heartbeat ACK to be sent by Discord. This response time can differ based on server host location.
            // For now, waiting ~2 seconds seems like enough time to believe that a possible "zombie" connection occurred.
            var timeout = TimeSpan.FromSeconds(2);
            try
            {
                await Task.Delay(timeout, _cts.Token);
                if (!_heartbeatResponse)
                {
                    Dev.Log($"[GW] Heartbeat timed out ({timeout.Seconds}s - attempting resume");
                    await ConnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                Dev.Log($"[GW] Heartbeat verification failed/ignored due to {ex.GetType()}: {ex.Message}");
            }
        }
    }

    // Sets the most recent sequence number so the gateway can conduct a session Resume.
    private void UpdateSequence(GatewayPayload payload)
    {
        if (payload.S.HasValue)
            _lastSequence = payload.S.Value;
    }

    internal static JsonElement GetElementValue(JsonElement element, string key) => 
        element.GetProperty(key);
    
    internal static T DeserializeWithNewtonsoft<T>(JsonElement element)
    {
        string json = element.GetRawText();
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
                        _sessionId = GetElementValue(D(), "session_id").ToString();
                        _resumeGatewayUrl = GetElementValue(D(), "resume_gateway_url").ToString();
                        Dev.Log($"[GW] READY received, session ID: {_sessionId} - Resume URL: {_resumeGatewayUrl}");
                        
                        var userElement = GetElementValue(D(), "user");
                        _bot.User = DeserializeWithNewtonsoft<User>(userElement);
                        break;
                    case "RESUMED":
                        Dev.Log("[GW] Successfully resumed");
                        break;
                    case "MESSAGE_CREATE":
                        var messageCreated = DeserializeWithNewtonsoft<Message>(D());
                        messageCreated.Bot = _bot;
                        
                        _bot.CacheMessage(messageCreated);
                        OnMessageCreate?.Invoke(this, messageCreated);
                        break;
                    case "GUILD_CREATE":
                        var guildCreatedId = Convert.ToUInt64(GetElementValue(D(), "id").ToString());

                        // If the guild is already in cache, this is most likely being dispatched again due to it
                        // recovering from an outage; or from a Connect() from a user controlled Disconnect().
                        //
                        // The amount of information inside a guild can be significant, especially if it was previously
                        // chunked, so simply replacing the guild with this new one would most likely get rid of a lot
                        // of information. To avoid this, see if the guild is already in cache and if so, update it.
                        if (_bot.GetGuild(guildCreatedId) is { } gc)
                        {
                            gc.Update(D());
                            OnGuildCreate?.Invoke(this, gc);
                        }
                        else
                        {
                            var guildCreated = DeserializeWithNewtonsoft<Guild>(D());
                            guildCreated.Bot = _bot;
                            guildCreated.CacheMembersFromCreate(payload, _bot.User!.Id);
                            _bot._rest.SetEmojiValues(guildCreated._emojis, guildCreatedId);
                            _bot._rest.SetRoleValues(guildCreated._roles, guildCreatedId);
                            _bot._guilds.Add(guildCreated);
                            OnGuildCreate?.Invoke(this, guildCreated);
                        }
                        break;
                    case "GUILD_UPDATE":
                        break;
                    case "GUILD_DELETE":
                        var gdId = GetElementValue(D(), "id").GetUInt64();
                        var gduElement = GetElementValue(D(), "unavailable");
                        bool? gdUnavailable = gduElement.ValueKind == JsonValueKind.Null ? null : gduElement.GetBoolean();
                        _bot._guilds.RemoveWhere(g => g.Id == gdId);
                        _bot._cachedMessages.RemoveWhere(m => m.GuildId == gdId);
                        OnGuildDelete?.Invoke(this, (gdId, gdUnavailable));
                        break;
                    case "GUILD_MEMBERS_CHUNK":
                        var chunkedGuildId = Convert.ToUInt64(GetElementValue(D(), "guild_id").ToString());
                        var chunkedMembers = GetElementValue(D(), "members");
                        var convertedChunkedMembers = DeserializeWithNewtonsoft<List<Member>>(chunkedMembers);
                        _bot._rest.SetMemberValues(convertedChunkedMembers, chunkedGuildId);
                        if (_bot.GetGuild(chunkedGuildId) is { } chunkedGuild)
                            chunkedGuild._members.UnionWith(convertedChunkedMembers);
                        break;
                    case "GUILD_SOUNDBOARD_SOUND_CREATE":
                        var gssCreate = DeserializeWithNewtonsoft<SoundboardSound>(D());
                        _bot._rest.SetSoundboardSoundValues([gssCreate]);
                        if (_bot.GetGuild(gssCreate.GuildId!.Value) is { } gscGuild)
                            gscGuild._soundboardSounds.Add(gssCreate);
                        OnGuildSoundboardSoundCreate?.Invoke(this, gssCreate);
                        break;
                    case "GUILD_SOUNDBOARD_SOUND_UPDATE":
                        var gssUpdate = DeserializeWithNewtonsoft<SoundboardSound>(D());
                        _bot._rest.SetSoundboardSoundValues([gssUpdate]);
                        if (_bot.GetGuild(gssUpdate.GuildId!.Value) is { } gsuGuild)
                        {
                            gsuGuild._soundboardSounds.RemoveWhere(s => s.SoundId == gssUpdate.SoundId);
                            gsuGuild._soundboardSounds.Add(gssUpdate);
                        }
                        OnGuildSoundboardSoundUpdate?.Invoke(this, gssUpdate);
                        break;
                    case "GUILD_SOUNDBOARD_SOUND_DELETE":
                        var gsdSoundId = Convert.ToUInt64(GetElementValue(D(), "sound_id").ToString());
                        var gsdGuildId = Convert.ToUInt64(GetElementValue(D(), "guild_id").ToString());
                        SoundboardSound? gsdSound = null;
                        if (_bot.GetGuild(gsdGuildId) is { } gsdGuild)
                        {
                            gsdSound = gsdGuild.GetSoundboardSound(gsdSoundId);
                            gsdGuild._soundboardSounds.RemoveWhere(s => s.SoundId == gsdSoundId);
                        }
                        OnGuildSoundboardSoundDelete?.Invoke(this, (gsdGuildId, gsdSoundId, gsdSound));
                        break;
                }
                break;

            case 1: // Heartbeat request
                Dev.Log("[GW] HEARTBEAT request received - sending requested heartbeat");
                await SendHeartbeatAsync();
                break;
            
            case 7: // Reconnect
                Dev.Log("[GW] RECONNECT request received - closing/resuming session");
                _reconnectRequested = true;
                await ConnectAsync(true);
                break;

            case 9: // Invalid Session
                var resumable = GetElementValue(D(), "d").GetBoolean();
                Dev.Log($"[GW] INVALID SESSION received, resumable: {resumable}");
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
                    _heartbeatInterval = GetElementValue(D(), "heartbeat_interval") .GetInt32();
                    Dev.Log($"[GW] HELLO received, heartbeat interval set ({_heartbeatInterval}ms)");
                    await SendIdentifyAsync();
                    _identifyRequired = false;
                }
                else
                    Dev.Log("[GW] HELLO received - IDENTIFY not required for RESUME (continuing)");
                break;

            case 11: // Heartbeat ACK
                Dev.Log("[GW] HEARTBEAT ACK");
                _heartbeatResponse = true;
                break;

            default:
                Dev.Log($"Unhandled opcode: {payload.Op}");
                break;
        }

        return;

        JsonElement D() => payload.D!.Value;
    }
}

// Represents a Gateway payload.
internal record GatewayPayload(int Op, JsonElement? D, ulong? S, string? T)
{
    internal static GatewayPayload FromJson(JsonElement root)
    {
        int op = root.GetProperty("op").GetInt32();
        JsonElement? d = root.TryGetProperty("d", out var dVal) ? dVal : null;
        ulong? s = root.TryGetProperty("s", out var sVal) && sVal.ValueKind != JsonValueKind.Null ? sVal.GetUInt64() : null;
        string? t = root.TryGetProperty("t", out var tVal) && tVal.ValueKind != JsonValueKind.Null ? tVal.GetString() : null;
        return new GatewayPayload(op, d, s, t);
    }
}