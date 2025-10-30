using Newtonsoft.Json;

namespace Discord.Models;

/// <summary>
/// Represents a code that when used, adds a user to a guild.
/// </summary>
public class Invite
{
    // DOCS: https://discord.com/developers/docs/resources/invite#invite-object
    
    /// <summary>
    /// The type of invite.
    /// </summary>
    [JsonProperty("type")]
    public InviteType Type { get; init; }
    
    /// <summary>
    /// The invite code (unique ID).
    /// </summary>
    [JsonProperty("code")]
    public required string Code { get; init; }
    
    /// <summary>
    /// Guild this invite is for.
    /// </summary>
    [JsonProperty("guild")]
    public PartialInviteGuild? Guild { get; init; }
    
    /// <summary>
    /// Channel this invite is for.
    /// </summary>
    [JsonProperty("channel")]
    public PartialInviteChannel? Channel { get; init; }
    
    /// <summary>
    /// User who created the invite.
    /// </summary>
    [JsonProperty("inviter")]
    public User? Inviter { get; init; }
    
    /// <summary>
    /// The type of target for this voice channel invite.
    /// </summary>
    [JsonProperty("target_type")]
    public InviteTargetType? TargetType { get; init; }
    
    /// <summary>
    /// Approximate count of online members. This will always be <c>null</c> unless called via <see cref="Bot.RequestInviteAsync"/>
    /// and the proper values are set.
    /// </summary>
    [JsonProperty("approximate_presence_count")]
    public int? ApproximatePresenceCount { get; init; }
    
    /// <summary>
    /// Approximate count of total members. This will always be <c>null</c> unless called via <see cref="Bot.RequestInviteAsync"/>
    /// and the proper values are set.
    /// </summary>
    [JsonProperty("approximate_member_count")]
    public int? ApproximateMemberCount { get; init; }
    
    /// <summary>
    /// The expiration date of this invite.
    /// </summary>
    [JsonProperty("expires_at")]
    public DateTime? ExpiresAt { get; init; }
    
    /// <summary>
    /// Guild scheduled event data associated with the invite. This will always be <c>null</c> unless called via <see cref="Bot.RequestInviteAsync"/>
    /// and the proper values are set.
    /// </summary>
    [JsonProperty("guild_scheduled_event")]
    public ScheduledEvent? GuildScheduledEvent { get; init; }
    
    /// <summary>
    /// Guild invite flags for guild invites.
    /// </summary>
    [JsonProperty("flags")]
    public InviteFlags? Flags { get; init; }

    #region METADATA

    /// <summary>
    /// Number of times this invite has been used.
    /// </summary>
    [JsonProperty("uses")]
    public int Uses { get; init; }
    
    /// <summary>
    /// Max number of times this invite can be used. A value of zero represents an infinite amount of uses.
    /// </summary>
    [JsonProperty("max_uses")]
    public int MaxUses { get; init; }
    
    /// <summary>
    /// Duration (in seconds) after which the invite expires.
    /// </summary>
    [JsonProperty("max_age")]
    public int MaxAge { get; init; }
    
    /// <summary>
    /// Whether this invite only grants temporary membership.
    /// </summary>
    [JsonProperty("temporary")]
    public bool IsTemporary { get; init; }
    
    /// <summary>
    /// When this invite was created.
    /// </summary>
    [JsonProperty("created_at")]
    public DateTime? CreatedAt { get; init; }

    #endregion

    #region CUSTOM
    
    /// <summary>
    /// The invite URL.
    /// </summary>
    public string Url => $"https://discord.gg/{Code}";
    
    /// <summary>
    /// Your bot instance.
    /// </summary>
    public Bot Bot { get; internal set; }

    #endregion
    
    private Invite() { }

    /// <summary>
    /// Delete the invite.
    /// </summary>
    /// <param name="reason">The reason for deleting the invite. This is displayed in the audit-log.</param>
    /// <remarks>Requires <see cref="Permission.ManageChannels"/> on the channel this invite belongs to, or
    /// <see cref="Permission.ManageGuild"/> to remove any invite across the guild.</remarks>
    public async Task DeleteAsync(string? reason = null)
    {
        await Bot._rest.DeleteInviteAsync(Code, reason);
    }
}

/// <summary>
/// Represents an <see cref="Invite"/> type.
/// </summary>
public enum InviteType
{
    // DOCS: https://discord.com/developers/docs/resources/invite#invite-object-invite-types
    
    Guild,
    GroupDm,
    Friend
}

/// <summary>
/// Represents an <see cref="Invite"/> target.
/// </summary>
public enum InviteTargetType
{
    // DOCS: https://discord.com/developers/docs/resources/invite#invite-object-invite-target-types
    
    Stream = 1,
    EmbeddedApplication
}

/// <summary>
/// Represents flags for an <see cref="Invite"/>.
/// </summary>
[Flags]
public enum InviteFlags
{
    // DOCS: https://discord.com/developers/docs/resources/invite#invite-object-guild-invite-flags
    
    /// <summary>
    /// This invite is a guest invite for a voice channel.
    /// </summary>
    IsGuestInvite = 1 << 0,
}

/// <summary>
/// Represents the partial guild information with an <see cref="Invite"/>.
/// </summary>
public record PartialInviteGuild
{
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
    [JsonProperty("splash")] private string? _splash;
    
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
    /// Description of the guild.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; init; }
    
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
    /// Enabled guild features.
    /// </summary>
    public IReadOnlyCollection<GuildFeature> Features => Guild.ParseFeatures(_features);
    [JsonProperty("features")] internal List<string> _features = [];
    
    /// <summary>
    /// Verification level required for the guild.
    /// </summary>
    [JsonProperty("verification_level")]
    public GuildVerificationLevel VerificationLevel { get; init; }
    
    /// <summary>
    /// The vanity URL code for the guild.
    /// </summary>
    [JsonProperty("vanity_url_code")]
    public string? VanityUrlCode { get; init; }
    
    /// <summary>
    /// Number of boosts the guild currently has.
    /// </summary>
    [JsonProperty("premium_subscription_count")]
    public int PremiumSubscriptionCount { get; init; }
    
    /// <summary>
    /// Guild NSFW level.
    /// </summary>
    [JsonProperty("nsfw_level")]
    public GuildNsfwLevel NsfwLevel { get; init; }
    
    /// <summary>
    /// The welcome screen of a Community guild, shown to new members.
    /// </summary>
    [JsonProperty("welcome_screen")]
    public WelcomeScreen? WelcomeScreen { get; init; }
    
    private PartialInviteGuild() { }
}

/// <summary>
/// Represents a channel associated with an <see cref="Invite"/>.
/// </summary>
public record PartialInviteChannel
{
    /// <summary>
    /// Channel ID.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; init; }
    
    /// <summary>
    /// Channel name.
    /// </summary>
    [JsonProperty("name")]
    public required string Name { get; init; }
    
    /// <summary>
    /// Channel type.
    /// </summary>
    [JsonProperty("type")]
    public ChannelType Type { get; init; }
    
    private PartialInviteChannel() { }
}
