namespace Discord.Models;

/// <summary>
/// Represents a permission.
/// </summary>
public enum Permission : ulong
{
    CreateInstantInvite              = 1UL << 0,
    KickMembers                      = 1UL << 1,
    BanMembers                       = 1UL << 2,
    Administrator                    = 1UL << 3,
    ManageChannels                   = 1UL << 4,
    ManageGuild                      = 1UL << 5,
    AddReactions                     = 1UL << 6,
    ViewAuditLog                     = 1UL << 7,
    PrioritySpeaker                  = 1UL << 8,
    Stream                           = 1UL << 9,
    ViewChannel                      = 1UL << 10,
    SendMessages                     = 1UL << 11,
    SendTtsMessages                  = 1UL << 12,
    ManageMessages                   = 1UL << 13,
    EmbedLinks                       = 1UL << 14,
    AttachFiles                      = 1UL << 15,
    ReadMessageHistory               = 1UL << 16,
    MentionEveryone                  = 1UL << 17,
    UseExternalEmojis                = 1UL << 18,
    ViewGuildInsights                = 1UL << 19,
    Connect                          = 1UL << 20,
    Speak                            = 1UL << 21,
    MuteMembers                      = 1UL << 22,
    DeafenMembers                    = 1UL << 23,
    MoveMembers                      = 1UL << 24,
    UseVoiceActivityDetection        = 1UL << 25,
    ChangeNickname                   = 1UL << 26,
    ManageNicknames                  = 1UL << 27,
    ManageRoles                      = 1UL << 28,
    ManageWebhooks                   = 1UL << 29,
    ManageGuildExpressions           = 1UL << 30,
    UseApplicationCommands           = 1UL << 31,
    RequestToSpeak                   = 1UL << 32,
    ManageEvents                     = 1UL << 33,
    ManageThreads                    = 1UL << 34,
    CreatePublicThreads              = 1UL << 35,
    CreatePrivateThreads             = 1UL << 36,
    UseExternalStickers              = 1UL << 37,
    SendMessagesInThreads            = 1UL << 38,
    UseEmbeddedActivities            = 1UL << 39,
    ModerateMembers                  = 1UL << 40,
    ViewCreatorMonetizationAnalytics = 1UL << 41,
    UseSoundboard                    = 1UL << 42,
    CreateGuildExpressions           = 1UL << 43,
    CreateEvents                     = 1UL << 44,
    UseExternalSoundboard            = 1UL << 45,
    SendVoiceMessages                = 1UL << 46,
    SendPolls                        = 1UL << 49,
    UseExternalApps                  = 1UL << 50,
    PinMessages                      = 1UL << 51,
    BypassSlowMode                   = 1UL << 52
}

/// <summary>
/// Represents the permissions for a channel, user, or guild.
/// </summary>
public class Permissions
{
    /// <summary>
    /// The bitset value for the permissions that are enabled/disabled.
    /// </summary>
    public readonly ulong Value;

    /// <summary>
    /// The permissions that are enabled.
    /// </summary>
    public IReadOnlyCollection<Permission> Enabled => _enabled;
    private readonly HashSet<Permission> _enabled = [];

    /// <summary>
    /// The permissions that are disabled.
    /// </summary>
    public IReadOnlyCollection<Permission> Disabled => _disabled;
    private readonly HashSet<Permission> _disabled = [];

    /// <summary>
    /// Returns a permissions object with the following enabled:
    /// <list type="bullet">
    ///     <item><see cref="Permission.ViewChannel"/></item>
    ///     <item><see cref="Permission.CreateInstantInvite"/></item>
    ///     <item><see cref="Permission.ChangeNickname"/></item>
    ///     <item><see cref="Permission.SendMessages"/></item>
    ///     <item><see cref="Permission.SendMessagesInThreads"/></item>
    ///     <item><see cref="Permission.EmbedLinks"/></item>
    ///     <item><see cref="Permission.AttachFiles"/></item>
    ///     <item><see cref="Permission.AddReactions"/></item>
    ///     <item><see cref="Permission.UseExternalEmojis"/></item>
    ///     <item><see cref="Permission.UseExternalStickers"/></item>
    ///     <item><see cref="Permission.ReadMessageHistory"/></item>
    ///     <item><see cref="Permission.UseApplicationCommands"/></item>
    ///     <item><see cref="Permission.Connect"/></item>
    ///     <item><see cref="Permission.Speak"/></item>
    ///     <item><see cref="Permission.Stream"/></item>
    ///     <item><see cref="Permission.UseEmbeddedActivities"/></item>
    ///     <item><see cref="Permission.UseVoiceActivityDetection"/></item>
    ///     <item><see cref="Permission.UseSoundboard"/></item>
    ///     <item><see cref="Permission.UseExternalSoundboard"/></item>
    ///     <item><see cref="Permission.SendVoiceMessages"/></item>
    ///     <item><see cref="Permission.RequestToSpeak"/></item>
    /// </list>
    /// </summary>
    public static readonly Permissions Default = new(enable: [
        Permission.ViewChannel,
        Permission.CreateInstantInvite,
        Permission.ChangeNickname,
        Permission.SendMessages,
        Permission.SendMessagesInThreads,
        Permission.EmbedLinks,
        Permission.AttachFiles,
        Permission.AddReactions,
        Permission.UseExternalEmojis,
        Permission.UseExternalStickers,
        Permission.ReadMessageHistory,
        Permission.UseApplicationCommands,
        Permission.Connect,
        Permission.Speak,
        Permission.Stream,
        Permission.UseEmbeddedActivities,
        Permission.UseVoiceActivityDetection,
        Permission.UseSoundboard,
        Permission.UseExternalSoundboard,
        Permission.SendVoiceMessages,
        Permission.RequestToSpeak
    ]);

    /// <summary>
    /// Returns a permissions object with all permissions disabled.
    /// </summary>
    public static readonly Permissions None = new(0);

    /// <summary>
    /// Initializes a new permissions instance.
    /// </summary>
    /// <param name="value">The permissions value.</param>
    public Permissions(ulong value)
    {
        Value = value;
        foreach (Permission perm in Enum.GetValues(typeof(Permission)))
        {
            if ((value & (ulong)perm) == (ulong)perm)
                _enabled.Add(perm);
            else
                _disabled.Add(perm);
        }
    }

    /// <summary>
    /// Initializes new permissions instance.
    /// </summary>
    /// <param name="permissions">The permissions to enable or disable.</param>
    public Permissions(Dictionary<Permission, bool> permissions)
    {
        ulong bitValue = 0;
        foreach (var kv in permissions)
        {
            var perm = kv.Key;
            var isEnabled = kv.Value;
            if (isEnabled)
            {
                bitValue |= (ulong)perm;
                _enabled.Add(perm);
            }
            else
            {
                bitValue &= (ulong)~perm;
                _disabled.Add(perm);
            }
        }
        Value = bitValue;
    }

    /// <summary>
    /// Initializes new permissions instance.
    /// </summary>
    /// <param name="enable">The permissions to enable.</param>
    public Permissions(IEnumerable<Permission> enable)
    {
        ulong bitValue = 0;
        foreach (var perm in enable)
        {
            bitValue |= (ulong)perm;
            _enabled.Add(perm);
        }
        Value = bitValue;
    }
}

/// <summary>
/// Represents the permission overwrites for a channel.
/// </summary>    
public class PermissionOverwrites : IEquatable<PermissionOverwrites>
{
    /// <summary>
    /// The overwrite type.
    /// </summary>        
    public PermissionOverwriteType Type { get; }

    /// <summary>
    /// ID of the <see cref="Member"/>  or <see cref="Role"/> that the overwrites will be applied to.
    /// </summary>
    public ulong Id { get; }

    /// <summary>
    /// The permissions that are enabled.
    /// </summary>        
    public IReadOnlyCollection<Permission> Enabled => _enabled;
    private readonly HashSet<Permission> _enabled;

    /// <summary>
    /// The permissions that are disabled.
    /// </summary>        
    public IReadOnlyCollection<Permission> Disabled => _disabled;
    private readonly HashSet<Permission> _disabled;

    private readonly ulong _allowedBitSet;
    private readonly ulong _deniedBitSet;
    
    /// <summary>
    /// Set permissions to be enabled or disabled.
    /// </summary>
    /// <param name="type">The overwrite type.</param>
    /// <param name="id">ID of the <see cref="Member"/> or <see cref="Role"/> where overwrites will be applied to.</param>
    /// <param name="enable">Permissions to enable.</param>
    /// <param name="disable">Permissions to disable.</param>   
    public PermissionOverwrites(PermissionOverwriteType type, ulong id, IReadOnlyCollection<Permission> enable, IReadOnlyCollection<Permission> disable)
    {
        Type = type;
        Id = id;
        _enabled = enable.ToHashSet();
        _disabled = disable.ToHashSet();
        foreach (var perm in _enabled)
            _allowedBitSet |= (ulong)perm;
        foreach (var perm in _disabled)
            _deniedBitSet |= (ulong)perm;
    }

    public override int GetHashCode() =>
        (_allowedBitSet | _allowedBitSet).GetHashCode();

    public override bool Equals(object? obj)
    {
        if (obj is PermissionOverwrites po)
            return Equals(po);
        return false;
    }

    public bool Equals(PermissionOverwrites? other)
    {
        if (other is null) return false;
        bool[] results =
        [
            Id == other.Id,
            Type == other.Type,
            _enabled.SetEquals(other._enabled),
            _disabled.SetEquals(other._disabled)
        ];
        return results.All(item => item);
    }

    public static bool operator ==(PermissionOverwrites left, PermissionOverwrites right) =>
        left.Equals(right);

    public static bool operator !=(PermissionOverwrites left, PermissionOverwrites right) =>
        !left.Equals(right);

    private PermissionOverwrites(ulong id, PermissionOverwriteType type, ulong allow, ulong deny)
    {
        Id = id;
        Type = type;
        var (allowed, denied) = DecodePermissionsOverwritesPayload(allow, deny);
        _enabled = allowed;
        _disabled = denied;
    }

    internal JSON ToPayload() =>
        new()
        {
            {"id", Id},
            {"type", (int)Type},
            {"allow", _allowedBitSet.ToString()},
            {"deny", _deniedBitSet.ToString()}
        };

    internal static PermissionOverwrites[] Parse(ICollection<JSON> overwrites)
    {
        var po = new PermissionOverwrites[overwrites.Count];
        foreach (var (item, i) in overwrites.Select((item, index) => (item, index)))
        {
            var id = Convert.ToUInt64(item["id"]);
            var type = (PermissionOverwriteType)Convert.ToInt32(item["type"]);
            var allow = Convert.ToUInt64(item["allow"]);
            var deny = Convert.ToUInt64(item["deny"]);
            po[i] = new PermissionOverwrites(id, type, allow, deny);
        }
        return po;
    }

    private static (HashSet<Permission> allowed, HashSet<Permission> denied) DecodePermissionsOverwritesPayload(ulong allowedValue, ulong deniedValue)
    {
        var permsAllowed = new HashSet<Permission>();
        var permsDenied = new HashSet<Permission>();
        var allValues = Enum.GetValues(typeof(Permission)).Cast<Permission>().ToHashSet();

        // Admin perms overrides all perms.
        if ((allowedValue & (ulong)Permission.Administrator) == (ulong)Permission.Administrator)
        {
            var perms = (HashSet<Permission>)new HashSet<Permission>().Concat(allValues);
            return (perms, []);
        }
        
        foreach (Permission perm in allValues)
        {
            if ((allowedValue & (ulong)perm) == (ulong)perm)
                permsAllowed.Add(perm);
            if ((deniedValue & (ulong)perm) == (ulong)perm)
                permsDenied.Add(perm);
        }
        return (permsAllowed, permsDenied);
    }
}

/// <summary>
/// Represents the permission overwrite type.
/// </summary>
public enum PermissionOverwriteType
{
    Role,
    User
}