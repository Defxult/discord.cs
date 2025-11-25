using Discord.Channels.Models;
using Discord.Models;
using Discord.Utility;
using Newtonsoft.Json;

namespace Discord.Channels.Abstractions;

/// <summary>
/// Represents the values that can be edited for a <see cref="GuildChannel"/>. 
/// </summary>
public readonly struct GuildChannelEdit
{
    internal readonly JSON _payload = [];
    
    /// <summary>
    /// Initializes a guild channel edit instance.
    /// </summary>
    public GuildChannelEdit() { }

    /// <summary>
    /// Set the name of the channel.
    /// </summary>
    /// <param name="name">1-100 character channel name.</param>
    /// <returns>The edit instance.</returns>
    public GuildChannelEdit SetName(string name)
    {
        _payload["name"] = name;
        return this;
    }

    /// <summary>
    /// Set the channel type.
    /// </summary>
    /// <param name="type"> Only conversion between <see cref="ChannelType.GuildText"/> and <see cref="ChannelType.GuildAnnouncement"/>
    /// is supported and only in guilds with <see cref="GuildFeature.News"/>.
    /// </param>
    /// <returns>The edit instance.</returns>
    public GuildChannelEdit SetType(ChannelType type)
    {
        _payload["type"] = type;
        return this;
    }
    
    /// <summary>
    /// Set the channel position.
    /// </summary>
    /// <param name="position">The position of the channel in the left-hand listing.</param>
    /// <returns>The edit instance.</returns>
    public GuildChannelEdit SetPosition(int position)
    {
        _payload["position"] = position;
        return this;
    }
    
    /// <summary>
    /// Set the channel topic.
    /// </summary>
    /// <param name="topic">0-1024 character channel topic. 0-4096 characters for <see cref="ForumChannel"/> and
    /// <see cref="MediaChannel"/>.
    /// </param>
    /// <returns>The edit instance.</returns>
    /// <remarks>Only valid for <see cref="TextChannel"/>, <see cref="AnnouncementChannel"/>, <see cref="ForumChannel"/>,
    /// and <see cref="MediaChannel"/>.
    /// </remarks>
    public GuildChannelEdit SetTopic(string topic)
    {
        _payload["topic"] = topic;
        return this;
    }
    
    /// <summary>
    /// Set whether the channel is NSFW.
    /// </summary>
    /// <param name="nsfw">Whether the channel is NSFW.</param>
    /// <returns>The edit instance.</returns>
    /// <remarks>Only valid for <see cref="TextChannel"/>, <see cref="VoiceChannel"/>, <see cref="AnnouncementChannel"/>,
    /// <see cref="StageChannel"/>, <see cref="ForumChannel"/>, and <see cref="MediaChannel"/>.
    /// </remarks>
    public GuildChannelEdit SetNsfw(bool nsfw)
    {
        _payload["nsfw"] = nsfw;
        return this;
    }

    /// <summary>
    /// Set the slow mode seconds.
    /// </summary>
    /// <param name="seconds">Amount of seconds a user has to wait before sending another message (0-21600); bots, as well
    /// as users with <see cref="Permission.ManageMessages"/> or <see cref="Permission.ManageChannels"/>, are unaffected.</param>
    /// <returns>The edit instance.</returns>
    /// <remarks>Only valid for <see cref="TextChannel"/>, <see cref="VoiceChannel"/>, <see cref="StageChannel"/>,
    /// <see cref="ForumChannel"/>, and <see cref="MediaChannel"/>.
    /// </remarks>
    public GuildChannelEdit SetSlowModeSeconds(int? seconds)
    {
        _payload["rate_limit_per_user"] = seconds;
        return this;
    }
    
    /// <summary>
    /// Set the bitrate.
    /// </summary>
    /// <param name="bitrate">The bitrate (in bits); min 8000.</param>
    /// <returns>The edit instance.</returns>
    /// <remarks>Only valid for <see cref="VoiceChannel"/> and <see cref="StageChannel"/>.
    /// <list type="bullet">
    ///     <item>For voice channels, normal guilds can set bitrate up to 96000.</item>
    ///     <item>Guilds with Boost level 1 can set up to 128000.</item>
    ///     <item>Guilds with Boost level 2 can set up to 256000.</item>
    ///     <item>Guilds with Boost level 3 or <see cref="GuildFeature.VipRegions"/> can set up to 384000</item>
    ///     <item>For stage channels, bitrate can be set up to 64000.</item>
    /// </list>
    /// </remarks>
    public GuildChannelEdit SetBitrate(int bitrate)
    {
        _payload["bitrate"] = bitrate;
        return this;
    }
    
    /// <summary>
    /// Set the user limit.
    /// </summary>
    /// <param name="limit">The user limit of the <see cref="VoiceChannel"/> or <see cref="StageChannel"/>, max 99 for
    /// voice channels and 10,000 for stage channels (<c>null</c> refers to no limit)</param>
    /// <returns>The edit instance.</returns>
    public GuildChannelEdit SetUserLimit(int? limit)
    {
        _payload["user_limit"] = limit ?? 0;
        return this;
    }
    
    /// <summary>
    /// Set the channel overwrites.
    /// </summary>
    /// <param name="overwrites">Channel or category-specific permissions.</param>
    /// <returns>The edit instance.</returns>
    public GuildChannelEdit SetPermissionOverwrites(IEnumerable<PermissionOverwrites> overwrites)
    {
        var overwritesPayload = overwrites.Select(overwrite => overwrite.ToPayload()).ToList();
        _payload["permission_overwrites"] = overwritesPayload;
        return this;
    }
    
    /// <summary>
    /// Set the parent channel ID.
    /// </summary>
    /// <param name="id">ID of the new parent category for a channel.</param>
    /// <returns>The edit instance.</returns>
    /// <remarks>Only valid for <see cref="TextChannel"/>, <see cref="VoiceChannel"/>, <see cref="AnnouncementChannel"/>,
    /// <see cref="StageChannel"/>, <see cref="ForumChannel"/>, and <see cref="MediaChannel"/>.
    /// </remarks>
    public GuildChannelEdit SetParentChannelId(ulong? id)
    {
        _payload["parent_id"] = id;
        return this;
    }
    
    /// <summary>
    /// Set the voice region.
    /// </summary>
    /// <param name="location">Channel voice region.</param>
    /// <returns>The edit instance.</returns>
    public GuildChannelEdit SetVoiceRegionLocation(VoiceRegionLocation location)
    {
        _payload["rtc_region"] = location == VoiceRegionLocation.Automatic ? null : location.GetDescription();
        return this;
    }
    
    /// <summary>
    /// Set the quality mode.
    /// </summary>
    /// <param name="mode">The camera video quality mode of the voice channel.</param>
    /// <returns>The edit instance.</returns>
    /// <remarks>Only valid for <see cref="VoiceChannel"/> and <see cref="StageChannel"/>.</remarks>
    public GuildChannelEdit SetQualityMode(VideoQualityMode mode)
    {
        _payload["video_quality_mode"] = mode;
        return this;
    }
    
    /// <summary>
    /// Set the default auto archive duration.
    /// </summary>
    /// <param name="duration">	The default duration for newly created threads in the channel, in minutes, to automatically
    /// archive the thread after recent activity. Only valid options are:
    /// <list type="bullet">
    ///     <item>1 hour (60)</item>
    ///     <item>24 hours (1440)</item>
    ///     <item>3 days (4320)</item>
    ///     <item>1 week (10080)</item>
    /// </list>
    /// </param>
    /// <returns>The edit instance.</returns>
    /// <remarks>Only valid for <see cref="TextChannel"/>, <see cref="AnnouncementChannel"/>, <see cref="ForumChannel"/>,
    /// and <see cref="MediaChannel"/>.
    /// </remarks>
    public GuildChannelEdit SetDefaultAutoArchiveDuration(int duration)
    {
        _payload["default_auto_archive_duration"] = duration;
        return this;
    }
    
    /// <summary>
    /// Set the channels flags
    /// </summary>
    /// <param name="flags">Flags to set.</param>
    /// <returns>The edit instance.</returns>
    /// <remarks>Only valid for <see cref="ForumChannel"/> and <see cref="MediaChannel"/>.</remarks>
    public GuildChannelEdit SetFlags(IEnumerable<ChannelFlags> flags)
    {
        var value = flags.Sum(flag => (int)flag);
        Util.FromBitfield<ChannelFlags>(value);
        _payload["flags"] = value;
        return this;
    }
    
    /// <summary>
    /// Set the available tags.
    /// </summary>
    /// <param name="tags">Set of tags that can be used in a <see cref="ForumChannel"/> or <see cref="MediaChannel"/>;
    /// limited to 20.
    /// </param>
    /// <returns>The edit instance.</returns>
    public GuildChannelEdit SetAvailableTags(IEnumerable<Tag> tags)
    {
        _payload["available_tags"] = JsonConvert.DeserializeObject<List<Tag>>(JsonConvert.SerializeObject(tags));
        return this;
    }
    
    /// <summary>
    /// Set the default reaction emoji.
    /// </summary>
    /// <param name="emoji">Emoji to show in the add reaction button on a thread in a <see cref="ForumChannel"/> or
    /// <see cref="MediaChannel"/>.
    /// </param>
    /// <returns>The edit instance.</returns>
    public GuildChannelEdit SetDefaultReactionEmoji(DefaultReactionEmoji? emoji)
    {
        _payload["default_reaction_emoji"] =
            JsonConvert.DeserializeObject<DefaultReactionEmoji>(JsonConvert.SerializeObject(emoji));
        return this;
    }
    
    /// <summary>
    /// Set the delay for new threads.
    /// </summary>
    /// <param name="seconds">Initial delay to set on newly created threads in a channel. This field is copied to the thread
    /// at creation time and does not live update.
    /// </param>
    /// <returns>The edit instance.</returns>
    /// <remarks>Only valid for <see cref="TextChannel"/>, <see cref="ForumChannel"/>, and <see cref="MediaChannel"/>.</remarks>

    public GuildChannelEdit SetDefaultThreadSlowModeSeconds(int seconds)
    {
        _payload["default_thread_rate_limit_per_user"] = seconds;
        return this;
    }
    
    /// <summary>
    /// Set the sort order.
    /// </summary>
    /// <param name="sortOrder">The default sort order type used to order posts in a <see cref="ForumChannel"/>, and
    /// <see cref="MediaChannel"/> channels.
    /// </param>
    /// <returns>The edit instance.</returns>
    public GuildChannelEdit SetDefaultSortOrder(SortOrder sortOrder)
    {
        _payload["default_sort_order"] = sortOrder;
        return this;
    }
    
    /// <summary>
    /// Set the forum layout.
    /// </summary>
    /// <param name="layout">The default forum layout type used to display posts in a <see cref="ForumChannel"/>.
    /// </param>
    /// <returns>The edit instance.</returns>
    public GuildChannelEdit SetDefaultForumLayout(Layout layout)
    {
        _payload["default_forum_layout"] = layout;
        return this;
    }
}
