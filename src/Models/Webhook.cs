using Newtonsoft.Json;

namespace Discord.Models;

/// <summary>
/// Represents a Webhook.
/// </summary>
public class Webhook
{
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
    
    private Webhook() { }
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
