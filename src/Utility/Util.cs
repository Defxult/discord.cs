using System.ComponentModel;
using System.Text.Json;
using Discord.Models;
using Discord.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Discord.Utility;

/// <summary>
/// Contains various methods helpful methods.
/// </summary>
public static class Util
{
    /// <summary>
    /// Discord's Unix timestamp, the first second of 2015.
    /// </summary>
    public const ulong DiscordEpoch = 1420070400000;
    
    /// <summary>
    /// Convert the snowflake to the DateTime it represents.
    /// </summary>
    /// <param name="id">The snowflake ID to convert.</param>
    /// <returns>The snowflake converted into a DateTime.</returns>
    public static DateTime SnowflakeToDateTime(ulong id)
    {
        var value = ((id >> 22) + DiscordEpoch) / 1000;
        var dto = DateTimeOffset.FromUnixTimeSeconds((long)value);
        return dto.UtcDateTime;
    }

    /// <summary>
    /// Convert the DateTime to the snowflake it represents.
    /// </summary>
    /// <param name="dt">A datetime.</param>
    /// <returns>The DateTime converted into a snowflake.</returns>
    public static ulong DateTimeToSnowflake(DateTime dt)
    {
        DateTimeOffset dto = dt.ToUniversalTime();
        var timestamp = (ulong)(dto.ToUnixTimeSeconds() * 1000) - DiscordEpoch;
        return (ulong)((timestamp << 22) + Math.Pow(2, 22));
    }
    
    /// <summary>
    /// Convert the URLs into files.
    /// </summary>
    /// <param name="uris">URLs to extract the data from. These must end in a path extension: <c>.png</c>, <c>.gif</c>, <c>.mp3</c> etc.</param>
    /// <param name="timeout">When the download will time out (defaults to 30 seconds).</param>
    /// <returns>All URLs converted into a <see cref="DFile"/>.</returns>
    /// <exception cref="ArgumentException">Not all URLs ended in a path extension.</exception>
    public static async IAsyncEnumerable<DFile> DownloadAsync(IReadOnlyCollection<Uri> uris, TimeSpan? timeout = null)
    {
        if (uris.Count == 0) yield break;
        
        using var http = new HttpClient();
        http.Timeout = timeout ?? TimeSpan.FromSeconds(30);
        
        if (uris.Any(u => Path.GetExtension(u.AbsolutePath) == string.Empty)) 
            throw new ArgumentException($"All URIs in '{nameof(uris)}' must be a file (end in an extension)");

        var tasks = uris.Select(async uri =>
        {
            var bytes = await http.GetByteArrayAsync(uri);
            return new DFile(Path.GetFileName(uri.AbsolutePath), bytes);
        }).ToList();

        while (tasks.Count > 0)
        {
            var finished = await Task.WhenAny(tasks);
            tasks.Remove(finished);
            yield return await finished;
        }
    }

    /// <summary>
    /// A non-IAsyncEnumerable wrapper shortcut for <see cref="DownloadAsync(IReadOnlyCollection{Uri},TimeSpan?)"/>
    /// </summary>
    /// <exception cref="InvalidOperationException">The file could not be downloaded.</exception>
    public static async Task<DFile> DownloadAsync(Uri uri, TimeSpan? timeout = null)
    {
        await foreach (var file in DownloadAsync([uri], timeout))
            return file;
        throw new InvalidOperationException("Could not download file");
    }

    internal static T ExtractFromJson<T>(string json, string key)
    {
        var document = JsonDocument.Parse(json);
        document.RootElement.TryGetProperty(key, out JsonElement element);
        return DiscordGatewayClient.DeserializeWithNewtonsoft<T>(element);
    }
    
    internal static List<T> FromBitfield<T>(int value)
    {
        var converted = new List<T>();
        foreach (var flag in Enum.GetValues(typeof(T)))
            if ((value & (int)flag) == (int)flag)
                converted.Add((T)flag);
        return converted;
    }
}

/// <summary>
/// Contains all methods related to Discords Markdown capabilities.
/// </summary>
public static class Markdown
{
    /// <summary>
    /// Enables the ability to click on the email and use "mailto:email".
    /// </summary>
    /// <param name="email">An email address</param>
    /// <returns>The clickable email.</returns>
    public static string MailTo(string email) => $"<{email}>";
    
    /// <summary>
    /// Bolds the given text.
    /// </summary>
    /// <param name="text">Text to bold.</param>
    /// <returns>The bolded text.</returns>
    public static string Bold(string text) => $"**{text}**";
    
    /// <summary>
    /// Italicizes the given text.
    /// </summary>
    /// <param name="text">Text to italicize.</param>
    /// <returns>The italicized text.</returns>
    public static string Italic(string text) => $"*{text}*";
    
    /// <summary>
    /// Underlines the given text.
    /// </summary>
    /// <param name="text">Text to underline.</param>
    /// <returns>The underlined text.</returns>
    public static string Underline(string text) => $"__{text}__";
    
    /// <summary>
    /// Converts the text into a formatted strikethrough.
    /// </summary>
    /// <param name="text">Text to strikethrough.</param>
    /// <returns>The formatted text.</returns>
    public static string Strikethrough(string text) => $"~~{text}~~";
    
    /// <summary>
    /// Converts the text to a formatted block quote.
    /// </summary>
    /// <param name="text">The text to block quote.</param>
    /// <param name="multiline">Whether the text should be wrapped entirely in a block quote. If <c>false</c>, only the first line will be in a block quote.</param>
    /// <returns>The formatted block quote.</returns>
    public static string BlockQuote(string text, bool multiline = false) =>
        multiline ? $">>> {text}" : $"> {text}";
    
    /// <summary>
    /// Converts the code into a formatted code block for the desired language.
    /// </summary>
    /// <param name="language">Language the text should be converted into, or <c>null</c> for plain text.</param>
    /// <param name="code">The code itself.</param>
    /// <returns>A formatted code block.</returns>
    public static string CodeBlock(string? language, string code) =>
        $"```{language ?? string.Empty}\n{code}\n```";
    
    /// <summary>
    /// Converts the code into a formatted inline code.
    /// </summary>
    /// <param name="code">The code itself.</param>
    /// <returns>The formatted inline code.</returns>
    public static string InlineCode(string code) => $"`{code}`";
    
    /// <summary>
    /// Wraps the given text in spoiler tags.
    /// </summary>
    /// <param name="text">Text to wrap.</param>
    /// <returns>The wrapped text.</returns>
    public static string Spoiler(string text) => $"||{text}||";
    
    /// <summary>
    /// Constructs a channel link.
    /// </summary>
    /// <param name="guildId">Guild ID of the channel.</param>
    /// <param name="channelId">Channel ID.</param>
    /// <returns>A URL for the channel.</returns>
    public static string ChannelLink(ulong guildId, ulong channelId) => 
        $"https://discord.com/channels/{guildId}/{channelId}";
    
    /// <summary>
    /// Constructs a message link.
    /// </summary>
    /// <param name="guildId">Guild ID of the message.</param>
    /// <param name="channelId">Channel ID of the message.</param>
    /// <param name="messageId">Message ID.</param>
    /// <returns>A URL for the channel.</returns>
    public static string MessageLink(ulong guildId, ulong channelId, ulong messageId) => 
        $"https://discord.com/channels/{guildId}/{channelId}/{messageId}";

    /// <summary>
    /// Prevents the website embed from being displayed when a URL is posted.
    /// </summary>
    /// <param name="uri">The URL.</param>
    /// <returns>Prevents the website embed from being displayed when a URL is posted.</returns>
    public static string SuppressLinkEmbed(Uri uri) => $"<{uri}>";

    /// <summary>
    /// Masks the given link.
    /// </summary>
    /// <param name="title">Title of the masked link. This is what is displayed in Discord.</param>
    /// <param name="uri">The URL.</param>
    /// <param name="suppressEmbed">Whether to suppress the link embed.</param>
    /// <returns></returns>
    public static string MaskedLink(string title, Uri uri, bool suppressEmbed = true) =>
        $"[{title}]({(suppressEmbed ? SuppressLinkEmbed(uri) : uri)})";

    /// <summary>
    /// Converts the text to a formatted header.
    /// </summary>
    /// <param name="size">Size of the header. 1 = big, 2 = medium, 3 = small.</param>
    /// <param name="text">Text to format.</param>
    /// <returns>The formatted text.</returns>
    public static string Header(int size, string text)
    {
        var s = size < 1 ? 1 : Math.Min(size, 3);
        var headers = new string('#', s);
        return $"{headers} {text}";
    }
    
    /// <summary>
    /// Reduces the text size to a footnote style size. 
    /// </summary>
    /// <param name="text">Text to format.</param>
    /// <returns></returns>
    public static string Minimize(string text) => $"-# {text}";

    /// <summary>
    /// Converts the given items into a formatted bullet point list. This does not support bullet point indentation
    /// for inner bullet points.
    /// </summary>
    /// <param name="items">Items in the list.</param>
    /// <returns>A bullet point list.</returns>
    public static string BulletPointList(IEnumerable<string> items)
    {
        var bulletPointList = items.Select(i => $"- {i}\n");
        return string.Concat(bulletPointList);
    }

    /// <summary>
    /// Converts the parameters to a custom guild emoji.
    /// </summary>
    /// <param name="name">Name of the custom emoji.</param>
    /// <param name="id">ID of the custom emoji.</param>
    /// <param name="animated">Whether the custom emoji is animated.</param>
    /// <returns>he custom emoji.</returns>
    public static string CustomEmoji(string name, ulong id, bool animated) =>
        $"<{(animated ? "a" : string.Empty)}:{name}:{id}>";
    
    /// <summary>
    /// Mentions the role.
    /// </summary>
    /// <param name="id">Role ID.</param>
    /// <returns>The role in the mentioned format.</returns>
    public static string MentionRole(ulong id) => $"<@&{id}>";
    
    /// <summary>
    /// Mentions the channel.
    /// </summary>
    /// <param name="id">Channel ID.</param>
    /// <returns>The channel in the mentioned format.</returns>
    public static string MentionChannel(ulong id) => $"<#{id}>";
    
    /// <summary>
    /// Mentions the user.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <returns>The user in the mentioned format.</returns>
    public static string MentionUser(ulong id) => $"<@{id}>";
    
    /// <summary>
    /// Mentions the "Channels and Roles" channel with the *Customize* tab pre-selected.
    /// </summary>
    public const string MentionChannelAndRoles = "<id:customize>";
    
    /// <summary>
    /// Mentions the "Channels and Roles" channel with the *Browse Channels* tab pre-selected. 
    /// </summary>
    public const string MentionBrowseChannels = "<id:browse>";

    /// <summary>
    /// Mentions the "Server Guide" channel.
    /// </summary>
    public const string MentionServerGuide = "<id:guide>";
    
    /// <summary>
    /// Mentions the linked roles menu.
    /// </summary>
    public const string MentionLinkedRole = "<id:linked-roles>";
    
    /// <summary>
    /// Mentions the <c>@everyone</c> role.
    /// </summary>
    public const string MentionEveryone = "@everyone";

    /// <summary>
    /// Mentions everyone who is currently active, aka <c>@here</c>.
    /// </summary>
    public const string MentionHere = "@here";
    
    /// <summary>
    /// Mentions the slash command. Subcommands and subcommand groups can also be mentioned by using names respectively:
    /// <code>
    ///     // This is the command: /tag get [name]
    ///     string mention = Markdown.MentionSlashCommand("tag get", 1234567890123456789);
    /// </code>
    /// </summary>
    /// <param name="name">Name of the slash command.</param>
    /// <param name="id">ID of the slash command.</param>
    /// <returns>Slash command in the mentioned format.</returns>
    public static string MentionSlashCommand(string name, ulong id) => $"</{name}:{id}>";

    /// <summary>
    /// Format a date to a Discord timestamp that will display the given timestamp in the user's timezone and locale.
    /// </summary>
    /// <param name="dt">Date/time to format.</param>
    /// <param name="style">The date/time style.</param>
    /// <returns>A formatted timestamp.</returns>
    public static string Timestamp(DateTime dt, TimestampStyle style = TimestampStyle.ShortDateTime)
    {
        var unix = ((DateTimeOffset)dt).ToUnixTimeSeconds();
        return $"<t:{unix}:{style.GetDescription()}>";
    }
}

/// <summary>
/// Represents a Discord timestamp. Timestamps will display the given timestamp in the user's timezone and locale.
/// </summary>
public enum TimestampStyle
{
    /// <summary>
    /// Example: 3:30PM
    /// </summary>
    [Description("t")]
    ShortTime,
    
    /// <summary>
    /// Example: 3:30:15PM
    /// </summary>
    [Description("T")]
    LongTime,
    
    /// <summary>
    /// Example: 12/25/2024
    /// </summary>
    [Description("d")]
    ShortDate,
    
    /// <summary>
    /// Example: December 25, 2024
    /// </summary>
    [Description("D")]
    LongDate,
    
    /// <summary>
    /// Example: December 25, 2024 at 3:30PM
    /// </summary>
    [Description("f")]
    ShortDateTime,
    
    /// <summary>
    /// Example: Wednesday, December 25, 2024 at 3:30PM
    /// </summary>
    [Description("F")]
    LongDateTime,
    
    /// <summary>
    /// Example: 2 months ago
    /// </summary>
    [Description("R")]
    Relative
}

internal static class Dev
{
    internal static void Log(string message, bool timestamp = true)
    {
        if (Environment.GetEnvironmentVariable("##set_logging##") is null) return;
        var m = timestamp ? $"[{DateTime.Now:MM-dd-yyyy HH:mm:ss.fff}] {message}" : message;
        Console.WriteLine(m);
    }

    internal static void PrettyPrint(string json)
    {
        var parsed = JToken.Parse(json);
        string prettyJson = parsed.ToString(Formatting.Indented);
        Console.WriteLine(prettyJson);
    }
}
