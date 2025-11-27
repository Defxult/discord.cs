using Discord.Channels.Abstractions;
using Discord.Channels.Services;
using Discord.Models;
using Newtonsoft.Json;

namespace Discord.Channels.Models;

/// <summary>
/// Represents a text channel for a <see cref="Guild"/>.
/// </summary>
public class TextChannel : GuildChannel, IMessageable, IThreadable, IInvitable, IPermissionEditable, ICoreGuildChannel
{
    /// <inheritdoc/>
    [JsonProperty("default_auto_archive_duration")]
    public ThreadArchiveDuration? DefaultAutoArchiveDuration { get; internal set; }
    
    private TextChannel() { }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Message>> RequestMessages(MessageHistory history = MessageHistory.Before,
        DateTime? dt = null, int limit = 50) =>
        await Bot._rest.GetChannelMessages(Id, history, dt, limit);

    /// <inheritdoc/>
    public async Task<Message> RequestMessage(ulong id) =>
        await Bot._rest.GetChannelMessage(Id, id);

    /// <inheritdoc/>
    public async Task<Message> SendAsync(string? content = null, bool tts = false, IEnumerable<Embed>? embeds = null,
        AllowedMentions? allowedMentions = null, IEnumerable<GuildSticker>? stickers = null, Poll? poll = null,
        ICollection<DFile>? files = null) =>
        await ChannelServicer.SendAsync(this, content, tts, embeds, allowedMentions, stickers, poll, files);

    /// <inheritdoc/>
    public async Task TriggerTypingAsync(Func<Task>? func = null, CancellationToken ct = default) =>
        await ChannelServicer.TriggerTypingAsync(this, func, ct);
    
    /// <summary>
    /// Create a private thread.
    /// </summary>
    /// <param name="name">Name of the thread (1-100 characters).</param>
    /// <param name="invitable">Whether non-moderators can add other non-moderators to a thread; only available when
    /// creating a private thread.</param>
    /// <param name="duration">When the thread will stop showing in the channel list after inactivity.</param>
    /// <param name="slowModeDelaySeconds">Amount of seconds a user has to wait before sending another message (1-21600),
    /// or <c>null</c> to disable it.
    /// </param>
    /// <param name="reason">Reason for creating the thread. This is displayed in the audit-log.</param>
    /// <returns>The created private thread.</returns>
    public async Task<ThreadChannel> CreatePrivateThreadAsync(string name, bool invitable = true,
        ThreadArchiveDuration duration = ThreadArchiveDuration.ThreeDays, int? slowModeDelaySeconds = null,
        string? reason = null) =>
        await ChannelServicer.CreatePrivateThreadAsync(this, Guild, name, invitable, duration, slowModeDelaySeconds, reason);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Invite>> InvitesAsync() =>
        await ChannelServicer.InvitesAsync(this);

    /// <inheritdoc/>
    public async Task<Invite> CreateInviteAsync(int? maxAge = 86400, int? maxUses = null, bool temporary = false,
        bool unique = false,
        InviteTargetType? targetType = null, ulong? targetUserId = null, ulong? targetApplicationId = null,
        string? reason = null) =>
        await ChannelServicer.CreateInviteAsync(this, maxAge, maxUses, temporary, unique, targetType, targetUserId,
            targetApplicationId, reason);
    
    /// <inheritdoc/>
    public async Task EditPermissionsAsync(PermissionOverwrites overwrites, string? reason = null) =>
        await ChannelServicer.EditChannelPermissions(this, overwrites, reason);

    /// <inheritdoc/>
    public async Task DeletePermissionsAsync(ulong id, string? reason = null) =>
        await ChannelServicer.DeletePermissions(this, id, reason);

    /// <inheritdoc/>
    public async Task<Webhook> CreateWebhookAsync(string name, DFile? avatar = null, string? reason = null) =>
        await Bot._rest.CreateWebhookAsync(Id, name, avatar, reason);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Webhook>> WebhooksAsync() =>
        await Bot._rest.GetChannelWebhooksAsync(Id);
}
