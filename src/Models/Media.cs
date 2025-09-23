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
    public readonly string Hash;

    #region CUSTOM

    /// <summary>
    /// Whether the media is animated.
    /// </summary>
    public readonly bool IsAnimated;

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
    
    private string GetImageType() =>
        IsAnimated ? ".gif" : ".png";
}