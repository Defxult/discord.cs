using Discord.Utility;
using Newtonsoft.Json;

namespace Discord.Models;

/// <summary>
/// Represents a Discord role.
/// </summary>
public class Role : IEquatable<Role>
{
    /// <summary>
    /// Role ID.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; }
    
    /// <summary>
    /// The guild this role belongs to.
    /// </summary>
    // public Guild Guild { get; internal set; } TODO

    /// <summary>
    /// Role name.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; internal set; } = string.Empty;

    /// <summary>
    /// Role color.
    /// </summary>
    public Color? Color;

    /// <summary>
    ///  Whether the role should be displayed separately in the sidebar.
    /// </summary>
    [JsonProperty("hoist")]
    public bool Hoist { get; internal set; }

    /// <summary>
    /// The role icon.
    /// </summary>
    public Media? Icon { get; internal set; }

    /// <summary>
    /// The role unicode emoji.
    /// </summary>
    [JsonProperty("unicode_emoji")]
    public string? UnicodeEmoji;

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
    public RoleTag? Tags { get; init; }

    /// <summary>
    /// Your bot instance.
    /// </summary>
    // public Bot? Bot => Guild.Bot; TODO

    #region API Separated

    /// <summary>
    /// Members who currently have this role.
    /// </summary>
    // public IReadOnlyCollection<Member> Members => Guild.Members.Where(m => m.Roles.Contains(this)).ToHashSet(); TODO

    /// <summary>
    /// Whether this role is the default role, aka the <c>@everyone</c> role.
    /// </summary>
    // public bool IsDefault => Id == Guild.Id; TODO

    /// <summary>
    /// Whether this role is the "Nitro Booster" role.
    /// </summary>
    public readonly bool IsPremiumSubscriber;

    /// <summary>
    /// Mention the role.
    /// </summary>
    // public string Mention => IsDefault ? Markdown.MentionEveryone : Markdown.MentionRole(Id); TODO

    #endregion

    [JsonConstructor]
    internal Role(int color, string? icon, ulong permissions, JSON? tags)
    {
        Color = color == 0 ? null : new Color(color);
        if (icon != null)
            Icon = new Media(icon, $"/role-icons/{Id}/{icon}");
        Permissions = new Permissions(permissions);

        /*
         * The API handles role tag values so weird:
         * 
         *      "Tags with type null represent booleans. They will be present and set to null if they
         *      are "true", and will be not present if they are "false".
         * 
         * Idk why they couldn't just set boolean values to simply true or false but OK...
         */
        if (tags != null)
        {
            bool hasBotId = tags.TryGetValue("bot_id", out var bId);
            bool hasIntId = tags.TryGetValue("integration_id", out var intId);
            bool isPreSub = tags.ContainsKey("premium_subscriber");

            ulong? botId = hasBotId ? Convert.ToUInt64(bId) : null;
            ulong? integrationId = hasIntId ? Convert.ToUInt64(intId) : null;
            Tags = new RoleTag(botId, integrationId, isPreSub);
        }
        IsPremiumSubscriber = Tags?.IsPremiumSubscriber ?? false;
    }
    
    public override bool Equals(object? other) => other is Role role && Equals(role);
    public bool Equals(Role? other) => Id == other?.Id;
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// Returns the roles name.
    /// </summary>
    public override string ToString() =>
        Name;
}

/// <summary>
/// Represents the tag belonging to a <see cref="Role"/>.
/// </summary>
public record RoleTag
{
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

    internal RoleTag(ulong? botId, ulong? integrationId, bool isPremiumSub) 
    {
        BotId = botId;
        IntegrationId = integrationId;
        IsPremiumSubscriber = isPremiumSub;
    }
}