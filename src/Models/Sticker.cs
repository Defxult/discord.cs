using Newtonsoft.Json;

namespace Discord.Models;

/// <summary>
/// Represents a sticker.
/// </summary>
public class Sticker : IEquatable<Sticker>
{
    // DOCS: https://discord.com/developers/docs/resources/sticker#sticker-object
    
    /// <summary>
    /// Sticker ID.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; init; }
    
    /// <summary>
    /// For standard stickers, ID of the pack the sticker is from.
    /// </summary>
    [JsonProperty("pack_id")]
    public ulong? PackId { get; init; }

    /// <summary>
    /// Name of the sticker.
    /// </summary>
    [JsonProperty("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Description of the sticker.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Type of sticker format.
    /// </summary>
    [JsonProperty("format_type")]
    public StickerFormat Format { get; init; }
    
    public override bool Equals(object? other) => other is Sticker sticker && Equals(sticker);
    public bool Equals(Sticker? other) => Id == other?.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

/// <summary>
/// Represents a pack of standard stickers.
/// </summary>
public class StickerPack : IEquatable<StickerPack>
{
    // DOCS: https://discord.com/developers/docs/resources/sticker#sticker-pack-object
    
    /// <summary>
    /// Sticker ID.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; init; }
    
    /// <summary>
    /// For standard stickers, ID of the pack the sticker is from.
    /// </summary>
    [JsonProperty("stickers")]
    public required IReadOnlyCollection<Sticker> Stickers { get; init; }

    /// <summary>
    /// Name of the sticker.
    /// </summary>
    [JsonProperty("name")]
    public required string Name { get; init; }
    
    /// <summary>
    /// ID of the pack's SKU.
    /// </summary>
    [JsonProperty("sku_id")]
    public ulong SkuId { get; init; }
    
    /// <summary>
    /// ID of a sticker in the pack which is shown as the pack's icon.
    /// </summary>
    [JsonProperty("cover_sticker_id")]
    public ulong? CoverStickerId { get; init; }

    /// <summary>
    /// Description of the sticker.
    /// </summary>
    [JsonProperty("description")]
    public required string Description { get; init; }

    /// <summary>
    /// The stickers banner.
    /// </summary>
    public Media? Banner
    {
        get
        {
            if (_bannerAssetId is { } bannerId)
                return new Media(bannerId.ToString(), $"/app-assets/710982414301790216/store/{bannerId}");
            return null;
        }
    }
    [JsonProperty("banner_asset_id")] private readonly ulong? _bannerAssetId;
    
    public override bool Equals(object? other) => other is StickerPack sticker && Equals(sticker);
    public bool Equals(StickerPack? other) => Id == other?.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

/// <summary>
/// Represents a guild sticker.
/// </summary>
public class GuildSticker : IEquatable<GuildSticker>
{
    // DOCS: https://discord.com/developers/docs/resources/sticker#sticker-object
    
    /// <summary>
    /// Guild sticker ID.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; init; }
    
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
    /// Type of sticker.
    /// </summary>
    [JsonProperty("type")]
    public StickerType Type { get; init; }

    /// <summary>
    /// Type of sticker format.
    /// </summary>
    [JsonProperty("format_type")]
    public StickerFormat Format { get; init; }

    /// <summary>
    /// Whether this guild sticker can be used, and may be false due to loss of server boosts.
    /// </summary>
    [JsonProperty("available")]
    // Discord says this is optional, so there's a chance it might not be there. But I can't find any circumstances as
    // to why it wouldn't be present, so a non-optional type will do for now.
    public bool IsAvailable { get; internal set; }
    
    /// <summary>
    /// ID of the guild that owns this sticker.
    /// </summary>
    [JsonProperty("guild_id")]
    public ulong GuildId { get; init; }

    /// <summary>
    /// The user that uploaded the guild sticker.
    /// </summary>
    [JsonProperty("user")]
    public User? User { get; init; }
    
    #region CUSTOM

    /// <summary>
    /// Your bot instance.
    /// </summary>
    public Bot? Bot { get; internal set; }
    
    /// <summary>
    /// Guild that owns this sticker.
    /// </summary>
    public Guild? Guild => Bot?.GetGuild(GuildId);

    #endregion
    
    public override bool Equals(object? other) => other is GuildSticker sticker && Equals(sticker);
    public bool Equals(GuildSticker? other) => Id == other?.Id;
    public override int GetHashCode() => Id.GetHashCode();
    
    /// <summary>
    /// Edit the guild sticker.
    /// </summary>
    /// <param name="edit">Guild sticker edit instance.</param>
    /// <param name="reason">The audit log reason for editing the guild sticker. </param>
    /// <returns></returns>
    public async Task<GuildSticker> EditAsync(GuildStickerEdit edit, string? reason = null) =>
        await Bot!._rest.ModifyGuildStickerAsync(GuildId, Id, edit, reason);
    
    /// <summary>
    /// Deletes the guild sticker.
    /// </summary>
    /// <param name="reason">Reason for deleting the guild sticker.</param>
    public async Task DeleteAsync(string? reason = null) =>
        await Bot!._rest.DeleteGuildStickerAsync(GuildId, Id, reason);
}

/// <summary>
/// Represents the values that can be edited for a <see cref="GuildSticker"/>.
/// </summary>
public struct GuildStickerEdit
{
    internal JSON _payload = [];

    /// <summary>
    /// Initializes a new guild sticker edit instance.
    /// </summary>
    public GuildStickerEdit() { }

    /// <summary>
    /// Name of the sticker (2-30 characters).
    /// </summary>
    public GuildStickerEdit SetName(string name)
    {
        _payload["name"] = name;
        return this;
    }
    
    /// <summary>
    /// Description of the sticker (2-100 characters).
    /// </summary>
    public GuildStickerEdit SetDescription(string? description)
    {
        _payload["description"] = description;
        return this;
    }
    
    /// <summary>
    /// The emoji that's related to the sticker.
    /// </summary>
    /// <param name="emoji">An emoji.</param>
    /// <returns></returns>
    public GuildStickerEdit SetEmoji(string emoji)
    {
        _payload["tags"] = emoji;
        return this;
    }
}

public enum StickerType
{
    // DOCS: https://discord.com/developers/docs/resources/sticker#sticker-object-sticker-types
    
    /// <summary>
    /// An official sticker in a pack.
    /// </summary>
    Standard = 1,
    
    /// <summary>
    /// A sticker uploaded to a guild for the guild's members.
    /// </summary>
    Guild
}

/// <summary>
/// Represents a sticker's format.
/// </summary>
public enum StickerFormat
{
    // DOCS: https://discord.com/developers/docs/resources/sticker#sticker-object-sticker-format-types
    
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