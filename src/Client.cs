using System.ComponentModel;
using System.Net.WebSockets;
using Discord.Net;
using Discord.Models;
using Newtonsoft.Json;

namespace Discord;

public class Bot
{
    /// <summary>
    /// The bot authentication token. 
    /// </summary>
    public readonly string Token;
    
    public IReadOnlyCollection<Guild> Guilds => _guilds;
    internal readonly HashSet<Guild> _guilds = [];

    public IReadOnlyCollection<Message> Messages => _cachedMessages;
    internal readonly List<Message> _cachedMessages = [];
    
    public DiscordGatewayClient Events => _client;
    private readonly DiscordGatewayClient _client;
    
    public int ShardId { get; }
    public Intents Intents { get; }
    public CacheManager CacheManager;

    internal readonly Rest _rest;
    internal Timer _messageCacheTimer;

    public Bot(string token, Intents intents, int shardId = 0, CacheManager? cacheManager = null)
    {
        Token = token;
        Intents = intents;
        ShardId = shardId;
        _client = new DiscordGatewayClient(this, token, intents);
        _rest = new Rest(this);
        CacheManager = cacheManager ?? CacheManager.Default;
        _messageCacheTimer = new Timer(state =>
        {
            if (_cachedMessages.Count > 0)
                _cachedMessages.RemoveAll(m => m._expiration >= DateTime.UtcNow);
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }
    
    #region PUBLIC
    
    /// <summary>
    /// Retrieves the guild from the cache, or <c>null</c> if not found.
    /// </summary>
    /// <param name="id">Guild ID.</param>
    /// <returns></returns>
    public Guild? GetGuild(ulong id) => _guilds.FirstOrDefault(g => g.Id == id);
    
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

    public async Task RunAsync()
    {
        if (_client._ws.State != WebSocketState.Open)
            await _client.ConnectAsync(false);
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

public record struct CacheManager
{
    public (uint, TimeSpan) Messages;
    public bool Channels;
    public bool Roles;
    public bool Stickers;
    public bool Emojis;
    
    public static readonly CacheManager None = new()
    {
        Messages = (0, TimeSpan.FromMinutes(0))
    };

    public static readonly CacheManager Default = new()
    {
        Messages = (1000, TimeSpan.FromMinutes(0)),
        Channels = true
    };
    
    public static readonly CacheManager Limited = new()
    {
        Messages = (2500, TimeSpan.FromMinutes(15)),
        Channels = true
    };
    
    public static readonly CacheManager Many = new()
    {
        Messages = (5000, TimeSpan.FromMinutes(30)),
        Channels = true,
        Roles = true,
        Stickers = true,
        Emojis = true
    };
}
