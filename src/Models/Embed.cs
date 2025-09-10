using Discord.Net;
using Newtonsoft.Json;

namespace Discord.Models;

/// <summary>
/// Represents a Discord embed.
/// </summary>
public class Embed
{
    /// <summary>
    /// Embed type.
    /// </summary>
    [JsonIgnore]
    public EmbedType Type { get; }

    /// <summary>
    /// The title; max 256 characters.
    /// </summary>
    [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
    public string? Title;

    /// <summary>
    /// The description; max 4096 characters.
    /// </summary>
    [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
    public string? Description;

    /// <summary>
    /// URL of embed.
    /// </summary>
    [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
    public string? Url;

    /// <summary>
    /// Timestamp of embed.
    /// </summary>
    [JsonProperty("timestamp", NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? Timestamp;

    /// <summary>
    /// Color of the embed.
    /// </summary>
    [JsonIgnore]
    public Color? Color;
    [JsonProperty("color", NullValueHandling = NullValueHandling.Ignore)] private int? _color => Color?.Value;

    /// <summary>
    /// Footer information.
    /// </summary>
    [JsonProperty("footer", NullValueHandling = NullValueHandling.Ignore)]
    public EmbedFooter? Footer;

    /// <summary>
    /// Image information.
    /// </summary>
    [JsonProperty("image", NullValueHandling = NullValueHandling.Ignore)]
    public EmbedImage? Image;

    /// <summary>
    /// Thumbnail information.
    /// </summary>
    [JsonProperty("thumbnail", NullValueHandling = NullValueHandling.Ignore)]
    public EmbedThumbnail? Thumbnail;

    /// <summary>
    /// Video information.
    /// </summary>
    [JsonProperty("video", NullValueHandling = NullValueHandling.Ignore)]
    public EmbedVideo? Video { get; private set; }

    /// <summary>
    /// Provider information.
    /// </summary>
    [JsonProperty("provider", NullValueHandling = NullValueHandling.Ignore)]
    public EmbedProvider? Provider { get; private set; }

    /// <summary>
    /// Author information.
    /// </summary>
    [JsonProperty("author", NullValueHandling = NullValueHandling.Ignore)]
    public EmbedAuthor? Author;

    /// <summary>
    /// Embed fields (max 25).
    /// </summary>
    [JsonProperty("fields", NullValueHandling = NullValueHandling.Ignore)]
    public List<EmbedField>? Fields;

    /// <summary>
    /// Total number of characters in the embed (max 6000).
    /// </summary>
    [JsonIgnore]
    public int Length
    {
        get
        {
            var count = 0;
            count += Title?.Length ?? 0;
            count += Description?.Length ?? 0;
            foreach (var f in Fields ?? [])
            {
                count += f.Name.Length;
                count += f.Value.Length;
            }
            count += Footer?.Text.Length ?? 0;
            count += Author?.Name.Length ?? 0;
            return count;
        }
    }

    /// <summary>
    /// Initializes a new embed instance.
    /// </summary>
    public Embed() 
    {
        Type = EmbedType.Rich;
    }

    [JsonConstructor]
    internal Embed(int color, string type)
    {
        Color = color != 0 ? new Color(color) : null;
        Type = GetEmbedType(type);
    }

    #region Public

    /// <summary>
    /// Creates a copy of an embed.
    /// </summary>
    /// <param name="embed">The embed to copy.</param>
    /// <returns>A unique copy of the given embed.</returns>
    public static Embed Copy(Embed embed) =>
        JsonConvert.DeserializeObject<Embed>(JsonConvert.SerializeObject(embed))!;

    /// <summary>
    /// Resets the embed to its empty state.
    /// </summary>
    public void Reset()
    {
        Url = null;
        Title = null;
        Description = null;
        Image = null;
        Color = null;
        Footer = null;
        Author = null;
        Thumbnail = null;
        Fields = null;
        Timestamp = null;
    }

    #endregion

    #region Private

    private static EmbedType GetEmbedType(string type)
    {
        return type switch
        {
            "rich" => EmbedType.Rich,
            "image" => EmbedType.Image,
            "video" => EmbedType.Video,
            "gifv" => EmbedType.Gifv,
            "article" => EmbedType.Article,
            "link" => EmbedType.Link,
            _ => EmbedType.Rich
        };
    }

    internal static List<Embed> CreateEmbeds(JSON[] embeds)
    {
        var convertedEmbeds = new List<Embed>();
        foreach (JSON e in embeds)
        {
            Embed ce = JsonConvert.DeserializeObject<Embed>(JsonConvert.SerializeObject(e))!;
            convertedEmbeds.Add(ce);
        }
        return convertedEmbeds;
    }

    #endregion
}

/// <summary>
/// Represents an embed footer.
/// </summary>
public record EmbedFooter
{
    /// <summary>
    /// Footer text.
    /// </summary>
    [JsonProperty("text")]
    public string Text;

    /// <summary>
    /// Icon URL.
    /// </summary>
    [JsonProperty("icon_url")]
    public string? IconUrl;

    /// <summary>
    /// A proxied URL of footer icon.
    /// </summary>
    [JsonProperty("proxy_icon_url")]
    public readonly string? ProxyIconUrl;

    /// <summary>
    /// Initializes a new embed footer instance.
    /// </summary>
    /// <param name="text">The text on the footer. Up to 2048 characters.</param>
    /// <param name="iconUrl">The URL for the image that will be displayed in the footer. Only supports HTTP(S).</param>
    public EmbedFooter(string text, string? iconUrl = null)
    {
        Text = text;
        IconUrl = iconUrl;
    }
}

/// <summary>
/// Represents the information for an <see cref="EmbedImage"/>, <see cref="EmbedThumbnail"/>, and <see cref="EmbedVideo"/>.
/// </summary>
public class EmbedMedia : Downloadable
{
    /// <summary>
    /// A proxied URL of the media.
    /// </summary>
    [JsonProperty("proxy_url")]
    public readonly string? ProxyUrl;

    /// <summary>
    /// Height of the media.
    /// </summary>
    [JsonProperty("height")]
    public readonly int? Height;

    /// <summary>
    /// Width of the media.
    /// </summary>
    [JsonProperty("width")]
    public readonly int? Width;

    internal EmbedMedia(string url, string? proxyUrl, int? height, int? width)
    {
        Url = url;
        ProxyUrl = proxyUrl;
        Height = height;
        Width = width;
    }
}

/// <summary>
/// Represents an embed image.
/// </summary>
public class EmbedImage : EmbedMedia
{
    /// <summary>
    /// Initializes a new embed image instance.
    /// </summary>
    /// <param name="url">Source URL of image. Only supports HTTP(S).</param>
    public EmbedImage(string url) : base(url, null, null, null) { }

    [JsonConstructor]
    internal EmbedImage(string url, string? proxy_url, int? height, int? width) : base(url, proxy_url, height, width) { }
}

/// <summary>
/// Represents an embed thumbnail.
/// </summary>
public class EmbedThumbnail : EmbedMedia
{
    /// <summary>
    /// Initializes a new embed thumbnail instance.
    /// </summary>
    /// <param name="url">Source URL of thumbnail. Only supports HTTP(S).</param>
    public EmbedThumbnail(string url) : base(url, null, null, null) { }

    [JsonConstructor]
    internal EmbedThumbnail(string url, string? proxy_url, int? height, int? width) : base(url, proxy_url, height, width) { }
}

/// <summary>
/// Represents embed video information.
/// </summary>
public class EmbedVideo : EmbedMedia
{
    [JsonConstructor]
    internal EmbedVideo(string url, string? proxy_url, int? height, int? width) : base(url, proxy_url, height, width) { }
}

/// <summary>
/// Represents embeds provider information.
/// </summary>
public record EmbedProvider
{
    /// <summary>
    /// Name of provider.
    /// </summary>
    [JsonProperty("name")]
    public readonly string? Name;

    /// <summary>
    /// URL of provider.
    /// </summary>
    [JsonProperty("url")]
    public readonly string? Url;
}

/// <summary>
/// Represents an embed author.
/// </summary>
public record EmbedAuthor
{
    [JsonProperty("name")]
    public string Name;
    
    [JsonProperty("url")]    
    public string? Url;

    [JsonProperty("icon_url")]
    public string? IconUrl;

    /// <summary>
    /// Initializes a new embed author instance.
    /// </summary>
    /// <param name="name">Name of the author. Up to 256 characters.</param>
    /// <param name="url">URL link for the author.</param>
    /// <param name="iconUrl">The URL for the image that will be displayed for the author. Only supports HTTP(S).</param>
    public EmbedAuthor(string name, string? url = null, string? iconUrl = null)
    {
        Name = name;
        Url = url;
        IconUrl = iconUrl;
    }
}

/// <summary>
/// Represents an embed field.
/// </summary>
public record EmbedField
{
    [JsonProperty("name")]
    public string Name;

    [JsonProperty("value")]
    public string Value;

    [JsonProperty("inline")]
    public bool Inline;

    /// <summary>
    /// Initializes a new embed field instance.
    /// </summary>
    /// <param name="name">Name of the field. Up to 256 characters.</param>
    /// <param name="value">Contents of the field. Up to 1024 characters.</param>
    /// <param name="inline">Whether this field should display inline.</param>
    public EmbedField(string name, string value, bool inline = false)
    {
        Name = name;
        Value = value;
        Inline = inline;
    }
}

/// <summary>
/// Represents an embed type.
/// </summary>
public enum EmbedType
{
    Rich,
    Image,
    Video,
    Gifv,
    Article,
    Link
}