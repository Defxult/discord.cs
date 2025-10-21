using Discord.Utility;
using Discord.Net;

namespace Discord.Models;

/// <summary>
/// Represents a Discord CDN.
/// </summary>
public class Media : Downloadable, IEquatable<Media>
{
    /// <summary>
    /// The hash value of the media.
    /// </summary>
    public string Hash { get; }

    #region CUSTOM

    /// <summary>
    /// Whether the media is animated.
    /// </summary>
    public bool IsAnimated { get; }

    #endregion

    internal Media(string hash, string cdnHashUrl)
    {
        if (cdnHashUrl.StartsWith('/'))
        {
            Hash = hash;
            IsAnimated = hash.StartsWith("a_");
            Url = ApiRoute.Cdn.GetDescription() + cdnHashUrl + GetImageType();
        }
        else
            throw new ArgumentException($"parameter {nameof(cdnHashUrl)} must start with '/'");
    }
    
    public override bool Equals(object? other) => other is Media media && Equals(media);
    public bool Equals(Media? other) => Url == other?.Url;
    public override int GetHashCode() => Url.GetHashCode();

    /// <summary>
    /// Returns the URL.
    /// </summary>
    public override string ToString() =>
        Url;
    
    /// <summary>
    /// Convert this media to its file representation.
    /// </summary>
    /// <returns>A file.</returns>
    public async Task<DFile> ToFile() =>
        await Util.DownloadAsync(new Uri(Url));
    
    private string GetImageType() =>
        IsAnimated ? ".gif" : ".png";
}