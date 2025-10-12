using Discord.Utility;
using Newtonsoft.Json;
using System.ComponentModel;

namespace Discord.Models;

/// <summary>
/// Represents a Discord user.
/// </summary>
public class User : IEquatable<User>
{
    /// <summary>
    /// User ID.
    /// </summary>
    public ulong Id { get; }
    
    /// <summary>
    /// The user's username.
    /// </summary>
    [JsonProperty("username")]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The user's Discord-tag.
    /// </summary>
    [JsonProperty("discriminator")]
    public string Discriminator { get; private set; } = string.Empty;

    /// <summary>
    /// The user's display name if set. For bots, this is the application name.
    /// </summary>
    [JsonProperty("global_name")]
    public string? DisplayName { get; private set; }

    /// <summary>
    /// The user's avatar.
    /// </summary>
    public Media? Avatar { get; private set; }

    /// <summary>
    /// Whether the user is a bot.
    /// </summary>
    [JsonProperty("bot")]
    public bool IsBot { get; init; }

    /// <summary>
    /// Whether the user is an Official Discord System user (part of the urgent message system).
    /// </summary>
    [JsonProperty("system")]
    public bool IsSystem { get; init; }

    /// <summary>
    /// The user's banner.
    /// </summary>
    [JsonIgnore]
    public Media? Banner { get; private set; }

    /// <summary>
    /// The user's banner color.
    /// </summary>
    [JsonIgnore]
    public Color? AccentColor { get; private set; }

    /// <summary>
    /// The public flags on a user's account.
    /// </summary>
    [JsonIgnore]
    public readonly List<UserFlag> Flags;

    /// <summary>
    /// The user's avatar decoration.
    /// </summary>
    [JsonIgnore]
    public readonly Media? AvatarDecoration;

    #region API Separated

    /// <summary>
    /// Mention the user.
    /// </summary>
    public string Mention => Markdown.MentionUser(Id);

    #endregion

    [JsonConstructor]
    internal User(ulong id, string? avatar, string? banner, int? accent_color, string? avatar_decoration, int flags)
    {
        Id = id;
        if (avatar != null) Avatar = new Media(avatar, $"/avatars/{id}/{avatar}");
        if (banner != null) Banner = new Media(banner, $"/banners/{id}/{banner}");
        if (accent_color != null) AccentColor = new Color((int)accent_color);
        if (avatar_decoration != null) AvatarDecoration = new Media(avatar_decoration, $"/avatar-decorations/{id}/{avatar_decoration}");
        Flags = Util.FromBitfield<UserFlag>(flags);
    }
    
    public override bool Equals(object? other) => other is User user && Equals(user);
    public bool Equals(User? other) => Id == other?.Id;
    public override int GetHashCode() => Id.GetHashCode();

    #region Public
    
    /// <summary>
    /// Returns their username. If the user is a bot, its discriminator is included.
    /// </summary>
    public override string ToString() => IsBot ? $"{Name}#{Discriminator}" : Name;
    
    #endregion
}

/// <summary>
/// Represents a bots user object.
/// </summary>
public class ClientUser : User
{
    /// <summary>
    /// Whether the user has two factor enabled on their account.
    /// </summary>
    [JsonProperty("mfa_enabled")]
    public bool IsMfaEnabled { get; }

    /// <summary>
    /// The user's chosen language option.
    /// </summary>
    [JsonIgnore]
    public Locale? Locale { get; }

    /// <summary>
    /// Whether the email on this account has been verified.
    /// </summary>
    [JsonProperty("verified")]
    public bool IsVerified { get; }

    [JsonConstructor]
    internal ClientUser(
        ulong id, 
        string? avatar, 
        string? banner, 
        int? accent_color, 
        string? avatar_decoration, 
        int flags, 
        string? locale) : base(id, avatar, banner, accent_color, avatar_decoration, flags)
    {
        if (locale == null) return;
        foreach (Locale loc in Enum.GetValues(typeof(Locale)))
        {
            if (loc.GetDescription() == locale) Locale = loc; break;
        }
    }
}


/// <summary>
/// Represents the public flags on a <see cref="User"/>'s account.
/// </summary>
public enum UserFlag
{
    /// <summary>
    /// Discord Employee
    /// </summary>
    Staff = 1 << 0,

    /// <summary>
    /// Partnered Server Owner.
    /// </summary>
    Partner = 1 << 1,

    /// <summary>
    /// HypeSquad Events Member.
    /// </summary>
    HypeSquad = 1 << 2,

    /// <summary>
    /// Bug Hunter Level 1.
    /// </summary>
    BugHunterLevel1 = 1 << 3,

    /// <summary>
    /// House Bravery Member.
    /// </summary>
    HypeSquadOnlineHouse1 = 1 << 6,

    /// <summary>
    /// House Brilliance Member.
    /// </summary>
    HypeSquadOnlineHouse2 = 1 << 7,

    /// <summary>
    /// House Balance Member.
    /// </summary>
    HypeSquadOnlineHouse3 = 1 << 8,

    /// <summary>
    /// Early Nitro Supporter.
    /// </summary>
    PremiumEarlySupporter = 1 << 9,

    /// <summary>
    /// User is a <seealso href="https://discord.com/developers/docs/topics/teams">team</seealso>.
    /// </summary>
    TeamPseudoUser = 1 << 10,

    /// <summary>
    /// Bug Hunter Level 2.
    /// </summary>
    BugHunterLevel2 = 1 << 14,

    /// <summary>
    /// Verified Bot.
    /// </summary>
    VerifiedBot = 1 << 16,

    /// <summary>
    /// Early Verified Bot Developer.
    /// </summary>
    VerifiedDeveloper = 1 << 17,

    /// <summary>
    /// Moderator Programs Alumni.
    /// </summary>
    CertifiedModerator = 1 << 18,

    /// <summary>
    /// Bot uses only <seealso href="https://discord.com/developers/docs/interactions/receiving-and-responding#receiving-an-interaction">HTTP interactions</seealso> and is shown in the online member list.
    /// </summary>
    BotHttpInteractions = 1 << 19,

    /// <summary>
    /// User is an <seealso href="https://support-dev.discord.com/hc/en-us/articles/10113997751447">Active Developer</seealso>.
    /// </summary>
    ActiveDeveloper = 1 << 22,
}

/// <summary>
/// Represents the level of premium a <see cref="User"/> has.
/// </summary>
public enum PremiumType
{
    None,
    NitroClassic,
    Nitro,
    NitroBasic
}

/// <summary>
/// Represents a Discord locale.
/// </summary>
public enum Locale
{
    /// <summary>
    /// Native name: Bahasa Indonesia
    /// </summary>
    [Description("id")]
    Indonesian,

    /// <summary>
    /// Native name: Dansk
    /// </summary>
    [Description("da")]
    Danish,

    /// <summary>
    /// Native name: Deutsch
    /// </summary>
    [Description("de")]
    German,

    /// <summary>
    /// Native name: English, UK
    /// </summary>
    [Description("en-GB")]
    EnglishUK,

    /// <summary>
    /// Native name: English, US
    /// </summary>
    [Description("en-US")]
    EnglishUS,

    /// <summary>
    /// Native name: Español
    /// </summary>
    [Description("es-ES")]
    Spanish,

    /// <summary>
    /// Native name: Français
    /// </summary>
    [Description("fr")]
    French,

    /// <summary>
    /// Native name: Hrvatski
    /// </summary>
    [Description("hr")]
    Croatian,

    /// <summary>
    /// Native name: Italiano
    /// </summary>
    [Description("it")]
    Italian,

    /// <summary>
    /// Native name: Lietuviškai
    /// </summary>
    [Description("lt")]
    Lithuanian,

    /// <summary>
    /// Native name: Magyar
    /// </summary>
    [Description("hu")]
    Hungarian,

    /// <summary>
    /// Native name: Nederlands
    /// </summary>
    [Description("nl")]
    Dutch,

    /// <summary>
    /// Native name: Norsk
    /// </summary>
    [Description("no")]
    Norwegian,

    /// <summary>
    /// Native name: Polski
    /// </summary>
    [Description("pl")]
    Polish,

    /// <summary>
    /// Native name: Português do Brasil
    /// </summary>
    [Description("pt-BR")]
    Portuguese,

    /// <summary>
    /// Native name: Română
    /// </summary>
    [Description("ro")]
    Romanian,

    /// <summary>
    /// Native name: Suomi
    /// </summary>
    [Description("fi")]
    Finnish,

    /// <summary>
    /// Native name: Svenska
    /// </summary>
    [Description("sv-SE")]
    Swedish,

    /// <summary>
    /// Native name: Tiếng Việt
    /// </summary>
    [Description("vi")]
    Vietnamese,

    /// <summary>
    /// Native name: Türkçe
    /// </summary>
    [Description("tr")]
    Turkish,

    /// <summary>
    /// Native name: Čeština
    /// </summary>
    [Description("cs")]
    Czech,

    /// <summary>
    /// Native name: Ελληνικά
    /// </summary>
    [Description("el")]
    Greek,

    /// <summary>
    /// Native name: български
    /// </summary>
    [Description("bg")]
    Bulgarian,

    /// <summary>
    /// Native name: Pусский
    /// </summary>
    [Description("ru")]
    Russian,

    /// <summary>
    /// Native name: Українська
    /// </summary>
    [Description("uk")]
    Ukrainian,

    /// <summary>
    /// Native name: हिन्दी
    /// </summary>
    [Description("hi")]
    Hindi,

    /// <summary>
    /// Native name: ไทย
    /// </summary>
    [Description("th")]
    Thai,

    /// <summary>
    /// Native name: 中文
    /// </summary>
    [Description("zh-CN")]
    ChineseChina,

    /// <summary>
    /// Native name: 日本語
    /// </summary>
    [Description("ja")]
    Japanese,

    /// <summary>
    /// Native name: 繁體中文
    /// </summary>
    [Description("zh-TW")]
    ChineseTaiwan,

    /// <summary>
    /// Native name: 한국어
    /// </summary>
    [Description("ko")]
    Korean
}