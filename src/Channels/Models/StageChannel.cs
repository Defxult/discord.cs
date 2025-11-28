using Discord.Models;
using Newtonsoft.Json;

namespace Discord.Channels.Models;

/// <summary>
/// Represents a stage channel for a <see cref="Guild"/>.
/// </summary>
public class StageChannel : VoiceChannel
{
    private StageChannel() { }

    /// <summary>
    /// Create a stage instance.
    /// </summary>
    /// <param name="topic">Topic of the Stage instance (1-120 characters).</param>
    /// <param name="privacyLevel">Privacy level of the Stage instance.</param>
    /// <param name="sendStartNotification">Notify @everyone that a Stage instance has started. The stage moderator must
    /// have <see cref="Permission.MentionEveryone"/> for this notification to be sent.</param>
    /// <param name="scheduledEvent">Guild scheduled event associated with this Stage instance.</param>
    /// <param name="reason">Reason for creating the stage instance. This is displayed in the audit-log</param>
    /// <returns>The created stage instance.</returns>
    public async Task<StageInstance> CreateInstanceAsync(string topic,
        StageInstancePrivacyLevel privacyLevel = StageInstancePrivacyLevel.GuildOnly, bool sendStartNotification = true,
        ScheduledEvent? scheduledEvent = null, string? reason = null)
    {
        var payload = new JSON
        {
            { "channel_id", Id },
            { "topic", topic },
            { "privacy_level", privacyLevel },
            { "send_start_notification", sendStartNotification },
            { "guild_scheduled_event_id", scheduledEvent?.Id }
        };
        return await Bot._rest.CreateStageInstanceAsync(payload, reason);
    }
}

/// <summary>
/// Represents a <see cref="StageChannel"/> when its live.
/// </summary>
public record StageInstance
{
    // DOCS: https://discord.com/developers/docs/resources/stage-instance#stage-instance-object
    
    /// <summary>
    /// ID of this Stage instance.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; init; }
    
    /// <summary>
    /// Guild ID of the associated Stage channel.
    /// </summary>
    [JsonProperty("guild_id")]
    public ulong GuildId { get; init; }
    
    /// <summary>
    /// ID of the associated Stage channel.
    /// </summary>
    [JsonProperty("channel_id")]
    public ulong ChannelId { get; init; }
    
    /// <summary>
    /// Topic of the Stage instance (1-120 characters).
    /// </summary>
    [JsonProperty("topic")]
    public required string Topic { get; init; }
    
    /// <summary>
    /// The privacy level of the Stage instance.
    /// </summary>
    [JsonProperty("privacy_level")]
    public StageInstancePrivacyLevel PrivacyLevel { get; init; }
    
    /// <summary>
    /// ID of the scheduled event for this Stage instance.
    /// </summary>
    [JsonProperty("guild_scheduled_event_id")]
    public ulong? GuildScheduledEventId { get; init; }

    /// <summary>
    /// Your bot instance.
    /// </summary>
    public Bot Bot { get; internal set; } = null!;
    
    private StageInstance() { }

    /// <summary>
    /// Edit the stage instance. Requires the user to be a moderator of the Stage channel.
    /// </summary>
    /// <param name="edit">A stage instance edit instance.</param>
    /// <param name="reason">Reason for editing the stage instance. This is displayed in the audit-log.</param>
    /// <returns>The edited stage instance.</returns>
    public async Task<StageInstance> EditAsync(StageInstanceEdit edit, string? reason = null) =>
        await Bot._rest.ModifyStageInstance(ChannelId, edit, reason);
    
    /// <summary>
    /// Delete the stage instance. Requires the user to be a moderator of the Stage channel.
    /// </summary>
    /// <param name="reason">Reason for deleting the stage instance. This is displayed in the audit-log.</param>
    public async Task DeleteAsync(string? reason = null) =>
        await Bot._rest.DeleteStageInstanceAsync(ChannelId, reason);
}

/// <summary>
/// Represents the values that can be edited for a <see cref="StageInstance"/>.
/// </summary>
public readonly struct StageInstanceEdit
{
    internal readonly JSON _payload = [];
    
    /// <summary>
    /// Initialize a new stage instance edit instance.
    /// </summary>
    public StageInstanceEdit() { }

    /// <summary>
    /// Set the topic.
    /// </summary>
    /// <param name="topic">Topic of the Stage instance (1-120 characters).</param>
    /// <returns>The edit instance.</returns>
    public StageInstanceEdit SetTopic(string topic)
    {
        _payload["topic"] = topic;
        return this;
    }

    /// <summary>
    /// Set the privacy level.
    /// </summary>
    /// <param name="privacyLevel">The privacy level of the Stage instance.</param>
    /// <returns>The edit instance.</returns>
    public StageInstanceEdit SetPrivacyLevel(StageInstancePrivacyLevel privacyLevel)
    {
        _payload["privacy_level"] = privacyLevel;
        return this;
    }
}

/// <summary>
/// Represents the privacy level for a <see cref="StageInstance"/>.
/// </summary>
public enum StageInstancePrivacyLevel
{
    // DOCS: https://discord.com/developers/docs/resources/stage-instance#stage-instance-object-privacy-level
    
    /// <summary>
    /// The Stage instance is visible to only guild members.
    /// </summary>
    GuildOnly = 2
}
