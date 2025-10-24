using System.ComponentModel;
using System.Net.WebSockets;
using Discord.Net;
using Discord.Models;
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
    /// All guilds the bot is currently in.
    /// </summary>
    public IReadOnlySet<Guild> Guilds => _guilds;
    internal readonly HashSet<Guild> _guilds = [];

    /// <summary>
    /// All messages in every guild that the bot has permissions to see.
    /// </summary>
    public IReadOnlySet<Message> Messages => _cachedMessages;
    private readonly HashSet<Message> _cachedMessages = [];
    private Timer _messageCacheTimer;
    
    /// <summary>
    /// Events that the bot can listen for.
    /// </summary>
    public DiscordGatewayClient Events => _gateway;
    internal readonly DiscordGatewayClient _gateway;

    /// <summary>
    /// A simple way to store items that are related to the bot's usage. This library never processes the information in said storage,
    /// and is entirely handled by you.
    /// </summary>
    public Dictionary<string, object> Storage = [];
    
    /// <summary>
    /// The bots gateway intents. Handles which events are dispatched by Discord.
    /// </summary>
    public Intents Intents { get; }
    
    /// <summary>
    /// Controls what will be cached.
    /// </summary>
    public CacheManager CacheManager { get; set; }
    
    // TODO
    public int ShardId { get; }

    internal readonly Rest _rest;

    public Bot(Intents intents, int shardId = 0, CacheManager? cacheManager = null)
    {
        if (Token is null) throw new DiscordException("Bot token not set");
        Intents = intents;
        ShardId = shardId;
        _gateway = new DiscordGatewayClient(this, intents);
        _rest = new Rest(this);
        CacheManager = cacheManager ?? CacheManager.Default;
        _messageCacheTimer = new Timer(_ =>
        {
            if (_cachedMessages.Count > 0)
                _cachedMessages.RemoveWhere(m => m._expiration >= DateTime.UtcNow);
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }
    
    #region PUBLIC

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
    
    /// <summary>
    /// Updates the bot's presence.
    /// </summary>
    /// <param name="status">Status to change to.</param>
    /// <param name="activity">The activity to display.</param>
    // public async Task UpdatePresenceAsync(StatusType status, Activity? activity)
    // {
    //     object? sinceValue = status == StatusType.Idle ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : null;
    //     var activityList = new List<Activity>();
    //     if (activity is not null)
    //         activityList.Add(activity);
    //     var dValue = new
    //     {
    //         status = status.GetDescription(),
    //         afk = status == StatusType.DoNotDisturb,
    //         since = sinceValue,
    //         activities = activityList
    //     };
    //     var payload = new
    //     {
    //         op = Opcode.PresenceUpdate,
    //         d = dValue
    //     };
    //     await _gateway.SendPayloadAsync(payload);
    // }

    /// <summary>
    /// Start the bot and connect to Discord.
    /// </summary>
    public async Task RunAsync()
    {
        if (_gateway._ws.State != WebSocketState.Open)
            await _gateway.ConnectAsync(false);
    }
}

/// <summary>
/// Represents the status type.
/// </summary>
public enum StatusType
{
    /// <summary>
    /// Green status icon.
    /// </summary>
    [Description("online")]
    Online,
    
    /// <summary>
    /// Red status icon.
    /// </summary>
    [Description("dnd")]
    DoNotDisturb,
    
    /// <summary>
    /// Yellow status icon.
    /// </summary>
    [Description("idle")]
    Idle
    
    // NOTE: Bot users can't use this type
    // Invisible
}

/// <summary>
/// Represents an activity status such as "Listening to <b>Spotify</b>" or "Playing <b>Call of Duty</b>."
/// </summary>
/// <param name="Type">Activity type</param>
/// <param name="Name">Activity name</param>
public record Activity(ActivityType Type, string Name)
{
    [JsonProperty("type")]
    public ActivityType Type = Type;
    
    [JsonProperty("name")]
    public string Name = Name;
}

/// <summary>
/// Represents an activity type.
/// </summary>
public enum ActivityType
{
    /// <summary>
    /// "Playing <b>Rocket League</b>"
    /// </summary>
    Playing,
    
    /// <summary>
    /// "Listening to <b>Spotify</b>"
    /// </summary>
    Listening = 2,
    
    /// <summary>
    /// "Watching <b>YouTube Together</b>"
    /// </summary>
    Watching,
    
    /// <summary>
    /// "Competing in <b>Arena World Champions</b>"
    /// </summary>
    Competing = 5
    
    // NOTE: Bot users can't use these types
    // Streaming
    // Custom
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
