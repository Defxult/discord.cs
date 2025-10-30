using System.ComponentModel;
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
    #region PROPERTIES
    
    /// <summary>
    /// Guild ID.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; init; }

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
    /// Channel ID the widget will generate an invitation to, or <c>null</c> if set to no invite.
    /// </summary>
    [JsonProperty("widget_channel_id")]
    public ulong? WidgetChannelId { get; internal set; }
    
    /// <summary>
    /// Verification level required for the guild.
    /// </summary>
    [JsonProperty("verification_level")]
    public GuildVerificationLevel VerificationLevel { get; internal set; }
    
    /// <summary>
    /// Default message notification level.
    /// </summary>
    [JsonProperty("default_message_notifications")]
    public GuildMessageNotificationLevel DefaultMessageNotificationsLevel {  get; internal set; }
    
    /// <summary>
    /// Explicit content filter level.
    /// </summary>
    [JsonProperty("explicit_content_filter")]
    public GuildExplicitContentFilterLevel ExplicitContentFilterLevel { get; internal set; }
    
    /// <summary>
    /// Roles in the guild.
    /// </summary>
    public IReadOnlyList<Role> Roles => _roles;
    [JsonProperty("roles")] internal List<Role> _roles = [];
    
    /// <summary>
    /// Custom guild emojis.
    /// </summary>
    public IReadOnlySet<Emoji> Emojis => _emojis.ToHashSet();
    [JsonProperty("emojis")] internal List<Emoji> _emojis = [];
    
    /// <summary>
    /// Enabled guild features.
    /// </summary>
    public IReadOnlyCollection<GuildFeature> Features => ParseFeatures(_features);
    [JsonProperty("features")] internal List<string> _features = [];
    
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
    [JsonProperty("premium_tier")] internal int _premiumTier;
    
    /// <summary>
    /// Number of boosts the guild currently has.
    /// </summary>
    [JsonProperty("premium_subscription_count")]
    public int PremiumSubscriptionCount { get; internal set; }

    /// <summary>
    /// The preferred locale of a Community guild.
    /// </summary>
    public Locale Locale => ParseLocale(_locale);
    [JsonProperty("preferred_locale")] internal string _locale = string.Empty;
    
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
    public Incidents? IncidentsData { get; internal set; }

    #endregion

    #region GUILD CREATE EXTRAS
    
    /// <summary>
    /// When this guild was joined at. Will be <c>null</c> if accessed via <see cref="Bot.RequestGuildAsync"/>.
    /// </summary>
    [JsonProperty("joined_at")]
    public DateTime? JoinedAt { get; init; }

    /// <summary>
    /// <c>true</c> if this is considered a large guild. Discord.cs considers guilds with 250+ members to be large.
    /// </summary>
    /// <remarks>This depends on <see cref="MemberCount"/> to be accurate.</remarks>
    public bool IsLarge => MemberCount >= 250;
    
    /// <summary>
    /// <c>true</c> if this guild is unavailable due to an outage.
    /// </summary>
    [JsonProperty("unavailable")]
    public bool IsUnavailable { get; internal set; }

    /// <summary>
    /// Total number of members in this guild. Requires <see cref="Intents.GuildMembers"/> to be accurate.
    /// </summary>
    [JsonProperty("member_count")]
    public int MemberCount { get; internal set; }
    
    /// <summary>
    /// States of members currently in voice channels.
    /// </summary>
    public IReadOnlyCollection<VoiceState> VoiceStates => _voiceStates;
    [JsonProperty("voice_states")] internal List<VoiceState> _voiceStates = [];

    /// <summary>
    /// Users in the guild.
    /// </summary>
    [JsonIgnore] public IReadOnlySet<Member> Members => _members;
    internal readonly HashSet<Member> _members = [];

    #endregion
    
    #region CUSTOM
    
    /// <summary>
    /// Your bot instance.
    /// </summary>
    public Bot Bot { get; internal set; }
    
    /// <summary>
    /// Your bots member object for this guild.
    /// </summary>
    public Member Self { get; internal set; }
    
    /// <summary>
    /// When the guild was last chunked, or <c>null</c> if never. This refers to when the chunk was initiated, not when
    /// it was completed.
    /// </summary>
    public DateTime? LastChunked { get; private set; }
    
    #endregion
    
    private Guild() { }
    
    public override bool Equals(object? other) => other is Guild guild && Equals(guild);
    public bool Equals(Guild? other) => Id == other?.Id;
    public override int GetHashCode() => Id.GetHashCode();
    
    #region PUBLIC

    // TODO
    // <remarks>Requires <see cref="Permission.ManageGuild"/>.</remarks>
    public async Task EditAsync(GuildEdit edit)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Requests all emojis in the guild.
    /// </summary>
    /// <returns>All emojis in the guild.</returns>
    /// <remarks>It's generally preferred to use <see cref="Emojis"/> unless this is needed.</remarks>
    public async Task<IReadOnlyCollection<Emoji>> RequestEmojisAsync() =>
        await Bot._rest.ListGuildEmojisAsync(Id);

    /// <summary>
    /// Requests an emoji in the guild.
    /// </summary>
    /// <returns>The requested emoji.</returns>
    /// <remarks>Unlike <see cref="GetEmoji"/> this is an API call.</remarks>
    public async Task<Emoji> RequestEmojiAsync(ulong id) =>
        await Bot._rest.GetGuildEmojiAsync(Id, id);
    
    /// <summary>
    /// Creates a new emoji for the guild.
    /// </summary>
    /// <param name="name">Emoji name.</param>
    /// <param name="image">The 128x128 emoji image.</param>
    /// <param name="roles">Roles allowed to use this emoji.</param>
    /// <param name="reason">Reason for creating the emoji. This is displayed in the audit-log.</param>
    /// <returns>The newly created emoji.</returns>
    public async Task<Emoji> CreateEmojiAsync(string name, DFile image, IReadOnlyCollection<Role>? roles = null, string? reason = null) =>
        await Bot._rest.CreateGuildEmojiAsync(Id, name, image, roles ?? [], reason);

    /// <summary>
    /// Access an emoji from the cache.
    /// </summary>
    /// <param name="id">ID of the emoji.</param>
    /// <returns>The emoji with the given ID or <c>null</c> if not found.</returns>
    /// <remarks>This method is generally preferred compared to <see cref="RequestEmojiAsync"/>.</remarks>
    public Emoji? GetEmoji(ulong id) =>
        Emojis.FirstOrDefault(e => e.Id == id);

    /// <summary>
    /// Create a scheduled event.
    /// </summary>
    /// <param name="name">Name of the scheduled event.</param>
    /// <param name="startTime">The time to schedule the scheduled event.</param>
    /// <param name="entityType">Entity type of the scheduled event.</param>
    /// <param name="endTime">The time when the scheduled event is scheduled to end. Required for events of entity type <see cref="ScheduledEventEntityType.External"/>.</param>
    /// <param name="channelId">Channel ID of the scheduled event. Optional for events of entity type <see cref="ScheduledEventEntityType.External"/>.</param>
    /// <param name="location">Location of the event (1-100 characters). Optional for events of entity type <see cref="ScheduledEventEntityType.External"/>.</param>
    /// <param name="description">Description of the scheduled event.</param>
    /// <param name="image">Cover image of the scheduled event.</param>
    /// <param name="recurrence">The definition for how often this event should recur.</param>
    /// <param name="reason">Reason for creating the scheduled event. This is displayed in the audit-log.</param>
    /// <returns>The scheduled event that was created.</returns>
    public async Task<ScheduledEvent> CreateScheduledEventAsync(
        string name,
        DateTime startTime,
        ScheduledEventEntityType entityType,
        DateTime? endTime = null,
        ulong? channelId = null,
        string? location = null,
        string? description = null,
        DFile? image = null,
        RecurrenceRule? recurrence = null,
        string? reason = null
    ) => await Bot._rest.CreateGuildScheduledEventAsync(
        Id, name, startTime, endTime, channelId, location, description, entityType, image, recurrence, reason);

    /// <summary>
    /// Requests all scheduled events for the guild.
    /// </summary>
    /// <returns>All scheduled events for the guild.</returns>
    public async Task<IReadOnlyCollection<ScheduledEvent>> RequestScheduledEventsAsync() =>
        await Bot._rest.ListScheduledEventsForGuildAsync(Id);

    /// <summary>
    /// Requests a scheduled event.
    /// </summary>
    /// <param name="id">ID of the scheduled event.</param>
    /// <returns>The requested scheduled event.</returns>
    public async Task<ScheduledEvent> RequestScheduledEventAsync(ulong id) =>
        await Bot._rest.GetGuildScheduledEventAsync(Id, id);

    /// <summary>
    /// Creates a guild sticker.
    /// </summary>
    /// <param name="name">Name of the sticker.</param>
    /// <param name="description">Description of the sticker.</param>
    /// <param name="emoji">Emoji that's related to the sticker.</param>
    /// <param name="file">File data for the sticker.</param>
    /// <param name="reason">Reason for creating the sticker. This is displayed in the audit-log.</param>
    public async Task CreateStickerAsync(string name, string description, string emoji, DFile file, string? reason = null) =>
        await Bot._rest.CreateGuildStickerAsync(Id, name, description, emoji, file, reason);
    
    /// <summary>
    /// Requests all guild stickers.
    /// </summary>
    /// <returns>All stickers in the guild.</returns>
    /// <remarks>It's generally preferred to use <see cref="Stickers"/> unless this is needed.</remarks>
    public async Task<IReadOnlyCollection<GuildSticker>> RequestStickersAsync() =>
        await Bot._rest.ListGuildStickersAsync(Id);
    
    /// <summary>
    /// Requests a guild sticker. Unlike <see cref="GetSticker"/>, this is an API call.
    /// </summary>
    /// <param name="id">Sticker ID.</param>
    /// <returns>The requested sticker</returns>
    public async Task<GuildSticker> RequestStickerAsync(ulong id) =>
        await Bot._rest.GetGuildStickerAsync(Id, id);
    
    /// <summary>
    /// Retrieves a sticker from the cache.
    /// </summary>
    /// <param name="id">ID of the sticker.</param>
    /// <returns>The sticker with the provided ID, or <c>null</c> if not found.</returns>
    public GuildSticker? GetSticker(ulong id) =>
        Stickers.FirstOrDefault(s => s.Id == id);

    /// <summary>
    /// Requests all roles in the guild.
    /// </summary>
    /// <returns>All roles in the guild.</returns>
    /// <remarks>It's generally preferred to use <see cref="Roles"/> unless this is needed.</remarks>
    public async Task<IReadOnlyCollection<Role>> RequestRolesAsync() =>
        await Bot._rest.GetGuildRolesAsync(Id);
    
    /// <summary>
    /// Requests a role. Unlike <see cref="GetRole"/>, this is an API call.
    /// </summary>
    /// <param name="id">Role ID.</param>
    /// <returns>The requested role.</returns>
    public async Task<Role> RequestRoleAsync(ulong id) =>
        await Bot._rest.GetGuildRoleAsync(Id, id);
    
    /// <summary>
    /// Retrieves a role from the cache.
    /// </summary>
    /// <param name="id">Role ID.</param>
    /// <returns>The role matching the given ID, or <c>null</c> if not found.</returns>
    public Role? GetRole(ulong id) =>
        Roles.FirstOrDefault(r => r.Id == id);

    /// <summary>
    /// Create a role.
    /// </summary>
    /// <param name="name">Name of the role, max 100 characters.</param>
    /// <param name="permissions">The role's permissions.</param>
    /// <param name="color">The role's colors.</param>
    /// <param name="hoist">Whether the role should be displayed separately in the sidebar.</param>
    /// <param name="icon">The role's icon image (if the guild has the <see cref="GuildFeature.RoleIcons"/> feature)</param>
    /// <param name="emoji">The role's Unicode emoji as a standard emoji (if the guild has the <see cref="GuildFeature.RoleIcons"/> feature).</param>
    /// <param name="mentionable">Whether the role should be mentionable.</param>
    /// <param name="reason">Reason for creating the role. This is displayed in the audit-log.</param>
    /// <returns>The created role.</returns>
    public async Task<Role> CreateRoleAsync(
        string? name = null, 
        Permissions? permissions = null, 
        RoleColor? color = null,
        bool hoist = false,
        DFile? icon = null,
        string? emoji = null,
        bool mentionable = false,
        string? reason = null)
    {
        var payload = new JSON
        {
            { "name", name ?? "new role"},
            { "permissions", permissions?.Value.ToString() ?? Permissions.None.Value.ToString() },
            { "colors", color ?? new RoleColor() },
            { "hoist", hoist },
            { "icon", icon?._mimeTypeBase64 },
            { "emoji", emoji },
            { "mentionable", mentionable },
        };
        return await Bot._rest.CreateGuildRoleAsync(Id, payload, reason);
    }

    /// <summary>
    /// Edit role positions.
    /// </summary>
    /// <param name="positions">A dictionary which indicates each role and its new position.</param>
    /// <param name="reason">Reason for editing the role positions. This is displayed in the audit-log.</param>
    /// <returns>All roles in the guild.</returns>
    public async Task<List<Role>> EditRolePositionsAsync(Dictionary<Role, int> positions, string? reason = null) =>
        await Bot._rest.ModifyGuildRolePositionsAsync(Id, positions, reason);
    
    /// <summary>
    /// Requests a member. Unlike <see cref="GetMember"/>, this is an API call.
    /// </summary>
    /// <param name="id">Member ID.</param>
    /// <returns>The requested member.</returns>
    public async Task<Member> RequestMemberAsync(ulong id) =>
        await Bot._rest.GetGuildMemberAsync(Id, id);

    /// <summary>
    /// Request members in the guild.
    /// </summary>
    /// <param name="amount">The amount of members to request (1000 max yielded per loop). Can be <c>null</c> to request
    /// <b>all</b> members in the guild, and depending on the guild size that can take a while.</param>
    /// <param name="after">Request members whose accounts were created after the given date.</param>
    /// <param name="cache">Whether to cache each member.</param>
    /// <returns>The requested amount of members.</returns>
    /// <remarks>Requires <see cref="Intents.GuildMembers"/>. Privileged Gateway Intents (server members) are also
    /// required and need to be enabled in your <a href="https://discord.com/developers/applications">Discord developer portal.</a>
    /// If you don't need access to each member but want all members, consider using <see cref="ChunkAsync"/> instead.
    /// </remarks>
    public async IAsyncEnumerable<List<Member>> RequestMembersAsync(int? amount = 1000, DateTime? after = null, bool cache = true)
    {
        const int indefinite = -1;
        var remaining = amount ?? indefinite;

        var afterSnowflakeTime = after is not null ? Util.DateTimeToSnowflake(after.Value) : 0;
        var hasMore = true;
        var yielded = new List<Member>();

        do
        {
            yielded.Clear();
            var requestAmount = remaining == indefinite ? 1000 : Math.Min(remaining, 1000);
            var members = await Bot._rest.ListGuildMembersAsync(Id, requestAmount, afterSnowflakeTime);
            if (members.Count < 1000)
                hasMore = false;

            foreach (var member in members)
            {
                yielded.Add(member);
                if (remaining == indefinite) continue;

                remaining -= 1;
                if (remaining == 0)
                    hasMore = false;
            }

            afterSnowflakeTime = members.Last().Id;
            if (cache) 
                _members.UnionWith(members);
            yield return yielded;
        } while (hasMore);
    }
    
    /// <summary>
    /// Retrieves a member from the cache.
    /// </summary>
    /// <param name="id">Member ID.</param>
    /// <returns>The member matching the given ID, or <c>null</c> if not found.</returns>
    public Member? GetMember(ulong id) =>
        Members.FirstOrDefault(m => m.Id == id);

    /// <summary>
    /// Searches all member usernames/nicknames and selects each member that start with the given string.
    /// </summary>
    /// <param name="startsWith">What to search for (case-insensitive).</param>
    /// <param name="limit">Maximum amount of members to return (1-1000).</param>
    /// <returns>Members whose username or nickname <i>start with</i> the given string.</returns>
    public async Task<List<Member>> SearchMembersAsync(string startsWith, int limit = 500) =>
        await Bot._rest.SearchGuildMembersAsync(Id, startsWith, limit);

    /// <summary>
    /// Requests all members in the guild. Requires <see cref="Intents.GuildMembers"/>. All members are cached regardless
    /// of cache manager settings.
    /// </summary>
    /// <exception cref="DiscordException">Missing <see cref="Intents.GuildMembers"/>.</exception>
    /// <remarks>This task is completed via the Websocket. If you'd like to access each member individually (batched) and
    /// control whether members are cached, consider using <see cref="RequestMembersAsync"/> instead; although that process
    /// is slower.
    /// </remarks>
    public async Task ChunkAsync()
    {
        if (!Bot.Intents.HasFlag(Intents.GuildMembers))
            throw new DiscordException($"Missing {Intents.GuildMembers} intent");
        var payload = new
        {
            op = Opcode.RequestGuildMembers,
            d = new
            {
                guild_id = Id,
                query = string.Empty,
                limit = 0
            }
        };
        await Bot._gateway.SendJsonAsync(payload);
        LastChunked = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Removes a member from the guild.
    /// </summary>
    /// <param name="member">Member to remove.</param>
    /// <param name="reason">The reason for removing the member. This is displayed in the audit-log.</param>
    /// <remarks>Requires <see cref="Permission.KickMembers"/>.</remarks>
    public async Task KickAsync(Member member, string? reason = null)
    {
        await Bot._rest.RemoveGuildMemberAsync(Id, member.Id, reason);
    }

    /// <summary>
    /// Requests ban records for the guild.
    /// </summary>
    /// <param name="amount">Amount of ban records to return (max 1000 yielded per loop), or <c>null</c> for all records.</param>
    /// <param name="before">A user ID. Considers only users before the given ID.</param>
    /// <param name="after">A user ID. Considers only users after the given ID.</param>
    /// <returns>The requested amount of ban records.</returns>
    /// <exception cref="DiscordException"> Only one parameter (<paramref name="before"/> or <paramref name="after"/>)
    /// can be used, not both.
    /// </exception>
    /// <remarks>Requires <see cref="Permission.BanMembers"/>. Each batch of records is sorted in ascending order (by Discord)
    /// according to their user ID.
    /// </remarks>
    public async IAsyncEnumerable<List<BanRecord>> BanRecordsAsync(int? amount = 1000, ulong? before = null, ulong? after = null)
    {
        // TODO: This method has not been fully tested due to not having access to a guild with more than 1000 bans.
        
        if (before is not null && after is not null)
            throw new DiscordException("Either parameter 'before' or 'after' can be used, not both.");
        
        const int indefinite = -1;
        var remaining = amount ?? indefinite;
        
        var hasMore = true;
        var yielded = new List<BanRecord>();
        
        do
        {
            yielded.Clear();
            var requestAmount = remaining == indefinite ? 1000 : Math.Min(remaining, 1000);
            var bans = await Bot._rest.GetGuildBansAsync(Id, requestAmount, before, after);
            if (bans.Count < 1000)
                hasMore = false;

            foreach (var ban in bans)
            {
                yielded.Add(ban);
                if (remaining == indefinite) continue;

                remaining -= 1;
                if (remaining == 0)
                    hasMore = false;
            }

            before = bans.LastOrDefault()?.User.Id ?? 0;
            after = before;
            yield return yielded;
        } while (hasMore);
    }

    /// <summary>
    /// Requests a ban record.
    /// </summary>
    /// <param name="id">User ID associated with the ban record.</param>
    /// <returns>The requested record.</returns>
    public async Task<BanRecord> BanRecordAsync(ulong id) =>
        await Bot._rest.GetGuildBanAsync(Id, id);

    /// <summary>
    /// Ban a user from the guild.
    /// </summary>
    /// <param name="id">ID of the user to ban.</param>
    /// <param name="deleteMessages">Amount of seconds (max 604800 aka 7 days) worth of messages to delete from the
    /// guild that were sent by the user. Defaults to 1 day.
    /// </param>
    /// <param name="reason">The reason for banning the user. This is displayed in the audit-log.</param>
    /// <remarks>Requires <see cref="Permission.BanMembers"/>.</remarks>
    public async Task BanAsync(ulong id, TimeSpan? deleteMessages = null, string? reason = null)
    {
        await Bot._rest.CreateGuildBanAsync(Id, id, deleteMessages ?? TimeSpan.FromDays(1), reason);
    }

    /// <summary>
    /// Unban a user from the guild.
    /// </summary>
    /// <param name="id">ID of the user to unban.</param>
    /// <param name="reason">The reason for unbanning the user. This is displayed in the audit-log.</param>
    /// <remarks>Requires <see cref="Permission.BanMembers"/>.</remarks>
    public async Task UnbanAsync(ulong id, string? reason = null)
    {
        await Bot._rest.RemoveGuildBanAsync(Id, id, reason);
    }

    /// <summary>
    /// Ban multiple users from the guild at once.
    /// </summary>
    /// <param name="ids">ID's of the users to ban (max 200).</param>
    /// <param name="deleteMessages">Amount of seconds (max 604800 aka 7 days) worth of messages to delete from the
    /// guild that were sent by the user. Defaults to 1 day.</param>
    /// <param name="reason">The reason for bulk-banning the users. This is displayed in the audit-log.</param>
    /// <returns>A named tuple containing <c>bannedUsers</c> (users who were successfully banned) and <c>failedUsers</c>
    /// (users who were not banned).</returns>
    /// <remarks>Requires <see cref="Permission.BanMembers"/> and <see cref="Permission.ManageGuild"/>.</remarks>
    public async Task<(List<ulong> bannedUsers, List<ulong> failedUsers)> BulkBanAsync(IEnumerable<ulong> ids,
        TimeSpan? deleteMessages = null, string? reason = null)
    {
        var payload = new JSON
        {
            { "user_ids", ids },
            { "delete_message_seconds", (deleteMessages ?? TimeSpan.FromDays(1)).TotalSeconds }
        };
        return await Bot._rest.BulkGuildBanAsync(Id, payload, reason);
    }

    /// <summary>
    /// Get the amount of members that would be pruned.
    /// </summary>
    /// <param name="days">Number of days to count prune for (1-30)</param>
    /// <param name="roles">Role(s) to include, due to the prune operation excluding members with roles by default.</param>
    /// <returns>The amount of members that would be pruned.</returns>
    /// <remarks>Requires <see cref="Permission.ManageGuild"/> and <see cref="Permission.KickMembers"/>.</remarks>
    public async Task<int> PruneCountAsync(int days, IEnumerable<Role>? roles = null) =>
        await Bot._rest.GetGuildPruneCountAsync(Id, days, roles);

    /// <summary>
    /// Prune the guild.
    /// </summary>
    /// <param name="days">Number of days to prune (1-30).</param>
    /// <param name="computePruneCount">Whether the pruned count is returned, discouraged for large guilds.</param>
    /// <param name="roles">Role(s) to include, due to the prune operation excluding members with roles by default.</param>
    /// <param name="reason">The reason for pruning the guild. This is displayed in the audit-log.</param>
    /// <returns>The amount of members that were pruned, or <c>null</c> if <paramref name="computePruneCount"/> is <c>false</c></returns>
    /// <remarks>Requires <see cref="Permission.ManageGuild"/> and <see cref="Permission.KickMembers"/>.</remarks>
    public async Task<int?> PruneAsync(int days, bool computePruneCount = true, IEnumerable<Role>? roles = null, string? reason = null) =>
        await Bot._rest.BeginGuildPruneAsync(Id, days, computePruneCount, roles, reason);
    
    /// <summary>
    /// Invites for the guild.
    /// </summary>
    /// <returns>All invites for the guild.</returns>
    /// <remarks>Requires <see cref="Permission.ManageGuild"/> or <see cref="Permission.ViewAuditLog"/>.</remarks>
    public async Task<IEnumerable<Invite>> InvitesAsync() =>
        await Bot._rest.GetGuildInvitesAsync(Id);

    /// <summary>
    /// Integrations for the guild.
    /// </summary>
    /// <returns>A maximum of 50 integrations. If a guild has more integrations, they cannot be accessed.</returns>
    /// <remarks>Requires <see cref="Permission.ManageGuild"/>.</remarks>
    public async Task<ICollection<Integration>> IntegrationsAsync() =>
        await Bot._rest.GetGuildIntegrations(Id);

    /// <summary>
    /// Basic information for the widget.
    /// </summary>
    /// <returns>The widgets settings.</returns>
    public async Task<WidgetSetting> WidgetSettingsAsync() =>
        await Bot._rest.GetGuildWidgetSettingsAsync(Id);

    /// <summary>
    /// Enable/disable the widget.
    /// </summary>
    /// <param name="enabled">Whether the widget is enabled.</param>
    /// <param name="channelId">The widget channel ID.</param>
    /// <param name="reason">The reason for editing the widget. This is displayed in the audit-log.</param>
    /// <returns>The updated widget settings.</returns>
    /// <remarks>Requires <see cref="Permission.ManageGuild"/>.</remarks>
    public async Task<WidgetSetting> EditWidgetAsync(bool enabled, ulong? channelId = null, string? reason = null) =>
        await Bot._rest.ModifyGuildWidgetAsync(Id, enabled, channelId, reason);

    /// <summary>
    /// Widget for the guild (if enabled). Use <see cref="WidgetSettingsAsync"/> to see if the widget is enabled/disabled.
    /// </summary>
    /// <returns>The active widget.</returns>
    public async Task<Widget> WidgetAsync() =>
        await Bot._rest.GetGuildWidgetAsync(Id);

    /// <summary>
    /// The vanity URL code and its uses.
    /// </summary>
    /// <returns>A named tuple containing the vanity code and its uses. <c>code</c> can be <c>null</c> if a vanity URL
    /// for the guild is not set.
    /// </returns>
    /// <remarks>Requires <see cref="Permission.ManageGuild"/>. The invite code can be accessed via <see cref="Guild.VanityUrlCode"/>
    /// without the above stated permission.
    /// </remarks>
    public async Task<(string? code, int uses)> VanityUrlAsync() =>
        await Bot._rest.GetGuildVanityUrlAsync(Id);
    
    /// <summary>
    /// The widget image.
    /// </summary>
    /// <param name="style">Widget style.</param>
    /// <returns>A file that represents the widget image.</returns>
    public async Task<DFile> WidgetImageAsync(WidgetStyle style = WidgetStyle.Shield) =>
        await Bot._rest.GetGuildWidgetImageAsync(Id, style);
    
    /// <summary>
    /// The onboarding for the guild.
    /// </summary>
    /// <returns>The guilds onboarding even if it's disabled.</returns>
    public async Task<Onboarding> OnboardingAsync() =>
        await Bot._rest.GetGuildOnboardingAsync(Id);
    
    /// <summary>
    /// Update the guilds onboarding.
    /// </summary>
    /// <param name="enabled">Whether onboarding is enabled in the guild.</param>
    /// <param name="prompts">Prompts shown during onboarding and in customize community.</param>
    /// <param name="mode">Current mode of onboarding.</param>
    /// <param name="defaultChannelIds">Channel IDs that members get opted into automatically.</param>
    /// <param name="reason">The reason for editing the onboarding. This is displayed in the audit-log.</param>
    /// <returns>The updated onboarding.</returns>
    /// <remarks>Requires <see cref="Permission.ManageGuild"/> and <see cref="Permission.ManageRoles"/>.
    /// Onboarding enforces constraints when enabled. These constraints are that there must be at least 7 Default Channels
    /// and at least 5 of them must allow sending messages to the <c>@everyone</c> role. <see cref="Onboarding.Mode"/>
    /// modifies what is considered when enforcing these constraints.
    /// </remarks>
    public async Task<Onboarding> EditOnboardingAsync(bool enabled, ICollection<OnboardingPrompt>? prompts = null,
        OnboardingPromptType? mode = null, ICollection<ulong>? defaultChannelIds = null, string? reason = null)
    {
        var payload = new JSON { { "enabled", enabled } };
        if (prompts is not null) payload.Add("prompts", prompts);
        if (mode is not null) payload.Add("mode", mode);
        if (defaultChannelIds is not null) payload.Add("default_channel_ids", defaultChannelIds);
        return await Bot._rest.ModifyGuildOnboardingAsync(Id, payload, reason);
    }

    /// <summary>
    /// Edit guild incident data such as disabling (pausing) invites, direct messages, etc.
    /// </summary>
    /// <param name="edit">An incident actions edit.</param>
    /// <remarks>Requires <see cref="Permission.ManageGuild"/>.</remarks>
    public async Task<Incidents> EditIncidentActionsAsync(IncidentActionsEdit edit)
    {
        var updated = new JSON();
        foreach (var (key, span) in edit._payload)
            updated[key] = span is not null ? DateTime.UtcNow.Add(span.Value) : null;
        return await Bot._rest.ModifyGuildIncidentActions(Id, updated);
    }

    /// <summary>
    /// A shortcut to determine whether invites are currently disabled (paused) for the guild.
    /// </summary>
    /// <returns>Whether invites are disabled.</returns>
    public bool InvitesDisabled() => IncidentsData?.InvitesDisabledUntil is not null;
    
    /// <summary>
    /// A shortcut to determine whether DMs are currently disabled (paused) for the guild.
    /// </summary>
    /// <returns>Whether DMs are disabled.</returns>
    public bool DmsDisabled() => IncidentsData?.DmDisabledUntil is not null;
    
    #endregion

    #region PRIVATE

    internal void CacheMembersFromCreate(GatewayPayload payload, ulong botId)
    {
        var members = DiscordGatewayClient.GetElementValue(payload.D!.Value, "members");
        
        if (Bot.CacheManager.Members)
        {
            var converted = DiscordGatewayClient.DeserializeWithNewtonsoft<List<Member>>(members);
            Bot._rest.SetMemberValues(converted, Id);
            _members.UnionWith(converted);
            Self = _members.First(m => m.Id == botId);
        }
        else
        {
            // Regardless of CacheManager.Members, the bot is still cached for all guilds.

            // Through testing, the bots ID is always the last one, so reverse it so it can potentially avoid the extra
            // loops. If for whatever reason it ends up not being the last one, no big deal continue as normal.
            var reversed = members.EnumerateArray().Reverse();
            
            foreach (var element in reversed)
            {
                var userId = element.GetProperty("user").GetProperty("id");
                if (Convert.ToUInt64(userId.ToString()) != Bot.User?.Id)
                    continue;
                Self = DiscordGatewayClient.DeserializeWithNewtonsoft<Member>(element);
                Bot._rest.SetMemberValues([Self], Id);
                _members.Add(Self);
                break;
            }
        }
    }
    
    // TODO
    internal void Update(JsonElement element)
    {
        throw new NotImplementedException();
    }
    
    internal static HashSet<GuildFeature> ParseFeatures(ICollection<string> features)
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
/// Represents the values that can be edited for a <see cref="Guild"/>s incident action.
/// </summary>
public struct IncidentActionsEdit
{
    internal Dictionary<string, TimeSpan?> _payload = [];
    
    /// <summary>
    /// Initializes a new incident actions edit instance.
    /// </summary>
    public IncidentActionsEdit() { }

    /// <summary>
    /// Set invites disabled.
    /// </summary>
    /// <param name="for">When invites will be enabled again (max 24 hours), or <c>null</c> to disable the action.</param>
    /// <returns>The edit instance.</returns>
    public IncidentActionsEdit SetInvitesDisabled(TimeSpan? @for)
    {
        _payload["invites_disabled_until"] = @for;
        return this;
    }
    
    /// <summary>
    /// Set DMs disabled.
    /// </summary>
    /// <param name="for">When direct messages will be enabled again (max 24 hours), or <c>null</c> to disable the action.</param>
    /// <returns>The edit instance.</returns>
    public IncidentActionsEdit SetDmsDisabled(TimeSpan? @for)
    {
        _payload["dms_disabled_until"] = @for;
        return this;
    }
}

/// <summary>
/// Represents the onboarding for a <see cref="Guild"/>.
/// </summary>
public record Onboarding
{
    // DOCS: https://discord.com/developers/docs/resources/guild#guild-onboarding-object-guild-onboarding-structure
    
    /// <summary>
    /// ID of the guild this onboarding is part of.
    /// </summary>
    [JsonProperty("guild_id")]
    public ulong GuildId { get; init; }
    
    /// <summary>
    /// Prompts shown during onboarding and in customize community.
    /// </summary>
    [JsonProperty("prompts")]
    public required IReadOnlyCollection<OnboardingPrompt> Prompts { get; init; }
    
    /// <summary>
    /// Channel IDs that members get opted into automatically.
    /// </summary>
    [JsonProperty("default_channel_ids")]
    public required IReadOnlyCollection<ulong> DefaultChannelIds { get; init; }
    
    /// <summary>
    /// Whether onboarding is enabled in the guild.
    /// </summary>
    [JsonProperty("enabled")]
    public bool Enabled { get; init; }
    
    /// <summary>
    /// Current mode of onboarding.
    /// </summary>
    [JsonProperty("mode")]
    public OnboardingMode Mode { get; init; }
    
    private Onboarding() { }
}

/// <summary>
/// Represents an <see cref="Onboarding"/> prompt.
/// </summary>
/// <param name="Id">ID of the prompt.</param>
/// <param name="Type">Type of prompt.</param>
/// <param name="Options">Options available within the prompt.</param>
/// <param name="Title">Title of the prompt.</param>
/// <param name="IsSingleSelect">Indicates whether users are limited to selecting one option for the prompt.</param>
/// <param name="IsRequired">Indicates whether the prompt is required before a user completes the onboarding flow.</param>
/// <param name="InOnboarding">Indicates whether the prompt is present in the onboarding flow. If <c>false</c>, the prompt will only appear in
/// the Channels and Roles tab.</param>
public record OnboardingPrompt(
    ulong Id,
    OnboardingPromptType Type,
    ICollection<OnboardingPromptOption> Options,
    string Title,
    bool IsSingleSelect,
    bool IsRequired,
    bool InOnboarding)
{
    // DOCS: https://discord.com/developers/docs/resources/guild#guild-onboarding-object-onboarding-prompt-structure
    
    /// <summary>
    /// ID of the prompt.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; set; } = Id;

    /// <summary>
    /// Type of prompt.
    /// </summary>
    [JsonProperty("type")]
    public OnboardingPromptType Type { get; set; } = Type;

    /// <summary>
    /// Options available within the prompt.
    /// </summary>
    [JsonProperty("options")]
    public ICollection<OnboardingPromptOption> Options { get; set; } = Options;

    /// <summary>
    /// Title of the prompt.
    /// </summary>
    [JsonProperty("title")]
    public string Title { get; set; } = Title;

    /// <summary>
    /// Indicates whether users are limited to selecting one option for the prompt.
    /// </summary>
    [JsonProperty("single_select")]
    public bool IsSingleSelect { get; set; } = IsSingleSelect;

    /// <summary>
    /// Indicates whether the prompt is required before a user completes the onboarding flow.
    /// </summary>
    [JsonProperty("required")]
    public bool IsRequired { get; set; } = IsRequired;

    /// <summary>
    /// Indicates whether the prompt is present in the onboarding flow. If <c>false</c>, the prompt will only appear in
    /// the Channels and Roles tab.
    /// </summary>
    [JsonProperty("in_onboarding")]
    public bool InOnboarding { get; set; } = InOnboarding;
}

/// <summary>
/// Represents an <see cref="OnboardingPrompt"/> option.
/// </summary>
public record OnboardingPromptOption
{
    // DOCS: https://discord.com/developers/docs/resources/guild#guild-onboarding-object-prompt-option-structure
    
    /// <summary>
    /// ID of the prompt option.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; set; }

    /// <summary>
    /// IDs for channels a member is added to when the option is selected.
    /// </summary>
    [JsonProperty("channel_ids")]
    public ICollection<ulong> ChannelIds { get; set; }

    /// <summary>
    /// IDs for roles assigned to a member when the option is selected.
    /// </summary>
    [JsonProperty("role_ids")]
    public ICollection<ulong> RoleIds { get; set; }
    
    /// <summary>
    /// Emoji of the option.
    /// </summary>
    [JsonProperty("emoji")]
    public PartialEmoji? Emoji { get; set; }
    
    /// <summary>
    /// Emoji ID of the option.
    /// </summary>
    [JsonProperty("emoji_id")]
    public ulong? EmojiId { get; set; }
    
    /// <summary>
    /// Emoji name of the option.
    /// </summary>
    [JsonProperty("emoji_name")]
    public string? EmojiName { get; set; }
    
    /// <summary>
    /// Whether the emoji is animated.
    /// </summary>
    [JsonProperty("emoji_animated")]
    public bool? EmojiAnimated { get; set; }
    
    /// <summary>
    /// Title of the option.
    /// </summary>
    [JsonProperty("title")]
    public string Title { get; set; }
    
    /// <summary>
    /// Description of the option.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Initialize a prompt option.
    /// </summary>
    /// <param name="id">ID of the prompt option.</param>
    /// <param name="channelIds">IDs for channels a member is added to when the option is selected.</param>
    /// <param name="roleIds">IDs for roles assigned to a member when the option is selected.</param>
    /// <param name="title">Title of the option.</param>
    /// <param name="description">Description of the option.</param>
    /// <param name="emoji">Emoji of the option. After instantiation, properties <see cref="EmojiId"/>, <see cref="EmojiName"/>,
    /// and <see cref="EmojiAnimated"/> should not be updated separately.
    /// </param>
    public OnboardingPromptOption(ulong id, ICollection<ulong> channelIds, ICollection<ulong> roleIds, string title,
        string? description, PartialEmoji? emoji)
    {
        Id = id;
        ChannelIds = channelIds;
        RoleIds = roleIds;
        Title = title;
        Description = description;
        EmojiId = emoji?.Id;
        EmojiName = emoji?.Name;
        EmojiAnimated = emoji?.IsAnimated;
    }
}

/// <summary>
/// Represents an <see cref="Onboarding"/> mode.
/// </summary>
public enum OnboardingMode
{
    // DOCS: https://discord.com/developers/docs/resources/guild#guild-onboarding-object-onboarding-mode
    
    /// <summary>
    /// Counts only Default Channels towards constraints.
    /// </summary>
    Default,
    
    /// <summary>
    /// Counts Default Channels and Questions towards constraints.
    /// </summary>
    Advanced
}

/// <summary>
/// Represents an <see cref="OnboardingPrompt"/> type.
/// </summary>
public enum OnboardingPromptType
{
    // DOCS: https://discord.com/developers/docs/resources/guild#guild-onboarding-object-prompt-types
    
    MultipleChoice,
    Dropdown
}

/// <summary>
/// Represents a <see cref="Widget"/> image style.
/// </summary>
public enum WidgetStyle
{
    // DOCS: https://discord.com/developers/docs/resources/guild#get-guild-widget-image-widget-style-options
    
    /// <summary>
    /// Shield style widget with Discord icon and guild members online count
    /// (<a href="https://discord.com/api/guilds/81384788765712384/widget.png?style=shield">example</a>)
    /// </summary>
    [Description("shield")]
    Shield,
    
    /// <summary>
    /// Large image with guild icon, name and online count. "POWERED BY DISCORD" as the footer of the widget
    /// (<a href="https://discord.com/api/guilds/81384788765712384/widget.png?style=banner1">example</a>)
    /// </summary>
    [Description("banner1")]
    Banner1,
    
    /// <summary>
    /// Smaller widget style with guild icon, name and online count. Split on the right with Discord logo
    /// (<a href="https://discord.com/api/guilds/81384788765712384/widget.png?style=banner2">example</a>)
    /// </summary>
    [Description("banner2")]
    Banner2,
    
    /// <summary>
    /// Large image with guild icon, name and online count. In the footer, Discord logo on the left and "Chat Now" on
    /// the right (<a href="https://discord.com/api/guilds/81384788765712384/widget.png?style=banner3">example</a>)
    /// </summary>
    [Description("banner3")]
    Banner3,
    
    /// <summary>
    /// Large Discord logo at the top of the widget. Guild icon, name and online count in the middle portion of the
    /// widget and a "JOIN MY SERVER" button at the bottom (<a href="https://discord.com/api/guilds/81384788765712384/widget.png?style=banner4">example</a>)
    /// </summary>
    [Description("banner4")]
    Banner4
}

/// <summary>
/// Represents a <see cref="Widget"/> setting for a <see cref="Guild"/>.
/// </summary>
public record WidgetSetting
{
    // DOCS: https://discord.com/developers/docs/resources/guild#guild-widget-settings-object
    
    /// <summary>
    /// Whether the widget is enabled.
    /// </summary>
    [JsonProperty("enabled")]
    public bool Enabled { get; init; }
    
    /// <summary>
    /// The widget channel ID.
    /// </summary>
    [JsonProperty("channel_id")]
    public ulong? ChannelId { get; init; }
    
    private WidgetSetting() { }
}

/// <summary>
/// Represents a <see cref="Guild"/> widget.
/// </summary>
public record Widget
{
    // DOCS: https://discord.com/developers/docs/resources/guild#guild-widget-object
    
    /// <summary>
    /// Guild ID.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; init; }
    
    /// <summary>
    /// Guild name.
    /// </summary>
    [JsonProperty("name")]
    public required string Name { get; init; }
    
    /// <summary>
    /// Instant invite for the guilds specified widget invite channel.
    /// </summary>
    [JsonProperty("instant_invite")]
    public string? InstantInvite { get; init; }
    
    /// <summary>
    /// Voice and stage channels which are accessible by <c>@everyone</c>.
    /// </summary>
    [JsonProperty("channels")]
    public required IReadOnlyCollection<PartialWidgetChannel> Channels { get; init; } 
    
    /// <summary>
    /// Users in the guild (max 100).
    /// </summary>
    [JsonProperty("members")]
    public required IReadOnlyCollection<PartialWidgetMember> Members { get; init; } 
    
    /// <summary>
    /// Number of online members in this guild.
    /// </summary>
    [JsonProperty("presence_count")]
    public int PresenceCount { get; init; }
    
    private Widget() { }
}

/// <summary>
/// Represents a partial member associated with a <see cref="Widget"/>.
/// </summary>
public record PartialWidgetMember
{
    /// <summary>
    /// This ID is not your typical Snowflake. According to Discord, this is anonymized to prevent abuse.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; init; }
    
    /// <summary>
    /// The user's username, not unique across the platform.
    /// </summary>
    [JsonProperty("username")]
    public required string Username { get; init; }

    /// <summary>
    /// The user's status.
    /// </summary>
    public StatusType Status => User.ParseStatus(_status);
    [JsonProperty("status")] private string _status = string.Empty;
    
    /// <summary>
    /// Avatar URL. According to Discord, this is anonymized to prevent abuse.
    /// </summary>
    [JsonProperty("avatar_url")] 
    public required string AvatarUrl { get; init; }
    
    private PartialWidgetMember() { }
}

/// <summary>
/// Represents a partial channel associated with a <see cref="Widget"/>.
/// </summary>
public record PartialWidgetChannel
{
    /// <summary>
    /// Channel ID.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; init; }
    
    /// <summary>
    /// Name of the channel.
    /// </summary>
    [JsonProperty("name")]
    public required string Name { get; init; }
    
    /// <summary>
    /// Sorting position of the channel.
    /// </summary>
    [JsonProperty("position")]
    public int Position { get; init; }
    
    private PartialWidgetChannel() { }
}

/// <summary>
/// Represents a ban for a <see cref="Guild"/>.
/// </summary>
public record BanRecord
{
    // DOCS: https://discord.com/developers/docs/resources/guild#ban-object
    
    /// <summary>
    /// The reason for the ban.
    /// </summary>
    [JsonProperty("reason")]
    public string? Reason { get; init; }
    
    /// <summary>
    /// The banned user.
    /// </summary>
    [JsonProperty("user")]
    public required User User { get; init; }
    
    private BanRecord() {}
}

/// <summary>
/// Represents a user's voice state.
/// </summary>
public record VoiceState
{
    // DOCS: https://discord.com/developers/docs/resources/voice#voice-state-object
    
    /// <summary>
    /// The channel ID this user is connected to.
    /// </summary>
    [JsonProperty("channel_id")]
    public ulong ChannelId { get; init; }
    
    /// <summary>
    /// The user ID this voice state is for.
    /// </summary>
    [JsonProperty("user_id")]
    public ulong UserId { get; init; }
    
    /// <summary>
    /// The session ID for this voice state.
    /// </summary>
    [JsonProperty("session_id")]
    public string SessionId { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether this user is deafened by the server.
    /// </summary>
    [JsonProperty("deaf")]
    public bool IsDeafened { get; init; }
    
    /// <summary>
    /// Whether this user is muted by the server.
    /// </summary>
    [JsonProperty("mute")]
    public bool IsMuted { get; init; }
    
    /// <summary>
    /// Whether this user is locally deafened.
    /// </summary>
    [JsonProperty("self_deaf")]
    public bool IsSelfDeafened { get; init; }
    
    /// <summary>
    /// Whether this user is locally muted.
    /// </summary>
    [JsonProperty("self_mute")]
    public bool IsSelfMuted { get; init; }
    
    /// <summary>
    /// Whether this user is streaming using "Go Live".
    /// </summary>
    public bool IsStreaming => _isStreaming is not null;
    [JsonProperty("self_stream")] internal bool? _isStreaming;
    
    /// <summary>
    /// Whether this user's camera is enabled.
    /// </summary>
    [JsonProperty("self_video")]
    public bool IsCameraEnabled { get; init; }
    
    /// <summary>
    /// Whether this user's permission to speak is denied.
    /// </summary>
    [JsonProperty("suppress")]
    public bool IsSuppressed { get; init; }
    
    /// <summary>
    /// The time at which the user requested to speak.
    /// </summary>
    [JsonProperty("request_to_speak_timestamp")]
    public DateTime? RequestToSpeakTimestamp { get; init; }
    
    private VoiceState() { }
}

/// <summary>
/// Represents the values that can be edited for a <see cref="Guild"/>. 
/// </summary>
public struct GuildEdit
{
    internal JSON _payload = [];
    private readonly HashSet<string> _features = [];

    /// <summary>
    /// Initializes a new guild edit instance.
    /// </summary>
    public GuildEdit() { }

    /// <summary>
    /// Set the guild name.
    /// </summary>
    /// <param name="name">Guild name.</param>
    /// <returns>The edit instance.</returns>
    public GuildEdit SetName(string name)
    {
        _payload["name"] = name;
        return this;
    }

    /// <summary>
    /// Set the guild verification level.
    /// </summary>
    /// <param name="verificationLevel">Verification level.</param>
    /// <returns>The edit instance.</returns>
    public GuildEdit SetVerificationLevel(GuildVerificationLevel? verificationLevel)
    {
        _payload["verification_level"] = verificationLevel;
        return this;
    }

    /// <summary>
    /// Set the guild message notification level.
    /// </summary>
    /// <param name="notificationLevel">Notification level.</param>
    /// <returns>The edit instance.</returns>
    public GuildEdit SetMessageNotificationLevel(GuildMessageNotificationLevel? notificationLevel)
    {
        _payload["default_message_notifications"] = notificationLevel;
        return this;
    }

    /// <summary>
    /// Set the guild explicit content filter.
    /// <param name="explicitContentFilterLevel">Filter level.</param>
    /// </summary>
    /// <returns>The edit instance.</returns>
    public GuildEdit SetExplicitContentFilterLevel(GuildExplicitContentFilterLevel? explicitContentFilterLevel)
    {
        _payload["explicit_content_filter"] = explicitContentFilterLevel;
        return this;
    }

    /// <summary>
    /// Set the AFK channel. Can be set to <c>null</c> to disable AFK channels.
    /// <param name="id">Channel ID.</param>
    /// </summary>
    /// <returns>The edit instance.</returns>
    public GuildEdit SetAfkChannel(ulong? id)
    {
        _payload["afk_channel_id"] = id;
        return this;
    }

    /// <summary>
    /// Update the amount of time it takes for someone to be automatically moved to the AFK channel.
    /// </summary>
    /// <param name="value">The only valid time intervals are: 60 (1 minute), 300 (5 minutes), 900 (15 minutes),
    /// 1800 (30 minutes), and 3600 (60 minutes)
    /// </param>
    /// <returns>The edit instance.</returns>
    public GuildEdit SetAfkTimeout(int value)
    {
        _payload["afk_timeout"] = value;
        return this;
    }

    /// <summary>
    /// Set the guild icon.
    /// <param name="file">A file. Can be animated if the guild has <see cref="GuildFeature.AnimatedIcon"/>,
    /// or <c>null</c> to remove the icon.</param>
    /// </summary>
    /// <returns>The edit instance.</returns>
    public GuildEdit SetIcon(DFile? file)
    {
        _payload["icon"] = file?._mimeTypeBase64;
        return this;
    }

    /// <summary>
    /// Set (transfer) guild ownership. The bot must be the owner of the guild.
    /// <param name="id">ID of the user to transfer ownership to.</param>
    /// </summary>
    /// <returns>The edit instance.</returns>
    public GuildEdit SetOwner(ulong id)
    {
        _payload["owner_id"] = id;
        return this;
    }

    /// <summary>
    /// Set the splash image. Guild must have <see cref="GuildFeature.InviteSplash"/>.
    /// <param name="file">A file, or <c>null</c> to remove the guild splash image.</param>
    /// </summary>
    /// <returns>The edit instance.</returns>
    public GuildEdit SetSplash(DFile? file)
    {
        _payload["splash"] = file?._mimeTypeBase64;
        return this;
    }

    /// <summary>
    /// Set the discovery splash image. Guild must have <see cref="GuildFeature.Discoverable"/>.
    /// <param name="file">A file, or <c>null</c> to remove the guild discovery splash image.</param>
    /// </summary>
    /// <returns>The edit instance.</returns>
    public GuildEdit SetDiscoverySplash(DFile? file)
    {
        _payload["discovery_splash"] = file?._mimeTypeBase64;
        return this;
    }

    /// <summary>
    /// Set the banner image. Guild must have <see cref="GuildFeature.Banner"/>. Can be animated if the guild has
    /// <see cref="GuildFeature.AnimatedBanner"/>.
    /// <param name="file">A file, or <c>null</c> to remove the guild banner.</param>
    /// </summary>
    /// <returns>The edit instance.</returns>
    public GuildEdit SetBanner(DFile? file)
    {
        _payload["banner"] = file?._mimeTypeBase64;
        return this;
    }

    /// <summary>
    /// Set the channel where guild notices such as welcome messages and boost events are posted.
    /// <param name="id">Channel ID, or <c>null</c> to disable the system channel.</param>
    /// </summary>
    /// <returns>The edit instance.</returns>
    public GuildEdit SetSystemChannel(ulong? id)
    {
        _payload["system_channel_id"] = id;
        return this;
    }

    /// <summary>
    /// Set the channel where admins and moderators of Community guilds receive notices from Discord. Only available for
    /// guilds with <see cref="GuildFeature.Community"/>.
    /// <param name="id">Channel ID, or <c>null</c> to disable the public updates channel.</param>
    /// </summary>
    /// <returns>The edit instance.</returns>
    public GuildEdit SetPublicUpdatesChannel(ulong? id)
    {
        _payload["public_updates_channel_id"] = id;
        return this;
    }

    /// <summary>
    /// Set the values for the guild system channel.
    /// <param name="flags">System channel flags.</param>
    /// </summary>
    /// <returns>The edit instance.</returns>
    public GuildEdit SetSystemChannelFlags(IEnumerable<GuildSystemChannelFlags> flags)
    {
        var value = 0;
        foreach (var flag in flags)
            value |= (int)flag;
        _payload["system_channel_flags"] = value;
        return this;
    }

    /// <summary>
    /// Set the preferred locale of a Community guild used in server discovery and notices from Discord.
    /// <param name="locale">The locale.</param>
    /// </summary>
    /// <returns>The edit instance.</returns>
    public GuildEdit SetPreferredLocal(Locale? locale)
    {
        _payload["preferred_locale"] = locale?.GetDescription();
        return this;
    }

    /// <summary>
    /// Enable/disable Community Features in the guild. Both parameters are required to be set in order for it to be enabled.
    /// To disable, set both parameters to <c>null</c>.
    /// <param name="rulesChannelId">Rules channel ID, or <c>null</c> to disable it.</param>
    /// <param name="publicUpdatesChannelId">Public updates channel ID, or <c>null</c> to disable it.</param>
    /// </summary>
    /// <returns>The edit instance.</returns>
    /// <exception cref="ArgumentException">Both parameters weren't set.</exception>
    public GuildEdit SetCommunityEnabled(ulong? rulesChannelId, ulong? publicUpdatesChannelId)
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
    /// Set guild discovery on/off.
    /// <param name="enabled">Whether discovery is enabled.</param>
    /// </summary>
    /// <returns>The edit instance.</returns>
    public GuildEdit SetDiscoveryEnabled(bool enabled)
    {
        if (enabled)
            _features.Add(GuildFeature.Discoverable.GetDescription());
        else
            _features.Remove(GuildFeature.Discoverable.GetDescription());
        _payload["features"] = _features;
        return this;
    }

    /// <summary>
    /// Set invites/access to the guild.
    /// <param name="disabled">Whether invites are disabled (paused).</param>
    /// </summary>
    /// <returns>The edit instance.</returns>
    public GuildEdit SetInvitesDisabled(bool disabled)
    {
        if (disabled)
            _features.Add(GuildFeature.InvitesDisabled.GetDescription());
        else
            _features.Remove(GuildFeature.InvitesDisabled.GetDescription());
        _payload["features"] = _features;
        return this;
    }

    /// <summary>
    /// Set alerts for join raids.
    /// <param name="disabled">Whether raid alerts are disabled.</param>
    /// </summary>
    /// <returns>The edit instance.</returns>
    public GuildEdit SetRaidAlertsDisabled(bool disabled)
    {
        if (disabled)
            _features.Add(GuildFeature.RaidAlertsDisabled.GetDescription());
        else
            _features.Remove(GuildFeature.RaidAlertsDisabled.GetDescription());
        _payload["features"] = _features;
        return this;
    }

    /// <summary>
    /// Set the description for the guild. Only available for guilds with <see cref="GuildFeature.Community"/>.
    /// <param name="description">Guild description, or <c>null</c> to remove it.</param>
    /// </summary>
    /// <returns>The edit instance.</returns>
    public GuildEdit SetDescription(string? description)
    {
        _payload["description"] = description;
        return this;
    }

    /// <summary>
    /// Set whether the guild's boost progress bar is enabled.
    /// <param name="enabled">Whether the guild's boost progress bar is enabled.</param>
    /// </summary>
    /// <returns>The edit instance.</returns>
    public GuildEdit SetPremiumProgressBarEnabled(bool enabled)
    {
        _payload["premium_progress_bar_enabled"] = enabled;
        return this;
    }

    /// <summary>
    /// Set the channel where admins and moderators of Community guilds receive safety alerts from Discord.
    /// <param name="id">Channel ID, or <c>null</c> to disable the safety channel.</param>
    /// </summary>
    /// <returns>The edit instance.</returns>
    public GuildEdit SetSafetyAlertsChannel(ulong? id)
    {
        _payload["safety_alerts_channel_id"] = id;
        return this;
    }
}

/// <summary>
/// Represents a <see cref="Guild"/> preview.
/// </summary>
public record GuildPreview
{
    // DOCS: https://discord.com/developers/docs/resources/guild#guild-preview-object
    
    /// <summary>
    /// Guild ID.
    /// </summary>
    public ulong Id { get; }
    
    /// <summary>
    /// Guild name.
    /// </summary>
    [JsonProperty("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Guild avatar.
    /// </summary>
    public Media? Icon { get; }

    /// <summary>
    /// Guild splash.
    /// </summary>
    public Media? Splash { get; }

    /// <summary>
    /// Guild discovery splash.
    /// </summary>
    public Media? DiscoverySplash { get; }

    /// <summary>
    /// Custom guild emojis.
    /// </summary>
    public IReadOnlySet<Emoji> Emojis => _emojis;
    [JsonProperty("emojis")] private HashSet<Emoji> _emojis = [];

    /// <summary>
    /// Enabled guild features.
    /// </summary>
    public IReadOnlySet<GuildFeature> Features { get; }

    /// <summary>
    /// Approximate number of members in this guild.
    /// </summary>
    [JsonProperty("approximate_member_count")]
    public int ApproximateMemberCount { get; init; }

    /// <summary>
    /// Approximate number of non-offline members in this guild.
    /// </summary>
    [JsonProperty("approximate_presence_count")]
    public int ApproximatePresenceCount { get; init; }

    /// <summary>
    /// The description of a guild.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Custom guild stickers.
    /// </summary>
    public IReadOnlySet<GuildSticker> Stickers => _stickers;
    [JsonProperty("stickers")] private HashSet<GuildSticker> _stickers = [];

    [JsonConstructor]
    private GuildPreview(ulong id, HashSet<string> features, string? icon, string? splash, string? discovery_splash)
    {
        Id = id;
        Features = Guild.ParseFeatures(features);
        if (icon != null)
            Icon = new Media(icon, $"/icons/{id}/{icon}");
        if (splash != null)
            Splash = new Media(splash, $"/splashes/{id}/{splash}");
        if (discovery_splash != null)
            DiscoverySplash = new Media(discovery_splash, $"/discovery-splashes/{id}/{discovery_splash}");
    }
}

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
    public required string Name { get; init; }

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
    public Bot Bot { get; internal set; }
    
    /// <summary>
    /// Guild the event belongs to.
    /// </summary>
    public Guild? Guild => Bot.GetGuild(GuildId);
    
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

    /// <summary>
    /// Starts the scheduled event.
    /// </summary>
    /// <param name="reason">The reason for starting the scheduled event. This is displayed in the audit-log.</param>
    /// <returns>The updated scheduled event.</returns>
    public async Task<ScheduledEvent> StartAsync(string? reason = null)
    {
        if (Status == ScheduledEventStatus.Scheduled)
            return await Bot!._rest.ModifyGuildScheduledEventAsync(GuildId, Id,
            new JSON { { "status", ScheduledEventStatus.Active } }, reason);
        return this;
    }
    
    /// <summary>
    /// Cancels/ends the scheduled event.
    /// </summary>
    /// <param name="reason">The reason for canceling/ending the scheduled event. This is displayed in the audit-log.</param>
    /// <returns>The updated scheduled event.</returns>
    public async Task<ScheduledEvent> StopAsync(string? reason = null)
    {
        return Status switch
        {
            ScheduledEventStatus.Active => await Bot._rest.ModifyGuildScheduledEventAsync(GuildId, Id,
                new JSON { { "status", ScheduledEventStatus.Completed } }, reason),
            ScheduledEventStatus.Scheduled => await Bot._rest.ModifyGuildScheduledEventAsync(GuildId, Id,
                new JSON { { "status", ScheduledEventStatus.Canceled } }, reason),
            _ => this
        };
    }

    /// <summary>
    /// Edit the scheduled event.
    /// </summary>
    /// <param name="edit">A scheduled event edit instance.</param>
    /// <param name="reason">The reason for editing the scheduled event. This is displayed in the audit-log.</param>
    /// <returns>The updated scheduled event.</returns>
    public async Task<ScheduledEvent> EditAsync(ScheduledEventEdit edit, string? reason = null) =>
        await Bot!._rest.ModifyGuildScheduledEventAsync(GuildId, Id, edit._payload, reason);

    /// <summary>
    /// Delete the scheduled event.
    /// </summary>
    public async Task DeleteAsync()
    {
        await Bot._rest.DeleteGuildScheduledEventAsync(GuildId, Id);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="amount">Amount of users to return (max 100 yielded per loop), or <c>null</c> for all users.</param>
    /// <param name="before">A user ID. Considers only users before the given ID.</param>
    /// <param name="after">A user ID. Considers only users after the given ID.</param>
    /// <returns></returns>
    /// <exception cref="DiscordException">Only one parameter (<paramref name="before"/> or <paramref name="after"/>)
    /// can be used, not both.</exception>
    public async IAsyncEnumerable<List<User>> UsersAsync(int? amount = 100, ulong? before = null, ulong? after = null)
    {
        // TODO:
        // This method has not been fully tested due to not having access to a guild with more than 100 users who
        // are interested in a scheduled event.
        
        if (before is not null && after is not null)
            throw new DiscordException("Either parameter 'before' or 'after' can be used, not both.");
        
        const int indefinite = -1;
        var remaining = amount ?? indefinite;
        
        var hasMore = true;
        var yielded = new List<User>();
        
        do
        {
            yielded.Clear();
            var requestAmount = remaining == indefinite ? 100 : Math.Min(remaining, 100);
            var users = await Bot._rest.GetGuildScheduledEventUsers(requestAmount, GuildId, Id, before, after);
            if (users.Count < 100)
                hasMore = false;

            foreach (var user in users)
            {
                yielded.Add(user);
                if (remaining == indefinite) continue;

                remaining -= 1;
                if (remaining == 0)
                    hasMore = false;
            }

            before = users.LastOrDefault()?.Id ?? 0;
            after = before;
            yield return yielded;
        } while (hasMore);
    }
}

/// <summary>
/// Represents the values that can be edited for a <see cref="ScheduledEvent"/>. 
/// </summary>
public struct ScheduledEventEdit
{
    internal JSON _payload = [];
    
    /// <summary>
    /// Initializes a new scheduled event edit instance.
    /// </summary>
    public ScheduledEventEdit() { }
    
    /// <summary>
    /// Set the event channel ID.
    /// </summary>
    /// <param name="id">Channel ID, or <c>null</c> to remove it.</param>
    /// <returns>The edit instance.</returns>
    public ScheduledEventEdit SetChannelId(ulong? id)
    {
        _payload["channel_id"] = id;
        return this;
    }

    /// <summary>
    /// Set the event location.
    /// </summary>
    /// <param name="location">Location name, or <c>null</c> to remove it.</param>
    /// <returns>The edit instance.</returns>
    public ScheduledEventEdit SetLocation(string? location)
    {
        if (location != null)
            _payload["entity_metadata"] = new JSON { { "location", location } };
        else
            _payload["entity_metadata"] = null;
        return this;
    }
    
    /// <summary>
    /// Set the event name.
    /// </summary>
    /// <param name="name">Event name.</param>
    /// <returns>The edit instance.</returns>
    public ScheduledEventEdit SetName(string name)
    {
        _payload["name"] = name;
        return this;
    }
    
    /// <summary>
    /// Set the event start time.
    /// </summary>
    /// <param name="startTime">Start time for the event.</param>
    /// <returns>The edit instance.</returns>
    public ScheduledEventEdit SetScheduledStartTime(DateTime startTime)
    {
        _payload["scheduled_start_time"] = startTime.ToUniversalTime();
        return this;
    }
    
    /// <summary>
    /// Set the event end time.
    /// </summary>
    /// <param name="endTime">End time for the event.</param>
    /// <returns>The edit instance.</returns>
    public ScheduledEventEdit SetScheduledEndTime(DateTime endTime)
    {
        _payload["scheduled_end_time"] = endTime.ToUniversalTime();
        return this;
    }
    
    /// <summary>
    /// Set the event description.
    /// </summary>
    /// <param name="description">Event description, or <c>null</c> to remove it.</param>
    /// <returns>The edit instance.</returns>
    public ScheduledEventEdit SetDescription(string? description)
    {
        _payload["description"] = description;
        return this;
    }
    
    /// <summary>
    /// If setting the entity type to <see cref="ScheduledEventEntityType.External"/>, the following is <b>required</b>:
    /// <list type="bullet">
    ///     <item>Channel ID must be set to <c>null</c></item>
    ///     <item>Location must be set</item>
    ///     <item>Scheduled end time must be set</item>
    /// </list>
    /// </summary>
    /// <param name="type">Entity type.</param>
    /// <returns>The edit instance.</returns>
    public ScheduledEventEdit SetEntityType(ScheduledEventEntityType type)
    {
        _payload["entity_type"] = type;
        return this;
    }
    
    /// <summary>
    /// Set the event image.
    /// </summary>
    /// <param name="image">Event image, or <c>null</c> to remove it.</param>
    /// <returns>The edit instance.</returns>
    public ScheduledEventEdit SetImage(DFile? image)
    {
        // NOTE:
        // Discord does not list this as a nullable type, only as optional. But it makes no sense that you could set
        // an image but not remove it. I tested setting it as null, which successfully removed the image, so I'm not sure
        // why its documented that way.
        _payload["image"] = image?._mimeTypeBase64;
        return this;
    }
    
    /// <summary>
    /// Set the recurrence rule.
    /// </summary>
    /// <param name="rule">The recurrence rule, or <c>null</c> to remove it.</param>
    /// <returns>The edit instance.</returns>
    public ScheduledEventEdit SetRecurrenceRule(RecurrenceRule? rule)
    {
        _payload["recurrence_rule"] = rule;
        return this;
    }
}

/// <summary>
/// Represents a <see cref="Guild"/>'s <see cref="ScheduledEvent"/> recurrence rule.
/// </summary>
public record RecurrenceRule
{
    // DOCS: https://discord.com/developers/docs/resources/guild-scheduled-event#guild-scheduled-event-recurrence-rule-object
    
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

    /// <summary>
    /// Initializes a default recurrence rule as follows:
    /// <list type="bullet">
    ///     <item>Start: in 1 hour</item>
    ///     <item>Frequency: weekly</item>
    ///     <item>Interval: once per week</item>
    /// </list>
    /// </summary>
    public RecurrenceRule()
    {
        Start = DateTime.UtcNow.AddHours(1);
        Frequency = RecurrenceRuleFrequency.Weekly;
        Interval = 1;
    }
    
    /// <summary>
    /// Initializes a recurrence rule.
    /// </summary>
    /// <param name="start">Starting time of the recurrence interval.</param>
    /// <param name="frequency">How often the event occurs.</param>
    /// <param name="interval">The spacing between the events, defined by <see cref="Frequency"/>.</param>
    public RecurrenceRule(DateTime start, RecurrenceRuleFrequency frequency, int interval)
    {
        Start = start;
        Frequency = frequency;
        Interval = interval;
    }
}

/// <summary>
/// Represents a frequency for <see cref="RecurrenceRule"/>.
/// </summary>
public enum RecurrenceRuleFrequency
{
    // DOCS: https://discord.com/developers/docs/resources/guild-scheduled-event#guild-scheduled-event-recurrence-rule-object-guild-scheduled-event-recurrence-rule-frequency
    
    Yearly,
    Monthly,
    Weekly,
    Daily
}

/// <inheritdoc cref="RecurrenceRuleFrequency"/>
public enum RecurrenceRuleFrequencyWeekday
{
    // DOCS: https://discord.com/developers/docs/resources/guild-scheduled-event#guild-scheduled-event-recurrence-rule-object-guild-scheduled-event-recurrence-rule-weekday
    
    Monday,
    Tuesday,
    Wednesday,
    Thursday,
    Friday,
    Saturday,
    Sunday
}

/// <inheritdoc cref="RecurrenceRuleFrequency"/>
public enum RecurrenceRuleFrequencyMonth
{
    // DOCS: https://discord.com/developers/docs/resources/guild-scheduled-event#guild-scheduled-event-recurrence-rule-object-guild-scheduled-event-recurrence-rule-month
    
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

/// <inheritdoc cref="RecurrenceRuleFrequency"/>
public record RecurrenceRuleFrequencyNWeekday
{
    // DOCS: https://discord.com/developers/docs/resources/guild-scheduled-event#guild-scheduled-event-recurrence-rule-object-guild-scheduled-event-recurrence-rule-nweekday-structure
    
    /// <summary>
    /// The week to reoccur on (1 - 5).
    /// </summary>
    [JsonProperty("n")]
    public int N { get; init; }
    
    /// <summary>
    /// The day within the week to reoccur on.
    /// </summary>
    [JsonProperty("day")]
    public RecurrenceRuleFrequencyWeekday Day { get; init; }
    
    private RecurrenceRuleFrequencyNWeekday() { }
}

/// <summary>
/// Represents incidents that occurred in a <see cref="Guild"/>.
/// </summary>
public record Incidents
{
    // DOCS: https://discord.com/developers/docs/resources/guild#incidents-data-object
    
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
    
    private Incidents() { }
}

/// <summary>
/// Represents a <see cref="Guild"/> welcome screen.
/// </summary>
public record WelcomeScreen
{
    // DOCS: https://discord.com/developers/docs/resources/guild#welcome-screen-object
    
    /// <summary>
    /// Guild description shown in the welcome screen.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; init; }
    
    /// <summary>
    /// The channels shown in the welcome screen.
    /// </summary>
    [JsonProperty("welcome_channels")]
    public required IReadOnlyCollection<WelcomeScreenChannel> Channels { get; init; }
    
    private WelcomeScreen() { }
}

/// <summary>
/// Represents a <see cref="WelcomeScreen"/> channel.
/// </summary>
public record WelcomeScreenChannel
{
    // DOCS: https://discord.com/developers/docs/resources/guild#welcome-screen-object-welcome-screen-channel-structure
    
    /// <summary>
    /// Channel ID.
    /// </summary>
    [JsonProperty("channel_id")]
    public ulong ChannelId { get; init; }

    /// <summary>
    /// Channel description.
    /// </summary>
    [JsonProperty("description")]
    public required string Description { get; init; }
    
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
    
    private WelcomeScreenChannel() { }
}

/// <summary>
/// Represents the status of a <see cref="ScheduledEvent"/>.
/// </summary>    
public enum ScheduledEventStatus
{
    // DOCS: https://discord.com/developers/docs/resources/guild-scheduled-event#guild-scheduled-event-object-guild-scheduled-event-status
    
    Scheduled = 1,
    Active,
    Completed,
    Canceled
}

/// <summary>
/// Represents the type of the <see cref="ScheduledEvent"/>.
/// </summary>    
public enum ScheduledEventEntityType
{
    // DOCS: https://discord.com/developers/docs/resources/guild-scheduled-event#guild-scheduled-event-object-guild-scheduled-event-entity-types
    
    StageInstance = 1,
    Voice,
    External
}

/// <summary>
/// Represents the privacy level of a <see cref="ScheduledEvent"/>.
/// </summary>
public enum ScheduledEventPrivacyLevel
{
    // DOCS: https://discord.com/developers/docs/resources/guild-scheduled-event#guild-scheduled-event-object-guild-scheduled-event-privacy-level
    
    GuildOnly = 2
}

/// <summary>
/// Represents the verification level of a <see cref="Guild"/>.
/// </summary>
public enum GuildVerificationLevel
{
    // DOCS: https://discord.com/developers/docs/resources/guild#guild-object-verification-level
    
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
    // DOCS: https://discord.com/developers/docs/resources/guild#guild-object-default-message-notification-level
    
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
    // DOCS: https://discord.com/developers/docs/resources/guild#guild-object-explicit-content-filter-level
    
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
    // DOCS: https://discord.com/developers/docs/resources/guild#guild-object-mfa-level
    
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
    // DOCS: https://discord.com/developers/docs/resources/guild#guild-object-guild-nsfw-level
    
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
    // DOCS: https://discord.com/developers/docs/resources/guild#guild-object-premium-tier
    
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
    // DOCS: https://discord.com/developers/docs/resources/guild#guild-object-system-channel-flags
    
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
    // DOCS: https://discord.com/developers/docs/resources/guild#guild-object-guild-features
    
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
    WelcomeScreenEnabled,
    
    /// <summary>
    /// Guild has access to guest invites.
    /// </summary>
    [Description("GUESTS_ENABLED")]
    GuestsEnabled,
    
    /// <summary>
    /// Guild has access to set guild tags.
    /// </summary>
    [Description("GUILD_TAGS")]
    GuildTags,
    
    /// <summary>
    /// Guild is able to set gradient colors to roles.
    /// </summary>
    [Description("ENHANCED_ROLE_COLORS")]
    EnhancedRoleColors
}
