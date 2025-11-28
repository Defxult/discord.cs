using System.ComponentModel;
using System.Net.WebSockets;
using Discord.Channels.Abstractions;
using Discord.Channels.Models;
using Discord.Net;
using Discord.Models;
using Discord.Utility;
using Newtonsoft.Json;

namespace Discord;

/// <summary>
/// Represents the client that connects to the Discord API.
/// </summary>
public class Bot
{
    /// <summary>
    /// The bot authentication token. 
    /// </summary>
    public string? Token => Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
    
    /// <summary>
    /// The bot user object.
    /// </summary>
    public User? User { get; internal set; }
    
    /// <summary>
    /// All users in every guild the bot has access to.
    /// </summary>
    /// <remarks>This is affected by your <see cref="CacheManager"/> settings.</remarks>
    public IReadOnlySet<User> Users => _guilds.SelectMany(g => g.Members.Select(m => m.User)).ToHashSet();
    
    /// <summary>
    /// All guilds the bot is currently in.
    /// </summary>
    public IReadOnlySet<Guild> Guilds => _guilds;
    internal readonly HashSet<Guild> _guilds = [];

    /// <summary>
    /// All messages in every guild that the bot has permissions to see.
    /// </summary>
    public IReadOnlySet<Message> Messages => _cachedMessages;
    internal readonly HashSet<Message> _cachedMessages = [];
    private Timer _messageCacheTimer;
    
    /// <summary>
    /// Events that the bot can listen for.
    /// </summary>
    public Gateway Events => _gateway;
    internal readonly Gateway _gateway;

    /// <summary>
    /// A simple way to store items that are related to the bot's usage. This library never processes the information in said storage,
    /// and is entirely handled by you.
    /// </summary>
    public Dictionary<string, object> Storage = [];
    
    /// <summary>
    /// The bots gateway intents. Handles which events are dispatched by Discord.
    /// </summary>
    public Intent Intents { get; }
    
    /// <summary>
    /// Controls what will be cached.
    /// </summary>
    public CacheManager CacheManager { get; set; }
    
    /// <summary>
    /// Direct messages (channels) the bot has received.
    /// </summary>
    public IReadOnlyCollection<DmChannel> DmChannels => _dmChannels;
    internal readonly List<DmChannel> _dmChannels = [];
    
    // TODO
    public int ShardId { get; }

    internal readonly Rest _rest;

    /// <summary>
    /// Initializes a client that interacts with the Discord API.
    /// </summary>
    /// <param name="intents">Gateway intents.</param>
    /// <param name="shardId">Bot shard.</param>
    /// <param name="cacheManager">Controls what will be cached. If <c>null</c>, defaults to <see cref="CacheManager.Default"/>.
    /// </param>
    /// <exception cref="DiscordException">Bot token was not set prior to instantiation.</exception>
    public Bot(Intent intents, int shardId = 0, CacheManager? cacheManager = null)
    {
        if (Token is null) throw new DiscordException("Bot token not set");
        Intents = intents;
        ShardId = shardId;
        
        // This needs to be before the instantiation of the gateway.
        _rest = new Rest(this);
        
        _gateway = new Gateway(this, intents);
        CacheManager = cacheManager ?? CacheManager.Default;
        _messageCacheTimer = new Timer(_ =>
        {
            if (_cachedMessages.Count > 0)
                _cachedMessages.RemoveWhere(m => DateTime.UtcNow >= m._expiration);
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }
    
    #region PUBLIC
    
    /// <summary>
    /// Request a webhook.
    /// </summary>
    /// <param name="id">ID of the webhook.</param>
    /// <returns>The requested webhook.</returns>
    /// <remarks>Requires <see cref="Permission.ManageWebhooks"/> unless the application owns the webhook.</remarks>
    public async Task<Webhook> RequestWebhookAsync(ulong id) =>
        await _rest.GetWebhookAsync(id);

    /// <summary>
    /// Retrieve a channel from the cache. This searches through all guild channels, threads, as well as DM channels.
    /// </summary>
    /// <param name="id">Channel ID.</param>
    /// <returns>
    /// The <see cref="GuildChannel"/> or <see cref="DmChannel"/> matching the given ID,
    /// or <c>null</c> if not found.
    /// </returns>
    public IChannel? GetAnyChannel(ulong id)
    {
        Task<IChannel?>[] tasks =
        [
            Task.Run(IChannel? () => GetDmChannel(id)),
            Task.Run(IChannel? () => GetChannel(id)),
            Task.Run(IChannel? () => GetThread(id))
        ];
        var remaining = tasks.ToList();
        while (remaining.Count > 0)
        {
            var index = Task.WaitAny(remaining.Cast<Task>().ToArray());
            var finished = remaining[index];
            var result = finished.Result;
            if (result != null)
                return result;
            remaining.RemoveAt(index);
        }
        return null;
    }
    
    /// <summary>
    /// Retrieve a DM channel from the cache.
    /// </summary>
    /// <param name="id">DM channel ID.</param>
    /// <returns>The DM channel matching the given ID, or <c>null</c> if not found.</returns>
    public DmChannel? GetDmChannel(ulong id) =>
        _dmChannels.FirstOrDefault(c => c.Id == id);
    
    /// <summary>
    /// Retrieve a guild channel from the cache.
    /// </summary>
    /// <param name="id">Guild channel ID.</param>
    /// <returns>The guild channel matching the given ID, or <c>null</c> if not found.</returns>
    public GuildChannel? GetChannel(ulong id)
    {
        foreach (var guild in _guilds)
            if (guild.GetChannel(id) is { } channel)
                return channel;
        return null;
    }
    
    /// <summary>
    /// Guild channels the bot has access to.
    /// </summary>
    /// <returns>All channels in every guild.</returns>
    public IReadOnlyCollection<GuildChannel> GetChannels() => 
        _guilds.SelectMany(g => g.Channels).ToList();
    
    /// <summary>
    /// Retrieve a thread channel from the cache.
    /// </summary>
    /// <param name="id">Thread ID.</param>
    /// <returns>The thread channel matching the given ID, or <c>null</c> if not found.</returns>
    public ThreadChannel? GetThread(ulong id)
    {
        foreach (var guild in _guilds)
            if (guild.GetThread(id) is { } thread)
                return thread;
        return null;
    }

    /// <summary>
    /// Requests the bot's application information.
    /// </summary>
    /// <returns>The application information.</returns>
    public async Task<Application> ApplicationAsync() =>
        await _rest.GetApplicationAsync();
    
    /// <summary>
    /// Requests a guild by its ID.
    /// </summary>
    /// <param name="id">Guild ID.</param>
    /// <returns>The requested guild.</returns>
    public async Task<Guild> RequestGuildAsync(ulong id) =>
        await _rest.GetGuildAsync(id);
    
    /// <summary>
    /// Previews a guild.
    /// </summary>
    /// <param name="id">Guild ID.</param>
    /// <returns>A guild preview.</returns>
    /// <remarks>If the bot is not in the guild, then the guild must have <see cref="GuildFeature.Discoverable"/>.</remarks>
    public async Task<GuildPreview> PreviewGuildAsync(ulong id) =>
        await _rest.GetGuildPreviewAsync(id);
    
    /// <summary>
    /// Retrieves a guild from the cache.
    /// </summary>
    /// <param name="id">Guild ID.</param>
    /// <returns>The guild matching the given ID, or <c>null</c> if not found.</returns>
    public Guild? GetGuild(ulong id) =>
        _guilds.FirstOrDefault(g => g.Id == id);

    /// <summary>
    /// Requests a sticker by its ID.
    /// </summary>
    /// <param name="id">Sticker ID</param>
    /// <returns>The requested sticker.</returns>
    public async Task<Sticker> RequestStickerAsync(ulong id) =>
        await _rest.GetStickerAsync(id);

    /// <summary>
    /// Requests a premium sticker packs.
    /// </summary>
    /// <returns>A list of sticker packs.</returns>
    public async Task<IReadOnlyCollection<StickerPack>> RequestStickerPacksAsync() =>
        await _rest.ListStickerPacksAsync();

    /// <summary>
    /// Requests a specific premium sticker pack.
    /// </summary>
    /// <param name="id">ID of the sticker pack.</param>
    /// <returns>The requested sticker pack.</returns>
    public async Task<StickerPack> RequestStickerPackAsync(ulong id) =>
        await _rest.GetStickerPackAsync(id);

    /// <summary>
    /// Retrieves a message from the cache.
    /// </summary>
    /// <param name="id">Message ID.</param>
    /// <returns>The message matching the given ID, or <c>null</c> if not found.</returns>
    public Message? GetMessage(ulong id)
    {
        if (_cachedMessages.FirstOrDefault(m => m.Id == id) is not { } message) return null;
        // If the message is found, update its expiration.
        message._expiration = DateTime.UtcNow.Add(CacheManager.Messages.Item2);
        return message;
    }
    
    /// <summary>
    /// Request an invite.
    /// </summary>
    /// <param name="code">Invite code.</param>
    /// <param name="withCounts">Whether the invite should contain approximate member counts.</param>
    /// <param name="scheduledEvent">Guild scheduled event to include with the invite.</param>
    /// <returns>The invite matching the given code.</returns>
    public async Task<Invite> RequestInviteAsync(string code, bool withCounts = true, ScheduledEvent? scheduledEvent = null) =>
        await _rest.GetInviteAsync(code, withCounts, scheduledEvent?.Id);
    
    /// <summary>
    /// Updates the bot's presence.
    /// </summary>
    /// <param name="type">Status to change to. Bot users can't use <see cref="StatusType.Invisible"/> or
    /// <see cref="StatusType.Offline"/>.
    /// </param>
    /// <param name="activity">The activity to display.</param>
    public async Task UpdatePresenceAsync(StatusType type, Activity? activity = null)
    {
        var ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        object? sinceValue = type == StatusType.Idle ? ms : null;
        if (activity is not null)
            activity._createdAt = ms;
        var dValue = new
        {
            since = sinceValue,
            activities = activity is null ? [] : new[] { activity },
            status = type.GetDescription(),
            afk = type == StatusType.Idle
        };
        var payload = new
        {
            op = Opcode.PresenceUpdate,
            d = dValue
        };
        await _gateway.SendJsonAsync(payload);
    }

    /// <summary>
    /// All default soundboard sounds.
    /// </summary>
    public async Task<ICollection<SoundboardSound>> DefaultSoundboardsAsync() =>
        await _rest.ListDefaultSoundboardSoundsAsync();
    
    /// <summary>
    /// Start the bot and connect to Discord.
    /// </summary>
    public async Task ConnectAsync()
    {
        if (_gateway._ws.State != WebSocketState.Open)
            await _gateway.ConnectAsync(false);
    }

    /// <summary>
    /// Disconnect from the gateway.
    /// </summary>
    /// <param name="instant">Whether to instantly show the bot as offline. If <c>false</c>, the bot will be shown as
    /// offline after about a minute.
    /// </param>
    public async Task DisconnectAsync(bool instant = true)
    {
        if (_gateway._ws.State == WebSocketState.Open)
        {
            _gateway._userTerminated = true;
            await _gateway.DisconnectAsync(instant);
        }
    }
    
    #endregion
    
    #region PRIVATE

    internal void CacheMessage(Message message)
    {
        var (maxCachedMessages, span) = CacheManager.Messages;
        if (maxCachedMessages == 0) return;
        message._expiration = DateTime.UtcNow.Add(span);
        if (_cachedMessages.Count == maxCachedMessages)
        {
            var oldest = _cachedMessages.OrderBy(m => m.Timestamp).First();
            _cachedMessages.Remove(oldest);
        }
        _cachedMessages.Add(message);
    }
    
    #endregion
}

/// <summary>
/// Represents a users status type.
/// </summary>
public enum StatusType
{
    // DOCS: https://discord.com/developers/docs/events/gateway-events#update-presence-status-types
    
    /// <summary>
    /// Online (green status icon).
    /// </summary>
    [Description("online")]
    Online,
    
    /// <summary>
    /// Do Not Disturb (red status icon).
    /// </summary>
    [Description("dnd")]
    Dnd,
    
    /// <summary>
    /// Idle (yellow status icon).
    /// </summary>
    [Description("idle")]
    Idle,
    
    /// <summary>
    /// Invisible.
    /// </summary>
    [Description("invisible")]
    Invisible,
    
    /// <summary>
    /// Offline.
    /// </summary>
    [Description("offline")]
    Offline
}

/// <summary>
/// Represents an activity status such as "Listening to <b>Spotify</b>" or "Playing <b>Call of Duty</b>."
/// </summary>
/// <param name="Name">Activity's name.</param>
/// <param name="Type">Activity type.</param>
public record Activity(ActivityType Type, string Name)
{
    // DOCS: https://discord.com/developers/docs/events/gateway-events#activity-object
    
    /// <summary>
    /// Activity's name.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = Name;

    /// <summary>
    /// Activity type.
    /// </summary>
    [JsonProperty("type")]
    public ActivityType Type { get; set; } = Type;
    
    /// <summary>
    /// Stream URL.
    /// </summary>
    [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
    public string? Url { get; init; }

    /// <summary>
    /// Unix timestamp (in milliseconds) of when the activity was added to the user's session.
    /// </summary>
    [JsonIgnore] public DateTime CreatedAt => DateTimeOffset.FromUnixTimeMilliseconds(_createdAt).UtcDateTime;
    [JsonProperty("created_at")] public long _createdAt;
    
    /// <summary>
    /// Timestamps for start and/or end of the game.
    /// </summary>
    [JsonProperty("timestamps", NullValueHandling = NullValueHandling.Ignore)]
    public ActivityTimestamp? Timestamps { get; init; }
    
    /// <summary>
    /// Application ID for the game.
    /// </summary>
    [JsonProperty("application_id", NullValueHandling = NullValueHandling.Ignore)]
    public ulong? ApplicationId { get; init; }
    
    /// <summary>
    /// Status display type; controls which field is displayed in the user's status text in the member list.
    /// </summary>
    [JsonProperty("status_display_type", NullValueHandling = NullValueHandling.Ignore)]
    public StatusDisplayType? DisplayType { get; init; }
    
    /// <summary>
    /// What the player is currently doing.
    /// </summary>
    [JsonProperty("details", NullValueHandling = NullValueHandling.Ignore)]
    public string? Details { get; init; }
    
    /// <summary>
    /// URL that is linked when clicking on the details text.
    /// </summary>
    [JsonProperty("details_url", NullValueHandling = NullValueHandling.Ignore)]
    public string? DetailsUrl { get; init; }
    
    /// <summary>
    /// User's current party status, or text used for a custom status.
    /// </summary>
    [JsonProperty("state", NullValueHandling = NullValueHandling.Ignore)]
    public string? State { get; init; }
    
    /// <summary>
    /// URL that is linked when clicking on the state text.
    /// </summary>
    [JsonProperty("state_url", NullValueHandling = NullValueHandling.Ignore)]
    public string? StateUrl { get; init; }
    
    /// <summary>
    /// Emoji used for a custom status.
    /// </summary>
    [JsonProperty("emoji", NullValueHandling = NullValueHandling.Ignore)]
    public PartialEmoji? Emoji { get; init; }
    
    /// <summary>
    /// Information for the current party of the player.
    /// </summary>
    [JsonProperty("party", NullValueHandling = NullValueHandling.Ignore)]
    public ActivityParty? Party { get; init; }
    
    /// <summary>
    /// Images for the presence and their hover texts.
    /// </summary>
    [JsonProperty("assets", NullValueHandling = NullValueHandling.Ignore)]
    public ActivityAsset? Asset { get; init; }
    
    /// <summary>
    /// Secrets for Rich Presence joining and spectating.
    /// </summary>
    [JsonProperty("secrets", NullValueHandling = NullValueHandling.Ignore)]
    public ActivitySecret? Secret { get; init; }
    
    /// <summary>
    /// Whether the activity is an instanced game session.
    /// </summary>
    [JsonProperty("instance", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Instance { get; init; }
    
    /// <summary>
    /// Activity flags, describes what the payload includes.
    /// </summary>
    [JsonIgnore] IReadOnlyCollection<ActivityFlags>? Flags => _flags is not null ? Util.FromBitfield<ActivityFlags>(_flags.Value) : null;
    [JsonProperty("flags", NullValueHandling = NullValueHandling.Ignore)] private int? _flags;
    
    /// <summary>
    /// Custom buttons shown in the Rich Presence (max 2).
    /// </summary>
    [JsonProperty("buttons", NullValueHandling = NullValueHandling.Ignore)]
    public IReadOnlyCollection<ActivityButton>? Buttons { get; init; }
}

/// <summary>
/// Represents an <see cref="Activity"/> button.
/// </summary>
public record ActivityButton
{
    // DOCS: https://discord.com/developers/docs/events/gateway-events#activity-object-activity-buttons
    
    /// <summary>
    /// Text shown on the button (1-32 characters).
    /// </summary>
    [JsonProperty("label")]
    public required string Label { get; init; }
    
    /// <summary>
    /// URL opened when clicking the button (1-512 characters).
    /// </summary>
    [JsonProperty("url")]
    public required string Url { get; init; }
    
    private ActivityButton() { }
}

/// <summary>
/// Represents am <see cref="Activity"/>s flags.
/// </summary>
[Flags]
public enum ActivityFlags
{
    // DOCS: https://discord.com/developers/docs/events/gateway-events#activity-object-activity-flags
    
    Instance = 1 << 0,
    Join = 1 << 1,
    Spectate = 1 << 2,
    JoinRequest = 1 << 3,
    Sync = 1 << 4,
    Play = 1 << 5,
    PartyPrivacyFriends = 1 << 6,
    PartyPrivacyVoiceChannel = 1 << 7,
    Embedded = 1 << 8
}

/// <summary>
/// Represents an <see cref="Activity"/> secret.
/// </summary>
public record ActivitySecret
{
    // DOCS: https://discord.com/developers/docs/events/gateway-events#activity-object-activity-secrets
    
    /// <summary>
    /// Secret for joining a party.
    /// </summary>
    [JsonProperty("join")]
    public string? Join { get; init; }
    
    /// <summary>
    /// Secret for spectating a game.
    /// </summary>
    [JsonProperty("spectate")]
    public string? Spectate { get; init; }
    
    /// <summary>
    /// Secret for a specific instanced match.
    /// </summary>
    [JsonProperty("match")]
    public string? Match { get; init; }
    
    private ActivitySecret() { }
}

/// <summary>
/// Represents an <see cref="Activity"/> asset.
/// </summary>
public record ActivityAsset
{
    // DOCS: https://discord.com/developers/docs/events/gateway-events#activity-object-activity-assets
    
    /// <summary>
    /// Large image value.
    /// </summary>
    [JsonProperty("large_image")]
    public string? LargeImage { get; init; }
    
    /// <summary>
    /// Text displayed when hovering over the large image of the activity.
    /// </summary>
    [JsonProperty("large_text")]
    public string? LargeText { get; init; }
    
    /// <summary>
    /// URL that is opened when clicking on the large image.
    /// </summary>
    [JsonProperty("large_url")]
    public string? LargeUrl { get; init; }
    
    /// <summary>
    /// Small image value.
    /// </summary>
    [JsonProperty("small_image")]
    public string? SmallImage { get; init; }
    
    /// <summary>
    /// Text displayed when hovering over the small image of the activity.
    /// </summary>
    [JsonProperty("small_text")]
    public string? SmallText { get; init; }
    
    /// <summary>
    /// URL that is opened when clicking on the small image.
    /// </summary>
    [JsonProperty("small_url")]
    public string? SmallUrl { get; init; }
    
    /// <summary>
    /// Displayed as a banner on a Game Invite.
    /// </summary>
    [JsonProperty("invite_cover_image")]
    public string? InviteCoverImage { get; init; }
    
    private ActivityAsset() { }
}

/// <summary>
/// Represents an <see cref="Activity"/> party.
/// </summary>
public record ActivityParty
{
    // DOCS: https://discord.com/developers/docs/events/gateway-events#activity-object-activity-party
    
    /// <summary>
    /// ID of the party.
    /// </summary>
    [JsonProperty("id")]
    public string? Id { get; init; }
    
    /// <summary>
    /// List of two integers (current size, max size), used to show the party's current and maximum size.
    /// </summary>
    [JsonProperty("size")]
    public List<int>? Size { get; init; }
    
    private ActivityParty() { }
}

/// <summary>
/// Represents an <see cref="Activity"/> status display type.
/// </summary>
public enum StatusDisplayType
{
    // DOCS: https://discord.com/developers/docs/events/gateway-events#activity-object-status-display-types
    
    /// <summary>
    /// "Listening to Spotify"
    /// </summary>
    Name,
    
    /// <summary>
    /// "Listening to Rick Astley"
    /// </summary>
    State,
    
    /// <summary>
    /// Listening to Never Gonna Give You Up"
    /// </summary>
    Details
}

/// <summary>
/// Represents an <see cref="Activity"/> timestamp.
/// </summary>
public record ActivityTimestamp
{
    // DOCS: https://discord.com/developers/docs/events/gateway-events#activity-object-activity-timestamps
    
    /// <summary>
    /// When the activity started.
    /// </summary>
    [JsonIgnore] public DateTime? Start => _start is not null ? DateTimeOffset.FromUnixTimeMilliseconds(_start.Value).UtcDateTime : null; 
    [JsonProperty("start")] private long? _start;
    
    /// <summary>
    /// When the activity ends.
    /// </summary>
    [JsonIgnore] public DateTime? End => _end is not null ? DateTimeOffset.FromUnixTimeMilliseconds(_end.Value).UtcDateTime : null; 
    [JsonProperty("end")] private long? _end;
    
    private ActivityTimestamp() { }
}

/// <summary>
/// Represents an <see cref="Activity"/> type.
/// </summary>
public enum ActivityType
{
    // DOCS: https://discord.com/developers/docs/events/gateway-events#activity-object-activity-types
    
    /// <summary>
    /// "Playing <b>Rocket League</b>"
    /// </summary>
    Playing,
    
    /// <summary>
    /// "Streaming <b>Rocket League</b>"
    /// </summary>
    Streaming,
    
    /// <summary>
    /// "Listening to <b>Spotify</b>"
    /// </summary>
    Listening,
    
    /// <summary>
    /// "Watching <b>YouTube Together</b>"
    /// </summary>
    Watching,
    
    /// <summary>
    /// 😎 I am cool
    /// </summary>
    Custom,
    
    /// <summary>
    /// "Competing in <b>Arena World Champions</b>"
    /// </summary>
    Competing
}

/// <summary>
/// Represents the bot's cache manager that controls which items will be cached.
/// </summary>
public record struct CacheManager
{
    /// <summary>
    /// Whether <see cref="Message"/>s are cached, and for how long.
    /// </summary>
    public (uint, TimeSpan) Messages;
    
    /// <summary>
    /// Whether <see cref="Guild.Members"/> are cached. The bot member object is always cached no matter the value and
    /// can be accessed via <see cref="Guild.Self"/>.
    /// </summary>
    public bool Members;

    /// <summary>
    /// Initializes a cache manager with the following settings:
    /// <list type="bullet">
    ///     <item><c>Messages</c> = 0 (messages are never cached)</item>
    ///     <item><c>Members</c> = <c>true</c></item>
    /// </list>
    /// </summary>
    public static readonly CacheManager MembersOnly = new()
    {
        Messages = (0, TimeSpan.FromMinutes(0)),
        Members = true
    };
    
    /// <summary>
    /// Initializes a cache manager with the following settings:
    /// <list type="bullet">
    ///     <item><c>Messages</c> = 0 (messages are never cached)</item>
    ///     <item><c>Members</c> = <c>false</c></item>
    /// </list>
    /// </summary>
    public static readonly CacheManager None = new()
    {
        Messages = (0, TimeSpan.FromMinutes(0)),
        Members = false
    };
    
    /// <summary>
    /// Initializes a cache manager with the following settings:
    /// <list type="bullet">
    ///     <item><c>Messages</c> = 1000 max, 5 minutes in cache</item>
    ///     <item><c>Members</c> = <c>false</c></item>
    /// </list>
    /// </summary>
    public static readonly CacheManager Limited = new()
    {
        Messages = (1000, TimeSpan.FromMinutes(5)),
        Members = false
    };
    
    /// <summary>
    /// Initializes a cache manager with the following settings:
    /// <list type="bullet">
    ///     <item><c>Messages</c> = 5000 max, 15 minutes in cache</item>
    ///     <item><c>Members</c> = <c>false</c></item>
    /// </list>
    /// </summary>
    public static readonly CacheManager Default = new()
    {
        Messages = (5000, TimeSpan.FromMinutes(15)),
        Members = false
    };
    
    /// <summary>
    /// Initializes a cache manager with the following settings:
    /// <list type="bullet">
    ///     <item><c>Messages</c> = 10,000 max, 30 minutes in cache</item>
    ///     <item><c>Members</c> = <c>true</c></item>
    /// </list>
    /// </summary>
    public static readonly CacheManager Many = new()
    {
        Messages = (10_000, TimeSpan.FromMinutes(30)),
        Members = true
    };
    
    /// <summary>
    /// Initializes a cache manager with its values equivalent to <see cref="None"/>.
    /// </summary>
    public CacheManager() { }

    /// <summary>
    /// Initializes a cache manager with the given values.
    /// </summary>
    /// <param name="messages">Whether <see cref="Message"/>s are cached, and for how long.</param>
    /// <param name="members">Whether <see cref="Guild.Members"/> are cached. The bot member object is always cached no
    /// matter the value and can be accessed via <see cref="Guild.Self"/>.
    /// </param>
    public CacheManager((uint, TimeSpan) messages, bool members)
    {
        Messages = messages;
        Members = members;
    }
}
