using System.ComponentModel;
using System.Text.Json;
using Discord.Net;
using Discord.Utility;
using Newtonsoft.Json;

namespace Discord.Models;

/// <summary>
/// Represents a channel type.
/// </summary>
public enum ChannelType
{
    // DOCS: https://discord.com/developers/docs/resources/channel#channel-object-channel-types
    
    /// <summary>
    /// A text channel within a guild.
    /// </summary>
    GuildText,
    
    /// <summary>
    /// A direct message between users.
    /// </summary>
    Dm,
    
    /// <summary>
    /// A voice channel within a guild.
    /// </summary>
    GuildVoice,
    
    /// <summary>
    /// An organizational category that contains up to 50 channels.
    /// </summary>
    GuildCategory = 4,
    
    /// <summary>
    /// A channel that users can follow and crosspost into their own guild.
    /// </summary>
    GuildAnnouncement,
    
    /// <summary>
    /// A temporary sub-channel within a <see cref="GuildAnnouncement"/> channel.
    /// </summary>
    AnnouncementThread = 10,
    
    /// <summary>
    /// A temporary sub-channel within a <see cref="GuildText"/> or <see cref="GuildForum"/> channel.
    /// </summary>
    PublicThread,

    /// <summary>
    /// A temporary sub-channel within a <see cref="GuildText"/> channel that is only viewable by those invited and those
    /// with <see cref="Permission.ManageThreads"/>.
    /// </summary>
    PrivateThread,
    
    /// <summary>
    /// A voice channel for hosting events with an audience.
    /// </summary>
    GuildStageVoice = 13,
    
    /// <summary>
    /// Channel that can only contain threads.
    /// </summary>
    GuildForum = 15,
    
    /// <summary>
    /// Similar to a <see cref="GuildForum"/>.
    /// </summary>
    GuildMedia
}

/// <summary>
/// Represents a channel's flags.
/// </summary>
[Flags]
public enum ChannelFlags
{
    // DOCS: https://discord.com/developers/docs/resources/channel#channel-object-channel-flags
    
    /// <summary>
    /// A thread is pinned to the top of its parent <see cref="ForumChannel"/> or <see cref="MediaChannel"/>.
    /// </summary>
    Pinned                   = 1 << 1,
    
    /// <summary>
    /// Whether a tag is required to be specified when creating a thread in a <see cref="ForumChannel"/> or
    /// <see cref="MediaChannel"/>. Tags are specified in <see cref="ForumChannel.AppliedTags"/>.
    /// </summary>
    RequireTag               = 1 << 4,
    
    /// <summary>
    /// When set, hides the embedded media download options (only for <see cref="MediaChannel"/>).
    /// </summary>
    HideMediaDownloadOptions = 1 << 15
}

/// <summary>
/// Represents a basic channel.
/// </summary>
public interface IChannel
{
    /// <summary>
    /// Channel ID.
    /// </summary>
    public ulong Id { get; }
    
    /// <summary>
    /// Channel type.
    /// </summary>
    public ChannelType Type { get; }
    
    /// <summary>
    /// Your bot instance object.
    /// </summary>
    public Bot Bot { get; }
}

/// <summary>
/// Represents a channel that belongs to a <see cref="Guild"/>.
/// </summary>
public interface IGuildChannel : IChannel
{
    /// <summary>
    /// Permission overwrites for the channel.
    /// </summary>
    public IReadOnlyCollection<PermissionOverwrites> Overwrites { get; }
    
    /// <summary>
    /// Channel name.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// ID of the guild the channel belongs to.
    /// </summary>
    public ulong GuildId { get; }
    
    /// <summary>
    /// Guild the channel belongs to.
    /// </summary>
    public Guild Guild { get; }
    
    /// <summary>
    /// ID of the category the channel belongs to.
    /// </summary>
    public ulong? ParentId { get; }
    
    /// <summary>
    /// Channel topic (0-4096 characters for GUILD_FORUM and GUILD_MEDIA channels, 0-1024 characters for all others)
    /// </summary>
    public string? Topic { get; }
    
    internal static (List<IGuildChannel> channels, List<ThreadChannel> threads) ParseAll(IEnumerable<JSON> reg_channels, IEnumerable<JSON> reg_threads)
    {
        var channels = new List<IGuildChannel>();
        var threads = new List<ThreadChannel>();

        // Normal channels, text, voice, etc.
        var doc = JsonDocument.Parse(JsonConvert.SerializeObject(reg_channels));
        foreach (var channelElement in doc.RootElement.EnumerateArray())
        {
            var value = Convert.ToInt32(channelElement.GetProperty("type").ToString());
            switch ((ChannelType)value)
            {
                case ChannelType.GuildText:
                    var text = DiscordGatewayClient.Deserialize<TextChannel>(channelElement);
                    channels.Add(text);
                    break;
                case ChannelType.GuildVoice:
                    var voice = DiscordGatewayClient.Deserialize<VoiceChannel>(channelElement);
                    channels.Add(voice);
                    break;
                case ChannelType.GuildCategory:
                    var cat = DiscordGatewayClient.Deserialize<CategoryChannel>(channelElement);
                    channels.Add(cat);
                    break;
                case ChannelType.GuildAnnouncement:
                    var announcement = DiscordGatewayClient.Deserialize<AnnouncementChannel>(channelElement);
                    channels.Add(announcement);
                    break;
                case ChannelType.GuildStageVoice:
                    var stage = DiscordGatewayClient.Deserialize<StageChannel>(channelElement);
                    channels.Add(stage);
                    break;
                case ChannelType.GuildForum:
                    var forum = DiscordGatewayClient.Deserialize<ForumChannel>(channelElement);
                    channels.Add(forum);
                    break;
                case ChannelType.GuildMedia:
                    var media = DiscordGatewayClient.Deserialize<MediaChannel>(channelElement);
                    channels.Add(media);
                    break;
                default:
                    continue;
            }
        }
        
        // Threads only.
        var docT = JsonDocument.Parse(JsonConvert.SerializeObject(reg_threads));
        foreach (var threadElement in docT.RootElement.EnumerateArray())
        {
            var thread = DiscordGatewayClient.Deserialize<ThreadChannel>(threadElement);
            threads.Add(thread);
        }
        return (channels, threads);
    }
}

/// <summary>
/// Represents a channel that can have messages sent to it.
/// </summary>
public abstract class Messageable : IChannel
{
    /// <inheritdoc/>
    [JsonProperty("id")]
    public ulong Id { get; init; }
    
    /// <inheritdoc/>
    [JsonProperty("type")]
    public ChannelType Type { get; init; }
    
    /// <summary>
    /// ID of the last message that was sent in this channel.
    /// </summary>
    [JsonProperty("last_message_id")]
    public ulong? LastMessageId { get; internal set; }
    
    /// <inheritdoc/>
    public Bot Bot { get; internal set; }
    
    /// <summary>
    /// All cached messages for this channel.
    /// </summary>
    public IReadOnlyCollection<Message> Messages => Bot._cachedMessages.Where(m => m.ChannelId == Id).ToList();
    
    // TODO
    public async Task<Message> SendAsync(string? content = null)
    {
        using var form = new MultipartFormDataContent(Dev.Boundary);
        
        if (content != null)
            form.Add(new StringContent(content), "content");

        if (Type == ChannelType.Dm)
        {
            var dmChannel = await Bot._rest.CreateDmAsync(((DmChannel)this).Recipient.Id);
            Bot._dmChannels.Add(dmChannel);
        }

        var message = await Bot._rest.CreateMessageAsync(Id, form);
        Bot._rest.SetMessageValues([message]);
        return message;
    }
}

/// <summary>
/// Represents a channel for a <see cref="Guild"/> where messages can be sent.
/// </summary>
public abstract class GuildChannelMessageable : Messageable, IGuildChannel
{
    /// <summary>
    /// Explicit permission overwrites for members and roles.
    /// </summary>
    public IReadOnlyCollection<PermissionOverwrites> Overwrites => PermissionOverwrites.Parse(_permissionOverwrites ?? []);
    [JsonProperty("permission_overwrites")] internal List<JSON>? _permissionOverwrites;
    
    /// <summary>
    /// Name of the channel (1-100 characters).
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; internal set; } = string.Empty;
    
    /// <summary>
    /// ID of the guild the channel belongs to.
    /// </summary>
    public ulong GuildId { get; internal set; }
    
    /// <inheritdoc/>
    public Guild Guild { get; internal set; }
    
    /// <summary>
    /// ID of the parent channel the channel belongs to.
    /// </summary>
    [JsonProperty("parent_id")]
    public ulong? ParentId { get; internal set; }
    
    /// <summary>
    /// When the last message pinned message was pinned. Can be <c>null</c> if initially accessing from event
    /// <see cref="DiscordGatewayClient.OnGuildCreate"/>.
    /// </summary>
    [JsonProperty("last_pin_timestamp")]
    public DateTime? LastPinned { get; internal set; }

    /// <inheritdoc/>
    [JsonProperty("topic")]
    public string? Topic { get; internal set; }

    /// <summary>
    /// Sorting position of the channel.
    /// </summary>
    [JsonProperty("position")]
    public int Position { get; internal set; }
    
    /// <summary>
    /// Amount of seconds a user has to wait before sending another message (0-21600); bots, as well as users with
    /// <see cref="Permission.ManageMessages"/> or <see cref="Permission.ManageChannels"/> , are unaffected.
    /// </summary>
    [JsonProperty("rate_limit_per_user")]
    public int? SlowModeSeconds { get; internal set; }
    
    /// <summary>
    /// Whether the channel is NSFW (age restricted).
    /// </summary>
    [JsonProperty("nsfw")]
    public bool? IsNsfw { get; internal set; }
    
    /// <summary>
    /// When threads will stop showing in the channel list after the specified period of inactivity, can be set to: 60,
    /// 1440, 4320, or 10080.
    /// </summary>
    [JsonProperty("default_auto_archive_duration")]
    public int? DefaultArchiveDuration { get; internal set; }

    public override string ToString() => Name;
}

/// <summary>
/// Represents a text channel for a <see cref="Guild"/>.
/// </summary>
public class TextChannel : GuildChannelMessageable
{
    private TextChannel() { }
}

/// <summary>
/// Represents an announcement channel for a <see cref="Guild"/>.
/// </summary>
public class AnnouncementChannel : GuildChannelMessageable
{
    private AnnouncementChannel() { }
}

/// <summary>
/// Represents a voice channel for a <see cref="Guild"/>.
/// </summary>
public class VoiceChannel : GuildChannelMessageable
{
    /// <summary>
    /// The bitrate (in bits).
    /// </summary>
    [JsonProperty("bitrate")]
    public int Bitrate { get; internal set; }

    /// <summary>
    /// The user limit. If 0 it has no user limit.
    /// </summary>
    [JsonProperty("user_limit")]
    public int UserLimit { get; internal set; }

    /// <summary>
    /// Voice region ID (location).
    /// </summary>
    public VoiceRegionLocation VoiceRegionLocation =>
        Enum.GetValues(typeof(VoiceRegionLocation)).Cast<VoiceRegionLocation>()
            .FirstOrDefault(loc => loc.GetDescription() == _voiceRegion);
    [JsonProperty("rtc_region")] private string? _voiceRegion;

    /// <summary>
    /// The camera video quality mode.
    /// </summary>
    public VideoQualityMode VideoQualityMode =>
        _videoQualityMode is not null ? (VideoQualityMode)_videoQualityMode : VideoQualityMode.Auto;
    [JsonProperty("video_quality_mode")] private int? _videoQualityMode;

    internal VoiceChannel() { }
}

/// <summary>
/// Represents the video quality of a camera for a <see cref="VoiceChannel"/>.
/// </summary>
public enum VideoQualityMode
{
    // DOCS: https://discord.com/developers/docs/resources/channel#channel-object-video-quality-modes
    
    Auto = 1,
    Full
}

/// <summary>
/// Represents the voice region location for a <see cref="VoiceChannel"/>.
/// </summary>
public enum VoiceRegionLocation
{
    // DOCS: In app
    
    Automatic,
    
    [Description("brazil")]
    Brazil,
    
    [Description("hongkong")]
    Hongkong,
    
    [Description("india")]
    India,
    
    [Description("japan")]
    Japan,
    
    [Description("rotterdam")]
    Rotterdam,
    
    [Description("singapare")]
    Singapore,
    
    [Description("southafrica")]
    SouthAfrica,
    
    [Description("sydney")]
    Sydney,
    
    [Description("us-central")]
    UsCentral,
    
    [Description("us-east")]
    UsEast,
    
    [Description("us-south")]
    UsSouth,
    
    [Description("us-west")]
    UsWest
}

// TODO
// /// <summary>
// /// Represents a <see cref="VoiceChannel"/> or <see cref="StageChannel"/> region.
// /// </summary>
// public record VoiceRegion
// {
//     // DOCS: https://discord.com/developers/docs/resources/voice#voice-region-object
//     
//     /// <summary>
//     /// Unique ID for the region.
//     /// </summary>
//     [JsonProperty("id")]
//     public ulong Id { get; init; }
//     
//     /// <summary>
//     /// Name of the region.
//     /// </summary>
//     [JsonProperty("name")]
//     public required string Name { get; init; }
//     
//     /// <summary>
//     /// <c>true</c> for a single server that is closest to the current user’s client.
//     /// </summary>
//     [JsonProperty("optimal")]
//     public bool IsOptimal { get; init; }
//     
//     /// <summary>
//     /// Whether this is a deprecated voice region (avoid switching to these).
//     /// </summary>
//     [JsonProperty("deprecated")]
//     public bool IsDeprecated { get; init; }
//     
//     /// <summary>
//     /// Whether this is a custom voice region (used for events/etc).
//     /// </summary>
//     [JsonProperty("custom")]
//     public bool IsCustom { get; init; }
//     
//     private VoiceRegion() { }
// }

/// <summary>
/// Represents a stage channel for a <see cref="Guild"/>.
/// </summary>
public class StageChannel : VoiceChannel
{
    private StageChannel() { }
}

/// <summary>
/// Represents a forum channel for a <see cref="Guild"/>.
/// </summary>
public class ForumChannel : IGuildChannel
{
    /// <inheritdoc/>
    [JsonProperty("id")]
    public ulong Id { get; init; }
    
    /// <inheritdoc/>
    [JsonProperty("type")]
    public ChannelType Type { get; init; }
    
    /// <inheritdoc/>
    public IReadOnlyCollection<PermissionOverwrites> Overwrites => PermissionOverwrites.Parse(_permissionOverwrites ?? []);
    [JsonProperty("permission_overwrites")] internal List<JSON>? _permissionOverwrites;
    
    /// <inheritdoc/>
    [JsonProperty("name")]
    public string Name { get; internal set; } = string.Empty;
    
    /// <inheritdoc/>
    [JsonProperty("guild_id")]
    public ulong GuildId { get; internal set; }
    
    /// <inheritdoc/>
    public Guild Guild { get; internal set; }
    
    /// <inheritdoc/>
    [JsonProperty("category_id")]
    public ulong? ParentId { get; internal set; }
    
    /// <inheritdoc/>
    [JsonProperty("topic")]
    public string? Topic { get; internal set; }
    
    /// <inheritdoc/>
    public Bot Bot { get; internal set; }

    /// <summary>
    /// The channels flag's.
    /// </summary>
    public IReadOnlyCollection<ChannelFlags> Flags => _flags is not null
        ? Util.FromBitfield<ChannelFlags>(_flags.Value)
        : Array.Empty<ChannelFlags>();
    [JsonProperty("flags")] private int? _flags;

    /// <summary>
    /// Tags that can be used.
    /// </summary>
    public IReadOnlyCollection<Tag>? AvailableTags => _availableTags;
    [JsonProperty("available_tags")] private List<Tag>? _availableTags;
    
    /// <summary>
    /// Tags that have been applied to a thread.
    /// </summary>
    public IReadOnlyCollection<Tag>? AppliedTags => _appliedTags;
    [JsonProperty("applied_tags")] private List<Tag>? _appliedTags;
    
    /// <summary>
    /// The emoji to show in the add reaction button on a thread.
    /// </summary>
    [JsonProperty("default_reaction_emoji")]
    public DefaultReactionEmoji? DefaultReactionEmoji { get; internal set; }
    
    /// <summary>
    /// The initial limit to set on newly created threads in a channel.
    /// </summary>
    [JsonProperty("default_thread_rate_limit_per_user")]
    public int? DefaultThreadSlowModeSeconds { get; internal set; }
    
    /// <summary>
    /// The default sort order used to order posts.
    /// </summary>
    [JsonProperty("default_sort_order")]
    public SortOrder? DefaultSortOrder { get; internal set; }
    
    /// <summary>
    /// The default layout view used to display posts.
    /// </summary>
    [JsonProperty("default_forum_layout")]
    public Layout DefaultForumLayout { get; internal set; }
    
    /// <summary>
    /// ID of the last thread that was created.
    /// </summary>
    [JsonProperty("last_message_id")]
    public ulong? LastThreadId { get; internal set; }

    #region CUSTOM

    /// <summary>
    /// Threads that belong to this channel.
    /// </summary>
    public IReadOnlyCollection<ThreadChannel> Threads => Guild.Threads.Where(t => t.ParentId == Id).ToList();

    #endregion
    
    internal ForumChannel() { }

    public override string ToString() => Name;
}

/// <summary>
/// Represents a <see cref="ForumChannel"/> or <see cref="MediaChannel"/> sort order.
/// </summary>
public enum SortOrder
{
    // DOCS: https://discord.com/developers/docs/resources/channel#channel-object-sort-order-types
    
    LatestActivity,
    CreationTime
}

/// <summary>
/// Represents a <see cref="ForumChannel"/> or <see cref="MediaChannel"/> layout.
/// </summary>
public enum Layout
{
    // DOCS: https://discord.com/developers/docs/resources/channel#channel-object-sort-order-types
    
    NotSet,
    ListView,
    GalleryView
}

/// <summary>
/// Represents a default reaction for a <see cref="ForumChannel"/> or <see cref="MediaChannel"/>.
/// </summary>
public record DefaultReactionEmoji
{
    // DOCS: https://discord.com/developers/docs/resources/channel#default-reaction-object
    
    /// <summary>
    /// ID of a guild's custom emoji.
    /// </summary>
    [JsonProperty("emoji_id")]
    public ulong? EmojiId { get; init; }
    
    /// <summary>
    /// Unicode character of the emoji.
    /// </summary>
    [JsonProperty("emoji_name")]
    public string EmojiName { get; init; } = string.Empty;
    
    private DefaultReactionEmoji() { }
}

/// <summary>
/// Represents a <see cref="ForumChannel"/> or <see cref="MediaChannel"/> tag.
/// </summary>
public record Tag
{
    // DOCS: https://discord.com/developers/docs/resources/channel#forum-tag-object
    
    /// <summary>
    /// ID of the tag.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; init; }
    
    /// <summary>
    /// Name of the tag (0-20 characters).
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether this tag can only be added to or removed from threads by a member with <see cref="Permission.ManageThreads"/>.
    /// </summary>
    [JsonProperty("moderated")]
    public bool IsModerated { get; init; }
    
    /// <inheritdoc cref="DefaultReactionEmoji.EmojiId"/>
    [JsonProperty("emoji_id")]
    public ulong? EmojiId { get; init; }
    
    /// <inheritdoc cref="DefaultReactionEmoji.EmojiName"/>
    [JsonProperty("emoji_name")]
    public string EmojiName { get; init; } = string.Empty;
    
    private Tag() { }
}

/// <summary>
/// Represents a <see cref="Guild"/> thread channel.
/// </summary>
public class ThreadChannel : GuildChannelMessageable
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
    public ThreadMetadata ThreadMetadata { get; internal set; }
    
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

    [JsonConstructor]
    internal ThreadChannel(ThreadMember? member)
    {
        // This member references the bot, think of it as a "SelfThreadMember".
        if (member is not null)
            _members.Add(member);
    }
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

/// <summary>
/// Represents a member that's in a <see cref="ThreadChannel"/>.
/// </summary>
public class ThreadMember
{
    // DOCS: https://discord.com/developers/docs/resources/channel#thread-member-object
    
    /// <summary>
    /// ID of the thread.
    /// </summary>
    [JsonProperty("id")]
    public ulong? Id { get; init; }
    
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
    
    /// <summary>
    /// The user's member object.
    /// </summary>
    [JsonProperty("member")]
    public Member? Member { get; init; }
    
    private ThreadMember() { }
}

/// <summary>
/// Represents a media channel for a <see cref="Guild"/>.
/// </summary>
public class MediaChannel : ForumChannel
{
    // DOCS: https://discord.com/developers/docs/resources/channel
    
    private MediaChannel() { }
}

/// <summary>
/// Represents a <see cref="IGuildChannel"/> category.
/// </summary>
public class CategoryChannel : IGuildChannel
{
    // DOCS: https://discord.com/developers/docs/resources/channel
    
    /// <summary>
    /// ID of the category.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; init; }

    /// <inheritdoc/>
    [JsonProperty("type")] 
    public ChannelType Type { get; init; }
    
    /// <inheritdoc/>
    public IReadOnlyCollection<PermissionOverwrites> Overwrites => PermissionOverwrites.Parse(_permissionOverwrites ?? []);
    [JsonProperty("permission_overwrites")] internal List<JSON>? _permissionOverwrites;
    
    /// <inheritdoc/>
    [JsonProperty("name")]
    public string Name { get; internal set; } = string.Empty;
    
    /// <inheritdoc/>
    [JsonProperty("guild_id")]
    public ulong GuildId { get; internal set; }
    
    /// <inheritdoc/>
    public Guild Guild { get; internal set; }
    
    /// <summary>
    /// Always <c>null</c> for this channel type.
    /// </summary>
    [JsonProperty("parent_id")]
    public ulong? ParentId { get; internal set; }

    /// <inheritdoc/>
    [JsonProperty("topic")]
    public string? Topic { get; internal set; }

    /// <inheritdoc/>
    public Bot Bot { get; internal set; }
    
    private CategoryChannel() { }
    
    public override string ToString() => Name;
}

/// <summary>
/// Represents a direct message channel.
/// </summary>
public class DmChannel : Messageable
{
    // DOCS: https://discord.com/developers/docs/resources/channel
    
    /// <summary>
    /// Channel type.
    /// </summary>
    public new ChannelType Type => ChannelType.Dm;
    
    /// <summary>
    /// User that is being interacted with.
    /// </summary>
    public User Recipient { get; }
    
    [JsonConstructor]
    internal DmChannel(ulong id, List<User> recipients)
    {
        Id = id;
        Recipient = recipients[0];
    }
}
