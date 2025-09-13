using Newtonsoft.Json;

namespace Discord.Models;

/// <summary>
/// Represents a sticker.
/// </summary>
public class Sticker : IEquatable<Sticker>
{
    /// <summary>
    /// Sticker ID.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; }
    
    /// <summary>
    /// For standard stickers, ID of the pack the sticker is from.
    /// </summary>
    [JsonProperty("pack_id")]
    public ulong? PackId { get; }

    /// <summary>
    /// Name of the sticker.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; } = string.Empty;

    /// <summary>
    /// Description of the sticker.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; }

    /// <summary>
    /// Type of sticker format.
    /// </summary>
    [JsonProperty("format_type")]
    public StickerFormat Format;
    
    public override bool Equals(object? other) => other is Sticker sticker && Equals(sticker);
    public bool Equals(Sticker? other) => Id == other?.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

/// <summary>
/// Represents a guild sticker.
/// </summary>
public class GuildSticker : IEquatable<GuildSticker>
{
    /// <summary>
    /// Guild sticker ID.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; }
    
    /// <summary>
    /// Name of the guild sticker.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; internal set; } = string.Empty;

    /// <summary>
    /// Description of the guild sticker.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; internal set; }

    /// <summary>
    /// The <b>name</b> of the Unicode emoji.
    /// </summary>
    [JsonProperty("tags")]
    public string Emoji { get; internal set; } = string.Empty;

    /// <summary>
    /// Type of sticker format.
    /// </summary>
    [JsonProperty("format_type")]
    public StickerFormat Format { get; }

    /// <summary>
    /// Whether this guild sticker can be used, and may be false due to loss of server boosts.
    /// </summary>
    [JsonProperty("available")]
    public bool IsAvailable { get; internal set; } // Discord says this is optional, so there's a chance it might not be there. But I can't find any circumstances as to why it wouldn't be present, so a non-optional type will do for now.

    /// <summary>
    /// The user that uploaded the guild sticker.
    /// </summary>
    [JsonProperty("user")]
    public User? User { get; }
    
    public override bool Equals(object? other) => other is GuildSticker sticker && Equals(sticker);
    public bool Equals(GuildSticker? other) => Id == other?.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

/// <summary>
/// Represents a sticker's format.
/// </summary>
public enum StickerFormat
{
    /// <summary>
    /// A sticker with a file format of png.
    /// </summary>        
    Png = 1,

    /// <summary>
    /// A sticker with a file format of apng.
    /// </summary>
    Apng,

    /// <summary>
    /// A sticker with a file format of lottie.
    /// </summary>
    Lottie,
    
    /// <summary>
    /// A sticker with a file format of gif.
    /// </summary>
    Gif
}