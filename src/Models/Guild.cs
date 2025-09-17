using System.ComponentModel;
using System.Data;
using System.Text.Json;
using Discord.Utility;
using Discord.Net;
using Newtonsoft.Json;

namespace Discord.Models;

/// <summary>
/// Represents a Discord server.
/// </summary>
public class Guild : IEquatable<Guild>
{
    #region Properties
    
    /// <summary>
    /// Guild ID.
    /// </summary>
    public ulong Id { get; }

    /// <summary>
    /// Guild name.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; internal set; } = string.Empty;

    /// <summary>
    /// Guild avatar.
    /// </summary>
    public Media? Icon
    {
        get
        {
            if (_icon is { } hash)
                return new Media(hash, $"/icons/{Id}/{hash}");
            return null;
        }
    }
    [JsonProperty("icon")] internal string? _icon;
    
    /// <summary>
    /// Guild splash.
    /// </summary>
    public Media? Splash
    {
        get
        {
            if (_splash is { } hash)
                return new Media(hash, $"/splashes/{Id}/{hash}");
            return null;
        }
    }
    [JsonProperty("splash")] internal string? _splash;
    
    /// <summary>
    /// Guild splash.
    /// </summary>
    public Media? DiscoverySplash
    {
        get
        {
            if (_discoverySplash is { } hash)
                return new Media(hash, $"/discovery-splashes/{Id}/{hash}");
            return null;
        }
    }
    [JsonProperty("discovery_splash")] internal string? _discoverySplash;
    
    /// <summary>
    /// The guild owners ID.
    /// </summary>
    [JsonProperty("owner_id")]
    public ulong OwnerId { get; internal set; }
    
    /// <summary>
    /// ID of the AFK channel.
    /// </summary>
    [JsonProperty("afk_channel_id")]
    public ulong? AfkChannelId { get; internal set; }
    
    /// <summary>
    /// AFk timeout in seconds.
    /// </summary>
    [JsonProperty("afk_timeout")]
    public int AfkTimeout { get; internal set; }
    
    /// <summary>
    /// If the guild widget is enabled.
    /// </summary>
    [JsonProperty("widget_enabled")]
    public bool WidgetEnabled { get; internal set; }
    
    /// <summary>
    /// The channel ID that the widget will generate an invite to, or <c>null</c> if set to no invite.
    /// </summary>
    [JsonProperty("widget_channel_id")]
    public ulong WidgetChannelId { get; internal set; }
    
    /// <summary>
    /// Verification level required for the guild.
    /// </summary>
    public GuildVerificationLevel VerificationLevel => (GuildVerificationLevel)_verificationLevel;
    [JsonProperty("verification_level")] internal int _verificationLevel;
    
    /// <summary>
    /// Default message notification level.
    /// </summary>
    public GuildMessageNotificationLevel DefaultMessageNotificationsLevel=> (GuildMessageNotificationLevel)_defaultMessageNotifications;
    [JsonProperty("default_message_notifications")] internal int _defaultMessageNotifications;
    
    /// <summary>
    /// Explicit content filter level.
    /// </summary>
    public GuildExplicitContentFilterLevel ExplicitContentFilterLevel => (GuildExplicitContentFilterLevel)_explicitContentFilter;
    [JsonProperty("explicit_content_filter")] internal int _explicitContentFilter;
    
    /// <summary>
    /// Roles in the guild.
    /// </summary>
    public IReadOnlyCollection<Role> Roles => _roles;
    internal List<Role> _roles;
    
    /// <summary>
    /// Custom guild emojis.
    /// </summary>
    public IReadOnlyCollection<Emoji> Emojis => _emojis;
    [JsonProperty("emojis")] internal List<Emoji> _emojis;
    
    /// <summary>
    /// Enabled guild features.
    /// </summary>
    public IReadOnlyCollection<GuildFeature> Features => ParseGuildFeatures(_features);
    [JsonProperty("features")] internal List<string> _features;
    
    /// <summary>
    /// Required MFA level for the guild.
    /// </summary>
    [JsonProperty("mfa_level")]
    public GuildMfaLevel MfaLevel { get; internal set; }
    
    /// <summary>
    /// ID of the channel where guild notices such as welcome messages and boost events are posted.
    /// </summary>
    [JsonProperty("system_channel_id")]
    public ulong? SystemChannelId { get; internal set; }

    /// <summary>
    /// System channel flags.
    /// </summary>
    public IReadOnlyCollection<GuildSystemChannelFlags> SystemChannelFlags => Util.FromBitfield<GuildSystemChannelFlags>(_systemChannelFlags);
    [JsonProperty("system_channel_flags")] internal int _systemChannelFlags;
    
    /// <summary>
    /// ID of the channel where Community guilds can display rules and/or guidelines.
    /// </summary>
    [JsonProperty("rules_channel_id")]
    public ulong? RulesChannelId { get; internal set; }
    
    /// <summary>
    /// Maximum number of presences for the guild (<c>null</c> is always returned, apart from the largest of guilds)
    /// </summary>
    [JsonProperty("max_presences")]
    public int? MaxPresences { get; internal set; }
    
    /// <summary>
    /// The maximum number of members for the guild.
    /// </summary>
    [JsonProperty("max_members")]
    public int? MaxMembers { get; internal set; }
    
    /// <summary>
    /// The vanity URL code for the guild.
    /// </summary>
    [JsonProperty("vanity_url_code")]
    public string? VanityUrlCode { get; internal set; }
    
    /// <summary>
    /// Description of the guild.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; internal set; }
    
    /// <summary>
    /// Guild banner.
    /// </summary>
    public Media? Banner
    {
        get
        {
            if (_banner is { } hash)
                return new Media(hash, $"/banners/{Id}/{hash}");
            return null;
        }
    }
    [JsonProperty("banner")] internal string? _banner;
    
    /// <summary>
    /// Server boost level.
    /// </summary>
    public GuildPremiumTier PremiumTier => (GuildPremiumTier)_premiumTier;
    [JsonProperty("premium_tier")]  internal int _premiumTier;
    
    /// <summary>
    /// Number of boosts the guild currently has.
    /// </summary>
    [JsonProperty("premium_subscription_count")]
    public int PremiumSubscriptionCount { get; internal set; }

    /// <summary>
    /// The preferred locale of a Community guild.
    /// </summary>
    public Locale Locale => ParseLocale(_locale);
    [JsonProperty("preferred_locale")]  internal string _locale;
    
    /// <summary>
    /// ID of the channel where admins and moderators of Community guilds receive notices from Discord.
    /// </summary>
    [JsonProperty("public_updates_channel_id")]
    public ulong? PublicUpdatesChannelId { get; internal set; }
    
    /// <summary>
    /// Maximum number of users in a video channel.
    /// </summary>
    [JsonProperty("max_video_channel_users")]
    public int? MaxVideoChannelUsers { get; internal set; }
    
    /// <summary>
    /// Maximum number of users in a video channel.
    /// </summary>
    [JsonProperty("max_stage_video_channel_users")]
    public int? MaxStageVideoChannelUsers { get; internal set; }
    
    /// <summary>
    /// The welcome screen of a Community guild, shown to new members.
    /// </summary>
    [JsonProperty("welcome_screen")]
    public WelcomeScreen? WelcomeScreen { get; internal set; }
    
    /// <summary>
    /// Guild NSFW level.
    /// </summary>
    [JsonProperty("nsfw_level")]
    public GuildNsfwLevel NsfwLevel { get; internal set; }
    
    /// <summary>
    /// Custom guild stickers.
    /// </summary>
    public IReadOnlyCollection<GuildSticker> Stickers => _stickers ?? [];
    [JsonProperty("stickers")] private List<GuildSticker>? _stickers;

    /// <summary>
    /// Whether the guild has the boost progress bar enabled.
    /// </summary>
    [JsonProperty("premium_progress_bar_enabled")]
    public bool PremiumProgressBarEnabled { get; internal set; }
    
    /// <summary>
    /// ID of the channel where admins and moderators of Community guilds receive safety alerts from Discord.
    /// </summary>
    [JsonProperty("safety_alerts_channel_id")]
    public ulong? SafetyAlertsChannelId { get; internal set; }
    
    /// <summary>
    /// Incidents data for the guild.
    /// </summary>
    [JsonProperty("incidents_data")]
    public Incidents? IncidentsData { get; }

    #endregion

    #region CUSTOM
    
    /// <summary>
    /// Your bot instance.
    /// </summary>
    public Bot? Bot {  get; internal set; } // Set in GUILD_CREATE
    
    #endregion
    
    [JsonConstructor]
    internal Guild(ulong id, List<Role> roles, bool _fromGateway = false)
    {
        Id = id;
        foreach (var role in roles)
            role.GuildId = id;
        _roles = roles;
    }
    
    public override bool Equals(object? other) => other is Guild guild && Equals(guild);
    public bool Equals(Guild? other) => Id == other?.Id;
    public override int GetHashCode() => Id.GetHashCode();
    
    #region SCHEDULED EVENT

    /// <summary>
    /// Retrieves a list of scheduled events for the guild.
    /// </summary>
    /// <returns>All scheduled events for the guild.</returns>
    public async Task<IReadOnlyCollection<ScheduledEvent>> ScheduledEventsAsync() =>
        await Bot!._rest.ListScheduledEventsForGuildAsync(Id);
    
    #endregion

    #region PRIVATE
    
    internal void Update(JsonElement element)
    {
        throw new NotImplementedException();
    }
    
    private static HashSet<GuildFeature> ParseGuildFeatures(ICollection<string> features)
    {
        HashSet<GuildFeature> fts = [];
        foreach (GuildFeature e in Enum.GetValues(typeof(GuildFeature)))
            foreach (var f in features)
                if (f.Equals(e.GetDescription()))
                    fts.Add(e);
        return fts;
    }

    private static Locale ParseLocale(string value)
    {
        var locale = Locale.EnglishUS; // Discord states this is the default locale for all guilds.
        foreach (Locale loc in Enum.GetValues(typeof(Locale)))
        {
            if (!value.Equals(loc.GetDescription())) continue;
            locale = loc;
            break;
        }
        return locale;
    }

    #endregion
}

/// <summary>
/// Represents the values that can be edited for a guild via <see cref="Guild.EditAsync(GuildEdit, string?)"/> 
/// </summary>
public struct GuildEdit
{
    internal JSON _payload = [];
    private HashSet<string> _features = [];

    /// <summary>
    /// Initializes a new guild edit instance.
    /// </summary>
    public GuildEdit() { }

    /// <summary>
    /// The new name for the guild.
    /// </summary>
    public readonly GuildEdit SetName(string name)
    {
        _payload["name"] = name;
        return this;
    }

    /// <summary>
    /// The new verification level for the guild.
    /// </summary>
    public readonly GuildEdit SetVerificationLevel(GuildVerificationLevel? verificationLevel)
    {
        _payload["verification_level"] = verificationLevel;
        return this;
    }

    /// <summary>
    /// The new default notification level for the guild.
    /// </summary>
    public readonly GuildEdit SetMessageNotificationLevel(GuildMessageNotificationLevel? notificationLevel)
    {
        _payload["default_message_notifications"] = notificationLevel;
        return this;
    }

    /// <summary>
    /// The new explicit content filter for the guild.
    /// </summary>
    public readonly GuildEdit SetExplicitContentFilterLevel(GuildExplicitContentFilterLevel? explicitContentFilterLevel)
    {
        _payload["explicit_content_filter"] = explicitContentFilterLevel;
        return this;
    }

    /// <summary>
    /// The new AFK channel. Can be set to <c>null</c> to disable AFK channels.
    /// </summary>
    public readonly GuildEdit SetAfkChannel(ulong? id)
    {
        _payload["afk_channel_id"] = id;
        return this;
    }

    /// <summary>
    /// Update the amount of time it takes for someone to be automatically moved to the AFK channel.
    /// </summary>
    /// <remarks>
    /// The only valid time intervals are:
    /// <list type="bullet">
    ///     <item>60 (1 minute)</item>
    ///     <item>300 (5 minutes)</item>
    ///     <item>900 (15 minutes)</item>
    ///     <item>1800 (30 minutes)</item>
    ///     <item>3600 (60 minutes)</item>
    /// </list>
    /// </remarks>
    public readonly GuildEdit SetAfkTimeout(int? value)
    {
        _payload["afk_timeout"] = value;
        return this;
    }

    /// <summary>
    /// The new guild icon. Can be animated if the guild has the <see cref="GuildFeature.AnimatedIcon"/> feature. Can be set to <c>null</c> to remove the icon.
    /// </summary>
    public readonly GuildEdit SetIcon(DFile? file)
    {
        _payload["icon"] = file?._mimeTypeBase64;
        return this;
    }

    /// <summary>
    /// Transfer guild ownership (bot must be the owner of the guild).
    /// </summary>
    public readonly GuildEdit SetOwner(ulong id)
    {
        _payload["owner_id"] = id;
        return this;
    }

    /// <summary>
    /// The new splash image. Guild must have the <see cref="GuildFeature.InviteSplash"/> feature.  Can be set to <c>null</c> to remove the guild splash image.
    /// </summary>
    public readonly GuildEdit SetSplash(DFile? file)
    {
        _payload["splash"] = file?._mimeTypeBase64;
        return this;
    }

    /// <summary>
    /// The new discovery splash image. Guild must have the <see cref="GuildFeature.Discoverable"/> feature. Can be set to <c>null</c> to remove the guild discovery splash image.
    /// </summary>
    public readonly GuildEdit SetDiscoverySplash(DFile? file)
    {
        _payload["discovery_splash"] = file?._mimeTypeBase64;
        return this;
    }

    /// <summary>
    /// The new banner image. Guild must have the <see cref="GuildFeature.Banner"/> feature. Can be animated if the guild has the <see cref="GuildFeature.AnimatedBanner"/> feature.
    /// Can be set to <c>null</c> to remove the guild banner.
    /// </summary>
    public readonly GuildEdit SetBanner(DFile? file)
    {
        _payload["banner"] = file?._mimeTypeBase64;
        return this;
    }

    /// <summary>
    /// The new channel where guild notices such as welcome messages and boost events are posted. Can be set to <c>null</c> to disable the system channel.
    /// </summary>
    public readonly GuildEdit SetSystemChannel(ulong? id)
    {
        _payload["system_channel_id"] = id;
        return this;
    }

    /// <summary>
    /// The new channel where admins and moderators of Community guilds receive notices from Discord. Only available for guilds with the <see cref="GuildFeature.Community"/> feature.
    /// </summary>
    public readonly GuildEdit SetPublicUpdatesChannel(ulong? id)
    {
        _payload["public_updates_channel_id"] = id;
        return this;
    }

    /// <summary>
    /// The new values for the guild system channel.
    /// </summary>
    public readonly GuildEdit SetSystemChannelFlags(IEnumerable<GuildSystemChannelFlags> flags)
    {
        var value = 0;
        foreach (var flag in flags)
            value |= (int)flag;
        _payload["system_channel_flags"] = value;
        return this;
    }

    /// <summary>
    /// The new preferred locale of a Community guild used in server discovery and notices from Discord.
    /// </summary>
    public readonly GuildEdit SetPreferredLocal(Locale? locale)
    {
        _payload["preferred_locale"] = locale?.GetDescription();
        return this;
    }

    /// <summary>
    /// Enable/disable Community Features in the guild. Both parameters are required to be set in order for it to be enabled.
    /// To disable, set both parameters to <c>null</c>.
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public readonly GuildEdit SetCommunityEnabled(ulong? rulesChannelId, ulong? publicUpdatesChannelId)
    {
        if (rulesChannelId == null && publicUpdatesChannelId == null)
        {
            _payload["rules_channel_id"] = null;
            _payload["public_updates_channel_id"] = null;
            _features.Remove(GuildFeature.Community.GetDescription());
        }
        else
        {
            if (rulesChannelId != null && publicUpdatesChannelId != null)
            {
                _payload["rules_channel_id"] = rulesChannelId;
                _payload["public_updates_channel_id"] = publicUpdatesChannelId;
                _features.Add(GuildFeature.Community.GetDescription());
            }
            else
            {
                throw new ArgumentException($"Both {nameof(rulesChannelId)} and {nameof(publicUpdatesChannelId)} must be set if enabling the community feature.");
            }
        }
        _payload["features"] = _features;
        return this;
    }

    /// <summary>
    /// Enable/disable discovery in the guild.
    /// </summary>
    public readonly GuildEdit SetDiscoveryEnabled(bool value)
    {
        if (value)
            _features.Add(GuildFeature.Discoverable.GetDescription());
        else
            _features.Remove(GuildFeature.Discoverable.GetDescription());
        _payload["features"] = _features;
        return this;
    }

    /// <summary>
    /// Pauses all invites/access to the guild.
    /// </summary>
    public readonly GuildEdit SetInvitesDisabled(bool value)
    {
        if (value)
            _features.Add(GuildFeature.InvitesDisabled.GetDescription());
        else
            _features.Remove(GuildFeature.InvitesDisabled.GetDescription());
        _payload["features"] = _features;
        return this;
    }

    /// <summary>
    /// Enable/disable alerts for join raids.
    /// </summary>
    public readonly GuildEdit SetRaidAlertsDisabled(bool value)
    {
        if (value)
            _features.Add(GuildFeature.RaidAlertsDisabled.GetDescription());
        else
            _features.Remove(GuildFeature.RaidAlertsDisabled.GetDescription());
        _payload["features"] = _features;
        return this;
    }

    /// <summary>
    /// The new description for the guild. Only available for guilds with the <see cref="GuildFeature.Community"/> feature.
    /// </summary>
    public readonly GuildEdit SetDescription(string? description)
    {
        _payload["description"] = description;
        return this;
    }

    /// <summary>
    /// Enable/disable the guild's boost progress bar.
    /// </summary>
    public readonly GuildEdit SetPremiumProgressBarEnabled(bool value)
    {
        _payload["premium_progress_bar_enabled"] = value;
        return this;
    }

    /// <summary>
    /// The channel where admins and moderators of Community guilds receive safety alerts from Discord. Can be set to <c>null</c> to disable the safety channel.
    /// </summary>
    public readonly GuildEdit SetSafetyAlertsChannel(ulong? id)
    {
        _payload["safety_alerts_channel_id"] = id;
        return this;
    }
}

// /// <summary>
// /// Represents a <see cref="Guild"/> preview.
// /// </summary>
// public class GuildPreview : IEquatable<GuildPreview>
// {
//     /// <summary>
//     /// Guild ID.
//     /// </summary>
//     [JsonProperty("id")]
//     public ulong Id { get; init; }
//     
//     /// <summary>
//     /// Guild name.
//     /// </summary>
//     [JsonProperty("name")]
//     public string Name { get; private set; } = string.Empty;
//
//     /// <summary>
//     /// Guild avatar.
//     /// </summary>
//     public Media? Icon { get; init; }
//
//     /// <summary>
//     /// Guild splash.
//     /// </summary>
//     public Media? Splash { get; init; }
//
//     /// <summary>
//     /// Guild discovery splash.
//     /// </summary>
//     public Media? DiscoverySplash { get; init; }
//
//     /// <summary>
//     /// Custom guild emojis.
//     /// </summary>
//     [JsonProperty("emojis")]
//     public HashSet<Emoji> Emojis { get; private set; } = [];
//
//     /// <summary>
//     /// Enabled guild _features.
//     /// </summary>
//     public HashSet<GuildFeature> Features { get; private set; } = [];
//
//     /// <summary>
//     /// Approximate number of members in this guild.
//     /// </summary>
//     [JsonProperty("approximate_member_count")]
//     public int ApproximateMemberCount { get; private set; }
//
//     /// <summary>
//     /// Approximate number of non-offline members in this guild.
//     /// </summary>
//     [JsonProperty("approximate_presence_count")]
//     public int ApproximatePresenceCount { get; private set; }
//
//     /// <summary>
//     /// The description of a guild.
//     /// </summary>
//     [JsonProperty("description")]
//     public string? Description { get; private set; }
//
//     /// <summary>
//     /// Custom guild stickers.
//     /// </summary>
//     [JsonProperty("stickers")]
//     public List<GuildSticker> Stickers { get; private set; } = [];
//
//     [JsonConstructor]
//     internal GuildPreview(ulong id, HashSet<string> features, string? icon, string? splash, string? discovery_splash)
//     {
//         Features = Guild.ParseFeatures(features);
//         if (icon != null)
//             Icon = new Media(icon, $"/icons/{id}/{icon}");
//         if (splash != null)
//             Splash = new Media(splash, $"/splashes/{id}/{splash}");
//         if (discovery_splash != null)
//             DiscoverySplash = new Media(discovery_splash, $"/discovery-splashes/{id}/{discovery_splash}");
//     }
//     
//     public override bool Equals(object? other) => other is GuildPreview preview && Equals(preview);
//     public bool Equals(GuildPreview? other) => Id == other?.Id;
//     public override int GetHashCode() => Id.GetHashCode();
// }

/// <summary>
/// Represents a guilds scheduled event.
/// </summary>
public class ScheduledEvent : IEquatable<ScheduledEvent>
{
    // DOCS: https://discord.com/developers/docs/resources/guild-scheduled-event
    
    /// <summary>
    /// Scheduled event ID.
    /// </summary>
    public ulong Id { get; }
    
    /// <summary>
    /// Guild ID the event belongs to.
    /// </summary>        
    [JsonProperty("guild_id")]
    public ulong GuildId { get; init; }

    /// <summary>
    /// The channel ID in which the scheduled event will be hosted, or <c>null</c> if the scheduled
    /// events entity type is <see cref="ScheduledEventEntityType.External"/>.
    /// </summary>
    [JsonProperty("channel_id")]
    public ulong? ChannelId { get; init; }
    
    /// <summary>
    /// ID of the user that created the scheduled event.
    /// </summary>        
    [JsonProperty("creator_id")]
    public ulong? CreatorId { get; init; }

    /// <summary>
    /// Name of the scheduled event.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; init; } = string.Empty; 

    /// <summary>
    /// Description of the scheduled event.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Time the scheduled event will start.
    /// </summary>
    [JsonProperty("scheduled_start_time")]
    public DateTime ScheduledStartTime { get; init; }

    /// <summary>
    /// Time the scheduled event will end.
    /// </summary>
    [JsonProperty("scheduled_end_time")]
    public DateTime? ScheduledEndTime { get; init; }

    /// <summary>
    /// Privacy level of the scheduled event.
    /// </summary>        
    [JsonProperty("privacy_level")]
    public ScheduledEventPrivacyLevel PrivacyLevel { get; init; }

    /// <summary>
    /// Status of the scheduled event.
    /// </summary>
    [JsonProperty("status")]
    public ScheduledEventStatus Status { get; init; }

    /// <summary>
    /// The type of the scheduled event.
    /// </summary>        
    [JsonProperty("entity_type")]
    public ScheduledEventEntityType EntityType { get; init; }

    /// <summary>
    /// ID of an entity associated with a guild scheduled event.
    /// </summary>        
    [JsonProperty("entity_id")]
    public ulong? EntityId { get; init; }
    
    /// <summary>
    /// User that created the scheduled event.
    /// </summary>
    [JsonProperty("creator")]
    public User? Creator { get; init; }

    /// <summary>
    /// Where the event will take place.
    /// </summary>        
    public string? Location { get; }

    /// <summary>
    /// Cover image of the scheduled event.
    /// </summary>        
    public Media? Image { get; }
    
    /// <summary>
    /// Number of users subscribed to the scheduled event
    /// </summary>
    [JsonProperty("user_count")]
    public int UserCount { get; init; }
    
    /// <summary>
    /// The definition for how often this event should recur.
    /// </summary>
    [JsonProperty("recurrence_rule")]
    public RecurrenceRule? RecurrenceRule { get; init; }
    
    #region CUSTOM

    /// <summary>
    /// Your bot instance.
    /// </summary>
    public Bot? Bot { get; internal set; } // Set via Rest.ListScheduledEventsForGuildAsync().
    
    /// <summary>
    /// Guild the event belongs to.
    /// </summary>
    public Guild? Guild => Bot?.GetGuild(GuildId);
    
    #endregion

    [JsonConstructor]
    internal ScheduledEvent(ulong id, JSON? entity_metadata, string? image)
    {
        Id = id;
        if (entity_metadata != null)
        {
            entity_metadata.TryGetValue("location", out object? location);
            if (location != null)
                Location = Convert.ToString(location);
        }
        if (image != null)
            Image = new Media(image, $"/guild-events/{Id}/{image}");
    }
    
    public override bool Equals(object? other) => other is ScheduledEvent scheduledEvent && Equals(scheduledEvent);
    public bool Equals(ScheduledEvent? other) => Id == other?.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

/// <summary>
/// Represents a <see cref="Guild"/>'s <see cref="ScheduledEvent"/> recurrence rule.
/// </summary>
public record RecurrenceRule
{
    /// <summary>
    /// Starting time of the recurrence interval.
    /// </summary>
    [JsonProperty("start")]
    public DateTime Start { get; init; }
    
    /// <summary>
    /// Ending time of the recurrence interval.
    /// </summary>
    [JsonProperty("end")]
    public DateTime? End { get; init; }
    
    /// <summary>
    /// How often the event occurs.
    /// </summary>
    [JsonProperty("frequency")]
    public RecurrenceRuleFrequency Frequency { get; init; }
    
    /// <summary>
    /// The spacing between the events, defined by <see cref="Frequency"/>. For example, frequency of
    /// <see cref="RecurrenceRuleFrequency.Weekly"/> and an interval of 2 would be "every-other week".
    /// </summary>
    [JsonProperty("interval")]
    public int Interval { get; init; }
    
    /// <summary>
    /// Set of specific days within a week for the event to recur on.
    /// </summary>
    [JsonProperty("by_weekday")]
    public IReadOnlyCollection<RecurrenceRuleFrequencyWeekday>? ByWeekday { get; init; }
    
    /// <summary>
    /// List of specific days within a specific week (1-5) to recur on.
    /// </summary>
    [JsonProperty("by_n_weekday")]
    public IReadOnlyCollection<RecurrenceRuleFrequencyNWeekday>? ByNWeekday { get; init; }
    
    /// <summary>
    /// Set of specific months to recur on.
    /// </summary>
    [JsonProperty("by_month")]
    public IReadOnlyCollection<RecurrenceRuleFrequencyMonth>? ByMonth { get; init; }
    
    /// <summary>
    /// Set of specific dates within a month to recur on.
    /// </summary>
    [JsonProperty("by_month_day")]
    public IReadOnlyCollection<int>? ByMonthDay { get; init; }
    
    /// <summary>
    /// Set of days within a year to recur on (1-364).
    /// </summary>
    [JsonProperty("by_year_day")]
    public IReadOnlyCollection<int>? ByYearDay { get; init; }
    
    /// <summary>
    /// The total amount of times that the event is allowed to recur before stopping.
    /// </summary>
    [JsonProperty("count")]
    public int? Count { get; init; }
    
}

public enum RecurrenceRuleFrequency
{
    Yearly,
    Monthly,
    Weekly,
    Daily
}

public enum RecurrenceRuleFrequencyWeekday
{
    Monday,
    Tuesday,
    Wednesday,
    Thursday,
    Friday,
    Saturday,
    Sunday
}

public enum RecurrenceRuleFrequencyMonth
{
    Jan = 1,
    Feb,
    Mar,
    Apr,
    May,
    Jun,
    Jul,
    Aug,
    Sep,
    Oct,
    Nov,
    Dec
}

public record RecurrenceRuleFrequencyNWeekday
{
    /// <summary>
    /// The week to reoccur on (1 - 5).
    /// </summary>
    [JsonProperty("n")]
    public int Week { get; init; }
    
    /// <summary>
    /// The day within the week to reoccur on.
    /// </summary>
    [JsonProperty("day")]
    public RecurrenceRuleFrequencyWeekday Day { get; init; }
}

/// <summary>
/// Represents incidents that occurred in a <see cref="Guild"/>.
/// </summary>
public record Incidents
{
    /// <summary>
    /// When invites get enabled again.
    /// </summary>
    [JsonProperty("invites_disabled_until")]
    public DateTime? InvitesDisabledUntil { get; init; }
    
    /// <summary>
    /// When direct messages get enabled again.
    /// </summary>
    [JsonProperty("dms_disabled_until")]
    public DateTime? DmDisabledUntil { get; init; }
    
    /// <summary>
    /// When the direct message spam was detected.
    /// </summary>
    [JsonProperty("dm_spam_detected_at")]
    public DateTime? DmSpamDetectedAt{ get; init; }
    
    /// <summary>
    /// When the raid was detected.
    /// </summary>
    [JsonProperty("raid_detected_at")]
    public DateTime? RaidDetectedAt{ get; init; }
}

/// <summary>
/// Represents a <see cref="Guild"/> welcome screen.
/// </summary>
public record WelcomeScreen
{
    /// <summary>
    /// Guild description shown in the welcome screen.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; init; }
    
    /// <summary>
    /// The channels shown in the welcome screen.
    /// </summary>
    [JsonProperty("welcome_channels")]
    public IReadOnlyCollection<WelcomeScreenChannel> Channels { get; init; }
}

/// <summary>
/// Represents a <see cref="WelcomeScreen"/> channel.
/// </summary>
public record WelcomeScreenChannel
{
    /// <summary>
    /// Channel ID.
    /// </summary>
    [JsonProperty("channel_id")]
    public ulong ChannelId { get; init; }

    /// <summary>
    /// Channel description.
    /// </summary>
    [JsonProperty("description")]
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Emoji ID, if the emoji is custom.
    /// </summary>
    [JsonProperty("emoji_id")]
    public ulong? EmojiId { get; init; }
    
    /// <summary>
    /// Emoji name if custom, the Unicode character if it's standard.
    /// </summary>
    [JsonProperty("emoji_name")]
    public string? EmojiName { get; init; }
}


/// <summary>
/// Represents the status of a <see cref="ScheduledEvent"/>.
/// </summary>    
public enum ScheduledEventStatus
{
    Scheduled = 1,
    Active,
    Completed,
    Canceled
}

/// <summary>
/// Represents the entity type of a <see cref="ScheduledEvent"/>.
/// </summary>    
public enum ScheduledEventEntityType
{
    StageInstance = 1,
    Voice,
    External
}

/// <summary>
/// Represents the privacy level of a <see cref="ScheduledEvent"/>.
/// </summary>
public enum ScheduledEventPrivacyLevel
{
    GuildOnly = 2
}

/// <summary>
/// Represents the verification level of a <see cref="Guild"/>.
/// </summary>
public enum GuildVerificationLevel
{
    /// <summary>
    /// Unrestricted.
    /// </summary>
    None,

    /// <summary>
    /// Must have verified email on account.
    /// </summary>
    Low,

    /// <summary>
    /// Must be registered on Discord for longer than 5 minutes.
    /// </summary>
    Medium,

    /// <summary>
    /// Must be a member of the server for longer than 10 minutes.
    /// </summary>
    High,

    /// <summary>
    /// Must have a verified phone number.
    /// </summary>
    VeryHigh
}

/// <summary>
/// Represents a <see cref="Guild"/>s message notification level.
/// </summary>
public enum GuildMessageNotificationLevel
{
    /// <summary>
    /// Members will receive notifications for all messages by default.
    /// </summary>
    AllMessages,

    /// <summary>
    /// Members will receive notifications only for messages that @mention them by default.
    /// </summary>
    OnlyMentions
}

/// <summary>
/// Represents a guilds explicit content filter level.
/// </summary>
public enum GuildExplicitContentFilterLevel
{
    /// <summary>
    /// Media content will not be scanned.
    /// </summary>
    Disabled,

    /// <summary>
    /// Media content sent by members without roles will be scanned.
    /// </summary>
    MembersWithoutRoles,

    /// <summary>
    /// Media content sent by all members will be scanned.
    /// </summary>
    AllMembers
}

/// <summary>
/// Represents a guilds MFA/2FA level.
/// </summary>
public enum GuildMfaLevel
{
    /// <summary>
    /// Guild has no MFA/2FA requirement for moderation actions.
    /// </summary>
    None,

    /// <summary>
    /// Guild has a 2FA requirement for moderation actions.
    /// </summary>
    Elevated
}

/// <summary>
/// Represents the NSFW level of a <see cref="Guild"/>.
/// </summary>
public enum GuildNsfwLevel
{
    Default,
    Explicit,
    Safe,
    AgeRestricted
}

/// <summary>
/// Represents the premium tier of a <see cref="Guild"/>.
/// </summary>
public enum GuildPremiumTier
{
    /// <summary>
    /// Guild has not unlocked any Server Boost perks.
    /// </summary>
    None,

    /// <summary>
    /// Guild has unlocked Server Boost level 1 perks.
    /// </summary>
    Tier1,

    /// <summary>
    /// Guild has unlocked Server Boost level 2 perks.
    /// </summary>
    Tier2,

    /// <summary>
    /// Guild has unlocked Server Boost level 3 perks.
    /// </summary>
    Tier3
}

/// <summary>
/// Represents the system channels flag of a <see cref="Guild"/>.
/// </summary>
public enum GuildSystemChannelFlags
{
    /// <summary>
    /// Suppress member join notifications.
    /// </summary>
    SuppressJoinNotifications = 1 << 0,

    /// <summary>
    /// Suppress server boost notifications.
    /// </summary>
    SuppressPremiumSubscriptions = 1 << 1,

    /// <summary>
    /// Suppress server setup tips.
    /// </summary>
    SuppressGuildReminderNotifications = 1 << 2,

    /// <summary>
    /// Hide member join sticker reply buttons.
    /// </summary>
    SuppressJoinNotificationReplies = 1 << 3,

    /// <summary>
    /// Suppress role subscription purchase and renewal notifications.
    /// </summary>
    SuppressRoleSubscriptionPurchaseNotifications = 1 << 4,

    /// <summary>
    /// Hide role subscription sticker reply buttons.
    /// </summary>
    SuppressRoleSubscriptionPurchaseNotificationReplies = 1 << 5
}

/// <summary>
/// Represents a <see cref="Guild"/> feature.
/// </summary>
public enum GuildFeature
{
    /// <summary>
    /// Guild has access to set an animated guild banner image.
    /// </summary>
    [Description("ANIMATED_BANNER")]
    AnimatedBanner,

    /// <summary>
    /// Guild has access to set an animated guild icon.
    /// </summary>
    [Description("ANIMATED_ICON")]
    AnimatedIcon,

    /// <summary>
    /// Guild is using the old permissions configuration behavior.
    /// </summary>
    [Description("APPLICATION_COMMAND_PERMISSIONS_V2")]
    ApplicationCommandPermissionsV2,

    /// <summary>
    /// Guild has set up auto moderation rules.
    /// </summary>
    [Description("AUTO_MODERATION")]
    AutoModeration,

    /// <summary>
    /// Guild has access to set a guild banner image.
    /// </summary>
    [Description("BANNER")]
    Banner,

    /// <summary>
    /// Guild can enable welcome screen, Membership Screening, stage channels and discovery, and receives community updates.
    /// </summary>
    [Description("COMMUNITY")]
    Community,

    /// <summary>
    /// Guild has enabled monetization.
    /// </summary>
    [Description("CREATOR_MONETIZABLE_PROVISIONAL")]
    CreatorMonetizableProvisional,

    /// <summary>
    /// Guild has enabled the role subscription promo page.
    /// </summary>
    [Description("CREATOR_STORE_PAGE")]
    CreatorStorePage,

    /// <summary>
    /// Guild has been set as a support server on the App Directory.
    /// </summary>
    [Description("DEVELOPER_SUPPORT_SERVER")]
    DeveloperSupportServer,

    /// <summary>
    /// Guild is able to be discovered in the directory.
    /// </summary>
    [Description("DISCOVERABLE")]
    Discoverable,

    /// <summary>
    /// Guild is able to be featured in the directory.
    /// </summary>
    [Description("FEATURABLE")]
    Featurable,

    /// <summary>
    /// Guild has paused invites, preventing new users from joining.
    /// </summary>
    [Description("INVITES_DISABLED")]
    InvitesDisabled,

    /// <summary>
    /// Guild has access to set an invite splash background.
    /// </summary>
    [Description("INVITE_SPLASH")]
    InviteSplash,

    /// <summary>
    /// Guild has enabled Membership Screening.
    /// </summary>
    [Description("MEMBER_VERIFICATION_GATE_ENABLED")]
    MemberVerificationGateEnabled,

    /// <summary>
    /// Guild has increased custom sticker slots.
    /// </summary>
    [Description("MORE_STICKERS")]
    MoreStickers,

    /// <summary>
    /// Guild has access to create news channels.
    /// </summary>
    [Description("NEWS")]
    News,

    /// <summary>
    /// Guild is partnered.
    /// </summary>
    [Description("PARTNERED")]
    Partnered,

    /// <summary>
    /// Guild can be previewed before joining via Membership Screening or the directory.
    /// </summary>
    [Description("PREVIEW_ENABLED")]
    PreviewEnabled,

    /// <summary>
    /// Guild has disabled alerts for join raids in the configured safety alerts channel.
    /// </summary>
    [Description("RAID_ALERTS_DISABLED")]
    RaidAlertsDisabled,

    /// <summary>
    /// Guild is able to set role icons.
    /// </summary>
    [Description("ROLE_ICONS")]
    RoleIcons,

    /// <summary>
    /// Guild has role subscriptions that can be purchased.
    /// </summary>
    [Description("ROLE_SUBSCRIPTIONS_AVAILABLE_FOR_PURCHASE")]
    RoleSubscriptionsAvailableForPurchase,

    /// <summary>
    /// Guild has enabled role subscriptions.
    /// </summary>
    [Description("ROLE_SUBSCRIPTIONS_ENABLED")]
    RoleSubscriptionsEnabled,

    /// <summary>
    /// Guild has enabled ticketed events.
    /// </summary>
    [Description("TICKETED_EVENTS_ENABLED")]
    TicketedEventsEnabled,

    /// <summary>
    /// Guild has access to set a vanity URL.
    /// </summary>
    [Description("VANITY_URL")]
    VanityUrl,

    /// <summary>
    /// Guild is verified.
    /// </summary>
    [Description("VERIFIED")]
    Verified,

    /// <summary>
    /// Guild has access to set 384kbps bitrate in voice (previously VIP voice servers).
    /// </summary>
    [Description("VIP_REGIONS")]
    VipRegions,

    /// <summary>
    /// Guild has enabled the welcome screen.
    /// </summary>
    [Description("WELCOME_SCREEN_ENABLED")]
    WelcomeScreenEnabled
}