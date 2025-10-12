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
    internal static readonly Regex EmojiRegex = new("<a?:.+?:[0-9]{17,20}>");

    /// <summary>
    /// Emoji ID.
    /// </summary>
    public ulong Id { get; }
    
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

    #region CUSTOM
    
    /// <summary>
    /// Your bot instance.
    /// </summary>
    public Bot? Bot { get; internal set; }
    
    /// <summary>
    /// ID of the guild this emoji belongs to, or <c>null</c> if it's an Application emoji.
    /// </summary>
    public ulong? GuildId { get; internal set; }
    
    /// <summary>
    /// Whether this emoji belongs directly to an application, and is not a part of any guild.
    /// </summary>
    public bool IsApplicationEmoji => GuildId is null;

    /// <summary>
    /// The URL of the emoji.
    /// </summary>
    public string Url { get; }
    
    #endregion
    
    [JsonConstructor]
    private Emoji(ulong id, bool animated)
    {
        Id = id;
        IsAnimated = animated;
        Url = ApiRoute.Cdn.GetDescription() + $"/emojis/{id}{(animated ? ".gif" : ".png")}";
    }
    
    public override bool Equals(object? other) => other is Emoji emoji && Equals(emoji);
    public bool Equals(Emoji? other) => Id == other?.Id;
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// Edits the emoji.
    /// </summary>
    /// <param name="edit">Emoji edit instance.</param>
    /// <param name="reason">Reason for editing the emoji. This is displayed in the audit-log.</param>
    /// <returns>The updated emoji.</returns>
    public async Task<Emoji> EditAsync(EmojiEdit edit, string? reason = null)
    {
        if (!IsApplicationEmoji) 
            return await Bot!._rest.ModifyGuildEmojiAsync(GuildId!.Value, Id, edit, reason);
        
        var app = await Bot!.ApplicationAsync();
        return await Bot!._rest.ModifyApplicationEmojiAsync(app.Id, Id, edit);
    }

    /// <summary>
    /// Deletes the emoji.
    /// </summary>
    /// <param name="reason">Reason for deleting the emoji. This is displayed in the audit-log.</param>
    public async Task DeleteAsync(string? reason = null)
    {
        if (!IsApplicationEmoji) 
            await Bot!._rest.DeleteGuildEmojiAsync(GuildId!.Value, Id, reason);
        else
        {
            var app = await Bot!.ApplicationAsync();
            await Bot!._rest.DeleteApplicationEmojiAsync(app.Id, Id);
        }
    }

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
/// Represents the values that can be edited for an Emoji via <see cref="Emoji.EditAsync"/>.
/// </summary>
public struct EmojiEdit
{
    internal JSON _payload = [];
    
    public EmojiEdit() { }

    /// <summary>
    /// Name for the emoji.
    /// </summary>
    public EmojiEdit SetName(string name)
    {
        _payload["name"] = name;
        return this;
    }
    
    /// <summary>
    /// Roles that are allowed to use the emoji or <c>null</c> for none. Not applicable for Application emojis.
    /// </summary>
    public EmojiEdit SetRoles(IReadOnlyCollection<Role>? roles)
    {
        _payload["roles"] = roles;
        return this;
    }
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
    /// Emoji name. May be <c>null</c> when custom emoji data is
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
    /// <param name="emoji">Either a Unicode or guild emoji.</param>
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
