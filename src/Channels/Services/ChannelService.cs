using Discord.Channels.Abstractions;
using Discord.Channels.Models;
using Discord.Models;
using Discord.Utility;

namespace Discord.Channels.Services;

internal static class ChannelServicer
{
    internal static async Task TriggerTypingAsync(IMessageable messageable, Func<Task>? func, CancellationToken ct)
    {
        if (func == null)
        {
            await messageable.Bot._rest.TriggerTypingIndicator(messageable);
            return;
        }

        var task = func();

        while (!task.IsCompleted && !ct.IsCancellationRequested)
        {
            await messageable.Bot._rest.TriggerTypingIndicator(messageable);

            // Race: whichever finishes first: task or delay
            var delay = Task.Delay(9500, ct);
            await Task.WhenAny(task, delay);
        }

        // Observe exceptions so they aren't lost
        try
        {
            await task;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // normal cancellation (ignored)
        }
    }
    
    internal static async Task EditChannelPermissions<T>(T channel, PermissionOverwrites overwrites, string? reason) where T : GuildChannel, IPermissionEditable =>
        await channel.Bot._rest.EditChannelPermissions(channel.Id, overwrites, reason);
    
    internal static async Task DeletePermissions<T>(T channel, ulong overwriteId, string? reason) where T : GuildChannel, IPermissionEditable =>
        await channel.Bot._rest.DeleteChannelPermissions(channel.Id, overwriteId, reason);
    
    internal static async Task<GuildChannel> EditAsync(GuildChannel channel, GuildChannelEdit edit,
        string? reason = null) =>
        await channel.Bot._rest.ModifyChannelAsync(edit, channel, reason);
    
    internal static async Task DeleteAsync(IChannel channel, string? reason = null) =>
        await channel.Bot._rest.DeleteCloseChannelAsync(channel.Id, reason);
    
    internal static async Task<Message> SendAsync(IMessageable messageable, string? content = null)
    {
        using var form = new MultipartFormDataContent(Dev.Boundary);
        var bot = messageable.Bot;
        
        if (content != null)
            form.Add(new StringContent(content), "content");

        if (messageable.Type == ChannelType.Dm)
        {
            var dmChannel = await bot._rest.CreateDmAsync(((DmChannel)messageable).Recipient.Id);
            bot._dmChannels.Add(dmChannel);
        }

        var message = await bot._rest.CreateMessageAsync(messageable.Id, form);
        bot._rest.SetMessageValues([message]);
        return message;
    }
    
    internal static async Task<IReadOnlyCollection<Invite>> InvitesAsync<T>(T channel) where T : GuildChannel, IInvitable =>
        await channel.Bot._rest.GetChannelInvitesAsync(channel.Id);

    internal static async Task<Invite> CreateInviteAsync<T>(T channel, int? maxAge, int? maxUses,
        bool temporary, bool unique,
        InviteTargetType? targetType, ulong? targetUserId, ulong? targetApplicationId,
        string? reason) where T : GuildChannel, IInvitable
    {
        var payload = new JSON
        {
            { "max_age", maxAge ?? 0 },
            { "max_uses", maxUses ?? 0 },
            { "temporary", temporary },
            { "unique", unique },
        };
        if (targetType != null)
            payload["target_type"] = targetType;
        if (targetUserId != null)
            payload["target_user_id"] = targetUserId;
        if (targetApplicationId != null)
            payload["target_application_id"] = targetApplicationId;
        return await channel.Bot._rest.CreateChannelInviteAsync(channel.Id, payload, reason);
    }
}
