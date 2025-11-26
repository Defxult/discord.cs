using System.ComponentModel;
using Discord.Channels.Abstractions;
using Discord.Channels.Services;
using Discord.Models;
using Newtonsoft.Json;

namespace Discord.Channels.Models;

/// <summary>
/// Represents a voice channel for a <see cref="Guild"/>.
/// </summary>
public class VoiceChannel : GuildChannel, IMessageable, IVoiceChannel, IInvitable, IPermissionEditable
{
    /// <inheritdoc/>
    [JsonProperty("bitrate")]
    public int Bitrate { get; internal set; }
    
    /// <inheritdoc/>
    [JsonProperty("user_limit")]
    public int UserLimit { get; internal set; }
    
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

    internal VoiceChannel() { }
    
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
}

/// <summary>
/// Represents the video quality of a camera for a <see cref="VoiceChannel"/>.
/// </summary>
public enum VideoQualityMode
{
    // DOCS: https://discord.com/developers/docs/resources/channel#channel-object-video-quality-modes
    
    Auto = 1,
    Full
}

/// <summary>
/// Represents the voice region location for a <see cref="VoiceChannel"/>.
/// </summary>
public enum VoiceRegionLocation
{
    // DOCS: In app
    
    Automatic,
    
    [Description("brazil")]
    Brazil,
    
    [Description("hongkong")]
    Hongkong,
    
    [Description("india")]
    India,
    
    [Description("japan")]
    Japan,
    
    [Description("rotterdam")]
    Rotterdam,
    
    [Description("singapare")]
    Singapore,
    
    [Description("southafrica")]
    SouthAfrica,
    
    [Description("sydney")]
    Sydney,
    
    [Description("us-central")]
    UsCentral,
    
    [Description("us-east")]
    UsEast,
    
    [Description("us-south")]
    UsSouth,
    
    [Description("us-west")]
    UsWest
}
