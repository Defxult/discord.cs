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
    public bool Managed { get; init; }

    /// <summary>
    /// Whether the role is mentionable.
    /// </summary>
    [JsonProperty("mentionable")]
    public bool Mentionable { get; internal set; }

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
    /// ID of the guild this role belongs to.
    /// </summary>
    public ulong GuildId { get; internal set; } // Set in Guild constructor

    /// <summary>
    /// Whether this role is the "Nitro Booster" role.
    /// </summary>
    public bool PremiumSubscriber { get; }

    /// <summary>
    /// Whether this is the <c>@everyone</c> role.
    /// </summary>
    public bool Default => Id == GuildId;

    /// <summary>
    /// Mention the role.
    /// </summary>
    public string Mention => Default ? Markdown.MentionEveryone : Markdown.MentionRole(Id);

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
        PremiumSubscriber = Tags?.IsPremiumSubscriber ?? false;
    }
    
    public override bool Equals(object? other) => other is Role role && Equals(role);
    public bool Equals(Role? other) => Id == other?.Id;
    public override int GetHashCode() => Id.GetHashCode();
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
/// Represents a <see cref="Role"/> color.
/// </summary>
public record struct RoleColor
{
    // DOCS: https://discord.com/developers/docs/topics/permissions#role-object-role-colors-object
    
    /// <summary>
    /// Primary color value.
    /// </summary>
    [JsonProperty("primary_color")]
    public int Primary;
    
    /// <summary>
    /// Secondary color value.
    /// </summary>
    [JsonProperty("secondary_color")]
    public int? Secondary;
    
    /// <summary>
    /// Tertiary color value.
    /// </summary>
    [JsonProperty("tertiary_color")]
    public int? Tertiary;

    /// <summary>
    /// Initializes a role color with its default values.
    /// </summary>
    public RoleColor() { }

    /// <summary>
    /// Initializes a role color.
    /// </summary>
    /// <param name="primary">The primary color for the role.</param>
    /// <param name="secondary">The secondary color for the role, this will make the role a gradient between the other provided colors.</param>
    /// <param name="tertiary">The tertiary color for the role, this will turn the gradient into a holographic style.</param>
    public RoleColor(int primary, int? secondary, int? tertiary)
    {
        Primary = primary;
        Secondary = secondary;
        Tertiary = tertiary;
    }
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