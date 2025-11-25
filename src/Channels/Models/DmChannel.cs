using Discord.Channels.Abstractions;
using Discord.Channels.Services;
using Discord.Models;
using Newtonsoft.Json;

namespace Discord.Channels.Models;

/// <summary>
/// Represents a direct message channel.
/// </summary>
public class DmChannel : IMessageable
{
    // DOCS: https://discord.com/developers/docs/resources/channel

    /// <inheritdoc/>
    public ulong Id { get; }

    /// <inheritdoc/>
    public ChannelType Type => ChannelType.Dm;
    
    /// <inheritdoc/>
    public Bot Bot { get; internal set; } = null!;
    
    /// <summary>
    /// User that is being interacted with.
    /// </summary>
    public User Recipient { get; }
    
    /// <inheritdoc cref="GuildChannel.LastMessageId"/>
    public ulong? LastMessageId { get; internal set; }
    
    [JsonConstructor]
    internal DmChannel(ulong id, List<User> recipients)
    {
        Id = id;
        Recipient = recipients[0];
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string? reason = null) =>
        await ChannelServicer.DeleteAsync(this, reason);
    
    /// <inheritdoc/>
    public async Task<Message> SendAsync(string? content = null) =>
        await ChannelServicer.SendAsync(this, content);

    /// <inheritdoc/>
    public async Task TriggerTypingAsync(Func<Task>? func = null, CancellationToken ct = default) =>
        await ChannelServicer.TriggerTypingAsync(this, func, ct);
}