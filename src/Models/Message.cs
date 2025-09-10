using Newtonsoft.Json;

namespace Discord.Models;

/// <summary>
/// Represents a Discord message.
/// </summary>
public class Message : IEquatable<Message>
{
    // Docs: https://discord.com/developers/docs/resources/message
    
    #region These fields are specific to the MESSAGE_CREATE/UPDATE events

    /// <summary>
    /// ID of the guild the message was sent in - unless it is an ephemeral message.
    /// </summary>
    [JsonProperty("guild_id")]
    public ulong? GuildId { get; }
    
    /// <summary>
    /// The <see cref="Author"/> of this message but returns their <see cref="Models.Member"/> object instead. Will be
    /// <c>null</c> for ephemeral messages and messages from webhooks.
    /// </summary>
    [JsonProperty("member")]
    public Member? Member { get; }

    /// <summary>
    /// Users specifically mentioned in the message.
    /// </summary>
    public IReadOnlyList<User> MentionedUsers => _mentions;
    [JsonProperty("mentions")] private List<User> _mentions = [];

    #endregion
    
    /// <summary>
    /// ID of the message.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; }
    
    /// <summary>
    /// ID of the channel the message was sent in.
    /// </summary>
    [JsonProperty("channel_id")]
    public ulong ChannelId { get; }
    
    /// <summary>
    /// Author of this message. If you need the <see cref="Models.Member"/> object instead, see <see cref="Member"/>.
    /// </summary>
    [JsonProperty("author")]
    public required User Author { get; init; }
    
    /// <summary>
    /// Content of the message. Will be an empty string if the <see cref="Intents.MessageContent"/> intent is disabled via
    /// the <see cref="Bot"/> constructor or Developer Portal.
    /// </summary>
    [JsonProperty("content")]
    public string Content { get; internal set; } = string.Empty;
    
    /// <summary>
    /// When the message was sent.
    /// </summary>
    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; }
    
    /// <summary>
    /// When the message was last edited (or <c>null</c>, if never).
    /// </summary>
    [JsonProperty("edited_timestamp")]
    public DateTime? EditedTimestamp { get; internal set; }
    
    /// <summary>
    /// Whether this was a TTS message.
    /// </summary>
    [JsonProperty("tts")]
    public bool Tts { get; }

    /// <summary>
    /// Whether this message mentions everyone.
    /// </summary>
    [JsonProperty("mention_everyone")]
    public bool EveryoneMentioned { get; }
    
    /// <summary>
    /// Roles specifically mentioned in this message.
    /// </summary>
    //public IReadOnlyList<Role> MentionedRoles => _roleMentions;
    [JsonProperty("mention_roles")] private List<ulong> _mentionedRoleIds = [];
    
    

    internal DateTime _expiration;
    
    public bool Equals(Message? other)
    {
        if (other is not null)
            return other.Id == Id;
        return false;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is Message m)
            return Equals(m);
        return false;
    }
    
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(Message? left, Message? right) => Equals(left, right);
    public static bool operator !=(Message? left, Message? right) => !Equals(left, right);
}
