using Newtonsoft.Json;

namespace Discord.Models;

/// <summary>
/// Represents a Discord message.
/// </summary>
public class Message : IEquatable<Message>
{
    /// <summary>
    /// Message ID.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; init; }
    
    /// <summary>
    /// Content of the message.
    /// </summary>
    [JsonProperty("content")]
    public string Content { get; internal set; } = string.Empty;
    
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
