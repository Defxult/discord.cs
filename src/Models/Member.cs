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
    public Bot Bot { get; internal set; }
    
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
    /// Edit the member.
    /// </summary>
    /// <param name="edit">Member edit instance.</param>
    /// <param name="reason">The reason for editing the member. This is displayed in the audit-log.</param>
    /// <returns>The updated member.</returns>
    public async Task<Member> EditAsync(MemberEdit edit, string? reason = null) =>
        await Bot._rest.ModifyGuildMemberAsync(GuildId, Id, edit, reason);

    /// <summary>
    /// Edit the member associated with <see cref="Guild.Self"/>.
    /// </summary>
    /// <param name="edit">Bot member edit instance.</param>
    /// <param name="reason">The reason for editing the bot member. This is displayed in the audit-log.</param>
    /// <returns>The updated bot member.</returns>
    /// <exception cref="DiscordException">Member is not <see cref="Guild.Self"/>.</exception>
    public async Task<Member> EditAsync(BotMemberEdit edit, string? reason = null)
    {
        if (!Equals(Bot.GetGuild(GuildId)?.Self))
            throw new DiscordException("Invalid member edit - member is not bot member Self");
        return await Bot._rest.ModifyCurrentMemberAsync(GuildId, edit, reason);
    }

    /// <summary>
    /// Add roles to the member.
    /// </summary>
    /// <param name="roles">Roles to add.</param>
    /// <param name="reason">The reason for adding roles. This is displayed in the audit-log.</param>
    /// <remarks>Requires <see cref="Permission.ManageRoles"/>.</remarks>
    public async Task AddRolesAsync(IEnumerable<Role> roles, string? reason = null)
    {
        foreach (var role in roles)
            await Bot._rest.AddGuildMemberRoleAsync(GuildId, Id, role.Id, reason);
    }
    
    /// <summary>
    /// Remove roles from the member.
    /// </summary>
    /// <param name="roles">Roles to remove.</param>
    /// <param name="reason">The reason for removing roles. This is displayed in the audit-log.</param>
    /// <remarks>Requires <see cref="Permission.ManageRoles"/>.</remarks>
    public async Task RemoveRolesAsync(IEnumerable<Role> roles, string? reason = null)
    {
        foreach (var role in roles)
            await Bot._rest.RemoveGuildMemberRoleAsync(GuildId, Id, role.Id, reason);
    }

    /// <summary>
    /// Removes member from the guild.
    /// </summary>
    /// <param name="reason">The reason for removing member. This is displayed in the audit-log.</param>
    /// <remarks>Requires <see cref="Permission.KickMembers"/>.</remarks>
    public async Task KickAsync(string? reason = null)
    {
        await Bot._rest.RemoveGuildMemberAsync(GuildId, Id, reason);
    }

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
/// Represents the values that can be edited for the <see cref="Member"/> object associated with <see cref="Guild.Self"/>. 
/// </summary>
public record struct BotMemberEdit
{
    internal JSON _payload = [];
    
    /// <summary>
    /// Initializes a new bot member edit instance.
    /// </summary>
    public BotMemberEdit() {}
    
    /// <summary>
    /// Set the bot nickname.
    /// </summary>
    /// <param name="nickname">Nickname to set, or <c>null</c> to remove it.</param>
    /// <returns>The edit instance.</returns>
    /// <remarks>Requires <see cref="Permission.ChangeNickname"/>.</remarks>
    public BotMemberEdit SetNickname(string? nickname)
    {
        _payload["nick"] = nickname;
        return this;
    }
    
    /// <summary>
    /// Set the bot banner.
    /// </summary>
    /// <param name="banner">Banner to set, or <c>null</c> to remove it.</param>
    /// <returns>The edit instance.</returns>
    public BotMemberEdit SetBanner(DFile? banner)
    {
        _payload["banner"] = banner?._mimeTypeBase64;
        return this;
    }
    
    /// <summary>
    /// Set the bot avatar.
    /// </summary>
    /// <param name="avatar">Avatar to set, or <c>null</c> to remove it.</param>
    /// <returns>The edit instance.</returns>
    public BotMemberEdit SetAvatar(DFile? avatar)
    {
        _payload["avatar"] = avatar?._mimeTypeBase64;
        return this;
    }
    
    /// <summary>
    /// Set the bot bio.
    /// </summary>
    /// <param name="bio">Bio to set, or <c>null</c> to remove it.</param>
    /// <returns>The edit instance.</returns>
    /// <remarks> If the bots bio (aka description) is set in your <a href="https://discord.com/developers/applications">Discord developer portal</a>
    /// the value set in the portal will override this. For example, if you set <paramref name="bio"/> to <c>null</c>
    /// but the description is set in the portal, it will not be removed. You would have to go into your portal and manually
    /// remove it. If you'd like complete control over the bio via the API, leave the description empty in the portal so
    /// it can be updated from the library without the need for external changes.
    /// </remarks>
    public BotMemberEdit SetBio(string? bio)
    {
        _payload["bio"] = bio;
        return this;
    }
}

/// <summary>
/// Represents the values that can be edited for a <see cref="Member"/>. 
/// </summary>
public record struct MemberEdit
{
    internal JSON _payload = [];
    
    /// <summary>
    /// Initializes a new member edit instance.
    /// </summary>
    public MemberEdit() {}

    /// <summary>
    /// Set the members nickname.
    /// </summary>
    /// <param name="nickname">Nickname to set, or <c>null</c> to remove it.</param>
    /// <returns>The edit instance.</returns>
    /// <remarks>Requires <see cref="Permission.ManageNicknames"/>.</remarks>
    public MemberEdit SetNickname(string? nickname)
    {
        _payload["nick"] = nickname;
        return this;
    }

    /// <summary>
    /// Set the members roles.
    /// </summary>
    /// <param name="roles">Roles to apply, or <c>null</c> to remove them. The member will only have these roles, and all
    /// others will be removed.</param>
    /// <returns>The edit instance.</returns>
    /// <remarks>Requires <see cref="Permission.ManageRoles"/>.</remarks>
    public MemberEdit SetRoles(IEnumerable<Role>? roles)
    {
        _payload["roles"] = roles?.Select(r => r.Id).ToArray();
        return this;
    }

    /// <summary>
    /// Set whether the member is muted server wide.
    /// </summary>
    /// <param name="muted">Muted value.</param>
    /// <returns>The edit instance.</returns>
    /// <remarks>Requires <see cref="Permission.MuteMembers"/>.</remarks>
    public MemberEdit SetMute(bool muted)
    {
        _payload["mute"] = muted;
        return this;
    }

    /// <summary>
    /// Set whether the member is deafened server wide.
    /// </summary>
    /// <param name="deafen">Deafened value.</param>
    /// <returns>The edit instance.</returns>
    /// <remarks>Requires <see cref="Permission.DeafenMembers"/>.</remarks>
    public MemberEdit SetDeafen(bool deafen)
    {
        _payload["deaf"] = deafen;
        return this;
    }

    /// <summary>
    /// Move a member to the given channel.
    /// </summary>
    /// <param name="channelId">Voice channel ID, or <c>null</c> to disconnect them.</param>
    /// <returns>The edit instance.</returns>
    /// <remarks>Requires <see cref="Permission.MoveMembers"/>.</remarks>
    public MemberEdit MoveTo(ulong? channelId)
    {
        _payload["channel_id"] = channelId;
        return this;
    }

    /// <summary>
    /// Timeout a member.
    /// </summary>
    /// <param name="until">When the timeout expires (up to 28 days in the future), or <c>null</c> to remove it.</param>
    /// <returns>The edit instance.</returns>
    /// <remarks>Requires <see cref="Permission.ModerateMembers"/>.</remarks>
    public MemberEdit SetTimeout(DateTime? until)
    {
        _payload["communication_disabled_until"] = until?.ToString("O");
        return this;
    }

    /// <summary>
    /// Set whether the member is exempt from guild verification requirements.
    /// </summary>
    /// <param name="bypassVerification">Bypass value.</param>
    /// <returns>The edit instance.</returns>
    /// <remarks>Requires <see cref="Permission.ManageGuild"/> or <see cref="Permission.ManageRoles"/> or
    /// (<see cref="Permission.ModerateMembers"/> and <see cref="Permission.KickMembers"/> and <see cref="Permission.BanMembers"/>)
    /// </remarks>
    public MemberEdit SetBypassVerification(bool bypassVerification)
    {
        _payload["flags"] = MemberFlags.BypassesVerification;
        return this;
    }
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