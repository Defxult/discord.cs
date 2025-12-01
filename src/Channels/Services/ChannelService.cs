using System.Net.Http.Headers;
using System.Text;
using Discord.Channels.Abstractions;
using Discord.Channels.Models;
using Discord.Models;
using Discord.Utility;
using Newtonsoft.Json;

namespace Discord.Channels.Services;

internal static class ChannelServicer
{
    internal static async Task<ThreadChannel> CreatePrivateThreadAsync(IMessageable messageable, Guild guild, string name,
        bool invitable, ThreadArchiveDuration duration, int? slowModeDelaySeconds, string? reason) =>
        await messageable.Bot._rest.StartThreadWithoutMessage(messageable.Id, guild, name, invitable, duration,
            slowModeDelaySeconds, reason);
    
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
    
    internal static async Task DeleteAsync(IChannel channel, string? reason = null) =>
        await channel.Bot._rest.DeleteCloseChannelAsync(channel.Id, reason);

    internal static async Task<Message> SendAsync(IMessageable messageable, string? content, bool silent, bool tts,
        IEnumerable<Embed>? embeds, AllowedMentions? allowedMentions, IEnumerable<GuildSticker>? stickers, Poll? poll,
        ICollection<DFile>? files, MessageReference? reference)
    {
        var form = new MultipartFormDataContent(Dev.Boundary);
        var bot = messageable.Bot;

        var payload = new JSON();
        if (content != null)
            payload["content"] = silent ? $"@silent {content}" : content;
        payload["tts"] = tts;
        if (embeds != null)
            payload["embeds"] = embeds;
        if (allowedMentions is { } am)
            payload["allowed_mentions"] = am.ToJson();
        if (stickers != null)
            payload["sticker_ids"] = stickers.Select(s => s.Id);
        if (poll is { } p)
            payload["poll"] = p;
        if (reference != null)
            payload["message_reference"] = reference;
        
        
        // Files **** (leave as last due to adding to form) ****
        var jsonContent = new StringContent(JsonConvert.SerializeObject(payload));
        jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        
        // Add as payload_json
        form.Add(jsonContent, "payload_json");
        
        if (files != null)
        {
            var list = files.ToList();
            for (var i = 0; i < list.Count; i++)
            {
                var file = list[i];
                var bac = new ByteArrayContent(file.Bytes);
                bac.Headers.ContentType = new MediaTypeHeaderValue(file._mimeType);
                form.Add(bac, $"files[{i}]", file.Name);
            }
        }
        if (messageable.Type == ChannelType.Dm)
        {
            var dmChannel = await bot._rest.CreateDmAsync(((DmChannel)messageable).Recipient.Id);
            if (!bot._dmChannels.Exists(match => match.Id == dmChannel.Id))
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
