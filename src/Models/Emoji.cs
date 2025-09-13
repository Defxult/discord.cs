using System.Text.RegularExpressions;
using Discord.Utility;
using Discord.Net;
using Newtonsoft.Json;

namespace Discord.Models;

/// <summary>
/// Represents a Discord guild emoji.
/// </summary>
public class Emoji : IEquatable<Emoji>
{
    internal static Regex EmojiRegex = new("<a?:.+?:[0-9]{17,20}>");

    /// <summary>
    /// Emoji ID.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; init; }
    
    /// <summary>
    /// Emoji name.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; internal set; } = string.Empty;

    /// <summary>
    /// User that created this emoji.
    /// </summary>
    [JsonProperty("user")]
    public User? User { get; init; } // NOTE: This is only sent with GET:Emoji(s).

    /// <summary>
    /// Whether this emoji must be wrapped in colons to use.
    /// </summary>
    [JsonProperty("require_colons")]
    public bool RequiresColons { get; init; }

    /// <summary>
    /// Whether this emoji is managed by an integration.
    /// </summary>
    [JsonProperty("managed")]
    public bool IsManaged { get; init; }

    /// <summary>
    /// Whether this emoji is animated.
    /// </summary>
    [JsonProperty("animated")]
    public bool IsAnimated { get; init; }

    /// <summary>
    /// Whether this emoji can be used. May be <c>false</c> due to a loss of Server Boosts.
    /// </summary>
    [JsonProperty("available")]
    public bool IsAvailable { get; internal set; }

    #region API Separated

    /// <summary>
    /// The URL of the emoji.
    /// </summary>
    public string Url { get; init; }

    #endregion


    [JsonConstructor]
    private Emoji(ulong id, bool animated)
    {
        IsAnimated = animated;
        Url = ApiRoute.Cdn.GetDescription() + $"/emojis/{id}{(animated ? ".gif" : ".png")}";
    }
    
    public override bool Equals(object? other) => other is Emoji emoji && Equals(emoji);
    public bool Equals(Emoji? other) => Id == other?.Id;
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// Returns the actual representation of the emoji.
    /// <code>
    ///     Emoji crown = guild.GetEmoji(1234567890123456789);
    ///     await channel.sendAsync(crown.ToString());
    ///     // Sends "👑"
    /// </code>
    /// </summary>
    public override string ToString() =>
        IsAnimated ? $"<a:{Name}:{Id}>" : $"<:{Name}:{Id}>";

    /// <summary>
    /// Converts the emoji into a partial emoji.
    /// </summary>
    /// <returns>The partial emoji.</returns>
    public PartialEmoji ToPartial() =>
        new(Id, Name, IsAnimated);
}

/// <summary>
/// Represents a partial emoji on Discord.
/// </summary>
public class PartialEmoji : IEquatable<PartialEmoji>
{
    /// <summary>
    /// Guild emoji ID.
    /// </summary>
    [JsonProperty("id")]
    public ulong? Id { get; init; }

    /// <summary>
    /// Emoji name. If created via TODO or TODO, this property may be <c>null</c> when custom emoji data is
    /// not available (for example, if it was deleted from the guild).
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; init; }

    /// <summary>
    /// Whether this emoji is animated.
    /// </summary>
    [JsonProperty("animated")]
    public bool? IsAnimated { get; init; }

    /// <summary>
    /// Create a partial standard emoji.
    /// </summary>
    /// <param name="emoji">A standard unicode emoji.</param>
    public PartialEmoji(string emoji)
    {
        Name = emoji;
    }

    /// <summary>
    /// Create a partial guild emoji.
    /// </summary>
    /// <param name="id">ID of the guild emoji.</param>
    /// <param name="name">Name of the guild emoji.</param>
    /// <param name="animated">Whether the guild emoji is animated.</param>
    public PartialEmoji(ulong id, string name, bool animated)
    {
        Id = id;
        Name = name;
        IsAnimated = animated;
    }

    [JsonConstructor]
    internal PartialEmoji(ulong? id, string? name, bool? animated)
    {
        Id = id;
        Name = name;
        IsAnimated = animated;
    }
    
    public override bool Equals(object? other) => other is PartialEmoji partial && Equals(partial);
    public bool Equals(PartialEmoji? other) => Id == other?.Id;
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// Converts an emoji into a partial emoji.
    /// </summary>
    /// <param name="emoji">Either a unicode or guild emoji.</param>
    /// <returns>The partial emoji version of the given emoji.</returns>
    public static PartialEmoji FromString(string emoji)
    {
        if (!Emoji.EmojiRegex.IsMatch(emoji)) return new PartialEmoji(emoji);
        
        Regex nameRegex = new(":.+?:");
        Regex idRegex = new("[0-9]{17,20}");

        var name = nameRegex.Match(emoji).Value.Replace(":", string.Empty);
        var id = ulong.Parse(idRegex.Match(emoji).Value);
        var animated = emoji.StartsWith("<a:");

        return new PartialEmoji(id, name, animated);
    }

    /// <summary>
    /// Returns the actual representation of the partial emoji.
    /// </summary>
    public override string ToString()
    {
        if (Id == null && Name == null)
            return string.Empty;

        // Guild emoji.
        if (Name != null && Id != null)
        {
            if (IsAnimated != null)
                return (bool)IsAnimated ? $"<a:{Name}:{Id}>" : $"<:{Name}:{Id}>";
            return $"<:{Name}:{Id}>";
        }

        // Unicode emoji.
        if (Name != null && Id == null)
            return Name!;

        return string.Empty;
    }
}