using System.Text.Json;
using Discord.Net;
using Newtonsoft.Json;

namespace Discord.Models;

/// <summary>
/// Represents a <see cref="Message"/> reaction.
/// </summary>
public record Reaction
{
    // DOCS: https://discord.com/developers/docs/resources/message#reaction-object
    
    /// <summary>
    /// Total number of times this emoji have been used to react (including super reacts).
    /// </summary>
    [JsonProperty("count")]
    public int Count { get; internal set; }

    /// <summary>
    /// Contains the counts for normal and burst reactions.
    /// </summary>
    [JsonProperty("count_details")]
    public ReactionCountDetails CountDetails { get; internal set; }

    /// <summary>
    /// Whether the bot reacted using this emoji.
    /// </summary>
    [JsonProperty("me")]
    public bool Me { get; internal set; }

    /// <summary>
    /// Whether the bot super-reacted using this emoji.
    /// </summary>
    [JsonProperty("me_burst")]
    public bool MeBurst { get; internal set; }

    /// <summary>
    /// Emoji that represents the reaction.
    /// </summary>
    [JsonProperty("emoji")]
    public PartialEmoji Emoji { get; internal set; }

    /// <summary>
    /// Colors used for the super reaction.
    /// </summary>
    public IReadOnlyCollection<Color> BurstColors { get; }

    #region CUSTOM

    /// <summary>
    /// <c>true</c> if the emoji used to react is a <see cref="Guild"/> emoji, <c>false</c> if it's a Unicode emoji.
    /// </summary>
    public bool IsGuildEmoji => Emoji.Id != null;
    
    /// <summary>
    /// Reaction type.
    /// </summary>
    public ReactionType Type => BurstColors.Count == 0 ? ReactionType.Normal : ReactionType.Burst; 
    
    /// <summary>
    /// Additional details about the reaction. Will always be <c>null</c> unless <see cref="Intent.GuildMessageReactions"/>
    /// and or <see cref="Intent.DmReactions"/> are enabled.
    /// </summary>
    public ReactionDetails? Details { get; internal set; }

    #endregion

    [JsonConstructor]
    internal Reaction(JSON emoji, List<string> burst_colors)
    {
        var doc = JsonDocument.Parse(JsonConvert.SerializeObject(emoji));
        Emoji = Gateway.Deserialize<PartialEmoji>(doc.RootElement);
        BurstColors = burst_colors.Select(val => new Color(val)).ToList();
    }

    internal Reaction(int count, ReactionDetails details, bool me, bool meBurst, PartialEmoji emoji,
        List<Color> burstColors, ReactionCountDetails countDetails)
    {
        Count = count;
        Details = details;
        Me = me;
        MeBurst = meBurst;
        Emoji = emoji;
        BurstColors = burstColors;
        CountDetails = countDetails;
    }
}

/// <summary>
/// Represents the <see cref="Reaction"/> data that's dispatched via <see cref="Gateway.OnReactionAdd"/>.
/// </summary>
public record ReactionDetails
{
    // DOCS: https://discord.com/developers/docs/events/gateway-events#message-reaction-add-message-reaction-add-event-fields
    
    // These properties are not a part of the normal Reaction object. The normal reaction object has very limited info
    // but the MESSAGE_REACTION_ADD event provides a lot of details that are useful for the reaction. With that said,
    // these properties are only filled if the bot has intent GUILD_MESSAGE_REACTIONS and or DIRECT_MESSAGE_REACTIONS.
    
    /// <summary>
    /// ID of the user.
    /// </summary>
    [JsonProperty("user_id")]
    public ulong UserId { get; init; }
    
    /// <summary>
    /// ID of the channel.
    /// </summary>
    [JsonProperty("channel_id")]
    public ulong ChannelId { get; init; }
    
    /// <summary>
    /// ID of the message.
    /// </summary>
    [JsonProperty("message_id")]
    public ulong MessageId { get; init; }
    
    /// <summary>
    /// ID of the guild.
    /// </summary>
    [JsonProperty("guild_id")]
    public ulong? GuildId { get; init; }
    
    /// <summary>
    /// Emoji used to react.
    /// </summary>
    [JsonProperty("emoji")]
    public required PartialEmoji Emoji { get; init; }
    
    /// <summary>
    /// ID of the user who authored the message which was reacted to.
    /// </summary>
    [JsonProperty("message_author_id")]
    public ulong? MessageAuthorId { get; init; }
    
    /// <summary>
    /// <c>true</c> if this is a super-reaction.
    /// </summary>
    [JsonProperty("burst")]
    public bool IsBurst { get; init; }

    /// <summary>
    /// Colors used for super-reaction animation.
    /// </summary>
    public IReadOnlyCollection<Color> BurstColors => _burstColors;
    private readonly List<Color> _burstColors = [];
    
    /// <summary>
    /// The type of reaction.
    /// </summary>
    [JsonProperty("type")]
    public ReactionType Type { get; init; }

    [JsonConstructor]
    private ReactionDetails(List<string>? burst_colors)
    {
        if (burst_colors != null)
            _burstColors = burst_colors.Select(val => new Color(val)).ToList();
    }
}

/// <summary>
/// Represents the details for a removed <see cref="Reaction"/>
/// </summary>
public record ReactionRemove
{
    /// <summary>
    /// ID of the user. Only available for <see cref="Gateway.OnReactionRemove"/>.
    /// </summary>
    [JsonProperty("user_id")]
    public ulong? UserId { get; init; }
    
    /// <summary>
    /// ID of the channel.
    /// </summary>
    [JsonProperty("channel_id")]
    public ulong ChannelId { get; init; }
    
    /// <summary>
    /// ID of the message.
    /// </summary>
    [JsonProperty("message_id")]
    public ulong MessageId { get; init; }
    
    /// <summary>
    /// ID of the guild.
    /// </summary>
    [JsonProperty("guild_id")]
    public ulong? GuildId { get; init; }
    
    /// <summary>
    /// Emoji used to react. Only available for <see cref="Gateway.OnReactionRemove"/> and <see cref="Gateway.OnReactionRemoveEmoji"/>.
    /// </summary>
    [JsonProperty("emoji")]
    public PartialEmoji? Emoji { get; init; }
    
    /// <summary>
    /// <c>true</c> if this was a super-reaction. Only available for <see cref="Gateway.OnReactionRemove"/>.
    /// </summary>
    [JsonProperty("burst")]
    public bool? IsBurst { get; init; }
    
    /// <summary>
    /// The type of reaction. Only available for <see cref="Gateway.OnReactionRemove"/>.
    /// </summary>
    [JsonProperty("type")]
    public ReactionType? Type { get; init; }
    
    private ReactionRemove() { }
}

/// <summary>
/// Represents the type of reaction for <see cref="ReactionDetails"/>
/// </summary>
public enum ReactionType
{
    // DOCS: https://discord.com/developers/docs/resources/message#get-reactions-reaction-types
    
    Normal,
    Burst
}

/// <summary>
/// Represents a <see cref="Reaction"/> count for normal and super reactions.
/// </summary>
public record ReactionCountDetails
{
    // DOCS: https://discord.com/developers/docs/resources/message#reaction-count-details-object
    
    /// <summary>
    /// Count of super reactions.
    /// </summary>
    [JsonProperty("burst")]
    public int Burst { get; internal set; }
    
    /// <summary>
    /// Count of normal reactions.
    /// </summary>
    [JsonProperty("normal")]
    public int Normal { get; internal set; }

    internal ReactionCountDetails(int burst, int normal)
    {
        Burst = burst;
        Normal = normal;
    }
}
