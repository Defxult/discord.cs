using Discord.Net;
using Discord.Utility;
using Newtonsoft.Json;

namespace Discord.Models;

/// <summary>
/// Represents a Discord guild member.
/// </summary>
public class Member : IEquatable<Member>
{
    /// <summary>
    /// User object for the member. Contains information such as their ID, username, avatar, etc.
    /// </summary>
    [JsonProperty("user")]
    public User User { get; internal set; }

    /// <summary>
    /// The user's guild nickname.
    /// </summary>
    [JsonProperty("nick")]
    public string? Nickname { get; internal set; }

    /// <summary>
    /// The user's avatar specific to this guild. For their global avatar, use <see cref="Models.User.Avatar"/>.
    /// </summary>
    public Media? Avatar
    {
        get
        {
            if (_avatarHash is { } hash)
                return new Media(hash, $"/guilds/{GuildId}/users/{Id}/avatars/{hash}");
            return null;
        }
    }
    [JsonProperty("avatar")] private string? _avatarHash;

    /// <summary>
    /// All roles applied to the member.
    /// </summary>
    public IReadOnlyCollection<Role> Roles
    {
        get
        {
            List<Role> roles = [];
            foreach (var roleId in _roleIds)
                if (Bot.GetGuild(GuildId)?.GetRole(roleId) is { } r)
                    roles.Add(r);
            return roles;
        }
    }
    [JsonProperty("roles")] private ulong[] _roleIds = [];

    /// <summary>
    /// When the member joined the guild.
    /// </summary>
    [JsonProperty("joined_at")]
    public DateTime JoinedAt { get; init; }

    /// <summary>
    /// When the member started boosting the guild.
    /// </summary>
    [JsonProperty("premium_since")]
    public DateTime? PremiumSince { get; init; }
    
    /// <summary>
    /// Whether the member is deafened in voice channels.
    /// </summary>
    [JsonProperty("deaf")]
    public bool IsDeafened { get; internal set; }
    
    /// <summary>
    /// Whether the member is muted in voice channels.
    /// </summary>
    [JsonProperty("mute")]
    public bool IsMuted { get; internal set; }

    /// <summary>
    /// The member flags. Contains information such as <see cref="MemberFlags.DidRejoin"/> and more.
    /// </summary>
    public IReadOnlySet<MemberFlags> Flags { get; internal set; }
    
    /// <summary>
    /// Whether the member has not yet passed the guild's Membership Screening requirements.
    /// </summary>
    [JsonProperty("pending")]
    public bool IsPending { get; internal set; }

    /// <summary>
    /// Permissions of the member in the channel, including overwrites. Only available when in the interaction object. TODO >>> Correct documentation
    /// </summary>
    public Permissions? Permissions { get; internal set; }

    /// <summary>
    /// When the members timeout will expire and will be able to communicate in the guild again. If <c>null</c>, the member is not timed out.
    /// </summary>
    [JsonProperty("communication_disabled_until")]
    public DateTime? TimedOutUntil { get; internal set; }

    /// <summary>
    /// The member's guild avatar decoration.
    /// </summary>
    public Media? AvatarDecoration
    {
        get
        {
            if (_avatarDecorationData is not { } data) return null;
            var hash = data["asset"].ToString()!; 
            
            // Avatar decorations are a little different. Usually the Media class would automatically assign the file
            // type (.png or .gif) based on whether the hash starts with "a_". But with avatar decorations they will
            // always be .png
            var media = new Media(hash, $"/avatar-decoration-presets/{hash}");
            media.Url = media.Url.Replace(".gif", ".png");
            return media;
        }
    }
    [JsonProperty("avatar_decoration_data")] Dictionary<string, object>? _avatarDecorationData;

    #region CUSTOM
    
    /// <summary>
    /// Your bot instance.
    /// </summary>
    public Bot Bot {  get; internal set; }
    
    /// <summary>
    /// The member's ID. This is a shortcut that is accessing the value from the <see cref="User"/> property.
    /// </summary>
    public ulong Id => User.Id;
    
    /// <summary>
    /// The member's name. This is a shortcut that is accessing the value from the <see cref="User"/> property.
    /// </summary>
    public string Name => User.Name;
    
    /// <summary>
    /// ID of the guild this member belongs to.
    /// </summary>
    public ulong GuildId { get; internal set; }

    #endregion

    [JsonConstructor]
    internal Member(int flags, ulong? permissions)
    {
        Flags = GetMemberFlags(flags);
        if (permissions is { } value)
            Permissions = new Permissions(value);
    }
    
    public override bool Equals(object? other) => other is Member member && Equals(member);
    public bool Equals(Member? other) => Id == other?.Id;
    public override int GetHashCode() => Id.GetHashCode();

    #region PUBLIC

    /// <summary>
    /// Create a direct message channel.
    /// </summary>
    /// <returns>A channel that's capable of sending the user a private message.</returns>
    // public async Task<DmChannel> CreateDmAsync() =>
    //     await Guild._rest.CreatDmAsync(Id!.Value);

    #endregion

    #region PRIVATE
    
    // Updates the member object with the new data from ON_MEMBER_UPDATE.
    internal void Update(JSON payload)
    {
        throw new NotImplementedException();
    }

    private static HashSet<MemberFlags> GetMemberFlags(int value)
    {
        HashSet<MemberFlags> flags = [];
        foreach (MemberFlags f in Enum.GetValues(typeof(MemberFlags)))
            if ((value & (int)f) == (int)f)
                flags.Add(f);
        return flags;
    }

    #endregion
}

/// <summary>
/// Represents a <see cref="Member"/>s flags.
/// </summary>
public enum MemberFlags
{
    /// <summary>
    /// Member has left and rejoined the guild.
    /// </summary>
    DidRejoin = 1 << 0,

    /// <summary>
    /// Member has completed onboarding.
    /// </summary>
    CompletedOnboarding = 1 << 1,

    /// <summary>
    /// Member is exempt from guild verification requirements.
    /// </summary>
    BypassesVerification = 1 << 2,

    /// <summary>
    /// Member has started onboarding.
    /// </summary>
    StartedOnboarding = 1 << 3
}