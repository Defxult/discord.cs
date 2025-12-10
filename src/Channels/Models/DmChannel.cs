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
    public async Task<IReadOnlyCollection<Message>> RequestMessages(MessageHistory history = MessageHistory.Before,
        DateTime? dt = null, int limit = 50) =>
        await Bot._rest.GetChannelMessages(Id, history, dt, limit);

    /// <inheritdoc/>
    public async Task<Message> RequestMessage(ulong id) =>
        await Bot._rest.GetChannelMessage(Id, id);

    /// <inheritdoc/>
    public async Task<Message> SendAsync(string? content = null, bool silent = false, bool tts = false,
        IEnumerable<Embed>? embeds = null,
        AllowedMentions? allowedMentions = null, IEnumerable<GuildSticker>? stickers = null, Poll? poll = null,
        ICollection<DFile>? files = null, MessageReference? reference = null, bool suppressEmbeds = false) =>
        await ChannelServicer.SendAsync(this, content, silent, tts, embeds, allowedMentions, stickers, poll, files,
            reference, suppressEmbeds);

    /// <inheritdoc/>
    public async Task TriggerTypingAsync(Func<Task>? func = null, CancellationToken ct = default) =>
        await ChannelServicer.TriggerTypingAsync(this, func, ct);

    /// <inheritdoc/>
    public async Task DeleteMessagesAsync(HashSet<Message> messages, string? reason = null) =>
        await ChannelServicer.DeleteMessagesAsync(this, messages, reason);
}