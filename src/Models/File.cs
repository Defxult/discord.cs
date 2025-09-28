using Newtonsoft.Json;
using Discord.Utility;

namespace Discord.Models;

/// <summary>
/// Represents a Discord file.
/// </summary>
public record DFile
{
    /// <summary>
    /// Name of the file and its extension.
    /// </summary>
    public string Name;

    /// <summary>
    /// Bytes that represent the file.
    /// </summary>
    public readonly byte[] Bytes;

    /// <summary>
    /// Whether the file is blurred as a spoiler.
    /// </summary>
    public bool Spoiler => Name.StartsWith(SpoilerFlag);

    internal string _mimeTypeBase64;
    internal string _mimeType;
    private const string SpoilerFlag = "SPOILER_";
    
    /// <summary>
    /// Initializes a new file instance.
    /// </summary>
    /// <param name="name">Path or name of the file to include its extension.</param>
    public DFile(string name)
    {
        Name = Path.GetFileName(name);
        Bytes = File.ReadAllBytes(name);
        SetMimeType();
    }

    /// <summary>
    /// Initializes a new file instance.
    /// </summary>
    /// <param name="name">Name of the file and its extension.</param>
    /// <param name="bytes">The bytes representing that file.</param>
    /// <param name="spoiler">Whether the file should be marked as a spoiler when initially posted.</param>
    public DFile(string name, byte[] bytes, bool spoiler = false)
    {
        Name = spoiler ? SpoilerFlag + name : name;
        Bytes = bytes;
        SetMimeType();
    }

    private void SetMimeType()
    {
        var (type64, mimeType) = GetMimeTypeBase64(Name, Bytes);
        _mimeTypeBase64 = type64;
        _mimeType = mimeType;
    }

    private static (string mimeType64, string mimeType) GetMimeTypeBase64(string fileName, byte[] bytes)
    {
        var types = new Dictionary<string, string>
        {
            {"html", "text/html"},
            {"htm", "text/html"},
            {"shtml", "text/html"},
            {"css", "text/css"},
            {"xml", "text/xml"},
            {"gif", "image/gif"},
            {"jpeg", "image/jpeg"},
            {"jpg", "image/jpeg"},
            {"js", "application/javascript"},
            {"atom", "application/atom+xml"},
            {"rss", "application/rss+xml"},
            {"mml", "text/mathml"},
            {"txt", "text/plain"},
            {"jad", "text/vnd.sun.j2me.app-descriptor"},
            {"wml", "text/vnd.wap.wml"},
            {"htc", "text/x-component"},
            {"png", "image/png"},
            {"tif", "image/tiff"},
            {"tiff", "image/tiff"},
            {"wbmp", "image/vnd.wap.wbmp"},
            {"ico", "image/x-icon"},
            {"jng", "image/x-jng"},
            {"bmp", "image/x-ms-bmp"},
            {"svg", "image/svg+xml"},
            {"svgz", "image/svg+xml"},
            {"webp", "image/webp"},
            {"woff", "application/font-woff"},
            {"jar", "application/java-archive"},
            {"war", "application/java-archive"},
            {"ear", "application/java-archive"},
            {"json", "application/json"},
            {"hqx", "application/mac-binhex40"},
            {"doc", "application/msword"},
            {"pdf", "application/pdf"},
            {"ps", "application/postscript"},
            {"eps", "application/postscript"},
            {"ai", "application/postscript"},
            {"rtf", "application/rtf"},
            {"m3u8", "application/vnd.apple.mpegurl"},
            {"xls", "application/vnd.ms-excel"},
            {"eot", "application/vnd.ms-fontobject"},
            {"ppt", "application/vnd.ms-powerpoint"},
            {"wmlc", "application/vnd.wap.wmlc"},
            {"kml", "application/vnd.google-earth.kml+xml"},
            {"kmz", "application/vnd.google-earth.kmz"},
            {"7z", "application/x-7z-compressed"},
            {"cco", "application/x-cocoa"},
            {"jardiff", "application/x-java-archive-diff"},
            {"jnlp", "application/x-java-jnlp-file"},
            {"run", "application/x-makeself"},
            {"pl", "application/x-perl"},
            {"pm", "application/x-perl"},
            {"prc", "application/x-pilot"},
            {"pdb", "application/x-pilot"},
            {"rar", "application/x-rar-compressed"},
            {"rpm", "application/x-redhat-package-manager"},
            {"sea", "application/x-sea"},
            {"swf", "application/x-shockwave-flash"},
            {"sit", "application/x-stuffit"},
            {"tcl", "application/x-tcl"},
            {"tk", "application/x-tcl"},
            {"der", "application/x-x509-ca-cert"},
            {"pem", "application/x-x509-ca-cert"},
            {"crt", "application/x-x509-ca-cert"},
            {"xpi", "application/x-xpinstall"},
            {"xhtml", "application/xhtml+xml"},
            {"xspf", "application/xspf+xml"},
            {"zip", "application/zip"},
            {"bin", "application/octet-stream"},
            {"exe", "application/octet-stream"},
            {"dll", "application/octet-stream"},
            {"deb", "application/octet-stream"},
            {"dmg", "application/octet-stream"},
            {"iso", "application/octet-stream"},
            {"img", "application/octet-stream"},
            {"msi", "application/octet-stream"},
            {"msp", "application/octet-stream"},
            {"msm", "application/octet-stream"},
            {"docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
            {"xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
            {"pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"},
            {"mid", "audio/midi"},
            {"midi", "audio/midi"},
            {"kar", "audio/midi"},
            {"mp3", "audio/mpeg"},
            {"ogg", "audio/ogg"},
            {"m4a", "audio/x-m4a"},
            {"ra", "audio/x-realaudio"},
            {"3gpp", "video/3gpp"},
            {"3gp", "video/3gpp"},
            {"ts", "video/mp2t"},
            {"mp4", "video/mp4"},
            {"mpeg", "video/mpeg"},
            {"mpg", "video/mpeg"},
            {"mov", "video/quicktime"},
            {"webm", "video/webm"},
            {"flv", "video/x-flv"},
            {"m4v", "video/x-m4v"},
            {"mng", "video/x-mng"},
            {"asx", "video/x-ms-asf"},
            {"asf", "video/x-ms-asf"},
            {"wmv", "video/x-ms-wmv"},
            {"avi", "video/x-msvideo"}
        };
        string mimetype;
        string ext = Path.GetExtension(fileName);
        if (!string.IsNullOrEmpty(ext))
            mimetype = types.GetValueOrDefault(ext.Replace(".", string.Empty), "application/octet-stream");
        else
            throw new ArgumentException("A file extension was not provided");
        return ($"data:{mimetype};base64,{Convert.ToBase64String(bytes)}", mimetype);
    }
}

/// <summary>
/// Represents a Discord object that can be downloaded/converted into a <see cref="DFile"/> 
/// </summary>
public abstract class Downloadable
{
    /// <summary>
    /// Source URL of media.
    /// </summary>
    [JsonProperty("url")]
    public string Url { get; init; } = string.Empty;
    
    /// <summary>
    /// Convert the downloadable into a <see cref="DFile"/>.
    /// </summary>
    /// <param name="timeout">When the download will time out (defaults to 30 seconds).</param>
    /// <returns>The downloadable as a file.</returns>
    public async Task<DFile> DownloadAsync(TimeSpan? timeout = null)
    {
        DFile? f = null;
        await foreach (var file in Util.DownloadAsync([new Uri(Url)], timeout))
            f = file;
        return f!;
    }
}
