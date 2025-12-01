using Discord.Channels.Abstractions;
using Discord.Channels.Services;
using Discord.Models;
using Discord.Utility;
using Newtonsoft.Json;

namespace Discord.Channels.Models;

/// <summary>
/// Represents a <see cref="Guild"/> thread channel.
/// </summary>
public class ThreadChannel : GuildChannel, IMessageable
{
    // DOCS: https://discord.com/developers/docs/topics/threads
    
    /// <summary>
    /// ID of the thread creator.
    /// </summary>
    [JsonProperty("owner_id")]
    public ulong? OwnerId { get; init; }
    
    /// <summary>
    /// Number of messages in a thread. This does not include the initial message or deleted messages.
    /// </summary>
    [JsonProperty("message_count")]
    public int MessageCount { get; internal set; }
    
    /// <summary>
    /// An approximate count of users in a thread, stops counting at 50.
    /// </summary>
    [JsonProperty("member_count")]
    public int MemberCount { get; internal set; }
    
    /// <summary>
    /// Number of messages ever sent in a thread. Similar to <see cref="MessageCount"/> on message creation, but will not
    /// decrement when a message is deleted.
    /// </summary>
    [JsonProperty("total_message_sent")]
    public int TotalMessagesSent { get; internal set; }

    /// <summary>
    /// Additional information for the thread.
    /// </summary>
    [JsonProperty("thread_metadata")]
    public ThreadMetadata ThreadMetadata { get; internal set; } = null!;
    
    /// <inheritdoc cref="ForumChannel.Flags"/>
    public IReadOnlyCollection<ChannelFlags> Flags => _flags is not null
        ? Util.FromBitfield<ChannelFlags>(_flags.Value)
        : Array.Empty<ChannelFlags>();
    [JsonProperty("flags")] private int? _flags;

    /// <summary>
    /// Members in the thread.
    /// </summary>
    public IReadOnlyCollection<ThreadMember> Members => _members;
    internal readonly List<ThreadMember> _members = [];
    
    private ThreadChannel() { }
    
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Message>> RequestMessages(MessageHistory history = MessageHistory.Before,
        DateTime? dt = null, int limit = 50) =>
        await Bot._rest.GetChannelMessages(Id, history, dt, limit);
    
    /// <inheritdoc/>
    public async Task<Message> RequestMessage(ulong id) =>
        await Bot._rest.GetChannelMessage(Id, id);

    /// <inheritdoc/>
    public async Task<Message> SendAsync(string? content = null, bool silent = false, bool tts = false,
        IEnumerable<Embed>? embeds = null,
        AllowedMentions? allowedMentions = null, IEnumerable<GuildSticker>? stickers = null, Poll? poll = null,
        ICollection<DFile>? files = null, MessageReference? reference = null) =>
        await ChannelServicer.SendAsync(this, content, silent, tts, embeds, allowedMentions, stickers, poll, files,
            reference);

    /// <inheritdoc/>
    public async Task TriggerTypingAsync(Func<Task>? func = null, CancellationToken ct = default) =>
        await ChannelServicer.TriggerTypingAsync(this, func, ct);
}

/// <summary>
/// Represents a <see cref="ThreadChannel"/> archive duration.
/// </summary>
public enum ThreadArchiveDuration
{
    // DOCS: In app.
    
    /// <summary>
    /// 1 hour duration.
    /// </summary>
    OneHour = 60,
    
    /// <summary>
    /// 24 hour duration.
    /// </summary>
    TwentyFourHours = 1440,
    
    /// <summary>
    /// 3 day duration.
    /// </summary>
    ThreeDays = 4320,
    
    /// <summary>
    /// 7 day duration.
    /// </summary>
    OneWeek = 10080
}

/// <summary>
/// Represents a member that's in a <see cref="ThreadChannel"/>.
/// </summary>
public record ThreadMember
{
    // DOCS: https://discord.com/developers/docs/resources/channel#thread-member-object
    
    /// <summary>
    /// ID of the thread.
    /// </summary>
    [JsonProperty("id")]
    public ulong? ThreadId { get; init; }
    
    /// <summary>
    /// ID of the user.
    /// </summary>
    [JsonProperty("user_id")]
    public ulong? UserId { get; init; }
    
    /// <summary>
    /// Time the user last joined the thread.
    /// </summary>
    [JsonProperty("join_timestamp")]
    public DateTime JoinTimestamp { get; init; }
    
    private ThreadMember() { }
}

/// <summary>
/// Represents additional information for a <see cref="ThreadChannel"/>.
/// </summary>
public record ThreadMetadata
{
    /// <summary>
    /// Whether the thread is archived.
    /// </summary>
    [JsonProperty("archived")]
    public bool IsArchived { get; internal set; }

    /// <summary>
    /// When the thread will stop showing in the channel list after a specified amount of minutes. Can be set to
    /// 60, 1440, 4320, or 10080.
    /// </summary>
    [JsonProperty("auto_archive_duration")]
    public int AutoArchiveDuration { get; init; }

    /// <summary>
    /// Whether the thread is locked. When a thread is locked, only users with <see cref="Permission.ManageThreads"/> can
    /// unarchive it.
    /// </summary>
    [JsonProperty("locked")]
    public bool IsLocked { get; init; }

    /// <summary>
    /// Whether non-moderators can add other non-moderators to a thread; only available on private threads.
    /// </summary>
    [JsonProperty("invitable")]
    public bool? IsInvitable { get; init; }

    /// <summary>
    /// When the thread was created; only populated for threads created after January 9th, 2022.
    /// </summary>
    [JsonProperty("create_timestamp")]
    public DateTime? CreateTimestamp { get; init; }

    private ThreadMetadata() { }
}
