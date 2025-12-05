using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Discord.Channels.Abstractions;
using Discord.Channels.Models;
using Discord.Net;
using Discord.Utility;
using Newtonsoft.Json;

namespace Discord.Models;

/// <summary>
/// Represents a Webhook.
/// </summary>
public class Webhook
{
    // DOCS: https://discord.com/developers/docs/resources/webhook#webhook-object
    
    /// <summary>
    /// Webhook ID.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; init; }
    
    /// <summary>
    /// The type of webhook.
    /// </summary>
    [JsonProperty("type")]
    public WebhookType Type { get; init; }
    
    /// <summary>
    /// Guild ID this webhook is for, if any.
    /// </summary>
    [JsonProperty("guild_id")]
    public ulong? GuildId { get; init; }
    
    /// <summary>
    /// Channel ID this webhook is for, if any.
    /// </summary>
    [JsonProperty("channel_id")]
    public ulong? ChannelId { get; init; }
    
    /// <summary>
    /// User this webhook was created by (not returned when getting a webhook with its token).
    /// </summary>
    [JsonProperty("user")]
    public User? User { get; init; }

    /// <summary>
    /// Default name of the webhook.
    /// </summary>
    [JsonProperty("name")] 
    public string? Name { get; init; }

    /// <summary>
    /// Default user avatar of the webhook.
    /// </summary>
    public Media? Avatar => _defaultAvatarHash != null
        ? new Media(_defaultAvatarHash, $"/avatars/{Id}/{_defaultAvatarHash}")
        : null;
    [JsonProperty("avatar")] private string? _defaultAvatarHash;
    
    /// <summary>
    /// The secure token of the webhook (returned for <see cref="WebhookType.Incoming"/> Webhooks).
    /// </summary>
    [JsonProperty("token")]
    public string? Token { get; init; }
    
    /// <summary>
    /// The bot/OAuth2 application that created this webhook.
    /// </summary>
    [JsonProperty("application_id")]
    public ulong? ApplicationId { get; init; }
    
    /// <summary>
    /// Guild of the channel that this webhook is following (returned for <see cref="WebhookType.ChannelFollower"/> Webhooks).
    /// </summary>
    [JsonProperty("source_guild")]
    public PartialWebhookGuild? Guild { get; init; }
    
    /// <summary>
    /// Channel that this webhook is following (returned for <see cref="WebhookType.ChannelFollower"/> Webhooks).
    /// </summary>
    [JsonProperty("source_channel")]
    public PartialWebhookChannel? Channel { get; init; }
    
    /// <summary>
    /// URL used for executing the webhook (returned by the webhooks OAuth2 flow).
    /// </summary>
    [JsonProperty("url")]
    public string? Url { get; init; }

    /// <summary>
    /// Your bot instance.
    /// </summary>
    public Bot Bot { get; internal set; } = null!;
    
    private Webhook() { }

    /// <summary>
    /// Edit the webhook.
    /// </summary>
    /// <param name="edit">A webhook edit instance.</param>
    /// <param name="reason">Reason for editing the webhook. This is displayed in the audit-log.</param>
    /// <returns>The updated webhook.</returns>
    public async Task<Webhook> EditAsync(WebhookEdit edit, string? reason = null) =>
        await Bot._rest.ModifyWebhookAsync(Id, edit, reason);
    
    /// <summary>
    /// Delete the webhook.
    /// </summary>
    /// <param name="reason">Reason for deleting the webhook. This is displayed in the audit-log.</param>
    public async Task DeleteAsync(string? reason = null) =>
        await Bot._rest.DeleteWebhookAsync(Id, reason);

    /// <summary>
    /// Request a webhook based on its URL.
    /// </summary>
    /// <param name="url">Webhook URL.</param>
    /// <param name="bot">Bot instance to assist with initialization.</param>
    /// <param name="http">An HTTP client. The library does not handle disposing the client.</param>
    /// <returns>The webhook matching the URL.</returns>
    /// <exception cref="FormatException">Parameter <paramref name="url"/> was not in the correct format.</exception>
    public static async Task<Webhook> FromUrl(string url, Bot bot, HttpClient http)
    {
        var match = Regex.Match(url, @"https:\/\/discord[.]com\/api\/webhooks\/\d{17,19}\/.+");
        if (!match.Success) throw new FormatException("Parameter 'url' was not in the proper webhook URL format.");
        var splits = match.Value.Split("/");
        var id = ulong.Parse(splits[^2]);
        var token = splits.Last();
        return await WithTokenIdAsync(id, token, bot, http);
    }

    /// <summary>
    /// Request a webhook by its ID and token which does not require authentication.
    /// </summary>
    /// <param name="id">ID of the webhook.</param>
    /// <param name="token">Token of the webhook.</param>
    /// <param name="bot">Bot instance to assist with initialization.</param>
    /// <param name="http">An HTTP client. The library does not handle disposing the client.</param>
    /// <returns>The requested webhook.</returns>
    public static async Task<Webhook> WithTokenIdAsync(ulong id, string token, Bot bot, HttpClient http)
    {
        var webhook = await Rest.GetWebhookWithTokenAsync(id, token, http);
        webhook.Bot = bot;
        return webhook;
    }
    
    /// <summary>
    /// Edit the webhook by its ID and token which does not require authentication.
    /// </summary>
    /// <param name="id">ID of the webhook.</param>
    /// <param name="token">Token of the webhook.</param>
    /// <param name="edit">Webhook edit instance..</param>
    /// <param name="bot">Bot instance to assist with initialization.</param>
    /// <param name="http">An HTTP client. The library does not handle disposing the client.</param>
    /// <returns>The requested webhook.</returns>
    /// <remarks>Does not support edit value <see cref="WebhookEdit.SetChannel"/>.</remarks>
    public static async Task<Webhook> EditWithTokenIdAsync(ulong id, string token, WebhookEdit edit, Bot bot, HttpClient http)
    {
        var webhook = await Rest.ModifyWebhookWithTokenAsync(id, token, edit, http);
        webhook.Bot = bot;
        return webhook;
    }
    
    /// <summary>
    /// Delete the webhook by its ID and token which does not require authentication.
    /// </summary>
    /// <param name="id">ID of the webhook.</param>
    /// <param name="token">Token of the webhook.</param>
    /// <param name="http">An HTTP client. The library does not handle disposing the client.</param>
    public static async Task DeleteWithTokenIdAsync(ulong id, string token, HttpClient http) =>
        await Rest.DeleteWebhookWithTokenAsync(id, token, http);

    /// <summary>
    /// Send a message to the channel of this webhook.
    /// </summary>
    /// <param name="content">Message contents (up to 2000 characters).</param>
    /// <param name="silent">If <c>true</c>, mentions will not provide a desktop or push notification.</param>
    /// <param name="username">Override the default username of the webhook.</param>
    /// <param name="avatarUrl">Override the default avatar of the webhook.</param>
    /// <param name="tts">Whether this is a TTS message.</param>
    /// <param name="embeds">Embeds (max 10), up to 6000 characters total.</param>
    /// <param name="allowedMentions">Allowed mentions for the message.</param>
    /// <param name="threadId">Send a message to the specified thread within a webhook's channel. The thread will
    /// automatically be unarchived.
    /// </param>
    /// <param name="threadName">Name of the thread to create.</param>
    /// <param name="wait">Whether it should wait for server confirmation of message send before response, and returns
    /// the created message.
    /// </param>
    /// <param name="appliedTags">Tags to apply if thread is created in a <see cref="ForumChannel"/> or <see cref="MediaChannel"/>.</param>
    /// <param name="poll">A poll.</param>
    /// <param name="files">Files to upload with the message.</param>
    /// <returns>A webhook message if <paramref name="wait"/> is <c>true</c>, <c>null</c> otherwise.</returns>
    /// <remarks>
    /// If the webhook channel is a <see cref="ForumChannel"/> or <see cref="MediaChannel"/>, you must provide either
    /// <paramref name="threadId"/> or <paramref name="threadName"/>. If <paramref name="threadId"/> is provided, the
    /// message will send in that thread. If <paramref name="threadName"/> is provided, a thread with that name will be
    /// created in the channel.
    /// </remarks>
    public async Task<WebhookMessage?> SendAsync(
        string? content = null,
        bool silent = false,
        string? username = null,
        string? avatarUrl = null,
        bool tts = false,
        IEnumerable<Embed>? embeds = null,
        AllowedMentions? allowedMentions = null,
        ulong? threadId = null,
        string? threadName = null,
        bool wait = false,
        IEnumerable<Tag>? appliedTags = null,
        Poll? poll = null,
        ICollection<DFile>? files = null)
    {
        var form = new MultipartFormDataContent(Dev.Boundary);
        var payload = new JSON();

        if (content != null)
        {
            payload["content"] = content;
            if (silent)
                payload["flags"] = Util.FromFlags([MessageFlag.SuppressNotifications]);
        }
        if (username != null)
            payload["username"] = username;
        if (avatarUrl != null)
            payload["avatar_url"] = avatarUrl;
        payload["tts"] = tts;
        if (embeds != null)
            payload["embeds"] = embeds;
        if (allowedMentions is { } am)
            payload["allowed_mentions"] = am.ToJson();
        if (appliedTags != null)
            payload["applied_tags"] = appliedTags.Select(t => t.Id).ToList();
        if (poll != null)
            payload["poll"] = poll;
        if (threadName != null)
            payload["thread_name"] = threadName;
        
        // Files **** (leave as last due to adding to form) ****
        var jsonContent = new StringContent(JsonConvert.SerializeObject(payload));
        jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        form.Add(jsonContent, "payload_json");
        
        if (files != null)
        {
            var list = files.ToList();
            for (var i = 0; i < list.Count; i++)
            {
                var file = list[i];
                var bac = new ByteArrayContent(file.Bytes);
                bac.Headers.ContentType = new MediaTypeHeaderValue(file._mimeType);
                form.Add(bac, $"files[{i}]", file.Name);
            }
        }

        return await Bot._rest.ExecuteWebhookAsync(this, threadId, wait, form);
    }
}

/// <summary>
/// Represents a message that was sent via <see cref="Webhook"/>.
/// </summary>
public record WebhookMessage
{
    /// <inheritdoc cref="Message.Content"/>
    [JsonProperty("content")]
    public string Content { get; private set; } = string.Empty;
    
    /// <inheritdoc cref="Message.Timestamp"/>
    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; init; }
    
    /// <inheritdoc cref="Message.Tts"/>
    [JsonProperty("tts")]
    public bool Tts { get; init; }

    /// <inheritdoc cref="Message.EveryoneMentioned"/>
    public bool EveryoneMentioned { get; init; }

    /// <inheritdoc cref="Message.Attachments"/>
    [JsonProperty("attachments")]
    public IReadOnlyCollection<MessageAttachment> Attachments { get; init; } = [];
    
    /// <inheritdoc cref="Message.Embeds"/>
    [JsonProperty("embeds")]
    public IReadOnlyList<Embed> Embeds { get; private set; } = [];
    
    private WebhookMessage() { }
}

/// <summary>
/// Represents the values that can be edited for a <see cref="Webhook"/>. 
/// </summary>
public readonly struct WebhookEdit
{
    internal readonly JSON _payload = [];
    
    /// <summary>
    /// Initialize a new webhook edit instance.
    /// </summary>
    public WebhookEdit() { }

    /// <summary>
    /// Set the webhook name.
    /// </summary>
    /// <param name="name">Default name of the webhook.</param>
    /// <returns>The edit instance.</returns>
    public WebhookEdit SetName(string name)
    {
        _payload["name"] = name;
        return this;
    }
    
    /// <summary>
    /// Set the webhook avatar.
    /// </summary>
    /// <param name="avatar">Image for the default webhook avatar, or <c>null</c> to replace it with a default avatar.</param>
    /// <returns>The edit instance.</returns>
    public WebhookEdit SetAvatar(DFile? avatar)
    {
        _payload["avatar"] = avatar?._mimeTypeBase64;
        return this;
    }
    
    /// <summary>
    /// Set the webhook channel.
    /// </summary>
    /// <param name="channel">The new channel (<see cref="ICoreGuildChannel"/>) this webhook should be moved to.</param>
    /// <returns>The edit instance.</returns>
    public WebhookEdit SetChannel<T>(T channel) where T : GuildChannel, ICoreGuildChannel
    {
        _payload["channel_id"] = channel.Id;
        return this;
    }
}

/// <summary>
/// Represents a <see cref="Webhook"/> type.
/// </summary>
public enum WebhookType
{
    /// <summary>
    /// Incoming Webhooks can post messages to channels with a generated token.
    /// </summary>
    Incoming = 1,
    
    /// <summary>
    /// Channel Follower Webhooks are internal webhooks used with Channel Following to post new messages into channels.
    /// </summary>
    ChannelFollower,
    
    /// <summary>
    /// Application webhooks are webhooks used with Interactions.
    /// </summary>
    Application
}

/// <summary>
/// Represents a partial guild from a <see cref="Webhook"/>.
/// </summary>
public record PartialWebhookGuild
{
    /// <summary>
    /// ID of the guild.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; init; }
    
    /// <summary>
    /// Name of the guild.
    /// </summary>
    [JsonProperty("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Guild icon.
    /// </summary>
    public Media? Icon => _icon != null ? new Media(_icon, $"/icons/{Id}/{_icon}") : null;
    [JsonProperty("icon")] private string? _icon;
    
    private PartialWebhookGuild() { }
}

/// <summary>
/// Represents a partial channel from a <see cref="Webhook"/>.
/// </summary>
public record PartialWebhookChannel
{
    /// <summary>
    /// ID of the channel.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; init; }
    
    /// <summary>
    /// Name of the channel.
    /// </summary>
    [JsonProperty("name")]
    public required string Name { get; init; }
    
    private PartialWebhookChannel() { }
}
