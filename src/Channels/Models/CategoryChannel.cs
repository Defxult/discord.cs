using Discord.Channels.Abstractions;
using Discord.Channels.Services;
using Discord.Models;
using Newtonsoft.Json;

namespace Discord.Channels.Models;

/// <summary>
/// Represents a category channel in a <see cref="Guild"/>.
/// </summary>
public class CategoryChannel : GuildChannel, IPermissionEditable
{
    /// <summary>
    /// Not applicable for this channel type and will always be <c>null</c>.
    /// </summary>
    public new bool? IsNsfw => null;

    /// <inheritdoc cref="IsNsfw"/>
    public new ulong? ParentId => null;

    /// <inheritdoc cref="IsNsfw"/>
    public new string? Topic => null;

    /// <inheritdoc cref="IsNsfw"/>
    public new ulong? LastMessageId => null;

    /// <inheritdoc cref="IsNsfw"/>
    public DateTime? LastPinned => null;

    /// <inheritdoc cref="IsNsfw"/>
    public int? SlowModeSeconds => null;
    
    private CategoryChannel() { }
    
    /// <inheritdoc/>
    public async Task EditPermissionsAsync(PermissionOverwrites overwrites, string? reason = null) =>
        await ChannelServicer.EditChannelPermissions(this, overwrites, reason);

    /// <inheritdoc/>
    public async Task DeletePermissionsAsync(ulong id, string? reason = null) =>
        await ChannelServicer.DeletePermissions(this, id, reason);
}
