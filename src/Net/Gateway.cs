using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Discord.Channels.Abstractions;
using Discord.Channels.Models;
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
public sealed class Gateway
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
    /// <remarks>Requires <see cref="Intent.Guilds"/>.</remarks>
    public event EventHandler<Guild>? OnGuildCreate;
    
    /// <summary>
    /// Dispatched when a guild is updated.
    /// </summary>
    /// <remarks>Requires <see cref="Intent.Guilds"/>.</remarks>
    public event EventHandler<Guild>? OnGuildUpdate;
    
    /// <summary>
    /// Dispatched when a guild becomes or was already unavailable due to an outage, or when the bot leaves or is removed
    /// from a guild. If <c>unavailable</c> is <c>null</c>, the bot was removed from the guild.
    /// </summary>
    /// <list type="bullet">
    ///     <item><c>guildId</c> ID of the guild that was deleted.</item>
    ///     <item><c>unavailable</c> See above.</item>
    /// </list>
    /// <remarks>Requires <see cref="Intent.Guilds"/>.</remarks>
    public event EventHandler<(ulong guildId, bool? unavailable)>? OnGuildDelete;

    /// <summary>
    /// Dispatched when a guild role is created.
    /// The values provided to the event handler contains the following:
    /// <list type="bullet">
    ///     <item><c>guildId</c> ID of the guild the role was created in.</item>
    ///     <item><c>role</c> Role that was created.</item>
    /// </list>
    /// </summary>
    /// <remarks>Requires <see cref="Intent.Guilds"/>.</remarks>
    public event EventHandler<(ulong guildId, Role role)>? OnGuildRoleCreate;
    
    /// <summary>
    /// Dispatched when a guild role is updated.
    /// The values provided to the event handler contains the following:
    /// <list type="bullet">
    ///     <item><c>guildId</c> ID of the guild the role was updated in.</item>
    ///     <item><c>before</c> Role before the update.</item>
    ///     <item><c>after</c> Role after the update.</item>
    /// </list>
    /// </summary>
    /// <remarks>Requires <see cref="Intent.Guilds"/>.</remarks>
    public event EventHandler<(ulong guildId, Role before, Role after)>? OnGuildRoleUpdate;
    
    /// <summary>
    /// Dispatched when a guild role is deleted.
    /// The values provided to the event handler contains the following:
    /// <list type="bullet">
    ///     <item><c>guildId</c> ID of the guild the role was deleted in.</item>
    ///     <item><c>roleId</c> ID of the role that was deleted.</item>
    /// </list>
    /// </summary>
    /// <remarks>Requires <see cref="Intent.Guilds"/>.</remarks>
    public event EventHandler<(ulong guildId, ulong roleId)>? OnGuildRoleDelete;
    
    /// <summary>
    /// Dispatched when a guild channel is created.
    /// The value provided to the event handler contains the channel that was created.
    /// </summary>
    /// <remarks>Requires <see cref="Intent.Guilds"/>.</remarks>
    public event EventHandler<GuildChannel>? OnChannelCreate;
    
    /// <summary>
    /// Dispatched when a guild channel is updated.
    /// The values provided to the event handler contains the following:
    /// <list type="bullet">
    ///     <item><c>before</c> Channel before the update.</item>
    ///     <item><c>after</c> Channel after the update.</item>
    /// </list>
    /// </summary>
    /// <remarks>Requires <see cref="Intent.Guilds"/>.</remarks>
    public event EventHandler<(GuildChannel before, GuildChannel after)>? OnChannelUpdate;
    
    /// <summary>
    /// Dispatched when a guild channel is deleted.
    /// The values provided to the event handler contains the following:
    /// <list type="bullet">
    ///     <item><c>guildId</c> ID of the guild where the channel was deleted.</item>
    ///     <item><c>channelId</c> ID of the channel that was deleted.</item>
    /// </list>
    /// </summary>
    /// <remarks>Requires <see cref="Intent.Guilds"/>.</remarks>
    public event EventHandler<(ulong guildId, ulong channelId)>? OnChannelDelete;

    /// <summary>
    /// Dispatched when a message is pinned or unpinned in a text channel. This is not dispatched when a pinned message is deleted.
    /// The values provided to the event handler contains the following:
    /// <list type="bullet">
    ///     <item><c>channelId</c> ID of the channel where the pin update occurred.</item>
    ///     <item><c>guildId</c> ID of the guild where the pin update occurred.</item>
    ///     <item><c>lastPinned</c> Time at which the most recent pinned message was pinned.</item>
    /// </list>
    /// </summary>
    /// <remarks>Requires <see cref="Intent.Guilds"/>.</remarks>
    public event EventHandler<(ulong channelId, ulong? guildId, DateTime? lastPinned)>? OnChannelPinsUpdate;
    
    /// <summary>
    /// Dispatched when a stage instance is created (stage channel becomes live).
    /// The value provided to the event handler contains the stage instance that was created.
    /// </summary>
    /// <remarks>Requires <see cref="Intent.Guilds"/>.</remarks>
    public event EventHandler<StageInstance>? OnStageInstanceCreate;
    
    /// <summary>
    /// Dispatched when a stage instance is updated.
    /// The values provided to the event handler contains the following:
    /// <list type="bullet">
    ///     <item><c>before</c> Stage instance before the update.</item>
    ///     <item><c>after</c> Stage instance after the update.</item>
    /// </list>
    /// </summary>
    /// <remarks>Requires <see cref="Intent.Guilds"/>.</remarks>
    public event EventHandler<(StageInstance before, StageInstance after)>? OnStageInstanceUpdate;
    
    /// <summary>
    /// Dispatched when a stage instance is deleted.
    /// The values provided to the event handler contains the following:
    /// <list type="bullet">
    ///     <item><c>guildId</c> ID of the guild where the stage instance was deleted from.</item>
    ///     <item><c>stageInstanceId</c> ID of the stage instance where the stage instance was deleted from.</item>
    ///     <item><c>stageChannelId</c> ID of the stage channel associated with  the stage instance where the stage instance was deleted from.</item>
    /// </list>
    /// </summary>
    /// <remarks>Requires <see cref="Intent.Guilds"/>.</remarks>
    public event EventHandler<(ulong guildId, ulong stageInstanceId, ulong stageChannelId)>? OnStageInstanceDelete;

    #endregion
    
    # region MESSAGES
    
    /// <summary>
    /// Dispatched when a message is sent.
    /// </summary>
    /// <remarks>Requires <see cref="Intent.GuildMessages"/> and or <see cref="Intent.DmMessages"/>.</remarks>
    public event EventHandler<Message>? OnMessageCreate;
    
    #endregion

    #region GUILD/DIRECT MESSAGE REACTIONS

    /// <summary>
    /// Dispatched when a reaction is added to a message.
    /// The values provided to the event handler contains the following:
    /// <list type="bullet">
    ///     <item><c>details</c> Reaction details.</item>
    ///     <item><c>reaction</c> The reaction object if the message was found in the cache.</item>
    /// </list>
    /// </summary>
    /// <remarks>Requires <see cref="Intent.GuildMessageReactions"/> and or <see cref="Intent.DmReactions"/>.</remarks>
    public event EventHandler<(ReactionDetails details, Reaction? reaction)>? OnReactionAdd; 
    
    /// <summary>
    /// Dispatched when a user removes a reaction from a message. Contains information about the reaction that was removed.
    /// </summary>
    /// <remarks>Requires <see cref="Intent.GuildMessageReactions"/> and or <see cref="Intent.DmReactions"/>.</remarks>
    public event EventHandler<ReactionRemove>? OnReactionRemove; 
    
    /// <summary>
    /// Dispatched when a user explicitly removes all reactions from a message. Contains information about the reaction
    /// that was removed.
    /// </summary>
    /// <remarks>Requires <see cref="Intent.GuildMessageReactions"/> and or <see cref="Intent.DmReactions"/>.</remarks>
    public event EventHandler<ReactionRemove>? OnReactionRemoveAll; 
    
    /// <summary>
    /// Dispatched when a bot removes all instances of a given emoji from the reactions of a message. Contains information about the reaction
    /// that was removed.
    /// </summary>
    /// <remarks>Requires <see cref="Intent.GuildMessageReactions"/> and or <see cref="Intent.DmReactions"/>.</remarks>
    public event EventHandler<ReactionRemove>? OnReactionRemoveEmoji; 

    #endregion

    #region SOUNDBOARD

    /// <summary>
    /// Dispatched when a soundboard sound is created.
    /// </summary>
    /// <remarks>Requires <see cref="Intent.GuildExpressions"/>.</remarks>
    public event EventHandler<SoundboardSound>? OnGuildSoundboardSoundCreate;
    
    /// <summary>
    /// Dispatched when a soundboard sound is updated.
    /// </summary>
    /// <remarks>Requires <see cref="Intent.GuildExpressions"/>.</remarks>
    public event EventHandler<SoundboardSound>? OnGuildSoundboardSoundUpdate;
    
    /// <summary>
    /// Dispatched when a soundboard sound is deleted.
    /// </summary>
    /// <remarks>Requires <see cref="Intent.GuildExpressions"/>.</remarks>
    public event EventHandler<(ulong guildId, ulong soundId, SoundboardSound? sound)>? OnGuildSoundboardSoundDelete;
    
    #endregion

    #region THREADS

    /// <summary>
    /// Dispatched when a <see cref="ThreadChannel"/> is created/when added to an existing private thread. Contains the
    /// thread that was created.
    /// </summary>
    /// <remarks>
    /// The <c>ThreadChannel</c> value provided to the event handler contains the thread that was updated. Requires
    /// <see cref="Intent.Guilds"/>.
    /// </remarks>
    public event EventHandler<ThreadChannel>? OnThreadCreate;
    
    /// <summary>
    /// Dispatched when a <see cref="ThreadChannel"/> is updated. Contains the thread that was updated.
    /// </summary>
    /// <remarks>
    /// The <c>ThreadChannel</c> value provided to the event handler contains the thread that was updated. Requires
    /// <see cref="Intent.Guilds"/>.
    /// </remarks>
    public event EventHandler<ThreadChannel>? OnThreadUpdate;
    
    /// <summary>
    /// Dispatched when a <see cref="ThreadChannel"/> is deleted.
    /// </summary>
    /// <remarks>
    /// The values provided to the event handler contains the following:
    /// <list type="bullet">
    ///     <item><c>guildId</c> ID of the guild the thread was deleted from.</item>
    ///     <item><c>threadId</c> ID of the thread that was deleted.</item>
    /// </list>
    /// Requires <see cref="Intent.Guilds"/>.
    /// </remarks>
    public event EventHandler<(ulong guildId, ulong threadId)>? OnThreadDelete;

    /// <summary>
    /// Dispatched when a <see cref="ThreadChannel"/> has its members updated.
    /// </summary>
    /// <remarks>
    /// The values provided to the event handler contains the following:
    /// <list type="bullet">
    ///     <item><c>threadId</c> ID of the thread where the update occurred.</item>
    ///     <item><c>guildId</c> ID of the guild where the update occurred.</item>
    ///     <item><c>added</c> Members that were added to the thread.</item>
    ///     <item><c>removed</c> IDs of the members that were removed.</item>
    /// </list>
    /// Requires <see cref="Intent.Guilds"/>.
    /// </remarks>
    public event EventHandler<(ulong threadId, ulong guildId, IEnumerable<ThreadMember> added, IEnumerable<ulong> removed)>? OnThreadMembersUpdate;
    
    /// <summary>
    /// Dispatched when the bot <i>gains</i> access to a <see cref="ThreadChannel"/>.
    /// </summary>
    /// <remarks>
    /// The values provided to the event handler contains the following:
    /// <list type="bullet">
    ///     <item><c>guildId</c> ID of the guild where the sync occurred.</item>
    ///     <item><c>threads</c> Threads that were synced.</item>
    /// </list>
    /// Requires <see cref="Intent.Guilds"/>.
    /// </remarks>
    public event EventHandler<(ulong guildId, IEnumerable<ThreadChannel> threads)>? OnThreadListSync;

    #endregion
    
    #endregion
    
    private string? _sessionId;
    private string? _resumeGatewayUrl;
    private const string UriParameters = "/?v=10&encoding=json";
    internal ClientWebSocket _ws;
    internal bool _userTerminated;
    
    private Bot _bot;
    private Rest _rest;
    private Intent _intents;
    private readonly string _token;
    private ulong? _lastSequence;
    private CancellationTokenSource _cts;
    private Task _heartbeatTask;
    private Task _receiveTask;
    private int _heartbeatInterval;
    private bool _heartbeatResponse;
    private bool _identifyRequired;
    private bool _reconnectRequested;

    internal Gateway(Bot bot, Intent intents)
    {
        _bot = bot;
        _rest = bot._rest;
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
    
    internal static T Deserialize<T>(JsonElement element)
    {
        string json = element.GetRawText();
        return JsonConvert.DeserializeObject<T>(json)!;
    }
    
    // Processes incoming Gateway events/payloads.
    private async Task HandleDiscordEventAsync(GatewayPayload payload)
    {
        UpdateSequence(payload);
        var d = D();
        switch (payload.Op)
        {
            case 0: // Dispatch (this contains every common event)
                switch (payload.T)
                {
                    // In order via https://discord.com/developers/docs/events/gateway#list-of-intents
                    
                    case "READY":
                        _sessionId = GetElementValue(d, "session_id").ToString();
                        _resumeGatewayUrl = GetElementValue(d, "resume_gateway_url").ToString();
                        Dev.Log($"[GW] READY received, session ID: {_sessionId} - Resume URL: {_resumeGatewayUrl}");
                        
                        var userElement = GetElementValue(d, "user");
                        _bot.User = Deserialize<User>(userElement);
                        break;
                    case "RESUMED":
                        Dev.Log("[GW] Successfully resumed");
                        break;
                    
                    #region GUILDS
                    
                    case "GUILD_CREATE":
                        var createdGuild = Deserialize<Guild>(d);
                        _rest.SetGuildValues(createdGuild);
                        
                        // If the guild is already in cache, this is most likely being dispatched again due to it
                        // recovering from an outage; or from a Connect() from a user controlled Disconnect().
                        //
                        // The amount of information inside a guild can be significant, especially if it was previously
                        // chunked, so simply replacing the guild with this new one would most likely get rid of a lot
                        // of information. To avoid this, see if the guild is already in cache and if so, update it.
                        if (_bot.GetGuild(createdGuild.Id) is { } fcg)
                        {
                            fcg.Update(createdGuild);
                            OnGuildCreate?.Invoke(this, fcg);
                        }
                        else
                        {
                            createdGuild.CacheMembersFromCreate(payload, _bot.User!.Id);
                            _bot._guilds.Add(createdGuild);
                            OnGuildCreate?.Invoke(this, createdGuild);
                        }
                        break;
                    case "GUILD_UPDATE":
                        var updatedGuild = Deserialize<Guild>(d);
                        if (_bot.GetGuild(updatedGuild.Id) is { } fug)
                        {
                            fug.Update(updatedGuild);
                            OnGuildUpdate?.Invoke(this, fug);
                        }
                        break;
                    case "GUILD_DELETE":
                        var gdId = GetElementValue(d, "id").GetUInt64();
                        var gduElement = GetElementValue(d, "unavailable");
                        bool? gdUnavailable = gduElement.ValueKind == JsonValueKind.Null ? null : gduElement.GetBoolean();
                        _bot._guilds.RemoveWhere(g => g.Id == gdId);
                        _bot._cachedMessages.RemoveWhere(m => m.GuildId == gdId);
                        OnGuildDelete?.Invoke(this, (gdId, gdUnavailable));
                        break;
                    case "GUILD_ROLE_CREATE":
                        var grcId = Convert.ToUInt64(GetElementValue(d, "guild_id").ToString());
                        var grcRole = Deserialize<Role>(GetElementValue(d, "role"));
                        _bot._rest.SetRoleValues([grcRole], grcId);
                        if (_bot.GetGuild(grcId) is { } grcGuild)
                            grcGuild._roles.Add(grcRole);
                        OnGuildRoleCreate?.Invoke(this, (grcId, grcRole));
                        break;
                    case "GUILD_ROLE_UPDATE":
                        var gruId = Convert.ToUInt64(GetElementValue(d, "guild_id").ToString());
                        var gruRole = Deserialize<Role>(GetElementValue(d, "role"));
                        _bot._rest.SetRoleValues([gruRole], gruId);
                        if (_bot.GetGuild(gruId) is { } gruGuild)
                        {
                            var before = gruGuild.GetRole(gruRole.Id)!;
                            gruGuild._roles.RemoveAll(r => r.Id == gruRole.Id);
                            gruGuild._roles.Add(gruRole);
                            OnGuildRoleUpdate?.Invoke(this, (gruId, before, gruRole));
                        }
                        break;
                    case "GUILD_ROLE_DELETE":
                        var grdGuildId = Convert.ToUInt64(GetElementValue(d, "guild_id").ToString());
                        var grdRoleId = Convert.ToUInt64(GetElementValue(d, "role_id").ToString());
                        if (_bot.GetGuild(grdGuildId) is { } grdGuild) 
                            grdGuild._roles.RemoveAll(r => r.Id == grdRoleId);
                        OnGuildRoleDelete?.Invoke(this, (grdGuildId, grdRoleId));
                        break;
                    case "CHANNEL_CREATE":
                        var ccChannelPayload = JsonConvert.DeserializeObject<JSON>(d.GetRawText())!;
                        var ccChannel = GuildChannel.ParseChannels([ccChannelPayload]).First();
                        if (_bot.GetGuild(ccChannel.GuildId) is { } ccGuild)
                        {
                            _bot._rest.SetChannelValuesIndividual(ccChannel, ccGuild);
                            ccGuild._channels.Add(ccChannel);
                            OnChannelCreate?.Invoke(this, ccChannel);
                        }
                        break;
                    case "CHANNEL_UPDATE":
                        var cuChannelPayload = JsonConvert.DeserializeObject<JSON>(d.GetRawText())!;
                        var cuChannel = GuildChannel.ParseChannels([cuChannelPayload]).First();
                        if (_bot.GetGuild(cuChannel.GuildId) is { } cuGuild)
                        {
                            var before = cuGuild.GetChannel(cuChannel.Id)!;
                            _bot._rest.SetChannelValuesIndividual(cuChannel, cuGuild);
                            cuGuild._channels.RemoveAll(c => c.Id == cuChannel.Id);
                            cuGuild._channels.Add(cuChannel);
                            OnChannelUpdate?.Invoke(this, (before, cuChannel));
                        }
                        break;
                    case "CHANNEL_DELETE":
                        // This provides the full payload for the channel that was deleted, but I don't see a point in
                        // creating it because anything done with said channel will fail due to it not existing anymore.
                        // So for this just dispatch the ID of the guild/channel that was deleted.
                        var cdChannelId = Convert.ToUInt64(GetElementValue(d, "id").ToString());
                        var cdGuildId = Convert.ToUInt64(GetElementValue(d, "guild_id").ToString());
                        if (_bot.GetGuild(cdGuildId) is { } cdGuild)
                            cdGuild._channels.RemoveAll(c => c.Id == cdChannelId);
                        OnChannelDelete?.Invoke(this, (cdGuildId, cdChannelId));
                        break;
                    case "CHANNEL_PINS_UPDATE":
                        var cpuChannelId = Convert.ToUInt64(GetElementValue(d, "channel_id").ToString());
                        ulong? cpuGuildId = null;
                        DateTime? cpuLastPinTimestamp = null;
                        if (d.TryGetProperty("guild_id", out var cpuGuildIdElement))
                            cpuGuildId = Convert.ToUInt64(cpuGuildIdElement.ToString());
                        if (d.TryGetProperty("last_pin_timestamp", out var cpuLastPinElement))
                        {
                            if (cpuLastPinElement.ValueKind != JsonValueKind.Null)
                                cpuLastPinTimestamp = Convert.ToDateTime(cpuLastPinElement.ToString());
                        }
                        
                        // Update GuildChannel.LastPinned
                        if (_bot.GetChannel(cpuChannelId) is { } cpuChannel && cpuLastPinTimestamp.HasValue)
                            cpuChannel.LastPinned = cpuLastPinTimestamp;
                        
                        OnChannelPinsUpdate?.Invoke(this, (cpuChannelId, cpuGuildId, cpuLastPinTimestamp));
                        break;
                    case "THREAD_CREATE":
                        var tcGuildId = Deserialize<ulong>(GetElementValue(d, "guild_id"));
                        var createdThread = Deserialize<ThreadChannel>(d);
                        
                        // If the thread was created via Message.CreateThreadAsync(), the ID of the thread is the ID of
                        // the message. So assigned this thread directly to that message (if it's cached).
                        if (_bot.GetMessage(createdThread.Id) is { } tcm)
                            tcm.Thread = createdThread;
                        
                        if (_bot.GetGuild(tcGuildId) is { } tcGuild)
                            _bot._rest.SetThreadValues([createdThread], tcGuild);
                        
                        // Threads always have a parent_id, whether it's a regular text channel or a forum; so update the
                        // last_message_id (last thread ID) for the forum channel.
                        if (createdThread.Guild.GetChannel(createdThread.ParentId!.Value) is ForumChannel tcfc)
                            tcfc.LastMessageId = createdThread.Id;
                            
                        createdThread.Guild._threads.Add(createdThread);
                        OnThreadCreate?.Invoke(this, createdThread);
                        break;
                    case "THREAD_UPDATE":
                        var tuGuildId = Deserialize<ulong>(GetElementValue(d, "guild_id"));
                        var updatedThread = Deserialize<ThreadChannel>(d);
                        if (_bot.GetGuild(tuGuildId) is { } tuGuild)
                        {
                            _bot._rest.SetThreadValues([updatedThread], tuGuild);
                            if (tuGuild.GetThread(updatedThread.Id) is { } currentThread)
                            {
                                var membersCopy = currentThread._members.ToList();
                                currentThread = updatedThread;
                                currentThread._members.AddRange(membersCopy);
                            }
                        }
                        OnThreadUpdate?.Invoke(this, updatedThread);
                        break;
                    case "THREAD_DELETE":
                        var tdGuildId = Deserialize<ulong>(GetElementValue(d, "guild_id"));
                        var tdThreadId = Deserialize<ulong>(GetElementValue(d, "id"));
                        if (_bot.GetGuild(tdGuildId) is { } tdg)
                            if (tdg.GetThread(tdThreadId) is { } tdt)
                            {
                                tdg._threads.Remove(tdt);
                                OnThreadDelete?.Invoke(this, (tdGuildId, tdThreadId));
                            }
                        break;
                    case "THREAD_LIST_SYNC":
                        var tlsGuildId = Deserialize<ulong>(GetElementValue(d, "guild_id"));
                        var tlsThreads = Deserialize<List<ThreadChannel>>(GetElementValue(d, "threads"));
                        var tlsThreadMembers = Deserialize<List<ThreadMember>>(GetElementValue(d, "members"));
                        if (_bot.GetGuild(tlsGuildId) is { } tlsGuild)
                        {
                            foreach (var newThread in tlsThreads)
                            {
                                // If the thread isn't already synced, process it.
                                if (tlsGuild.GetThread(newThread.Id) is { } found) continue;
                                
                                var match = tlsThreadMembers.Where(m => m.ThreadId == newThread.Id);
                                newThread._members.AddRange(match);
                                _bot._rest.SetThreadValues([newThread], tlsGuild);
                                tlsGuild._threads.Add(newThread);
                            }
                            OnThreadListSync?.Invoke(this, (tlsGuildId, tlsThreads));
                        }
                        break;
                    case "THREAD_MEMBER_UPDATE":
                        // Unused
                        break;
                    case "THREAD_MEMBERS_UPDATE":
                        var tmuThreadId = Deserialize<ulong>(GetElementValue(d, "id"));
                        var tmuGuildId = Deserialize<ulong>(GetElementValue(d, "guild_id"));
                        var tmuMemberCount = Deserialize<int>(GetElementValue(d, "member_count"));
                        
                        var tmuAddedMembers = new List<ThreadMember>();
                        if (d.TryGetProperty("added_members", out var addedMembers))
                            tmuAddedMembers.AddRange(Deserialize<List<ThreadMember>>(addedMembers));
                        
                        var tmuRemovedMemberIds = new List<ulong>();
                        if (d.TryGetProperty("removed_member_ids", out var removedMemberIds))
                            tmuRemovedMemberIds.AddRange(Deserialize<List<ulong>>(removedMemberIds));
                        
                        if (_bot.GetGuild(tmuGuildId) is { } tmug)
                            if (tmug.GetThread(tmuThreadId) is { } tmut)
                            {
                                foreach (var rmid in tmuRemovedMemberIds)
                                    tmut._members.RemoveAll(m => m.ThreadId == rmid);
                                tmut._members.AddRange(tmuAddedMembers);
                                tmut.MemberCount = tmuMemberCount;
                            }
                        OnThreadMembersUpdate?.Invoke(this, (tmuThreadId, tmuGuildId, tmuAddedMembers, tmuRemovedMemberIds));
                        break;
                    case "STAGE_INSTANCE_CREATE":
                        var sicStage = Deserialize<StageInstance>(d);
                        sicStage.Bot = _bot;
                        if (_bot.GetGuild(sicStage.GuildId) is { } sicGuild)
                        {
                            sicGuild._stageInstances.Add(sicStage);
                            OnStageInstanceCreate?.Invoke(this, sicStage);
                        }
                        break;
                    case "STAGE_INSTANCE_UPDATE":
                        var siuStage = Deserialize<StageInstance>(d);
                        siuStage.Bot = _bot;
                        if (_bot.GetGuild(siuStage.GuildId) is { } siuGuild)
                        {
                            var before = siuGuild._stageInstances.First(si => si.Id == siuStage.Id);
                            siuGuild._stageInstances.RemoveAll(si => si.Id == siuStage.Id);
                            siuGuild._stageInstances.Add(siuStage);
                            OnStageInstanceUpdate?.Invoke(this, (before, siuStage));
                        }
                        break;
                    case "STAGE_INSTANCE_DELETE":
                        var sidGuildId = Deserialize<ulong>(GetElementValue(d, "guild_id"));
                        var sidStageInstanceId = Deserialize<ulong>(GetElementValue(d, "id"));
                        var sidStageChannelId = Deserialize<ulong>(GetElementValue(d, "channel_id"));
                        if (_bot.GetGuild(sidGuildId) is { } sidGuild)
                            sidGuild._stageInstances.RemoveAll(s => s.Id == sidStageInstanceId);
                        OnStageInstanceDelete?.Invoke(this, (sidGuildId, sidStageInstanceId, sidStageChannelId));
                        break;
                    #endregion

                    #region GUILD MESSAGES

                    case "MESSAGE_CREATE":
                        var createdMessage = Deserialize<Message>(d);
                        _rest.SetMessageValues([createdMessage]);
                        
                        // Update the last_message_id for that channel.
                        if (createdMessage.GuildId is not null)
                        {
                            var cmChannel = createdMessage.Guild!.GetChannel(createdMessage.ChannelId);
                            if  (cmChannel is not null)
                                cmChannel.LastMessageId = createdMessage.Id;
                            
                            // If it's a thread, update the values associated with it.
                            if (createdMessage.Guild!.GetThread(createdMessage.ChannelId) is { } thread)
                            {
                                thread.MessageCount += 1;
                                thread.TotalMessagesSent += 1;
                            }
                            // Else: it's a non-messageable channel, AKA a ForumChannel etc
                        }
                        else
                        {
                            // Since GuildId is null, it's a direct message so create the DmChannel or if found, update
                            // its values.
                            if (_bot.GetDmChannel(createdMessage.ChannelId) is not { } dmChannel)
                            {
                                var createdDm = await _bot._rest.CreateDmAsync(createdMessage.Author.Id);
                                createdDm.LastMessageId = createdMessage.Id;
                                _bot._dmChannels.Add(createdDm);
                            }
                            else
                            {
                                dmChannel.LastMessageId = createdMessage.Id;
                            }
                        }
                        _bot.CacheMessage(createdMessage);
                        OnMessageCreate?.Invoke(this, createdMessage);
                        break;
                    case "MESSAGE_UPDATE":
                        break;

                    #endregion
                    
                    case "GUILD_MEMBERS_CHUNK":
                        var chunkedGuildId = Convert.ToUInt64(GetElementValue(d, "guild_id").ToString());
                        var chunkedMembers = GetElementValue(d, "members");
                        var convertedChunkedMembers = Deserialize<List<Member>>(chunkedMembers);
                        _bot._rest.SetMemberValues(convertedChunkedMembers, chunkedGuildId);
                        if (_bot.GetGuild(chunkedGuildId) is { } chunkedGuild)
                            chunkedGuild._members.UnionWith(convertedChunkedMembers);
                        break;
                    case "GUILD_SOUNDBOARD_SOUND_CREATE":
                        var gssCreate = Deserialize<SoundboardSound>(d);
                        _bot._rest.SetSoundboardSoundValues([gssCreate]);
                        if (_bot.GetGuild(gssCreate.GuildId!.Value) is { } gscGuild)
                            gscGuild._soundboardSounds.Add(gssCreate);
                        OnGuildSoundboardSoundCreate?.Invoke(this, gssCreate);
                        break;
                    case "GUILD_SOUNDBOARD_SOUND_UPDATE":
                        var gssUpdate = Deserialize<SoundboardSound>(d);
                        _bot._rest.SetSoundboardSoundValues([gssUpdate]);
                        if (_bot.GetGuild(gssUpdate.GuildId!.Value) is { } gsuGuild)
                        {
                            gsuGuild._soundboardSounds.RemoveWhere(s => s.SoundId == gssUpdate.SoundId);
                            gsuGuild._soundboardSounds.Add(gssUpdate);
                        }
                        OnGuildSoundboardSoundUpdate?.Invoke(this, gssUpdate);
                        break;
                    case "GUILD_SOUNDBOARD_SOUND_DELETE":
                        var gsdSoundId = Convert.ToUInt64(GetElementValue(d, "sound_id").ToString());
                        var gsdGuildId = Convert.ToUInt64(GetElementValue(d, "guild_id").ToString());
                        SoundboardSound? gsdSound = null;
                        if (_bot.GetGuild(gsdGuildId) is { } gsdGuild)
                        {
                            gsdSound = gsdGuild.GetSoundboardSound(gsdSoundId);
                            gsdGuild._soundboardSounds.RemoveWhere(s => s.SoundId == gsdSoundId);
                        }
                        OnGuildSoundboardSoundDelete?.Invoke(this, (gsdGuildId, gsdSoundId, gsdSound));
                        break;

                    #region GUILD/DM MESSAGE REACTIONS

                    case "MESSAGE_REACTION_ADD":
                        var mraReactionDetails = Deserialize<ReactionDetails>(d);
                        if (_bot.GetMessage(mraReactionDetails.MessageId) is { } mraMessage)
                        {
                            if (mraMessage.GetReaction(mraReactionDetails.Emoji.ToString()) is { } mraReaction)
                            {
                                mraReaction.Count += 1;
                                mraReaction.Details = mraReactionDetails;
                                if (mraReactionDetails.IsBurst)
                                    mraReaction.CountDetails.Burst += 1;
                                else
                                    mraReaction.CountDetails.Normal += 1;
                                OnReactionAdd?.Invoke(this, (mraReactionDetails, mraReaction));
                                break;
                            }
                            else
                            {
                                var isMe = mraReactionDetails.UserId == _bot.User!.Id;
                                var burstCount = mraReactionDetails.IsBurst ? 1 : 0;
                                var countDetails = new ReactionCountDetails(burstCount, burstCount == 0 ? 1 : 0);
                                var mraNewReaction = new Reaction(1, mraReactionDetails, isMe,
                                    mraReactionDetails.IsBurst && isMe, mraReactionDetails.Emoji,
                                    mraReactionDetails.BurstColors.ToList(), countDetails);
                                mraMessage._reactions.Add(mraNewReaction);
                                OnReactionAdd?.Invoke(this, (mraReactionDetails, mraNewReaction));
                                break;
                            }
                        }
                        OnReactionAdd?.Invoke(this, (mraReactionDetails, null));
                        break;
                    case "MESSAGE_REACTION_REMOVE":
                        var mrrReactionDelete = Deserialize<ReactionRemove>(d);
                        RemoveReaction(mrrReactionDelete);
                        OnReactionRemove?.Invoke(this, mrrReactionDelete);
                        break;
                    case "MESSAGE_REACTION_REMOVE_ALL":
                        var mrraReactionDelete = Deserialize<ReactionRemove>(d);
                        if (_bot.GetMessage(mrraReactionDelete.MessageId) is { } mrraMessage)
                            mrraMessage._reactions.Clear();
                        OnReactionRemoveAll?.Invoke(this, mrraReactionDelete);
                        break;
                    case "MESSAGE_REACTION_REMOVE_EMOJI":
                        var mrreReactionDelete = Deserialize<ReactionRemove>(d);
                        RemoveReaction(mrreReactionDelete);
                        OnReactionRemoveEmoji?.Invoke(this, mrreReactionDelete);
                        break;
                    #endregion
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

        void RemoveReaction(ReactionRemove rr)
        {
            if (_bot.GetMessage(rr.MessageId) is not { } message) return;
            if (message.GetReaction(rr.Emoji!.ToString()) is { } mrrReaction)
                message._reactions.Remove(mrrReaction);
        }
        
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