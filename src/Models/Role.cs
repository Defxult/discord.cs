using Discord.Utility;
using Newtonsoft.Json;

namespace Discord.Models;

/// <summary>
/// Represents a Discord role.
/// </summary>
public class Role : IEquatable<Role>
{
    // DOCS: https://discord.com/developers/docs/topics/permissions#role-object
    
    /// <summary>
    /// Role ID.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; init; }

    /// <summary>
    /// Role name.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; internal set; } = string.Empty;

    /// <summary>
    /// Role color.
    /// </summary>
    [JsonProperty("colors")]
    public RoleColor? Color { get; internal set; }

    /// <summary>
    /// Whether the role should be displayed separately in the sidebar.
    /// </summary>
    [JsonProperty("hoist")]
    public bool Hoist { get; internal set; }

    /// <summary>
    /// The role icon.
    /// </summary>
    public Media? Icon
    {
        get
        {
            if (_icon is { } hash)
                return new Media(hash, $"/role-icons/{Id}/{hash}");
            return null;
        }
    }
    [JsonProperty("icon")] internal string? _icon;

    /// <summary>
    /// The role unicode emoji.
    /// </summary>
    [JsonProperty("unicode_emoji")]
    public string? UnicodeEmoji { get; internal set; }

    /// <summary>
    /// Position of this role.
    /// </summary>
    [JsonProperty("position")]
    public int Position { get; internal set; }

    /// <summary>
    /// Permissions for the role.
    /// </summary>
    public Permissions Permissions { get; internal set; }

    /// <summary>
    /// Whether this role is managed by an integration.
    /// </summary>
    [JsonProperty("managed")]
    public bool IsManaged { get; init; }

    /// <summary>
    /// Whether the role is mentionable.
    /// </summary>
    [JsonProperty("mentionable")]
    public bool IsMentionable { get; internal set; }

    /// <summary>
    /// The tags this role has.
    /// </summary>
    public RoleTag? Tags { get; internal set; }

    /// <summary>
    /// The roles flag's.
    /// </summary>
    public IReadOnlyCollection<RoleFlag> Flags => Util.FromBitfield<RoleFlag>(_flags);
    [JsonProperty("flags")] internal int _flags;

    #region CUSTOM
    
    /// <summary>
    /// Your bot instance.
    /// </summary>
    public Bot Bot { get; internal set; }
    
    /// <summary>
    /// ID of the guild this role belongs to.
    /// </summary>
    public ulong GuildId { get; internal set; }

    /// <summary>
    /// Whether this role is the "Nitro Booster" role.
    /// </summary>
    public bool IsPremiumSubscriber { get; }

    /// <summary>
    /// Whether this is the <c>@everyone</c> role.
    /// </summary>
    public bool IsDefault => Id == GuildId;

    /// <summary>
    /// Mention the role.
    /// </summary>
    public string Mention => IsDefault ? Markdown.MentionEveryone : Markdown.MentionRole(Id);

    #endregion

    [JsonConstructor]
    internal Role(ulong permissions, JSON? tags)
    {
        Permissions = new Permissions(permissions);
        
        /*
         * The API handles role tag values so weird:
         * 
         *      "Tags with type null represent booleans. They will be present and set to null if they
         *      are "true", and will be not present if they are "false".
         * 
         * Idk why they couldn't just set boolean values to simply true or false but OK...
         */
        if (tags is not null)
            Tags = RoleTag.Parse(tags);
        IsPremiumSubscriber = Tags?.IsPremiumSubscriber ?? false;
    }
    
    public override bool Equals(object? other) => other is Role role && Equals(role);
    public bool Equals(Role? other) => Id == other?.Id;
    public override int GetHashCode() => Id.GetHashCode();
    
    /// <summary>
    /// Edit the role.
    /// </summary>
    /// <param name="edit">A role edit instance.</param>
    /// <param name="reason">The reason for editing the role. This is displayed in the audit-log.</param>
    /// <returns>The updated role.</returns>
    public async Task<Role> EditAsync(RoleEdit edit, string? reason = null) =>
        await Bot._rest.ModifyGuildRoleAsync(GuildId, Id, edit, reason);

    /// <summary>
    /// Delete the role.
    /// </summary>
    /// <param name="reason">The reason for deleting the role. This is displayed in the audit-log.</param>
    public async Task DeleteAsync(string? reason = null)
    {
        await Bot._rest.DeleteGuildRoleAsync(GuildId, Id, reason);
    }

    /// <summary>
    /// Make a copy of this role.
    /// </summary>
    /// <param name="name">Name of the cloned role, or <c>null</c> to keep the same name.</param>
    /// <returns>A new role with the same properties.</returns>
    public async Task<Role> CloneAsync(string? name = null)
    {
        if (Bot.GetGuild(GuildId) is not { } guild) throw new DiscordException("Cannot clone role, guild not found");
        return await guild.CreateRoleAsync(name ?? Name, Permissions, Color, Hoist, await File(), UnicodeEmoji, IsMentionable);

        async Task<DFile?> File()
        {
            if (Icon is { } icon) return await icon.ToFile();
            return null;
        }
    }
}

/// <summary>
/// Represents a <see cref="Role"/> flag.
/// </summary>
public enum RoleFlag
{
    // DOCS: https://discord.com/developers/docs/topics/permissions#role-object-role-flags
    
    /// <summary>
    /// Members can select this role in an onboarding prompt.
    /// </summary>
    InPrompt = 1 << 0
}

/// <summary>
/// Represents the values that can be edited for a <see cref="Role"/>.
/// </summary>
public struct RoleEdit
{
    internal JSON _payload = [];
    
    /// <summary>
    /// Initializes a new role edit instance.
    /// </summary>
    public RoleEdit() { }

    /// <summary>
    /// Set the role name.
    /// </summary>
    /// <param name="name">Name of the role, max 100 characters.</param>
    /// <returns>The edit instance.</returns>
    public RoleEdit SetName(string name)
    {
        _payload["name"] = name;
        return this;
    }
    
    /// <summary>
    /// Set the role permissions.
    /// </summary>
    /// <param name="permissions">Role permissions.</param>
    /// <returns>The edit instance.</returns>
    public RoleEdit SetPermissions(Permissions permissions)
    {
        _payload["permissions"] = permissions.Value.ToString();
        return this;
    }
    
    /// <summary>
    /// Set the role color.
    /// </summary>
    /// <param name="color">The role's color.</param>
    /// <returns>The edit instance.</returns>
    public RoleEdit SetColor(RoleColor color)
    {
        _payload["colors"] = color;
        return this;
    }
    
    /// <summary>
    /// Set the role hoist.
    /// </summary>
    /// <param name="hoist">Whether the role should be displayed separately in the sidebar.</param>
    /// <returns>The edit instance.</returns>
    public RoleEdit SetHoist(bool hoist)
    {
        _payload["hoist"] = hoist;
        return this;
    }
    
    /// <summary>
    /// Set the role icon, or <c>null</c> to remove it.
    /// </summary>
    /// <param name="icon">The role's icon image (if the guild has <see cref="GuildFeature.RoleIcons"/>)</param>
    /// <returns>The edit instance.</returns>
    public RoleEdit SetIcon(DFile? icon)
    {
        _payload["icon"] = icon?._mimeTypeBase64;
        return this;
    }
    
    /// <summary>
    /// Set the role's Unicode emoji, or <c>null</c> to remove it.
    /// </summary>
    /// <param name="emoji">The role's Unicode emoji as a standard emoji (if the guild has <see cref="GuildFeature.RoleIcons"/>)</param>
    /// <returns>The edit instance.</returns>
    public RoleEdit SetEmoji(string? emoji)
    {
        _payload["unicode_emoji"] = emoji;
        return this;
    }

    /// <summary>
    /// Set whether the role should be mentionable.
    /// </summary>
    /// <param name="mentionable"></param>
    /// <returns>The edit instance.</returns>
    public RoleEdit SetMentionable(bool mentionable)
    {
        _payload["mentionable"] = mentionable;
        return this;
    }

    /// <summary>
    /// Resets the role to its default state.
    /// </summary>
    /// <returns>The edit instance.</returns>
    public static RoleEdit Reset()
    {
        return new RoleEdit()
            .SetName("new role")
            .SetPermissions(Permissions.None)
            .SetColor(new RoleColor())
            .SetHoist(false)
            .SetIcon(null)
            .SetEmoji(null)
            .SetMentionable(false);
    }
}

/// <summary>
/// Represents a <see cref="Role"/> color.
/// </summary>
public record struct RoleColor
{
    // DOCS: https://discord.com/developers/docs/topics/permissions#role-object-role-colors-object

    /// <summary>
    /// Enables the role color to be holographic.
    /// </summary>
    /// <remarks>
    /// <b>Warning:</b> Changing the <see cref="Primary"/> or <see cref="Secondary"/> values after instantiation with
    /// this field will cause errors due to API enforcement of values. If the holographic style is not desired, use the
    /// available constructors.
    /// </remarks>
    public static readonly RoleColor Holographic = new()
    {
        Primary = 11127295,
        Secondary = 16759788,
        Tertiary = 16761760
    };

    /// <summary>
    /// Primary color value.
    /// </summary>
    [JsonProperty("primary_color")]
    public int Primary { get; set; }
    
    /// <summary>
    /// Secondary color value.
    /// </summary>
    [JsonProperty("secondary_color")]
    public int? Secondary { get; set; }
    
    /// <summary>
    /// Tertiary color value.
    /// </summary>
    [JsonProperty("tertiary_color")]
    public int? Tertiary { get; private set; }

    /// <summary>
    /// Initializes a role color with its default values.
    /// </summary>
    public RoleColor() { }

    /// <summary>
    /// Initializes a role color with only its primary value.
    /// </summary>
    public RoleColor(int primary)
    {
        Primary = primary;
    }
    
    /// <summary>
    /// Initializes a role color with a secondary color as its gradient.
    /// </summary>
    /// <param name="primary">The primary color for the role.</param>
    /// <param name="secondary">The secondary color for the role, this will make the role a gradient between the otherprovided colors.</param>
    public RoleColor(int primary, int secondary)
    {
        Primary = primary;
        Secondary = secondary;
    }

    /// <summary>
    /// Generates a random role color.
    /// </summary>
    /// <returns>A role color</returns>
    public static RoleColor Random() => 
        new(Color.Random().Value);

    /// <summary>
    /// Convert the color to its <see cref="RoleColor"/> equivalent.
    /// </summary>
    /// <returns>A role color.</returns>
    public static RoleColor FromColor(Color color) =>
        new(color.Value);
}


/// <summary>
/// Represents the tag belonging to a <see cref="Role"/>.
/// </summary>
public record RoleTag
{
    // DOCS: https://discord.com/developers/docs/topics/permissions#role-object-role-tags-structure
    
    /// <summary>
    /// ID of the bot this role belongs to.
    /// </summary>
    public readonly ulong? BotId;

    /// <summary>
    /// ID of the integration this role belongs to.
    /// </summary>
    public readonly ulong? IntegrationId;

    /// <summary>
    /// Whether this is the guild's premium subscriber role, aka the "Nitro Booster" role.
    /// </summary>
    public readonly bool IsPremiumSubscriber;
    
    /// <summary>
    /// ID of the role's subscription SKU and listing.
    /// </summary>
    public readonly ulong? SubscriptionListingId;
    
    /// <summary>
    /// Whether this role is available for purchase.
    /// </summary>
    public readonly bool AvailableForPurchase;
    
    /// <summary>
    /// Whether this role is a guild's linked role.
    /// </summary>
    public readonly bool GuildConnections;

    private RoleTag(ulong? botId, ulong? integrationId, bool isPremiumSub, ulong? subscriptionListingId, bool forPurchase, bool guildConnections) 
    {
        BotId = botId;
        IntegrationId = integrationId;
        IsPremiumSubscriber = isPremiumSub;
        SubscriptionListingId = subscriptionListingId;
        AvailableForPurchase = forPurchase;
        GuildConnections = guildConnections;
    }

    internal static RoleTag Parse(JSON json)
    {
        bool hasBotId = json.TryGetValue("bot_id", out var bId);
        bool hasIntId = json.TryGetValue("integration_id", out var intId);
        bool isPreSub = json.ContainsKey("premium_subscriber");
        bool hasSubListingId = json.TryGetValue("subscription_listing_id", out var subListingId);
        bool forPurchase = json.ContainsKey("available_for_purchase");
        bool guildConnections = json.ContainsKey("guild_connections");

        ulong? botId = hasBotId ? Convert.ToUInt64(bId) : null;
        ulong? integrationId = hasIntId ? Convert.ToUInt64(intId) : null;
        ulong? subListId = hasSubListingId ? Convert.ToUInt64(subListingId) : null;
        
        return new RoleTag(botId, integrationId, isPreSub, subListId, forPurchase, guildConnections);
    }
}