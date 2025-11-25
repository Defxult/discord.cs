using Discord.Channels.Abstractions;
using Discord.Channels.Services;
using Discord.Models;
using Discord.Utility;
using Newtonsoft.Json;

namespace Discord.Channels.Models;

/// <summary>
/// Represents a <see cref="GuildChannel"/> that can only contain <see cref="ThreadChannel"/>.
/// </summary>
public class ForumChannel : GuildChannel, IThreadable, IInvitable, IPermissionEditable
{
    /// <inheritdoc/>
    [JsonProperty("default_auto_archive_duration")]
    public ThreadArchiveDuration? DefaultAutoArchiveDuration { get; internal set; }
    
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
    /// ID of the last <b>thread</b> that was created.
    /// </summary>
    [JsonProperty("last_message_id")]
    public new ulong? LastMessageId { get; internal set; }

    #region CUSTOM

    /// <summary>
    /// Threads that belong to this channel.
    /// </summary>
    public IReadOnlyCollection<ThreadChannel> Threads => Guild.Threads.Where(t => t.ParentId == Id).ToList();

    #endregion
    
    internal ForumChannel() { }
    
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Invite>> InvitesAsync() =>
        await ChannelServicer.InvitesAsync(this);

    /// <inheritdoc/>
    public async Task<Invite> CreateInviteAsync(int? maxAge = 86400, int? maxUses = null, bool temporary = false,
        bool unique = false,
        InviteTargetType? targetType = null, ulong? targetUserId = null, ulong? targetApplicationId = null,
        string? reason = null) =>
        await ChannelServicer.CreateInviteAsync(this, maxAge, maxUses, temporary, unique, targetType, targetUserId,
            targetApplicationId, reason);
    
    /// <inheritdoc/>
    public async Task EditPermissionsAsync(PermissionOverwrites overwrites, string? reason = null) =>
        await ChannelServicer.EditChannelPermissions(this, overwrites, reason);

    /// <inheritdoc/>
    public async Task DeletePermissionsAsync(ulong id, string? reason = null) =>
        await ChannelServicer.DeletePermissions(this, id, reason);
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
    public ulong? EmojiId { get; set; }
    
    /// <summary>
    /// Unicode character of the emoji.
    /// </summary>
    [JsonProperty("emoji_name")]
    public string? EmojiName { get; set; }

    /// <summary>
    /// Initialize a default reaction emoji. Exactly one parameter must be set a non-null value.
    /// </summary>
    /// <param name="id">ID of a guild's custom emoji.</param>
    /// <param name="name">Unicode character of the emoji.</param>
    public DefaultReactionEmoji(ulong? id, string? name)
    {
        EmojiId = id;
        EmojiName = name;
    }
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
