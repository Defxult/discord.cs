using Discord.Channels.Abstractions;
using Discord.Channels.Services;
using Discord.Models;
using Newtonsoft.Json;

namespace Discord.Channels.Models;

/// <summary>
/// Represents an announcement channel for a <see cref="Guild"/>.
/// </summary>
public class AnnouncementChannel : GuildChannel, IMessageable, IThreadable, IInvitable, IPermissionEditable, ICoreGuildChannel
{
    /// <inheritdoc/>
    [JsonProperty("default_auto_archive_duration")]
    public ThreadArchiveDuration? DefaultAutoArchiveDuration { get; internal set; }
    
    private AnnouncementChannel() { }
    
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
    
    /// <inheritdoc cref="TextChannel.CreatePrivateThreadAsync"/>
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

    /// <summary>
    /// Follow the channel.
    /// </summary>
    /// <param name="channel">Channel where updates should be posted.</param>
    /// <param name="reason">Reason for following the channel. This is displayed in the audit-log.</param>
    /// <returns>The webhook associated with following the channel.</returns>
    /// <remarks>Requires <see cref="Permission.ManageWebhooks"/>.</remarks>
    public async Task<Webhook> FollowAsync(TextChannel channel, string? reason = null) =>
        await Bot._rest.FollowAnnouncementChannel(Id, channel.Id, reason);

    /// <inheritdoc/>
    public async Task<Webhook> CreateWebhookAsync(string name, DFile? avatar = null, string? reason = null) =>
        await Bot._rest.CreateWebhookAsync(Id, name, avatar, reason);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Webhook>> WebhooksAsync() =>
        await Bot._rest.GetChannelWebhooksAsync(Id);
}
