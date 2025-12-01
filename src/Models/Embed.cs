using Newtonsoft.Json;

namespace Discord.Models;

/// <summary>
/// Represents a Discord embed.
/// </summary>
public class Embed
{
    // DOCS: https://discord.com/developers/docs/resources/message#embed-object
    
    /// <summary>
    /// Embed type.
    /// </summary>
    [JsonIgnore]
    public EmbedType Type { get; }

    /// <summary>
    /// The title; max 256 characters.
    /// </summary>
    [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
    public string? Title { get; set; }

    /// <summary>
    /// The description; max 4096 characters.
    /// </summary>
    [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
    public string? Description { get; set; }

    /// <summary>
    /// URL of embed.
    /// </summary>
    [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
    public string? Url { get; set; }

    /// <summary>
    /// Timestamp of embed.
    /// </summary>
    [JsonProperty("timestamp", NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? Timestamp { get; set; }

    /// <summary>
    /// Color of the embed.
    /// </summary>
    [JsonIgnore]
    public Color? Color { get; set; }
    [JsonProperty("color", NullValueHandling = NullValueHandling.Ignore)] private int? _color => Color?.Value;

    /// <summary>
    /// Footer information.
    /// </summary>
    [JsonProperty("footer", NullValueHandling = NullValueHandling.Ignore)]
    public EmbedFooter? Footer { get; set; }

    /// <summary>
    /// Image information.
    /// </summary>
    [JsonProperty("image", NullValueHandling = NullValueHandling.Ignore)]
    public EmbedImage? Image { get; set; }

    /// <summary>
    /// Thumbnail information.
    /// </summary>
    [JsonProperty("thumbnail", NullValueHandling = NullValueHandling.Ignore)]
    public EmbedThumbnail? Thumbnail { get; set; }

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
    public EmbedAuthor? Author { get; set; }

    /// <summary>
    /// Embed fields (max 25).
    /// </summary>
    [JsonProperty("fields", NullValueHandling = NullValueHandling.Ignore)]
    public ICollection<EmbedField>? Fields { get; set; }

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
    /// Initializes a new embed instance. Example:
    /// <code>
    /// var embed = new Embed
    /// {
    ///     Title = "discord.cs",
    ///     Color = Color.Blurple
    /// }
    /// </code>
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
}

/// <summary>
/// Represents an embed footer.
/// </summary>
public record EmbedFooter
{
    // DOCS: https://discord.com/developers/docs/resources/message#embed-object-embed-footer-structure
    
    /// <summary>
    /// Footer text.
    /// </summary>
    [JsonProperty("text")]
    public string Text { get; set; }

    /// <summary>
    /// Icon URL.
    /// </summary>
    [JsonProperty("icon_url")]
    public string? IconUrl { get; set; }

    /// <summary>
    /// A proxied URL of footer icon.
    /// </summary>
    [JsonProperty("proxy_icon_url")]
    public string? ProxyIconUrl { get; init; }

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
    public string? ProxyUrl { get; init; }

    /// <summary>
    /// Height of the media.
    /// </summary>
    [JsonProperty("height")]
    public int? Height { get; init; }

    /// <summary>
    /// Width of the media.
    /// </summary>
    [JsonProperty("width")]
    public int? Width { get; init; }

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
    // DOCS: https://discord.com/developers/docs/resources/message#embed-object-embed-image-structure
    
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
    // DOCS: https://discord.com/developers/docs/resources/message#embed-object-embed-thumbnail-structure
    
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
    // DOCS: https://discord.com/developers/docs/resources/message#embed-object-embed-video-structure
    
    [JsonConstructor]
    internal EmbedVideo(string url, string? proxy_url, int? height, int? width) : base(url, proxy_url, height, width) { }
}

/// <summary>
/// Represents embeds provider information.
/// </summary>
public record EmbedProvider
{
    // DOCS: https://discord.com/developers/docs/resources/message#embed-object-embed-provider-structure
    
    /// <summary>
    /// Name of provider.
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; init; }

    /// <summary>
    /// URL of provider.
    /// </summary>
    [JsonProperty("url")]
    public string? Url { get; init; }
    
    private EmbedProvider() { }
}

/// <summary>
/// Represents an embed author.
/// </summary>
public record EmbedAuthor
{
    // DOCS: https://discord.com/developers/docs/resources/message#embed-object-embed-author-structure
    
    /// <summary>
    /// Name of the author. Up to 256 characters.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; }
    
    /// <summary>
    /// URL link for the author.
    /// </summary>
    [JsonProperty("url")]    
    public string? Url { get; set; }

    /// <summary>
    /// The URL for the image that will be displayed for the author. Only supports HTTP(S).
    /// </summary>
    [JsonProperty("icon_url")]
    public string? IconUrl { get; set; }

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
    // DOCS: https://discord.com/developers/docs/resources/message#embed-object-embed-field-structure
    
    /// <summary>
    /// Name of the field. Up to 256 characters.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; }

    /// <summary>
    /// Contents of the field. Up to 1024 characters.
    /// </summary>
    [JsonProperty("value")]
    public string Value { get; set; }

    /// <summary>
    /// Whether this field should display inline.
    /// </summary>
    [JsonProperty("inline")]
    public bool Inline { get; set; }

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
    // DOCS: https://discord.com/developers/docs/resources/message#embed-object-embed-types
    
    /// <summary>
    /// Generic embed rendered from embed attributes.
    /// </summary>
    Rich,
    
    /// <summary>
    /// Image embed.
    /// </summary>
    Image,
    
    /// <summary>
    /// Video embed.
    /// </summary>
    Video,
    
    /// <summary>
    /// Animated gif image embed rendered as a video embed.
    /// </summary>
    Gifv,
    
    /// <summary>
    /// Article embed.
    /// </summary>
    Article,
    
    /// <summary>
    /// Link embed.
    /// </summary>
    Link,
    
    /// <summary>
    /// Poll result embed.
    /// </summary>
    PollResult
}
