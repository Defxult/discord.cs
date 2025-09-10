using Discord.Net;
using Newtonsoft.Json;

namespace Discord.Models;

/// <summary>
/// Represents a Discord guild member.
/// </summary>
public class Member : IEquatable<Member>
{
    /// <summary>
    /// The member's ID. This is a shortcut that is accessing the value from the <see cref="User"/> property. If said
    /// property is <c>null</c> this will also be.
    /// </summary>
    public ulong? Id => User?.Id;

    /// <summary>
    /// User object for the member. Contains information such as their ID, username, avatar, etc.
    /// </summary>
    [JsonProperty("user")]
    public User? User { get; internal set; }

    /// <summary>
    /// The member's guild nickname.
    /// </summary>
    [JsonProperty("nick")]
    public string? Nickname { get; internal set; }

    /// <summary>
    /// The member's guild avatar. For their user avatar, use <see cref="Models.User.Avatar"/>.
    /// </summary>
    // public Media? Avatar
    // {
    //     get
    //     {
    //         if (_avatarHash is { } hash)
    //             return new Media(hash, $"/guilds/{Guild.Id}/users/{Id}/avatars/{hash}");
    //         return null;
    //     }
    // }
    // [JsonProperty("avatar")]
    // private string? _avatarHash;

    /// <summary>
    /// All roles applied to the member.
    /// </summary>
    // public IReadOnlyList<Role> Roles
    // {
    //     get
    //     {
    //         List<Role> roles = [];
    //         foreach (var roleId in _roleIds)
    //             if (Guild.GetRole(roleId) is { } r)
    //                 roles.Add(r);
    //         return roles;
    //     }
    // }
    // [JsonProperty("roles")]
    // private ulong[] _roleIds = [];

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
    /// The member flags. Contains information such as <see cref="MemberFlags.DidRejoin"/> and more.
    /// </summary>
    public IReadOnlySet<MemberFlags> Flags => _flags;
    private HashSet<MemberFlags> _flags;

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

    #region API Separated

    /// <summary>
    /// The guild this member belongs to.
    /// </summary>
    // public Guild Guild { get; internal set; } // This is initialized in GUILD_CREATE/`SetBotAndGuild()`.

    #endregion

    [JsonConstructor]
    internal Member(int flags, ulong? permissions)
    {
        _flags = GetMemberFlags(flags);
        if (permissions is { } value)
            Permissions = new Permissions(value);
    }
    
    public override bool Equals(object? other) => other is Member member && Equals(member);
    public bool Equals(Member? other) => Id == other?.Id;
    public override int GetHashCode() => Id.GetHashCode();

    #region Public

    /// <summary>
    /// Create a direct message channel.
    /// </summary>
    /// <returns>A channel that's capable of sending the user a private message.</returns>
    // public async Task<DmChannel> CreateDmAsync() =>
    //     await Guild._rest.CreatDmAsync(Id!.Value);

    #endregion

    #region Private
    
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