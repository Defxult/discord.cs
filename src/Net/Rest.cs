using System.Text.Json;
using Discord.Channels.Abstractions;
using Discord.Channels.Models;

namespace Discord.Net;
using System.Net.Http.Headers;
using System.Text;
using System.ComponentModel;
using Newtonsoft.Json;
using Discord;
using Models;
using Utility;

internal class Rest
{
    private readonly Bot _bot;
    private readonly HttpClient _http;
    private static HttpMethod Get => HttpMethod.Get;
    private static HttpMethod Post => HttpMethod.Post;
    private static HttpMethod Delete => HttpMethod.Delete;
    private static HttpMethod Patch => HttpMethod.Patch;
    private static HttpMethod Put => HttpMethod.Put;
    
    private static readonly Dictionary<string, RateLimitBucket> _buckets = new();
    private static readonly object _bucketLock = new();

    // Global rate limit handling
    private static readonly SemaphoreSlim _globalSemaphore = new(1, 1);
    private static DateTimeOffset _globalResetAt = DateTimeOffset.MinValue;


    internal Rest(Bot bot)
    {
        _bot = bot;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _http.DefaultRequestHeaders.Add("User-Agent", "discord.cs (https://github.com/Defxult/discord.cs)");
        _http.DefaultRequestHeaders.Add("Authorization", $"Bot {bot.Token}");
    }

    // Converts the JSON object (data) to its string representation.
    private static StringContent ToStringContent(object data)
    {
        string converted = JsonConvert.SerializeObject(data);
        return new StringContent(converted, Encoding.UTF8, "application/json");
    }

    #region APPLICATION

    // Returns the application object associated with the requesting bot user.
    // https://discord.com/developers/docs/resources/application#get-current-application
    internal async Task<Application> GetApplicationAsync()
    {
        string data = await RequestAsync(Get, Route("/applications/@me"));
        var app = JsonConvert.DeserializeObject<Application>(data)!;
        app.Bot = _bot;
        return app;
    }

    // Edit properties of the app associated with the requesting bot user. Only properties that are passed will be updated.
    // Returns the updated application object on success.
    // https://discord.com/developers/docs/resources/application#edit-current-application
    internal async Task<Application> EditCurrentApplicationAsync(ApplicationEdit edit)
    {
        string data = await RequestAsync(Patch, Route("/applications/@me"), edit._payload);
        var app = JsonConvert.DeserializeObject<Application>(data)!;
        app.Bot = _bot;
        return app;
    }

    #endregion

    #region CHANNEL

    internal void SetThreadValues(IEnumerable<ThreadChannel> threads, Guild guild)
    {
        foreach (var t in threads)
        {
            t.Guild = guild;
            t.Bot = _bot;
        }
    }

    internal void SetChannelValuesIndividual(GuildChannel channel, Guild guild)
    {
        channel.Bot = _bot;
        channel.Guild = guild;
    }

    private void SetChannelValues(Guild guild)
    {
        guild._channels.ForEach(channel =>
        {
            channel.Bot = _bot;
            channel.Guild = guild;
            channel.GuildId = guild.Id;
        });
    }
    
    // Update a channel's settings. Returns a channel on success, and a 400 BAD REQUEST on invalid parameters.
    // https://discord.com/developers/docs/resources/channel#modify-channel
    internal async Task<GuildChannel> ModifyChannelAsync(GuildChannelEdit edit, GuildChannel channel, string? reason)
    {
        string data = await RequestAsync(Patch, Route($"/channels/{channel.Id}"), edit._payload, reason);
        var converted = JsonConvert.DeserializeObject<JSON>(data)!;
        return channel.Type is ChannelType.PublicThread or ChannelType.PrivateThread or ChannelType.AnnouncementThread
            ? GuildChannel.ParseThreads([converted]).First()
            : GuildChannel.ParseChannels([converted]).First();
    }

    // Delete a channel, or close a private message. Requires the MANAGE_CHANNELS permission for the guild, or
    // MANAGE_THREADS if the channel is a thread. Deleting a category does not delete its child channels; they will have
    // their parent_id removed and a Channel Update Gateway event will fire for each of them. Returns a channel object on
    // success. Fires a Channel Delete Gateway event (or Thread Delete if the channel was a thread).
    // https://discord.com/developers/docs/resources/channel#deleteclose-channel
    internal async Task DeleteCloseChannelAsync(ulong channelId, string? reason)
    {
        await RequestAsync(Delete, Route($"/channels/{channelId}"), auditReason: reason);
    }
    
    // Edit the channel permission overwrites for a user or role in a channel. Only usable for guild channels. Requires
    // the MANAGE_ROLES permission. Only permissions your bot has in the guild or parent channel (if applicable) can be
    // allowed/denied (unless your bot has a MANAGE_ROLES overwrite in the channel). Returns a 204 empty response on success.
    // Fires a Channel Update Gateway event. For more information about permissions, see permissions.
    // https://discord.com/developers/docs/resources/channel#edit-channel-permissions
    internal async Task EditChannelPermissions(ulong channelId, PermissionOverwrites overwrites, string? reason)
    {
        await RequestAsync(Put, Route($"/channels/{channelId}/permissions/{overwrites.Id}"), overwrites.ToPayload(),
            reason);
    }
    
    
    // Returns a list of invite objects (with invite metadata) for the channel. Only usable for guild channels. Requires
    // the MANAGE_CHANNELS permission.
    // https://discord.com/developers/docs/resources/channel#get-channel-invites
    internal async Task<List<Invite>> GetChannelInvitesAsync(ulong channelId)
    {
        string data = await RequestAsync(Get, Route($"/channels/{channelId}/invites"));
        var invites = JsonConvert.DeserializeObject<List<Invite>>(data)!;
        SetInviteValues(invites);
        return invites;
    }
    
    // Create a new invite object for the channel. Only usable for guild channels. Requires the CREATE_INSTANT_INVITE
    // permission. All JSON parameters for this route are optional, however the request body is not. If you are not
    // sending any fields, you still have to send an empty JSON object ({}). Returns an invite object. Fires an Invite
    // Create Gateway event.
    // https://discord.com/developers/docs/resources/channel#create-channel-invite
    internal async Task<Invite> CreateChannelInviteAsync(ulong channelId, JSON payload, string? reason)
    {
        string data = await RequestAsync(Post, Route($"/channels/{channelId}/invites"));
        var invites = JsonConvert.DeserializeObject<Invite>(data)!;
        SetInviteValues([invites]);
        return invites;
    }
    
    // Delete a channel permission overwrite for a user or role in a channel. Only usable for guild channels. Requires
    // the MANAGE_ROLES permission. Returns a 204 empty response on success. Fires a Channel Update Gateway event.
    // For more information about permissions, see permissions
    // https://discord.com/developers/docs/resources/channel#delete-channel-permission
    internal async Task DeleteChannelPermissions(ulong channelId, ulong userOrRoleId, string? reason)
    {
        await RequestAsync(Delete, Route($"/channels/{channelId}/permissions/{userOrRoleId}"), reason);
    }
    
    // Follow an Announcement Channel to send messages to a target channel. Requires the MANAGE_WEBHOOKS permission in
    // the target channel. Returns a followed channel object. Fires a Webhooks Update Gateway event for the target channel.
    // https://discord.com/developers/docs/resources/channel#follow-announcement-channel
    internal async Task<Webhook> FollowAnnouncementChannel(ulong channelIdToFollow, ulong destinationChannelId, string? reason)
    {
        var payload = new JSON { { "webhook_channel_id", destinationChannelId } };
        string data = await RequestAsync(Post, Route($"/channels/{channelIdToFollow}/followers"), payload, reason);
        var converted = JsonConvert.DeserializeObject<JSON>(data)!;
        var webhookId = Convert.ToUInt64(converted["webhook_id"]);
        return await GetWebhookAsync(webhookId);
    }
    
    // Post a typing indicator for the specified channel, which expires after 10 seconds. Returns a 204 empty response
    // on success. Fires a Typing Start Gateway event.
    // https://discord.com/developers/docs/resources/channel#trigger-typing-indicator
    internal async Task TriggerTypingIndicator(IMessageable messageable)
    {
        await RequestAsync(Post, Route($"/channels/{messageable.Id}/typing"));
    }
    
    // Creates a new thread from an existing message. Returns a channel on success, and a 400 BAD REQUEST on invalid parameters.
    // Fires a Thread Create and a Message Update Gateway event.
    // 
    // When called on a GUILD_TEXT channel, creates a PUBLIC_THREAD. When called on a GUILD_ANNOUNCEMENT channel, creates
    // a ANNOUNCEMENT_THREAD. Does not work on a GUILD_FORUM or a GUILD_MEDIA channel. The id of the created thread will
    // be the same as the id of the source message, and as such a message can only have a single thread created from it.
    // https://discord.com/developers/docs/resources/channel#start-thread-from-message
    internal async Task<ThreadChannel> StartThreadFromMessage(Guild guild, ulong channelId, ulong messageId, JSON payload, string? reason)
    {
        string data = await RequestAsync(Post, Route($"/channels/{channelId}/messages/{messageId}/threads"),
            payload, reason);
        var thread = JsonConvert.DeserializeObject<ThreadChannel>(data)!;
        SetThreadValues([thread], guild);
        return thread;
    }
    
    // Creates a new thread that is not connected to an existing message. Returns a channel on success, and a 400
    // BAD REQUEST on invalid parameters. Fires a Thread Create Gateway event.
    // https://discord.com/developers/docs/resources/channel#start-thread-without-message
    internal async Task<ThreadChannel> StartThreadWithoutMessage(ulong channelId, Guild guild, string name,
        bool invitable, ThreadArchiveDuration duration, int? slowModeDelaySeconds, string? reason)
    {
        var payload = new JSON
        {
            { "name", name },
            { "invitable", invitable },
            { "auto_archive_duration", duration },
            { "rate_limit_per_user", slowModeDelaySeconds ?? 0 }
        };
        string data = await RequestAsync(Post, Route($"/channels/{channelId}/threads"), payload, reason);
        var thread = JsonConvert.DeserializeObject<ThreadChannel>(data)!;
        SetThreadValues([thread], guild);
        return thread;
    }

    #endregion

    #region EMOJI

    internal void SetEmojiValues(IEnumerable<Emoji> emojis, ulong? guildId)
    {
        foreach (var e in emojis)
        {
            e.GuildId = guildId;
            e.Bot = _bot;
        }
    }

    // Returns a list of emoji objects for the given guild. Includes user fields if the bot has the CREATE_GUILD_EXPRESSIONS
    // or MANAGE_GUILD_EXPRESSIONS permission.
    // https://discord.com/developers/docs/resources/emoji#list-guild-emojis
    internal async Task<List<Emoji>> ListGuildEmojisAsync(ulong guildId)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/emojis"));
        var emojis = JsonConvert.DeserializeObject<List<Emoji>>(data)!;
        SetEmojiValues(emojis, guildId);
        return emojis;
    }
    
    // Returns an emoji object for the given guild and emoji IDs. Includes the user field if the bot has the
    // MANAGE_GUILD_EXPRESSIONS permission, or if the bot created the emoji and has the CREATE_GUILD_EXPRESSIONS permission.
    // https://discord.com/developers/docs/resources/emoji#get-guild-emoji
    internal async Task<Emoji> GetGuildEmojiAsync(ulong guildId, ulong emojiId)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/emojis/{emojiId}"));
        var emoji = JsonConvert.DeserializeObject<Emoji>(data)!;
        SetEmojiValues([emoji], guildId);
        return emoji;
    }
    
    // Create a new emoji for the guild. Requires the CREATE_GUILD_EXPRESSIONS permission. Returns the new emoji object
    // on success. Fires a Guild Emojis Update Gateway event.
    // 
    // Emojis and animated emojis have a maximum file size of 256 KiB. Attempting to upload an emoji larger than this
    // limit will fail and return 400 Bad Request and an error message, but not a JSON status code.
    // https://discord.com/developers/docs/resources/emoji#create-guild-emoji
    internal async Task<Emoji> CreateGuildEmojiAsync(ulong guildId, string name, DFile file, IReadOnlyCollection<Role> roles, string? reason)
    {
        var payload = new JSON
        {
            { "name", name },
            { "image", file._mimeTypeBase64 },
            { "roles", roles.Select(f => f.Id) }
        };
        string data = await RequestAsync(Post, Route($"/guilds/{guildId}/emojis"), payload, reason);
        var emoji = JsonConvert.DeserializeObject<Emoji>(data)!;
        SetEmojiValues([emoji], guildId);
        return emoji;
    }
    
    // Modify the given emoji. For emojis created by the current user, requires either the CREATE_GUILD_EXPRESSIONS or
    // MANAGE_GUILD_EXPRESSIONS permission. For other emojis, requires the MANAGE_GUILD_EXPRESSIONS permission.
    // Returns the updated emoji object on success. Fires a Guild Emojis Update Gateway event.
    // https://discord.com/developers/docs/resources/emoji#modify-guild-emoji
    internal async Task<Emoji> ModifyGuildEmojiAsync(ulong guildId, ulong emojiId, EmojiEdit edit, string? reason)
    {
        string data = await RequestAsync(Patch, Route($"/guilds/{guildId}/emojis/{emojiId}"), edit._payload, reason);
        var emoji = JsonConvert.DeserializeObject<Emoji>(data)!;
        SetEmojiValues([emoji], guildId);
        return emoji;
    }
    
    // Delete the given emoji. For emojis created by the current user, requires either the CREATE_GUILD_EXPRESSIONS or
    // MANAGE_GUILD_EXPRESSIONS permission. For other emojis, requires the MANAGE_GUILD_EXPRESSIONS permission. Returns
    // 204 No Content on success. Fires a Guild Emojis Update Gateway event.
    // https://discord.com/developers/docs/resources/emoji#delete-guild-emoji
    internal async Task DeleteGuildEmojiAsync(ulong guildId, ulong emojiId, string? reason)
    { 
        await RequestAsync(Delete, Route($"/guilds/{guildId}/emojis/{emojiId}"), auditReason: reason);
    }
    
    // Returns an object containing a list of emoji objects for the given application under the items key. Includes a
    // user object for the team member that uploaded the emoji from the app's settings, or for the bot user if uploaded
    // using the API.
    // https://discord.com/developers/docs/resources/emoji#list-application-emojis
    internal async Task<List<Emoji>> ListApplicationEmojisAsync(ulong applicationId)
    {
        string data = await RequestAsync(Get, Route($"/applications/{applicationId}/emojis"));
        var element = JsonDocument.Parse(data).RootElement.GetProperty("items");
        var emojis = Gateway.Deserialize<List<Emoji>>(element);
        SetEmojiValues(emojis, null);
        return emojis;
    }
    
    // Returns an emoji object for the given application and emoji IDs. Includes the user field.
    // https://discord.com/developers/docs/resources/emoji#get-application-emoji
    internal async Task<Emoji> GetApplicationEmojiAsync(ulong applicationId, ulong emojiId)
    {
        string data = await RequestAsync(Get, Route($"/applications/{applicationId}/emojis/{emojiId}"));
        var emoji = JsonConvert.DeserializeObject<Emoji>(data)!;
        SetEmojiValues([emoji], null);
        return emoji;
    }
    
    // Create a new emoji for the application. Returns the new emoji object on success.
    //
    // Emojis and animated emojis have a maximum file size of 256 KiB. Attempting to upload an emoji larger than this
    // limit will fail and return 400 Bad Request and an error message, but not a JSON status code.
    //
    // We highly recommend that developers use the .webp extension when fetching emoji so they're rendered as WebP for
    // maximum performance and compatibility. See the Emoji Formats section above for more details.
    // https://discord.com/developers/docs/resources/emoji#create-application-emoji
    internal async Task<Emoji> CreateApplicationEmojiAsync(ulong applicationId, string name, DFile file)
    {
        var payload = new JSON
        {
            { "name", name },
            { "image", file._mimeTypeBase64 }
        };
        
        string data = await RequestAsync(Post, Route($"/applications/{applicationId}/emojis"), payload);
        var emoji = JsonConvert.DeserializeObject<Emoji>(data)!;
        SetEmojiValues([emoji], null);
        return emoji;
    }
    
    // Modify the given emoji. Returns the updated emoji object on success.
    // https://discord.com/developers/docs/resources/emoji#modify-application-emoji
    internal async Task<Emoji> ModifyApplicationEmojiAsync(ulong applicationId, ulong emojiId, EmojiEdit edit)
    {
        // "roles" value is only valid for guild emoji payloads.
        edit._payload.Remove("roles");
        
        string data = await RequestAsync(Patch, Route($"/applications/{applicationId}/emojis/{emojiId}"), edit._payload);
        var emoji = JsonConvert.DeserializeObject<Emoji>(data)!;
        SetEmojiValues([emoji], null);
        return emoji;
    }
    
    // Delete the given emoji. Returns 204 No Content on success.
    // https://discord.com/developers/docs/resources/emoji#delete-application-emoji
    internal async Task DeleteApplicationEmojiAsync(ulong applicationId, ulong emojiId)
    { 
        await RequestAsync(Delete, Route($"/applications/{applicationId}/emojis/{emojiId}"));
    }
    
    #endregion
    
    #region GUILD SCHEDULED EVENT

    // DOCS: https://discord.com/developers/docs/resources/guild-scheduled-event

    // Returns a list of guild scheduled event objects for the given guild.
    // https://discord.com/developers/docs/resources/guild-scheduled-event#list-scheduled-events-for-guild
    internal async Task<List<ScheduledEvent>> ListScheduledEventsForGuildAsync(ulong guildId)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/scheduled-events?with_user_count=true"));
        var events = JsonConvert.DeserializeObject<List<ScheduledEvent>>(data)!;
        events.ForEach(e => e.Bot = _bot);
        return events;
    }

    // Create a guild scheduled event in the guild. Returns a guild scheduled event object on success.
    // Fires a Guild Scheduled Event Create Gateway event.
    // https://discord.com/developers/docs/resources/guild-scheduled-event#create-guild-scheduled-event
    internal async Task<ScheduledEvent> CreateGuildScheduledEventAsync(
        ulong guildId,
        string name,
        DateTime startTime,
        DateTime? endTime,
        ulong? channelId,
        string? location,
        string? description,
        ScheduledEventEntityType entityType,
        DFile? image,
        RecurrenceRule? recurrence,
        string? reason
    )
    {
        JSON payload = new()
        {
            { "name", name },
            { "scheduled_start_time", startTime.ToString("O") },
            { "entity_type", (int)entityType },
            { "privacy_level", (int)ScheduledEventPrivacyLevel.GuildOnly }
        };

        if (endTime is { } et)
            payload.Add("scheduled_end_time", et.ToString("O"));
        if (channelId is { } cid)
            payload.Add("channel_id", cid);
        if (location != null)
            if (entityType == ScheduledEventEntityType.External)
            {
                var meta = new JSON { { "location", location } };
                payload.Add("entity_metadata", meta);
            }

        if (description != null)
            payload.Add("description", description);
        if (image is not null)
            payload.Add("image", image._mimeTypeBase64);
        if (recurrence is not null)
            payload.Add("recurrence_rule", recurrence);

        string data = await RequestAsync(Post, Route($"/guilds/{guildId}/scheduled-events"), payload, reason);
        var evt = JsonConvert.DeserializeObject<ScheduledEvent>(data)!;
        evt.Bot = _bot;
        return evt;
    }

    // Get a guild scheduled event. Returns a guild scheduled event object on success.
    // https://discord.com/developers/docs/resources/guild-scheduled-event#get-guild-scheduled-event
    internal async Task<ScheduledEvent> GetGuildScheduledEventAsync(ulong guildId, ulong eventId)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/scheduled-events/{eventId}"));
        var evt = JsonConvert.DeserializeObject<ScheduledEvent>(data)!;
        evt.Bot = _bot;
        return evt;
    }

    // Modify a guild scheduled event. Returns the modified guild scheduled event object on success.
    // Fires a Guild Scheduled Event Update Gateway event.
    // https://discord.com/developers/docs/resources/guild-scheduled-event#modify-guild-scheduled-event
    internal async Task<ScheduledEvent> ModifyGuildScheduledEventAsync(ulong guildId, ulong eventId, JSON payload,
        string? reason)
    {
        string data =
            await RequestAsync(Patch, Route($"/guilds/{guildId}/scheduled-events/{eventId}"), payload, reason);
        var evt = JsonConvert.DeserializeObject<ScheduledEvent>(data)!;
        evt.Bot = _bot;
        return evt;
    }

    // Delete a guild scheduled event. Returns a 204 on success. Fires a Guild Scheduled Event Delete Gateway event.
    // https://discord.com/developers/docs/resources/guild-scheduled-event#delete-guild-scheduled-event
    internal async Task DeleteGuildScheduledEventAsync(ulong guildId, ulong eventId)
    {
        await RequestAsync(Delete, Route($"/guilds/{guildId}/scheduled-events/{eventId}"));
    }

    // Get a list of guild scheduled event users subscribed to a guild scheduled event. Returns a list of guild scheduled
    // event user objects on success. Guild member data, if it exists, is included if the with_member query parameter is set.
    // https://discord.com/developers/docs/resources/guild-scheduled-event#get-guild-scheduled-event-users
    internal async Task<List<User>> GetGuildScheduledEventUsers(int limit, ulong guildId, ulong eventId, ulong? before, ulong? after)
    {
        string route = Route($"/guilds/{guildId}/scheduled-events/{eventId}/users?limit={limit}");
        if (before.HasValue) route += $"&before={before.Value}";
        if (after.HasValue) route += $"&after={after.Value}";
        string data = await RequestAsync(Get, route);
        var doc = JsonDocument.Parse(data);
        var users = new List<User>();
        foreach (var ele in doc.RootElement.EnumerateArray())
            users.Add(Gateway.Deserialize<User>(ele.GetProperty("user")));
        return users;
    }

    #endregion

    #region GUILD

    internal void SetGuildValues(Guild guild)
    {
        guild.Bot = _bot;
        SetEmojiValues(guild._emojis, guild.Id);
        SetRoleValues(guild._roles, guild.Id);
        SetChannelValues(guild);
        SetThreadValues(guild._threads, guild);
    }

    // Returns the guild object for the given id. If with_counts is set to true, this endpoint will also return
    // approximate_member_count and approximate_presence_count for the guild.
    // https://discord.com/developers/docs/resources/guild#get-guild
    internal async Task<Guild> GetGuildAsync(ulong guildId)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}?with_counts=true"));
        var guild = JsonConvert.DeserializeObject<Guild>(data)!;
        SetGuildValues(guild);
        return guild;
    }
    
    // Returns the guild preview object for the given id. If the user is not in the guild, then the guild must be discoverable.
    // https://discord.com/developers/docs/resources/guild#get-guild-preview
    internal async Task<GuildPreview> GetGuildPreviewAsync(ulong guildId)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/preview"));
        return JsonConvert.DeserializeObject<GuildPreview>(data)!;
    }
    
    // Modify a guild's settings. Requires the MANAGE_GUILD permission. Returns the updated guild object on success.
    // Fires a Guild Update Gateway event.
    // https://discord.com/developers/docs/resources/guild#modify-guild
    internal async Task<Guild> ModifyGuildAsync(ulong guildId, GuildEdit edit, string? reason)
    {
        string data = await RequestAsync(Patch, Route($"/guilds/{guildId}"), edit._payload, reason);
        var guild = JsonConvert.DeserializeObject<Guild>(data)!;
        SetGuildValues(guild);
        return guild;
    }
    
    // Returns a list of guild channel objects. Does not include threads.
    // https://discord.com/developers/docs/resources/guild#get-guild-channels
    internal async Task<List<GuildChannel>> GetGuildChannelsAsync(ulong guildId)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/channels"));
        var channelObjs = JsonConvert.DeserializeObject<List<JSON>>(data)!;
        var channels = GuildChannel.ParseChannels(channelObjs);
        if (_bot.GetGuild(guildId) is { } guild)
        {
            channels.ForEach(gc =>
            {
                gc.Bot = _bot;
                gc.Guild = guild;
            });
        }
        return channels;
    }

    internal void SetRoleValues(IEnumerable<Role> roles, ulong guildId)
    {
        foreach (var role in roles)
        {
            role.Bot = _bot;
            role.GuildId = guildId;
        }
    }

    // Returns a list of role objects for the guild.
    // https://discord.com/developers/docs/resources/guild#get-guild-roles
    internal async Task<List<Role>> GetGuildRolesAsync(ulong guildId)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/roles"));
        var roles = JsonConvert.DeserializeObject<List<Role>>(data)!;
        SetRoleValues(roles, guildId);
        return roles;
    }
    
    // Returns a role object for the specified role.
    // https://discord.com/developers/docs/resources/guild#get-guild-role
    internal async Task<Role> GetGuildRoleAsync(ulong guildId, ulong roleId)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/roles/{roleId}"));
        var role = JsonConvert.DeserializeObject<Role>(data)!;
        SetRoleValues([role], guildId);
        return role;
    }

    // Create a new role for the guild. Requires the MANAGE_ROLES permission. Returns the new role object on success.
    // Fires a Guild Role Create Gateway event. All JSON params are optional.
    // https://discord.com/developers/docs/resources/guild#create-guild-role
    internal async Task<Role> CreateGuildRoleAsync(ulong guildId, JSON payload, string? reason)
    {
        string data = await RequestAsync(Post, Route($"/guilds/{guildId}/roles"), payload, reason);
        var role = JsonConvert.DeserializeObject<Role>(data)!;
        SetRoleValues([role], guildId);
        return role;
    }
    
    // Modify the positions of a set of role objects for the guild. Requires the MANAGE_ROLES permission. Returns a list
    // of all guild role objects on success. Fires multiple Guild Role Update Gateway events.
    // https://discord.com/developers/docs/resources/guild#modify-guild-role-positions
    internal async Task<List<Role>> ModifyGuildRolePositionsAsync(ulong guildId, Dictionary<Role, int> positions, string? reason)
    {
        var payload = new List<Dictionary<string, object>>();
        foreach (var (k, v) in positions)
        {
            var dict = new Dictionary<string, object>
            {
                { "id", k.Id },
                { "position", v }
            };
            payload.Add(dict);
        }
        string data = await RequestAsync(Patch, Route($"/guilds/{guildId}/roles"), payload, reason);
        var roles = JsonConvert.DeserializeObject<List<Role>>(data)!;
        SetRoleValues(roles, guildId);
        return roles;
    }
    
    // Modify a guild role. Requires the MANAGE_ROLES permission. Returns the updated role on success. Fires a Guild Role
    // Update Gateway event.
    // https://discord.com/developers/docs/resources/guild#modify-guild-role
    internal async Task<Role> ModifyGuildRoleAsync(ulong guildId, ulong roleId, RoleEdit edit, string? reason)
    {
        string data = await RequestAsync(Patch, Route($"/guilds/{guildId}/roles/{roleId}"), edit._payload, reason);
        var role = JsonConvert.DeserializeObject<Role>(data)!;
        SetRoleValues([role], guildId);
        return role;
    }
    
    // Delete a guild role. Requires the MANAGE_ROLES permission. Returns a 204 empty response on success. Fires a Guild
    // Role Delete Gateway event.
    // https://discord.com/developers/docs/resources/guild#delete-guild-role
    internal async Task DeleteGuildRoleAsync(ulong guildId, ulong roleId, string? reason)
    {
        await RequestAsync(Delete, Route($"/guilds/{guildId}/roles/{roleId}"), auditReason: reason);
    }
    
    internal void SetMemberValues(IEnumerable<Member> members, ulong guildId)
    {
        foreach (var member in members)
        {
            member.Bot = _bot;
            member.GuildId = guildId;
        }
    }
    
    // Returns a guild member object for the specified user.
    // https://discord.com/developers/docs/resources/guild#get-guild-member
    internal async Task<Member> GetGuildMemberAsync(ulong guildId, ulong userId)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/members/{userId}"));
        var member = JsonConvert.DeserializeObject<Member>(data)!;
        SetMemberValues([member], guildId);
        return member;
    }
    
    // Returns a list of guild member objects that are members of the guild. This endpoint is restricted according to
    // whether the GUILD_MEMBERS Privileged Intent is enabled for your application.
    // https://discord.com/developers/docs/resources/guild#list-guild-members
    internal async Task<List<Member>> ListGuildMembersAsync(ulong guildId, int limit, ulong afterSnowflake)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/members?limit={limit}&after={afterSnowflake}"));
        var members = JsonConvert.DeserializeObject<List<Member>>(data)!;
        SetMemberValues(members, guildId);
        return members;
    }
    
    // Returns a list of guild member objects whose username or nickname starts with a provided string.
    // https://discord.com/developers/docs/resources/guild#search-guild-members
    internal async Task<List<Member>> SearchGuildMembersAsync(ulong guildId, string query, int limit)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/members/search?query={query}&limit={limit}"));
        var members = JsonConvert.DeserializeObject<List<Member>>(data)!;
        SetMemberValues(members, guildId);
        return members;
    }
    
    // Modify attributes of a guild member. Returns a 200 OK with the guild member as the body. Fires a Guild Member
    // Update Gateway event. If the channel_id is set to null, this will force the target user to be disconnected from voice.
    // https://discord.com/developers/docs/resources/guild#modify-guild-member
    internal async Task<Member> ModifyGuildMemberAsync(ulong guildId, ulong userId, MemberEdit edit, string? reason)
    {
        string data = await RequestAsync(Patch, Route($"/guilds/{guildId}/members/{userId}"), edit._payload, reason);
        var member = JsonConvert.DeserializeObject<Member>(data)!;
        SetMemberValues([member], guildId);
        return member;
    }
    
    // Modifies the current member in a guild. Returns a 200 with the updated member object on success. Fires a Guild
    // Member Update Gateway event.
    // https://discord.com/developers/docs/resources/guild#modify-current-member
    internal async Task<Member> ModifyCurrentMemberAsync(ulong guildId, BotMemberEdit edit, string? reason)
    {
        string data = await RequestAsync(Patch, Route($"/guilds/{guildId}/members/@me"), edit._payload, reason);
        var member = JsonConvert.DeserializeObject<Member>(data)!;
        SetMemberValues([member], guildId);
        return member;
    }
    
    // Adds a role to a guild member. Requires the MANAGE_ROLES permission. Returns a 204 empty response on success.
    // Fires a Guild Member Update Gateway event.
    // https://discord.com/developers/docs/resources/guild#add-guild-member-role
    internal async Task AddGuildMemberRoleAsync(ulong guildId, ulong userId, ulong roleID, string? reason)
    {
        await RequestAsync(Put, Route($"/guilds/{guildId}/members/{userId}/roles/{roleID}"), auditReason: reason);
    }
    
    // Removes a role to a guild member. Requires the MANAGE_ROLES permission. Returns a 204 empty response on success.
    // Fires a Guild Member Update Gateway event.
    // https://discord.com/developers/docs/resources/guild#remove-guild-member-role
    internal async Task RemoveGuildMemberRoleAsync(ulong guildId, ulong userId, ulong roleID, string? reason)
    {
        await RequestAsync(Delete, Route($"/guilds/{guildId}/members/{userId}/roles/{roleID}"), auditReason: reason);
    }
    
    // Remove a member from a guild. Requires KICK_MEMBERS permission. Returns a 204 empty response on success. Fires a
    // Guild Member Remove Gateway event.
    // https://discord.com/developers/docs/resources/guild#remove-guild-member
    internal async Task RemoveGuildMemberAsync(ulong guildId, ulong userId, string? reason)
    {
        await RequestAsync(Delete, Route($"/guilds/{guildId}/members/{userId}"), auditReason: reason);
    }
    
    // Returns a list of ban objects for the users banned from this guild. Requires the BAN_MEMBERS permission.
    // https://discord.com/developers/docs/resources/guild#get-guild-bans
    internal async Task<List<BanRecord>> GetGuildBansAsync(ulong guildId, int limit, ulong? before, ulong? after)
    {
        string route = Route($"/guilds/{guildId}/bans?limit={limit}");
        if (before.HasValue) route += $"&before={before.Value}";
        if (after.HasValue) route += $"&after={after.Value}";
        string data = await RequestAsync(Get, route);
        return JsonConvert.DeserializeObject<List<BanRecord>>(data)!;
    }
    
    // Returns a ban object for the given user or a 404 not found if the ban cannot be found. Requires the BAN_MEMBERS permission.
    // https://discord.com/developers/docs/resources/guild#get-guild-ban
    internal async Task<BanRecord> GetGuildBanAsync(ulong guildId, ulong userId)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/bans/{userId}"));
        return JsonConvert.DeserializeObject<BanRecord>(data)!;
    }
    
    // Create a guild ban, and optionally delete previous messages sent by the banned user. Requires the BAN_MEMBERS
    // permission. Returns a 204 empty response on success. Fires a Guild Ban Add Gateway event.
    // https://discord.com/developers/docs/resources/guild#create-guild-ban
    internal async Task CreateGuildBanAsync(ulong guildId, ulong userId, TimeSpan seconds, string? reason)
    {
        string route = Route($"/guilds/{guildId}/bans/{userId}?delete_message_seconds={seconds.TotalSeconds}");
        await RequestAsync(Put, route, auditReason: reason);
    }
    
    // Remove the ban for a user. Requires the BAN_MEMBERS permissions. Returns a 204 empty response on success. Fires a
    // Guild Ban Remove Gateway event.
    // https://discord.com/developers/docs/resources/guild#remove-guild-ban
    internal async Task RemoveGuildBanAsync(ulong guildId, ulong userId, string? reason)
    {
        await RequestAsync(Delete, Route($"/guilds/{guildId}/bans/{userId}"), auditReason: reason);
    }
    
    // Ban up to 200 users from a guild, and optionally delete previous messages sent by the banned users. Requires both
    // the BAN_MEMBERS and MANAGE_GUILD permissions. Returns a 200 response on success, including the fields banned_users
    // with the IDs of the banned users and failed_users with IDs that could not be banned or were already banned.
    // https://discord.com/developers/docs/resources/guild#bulk-guild-ban
    internal async Task<(List<ulong> bannedUsers, List<ulong> failedUsers)> BulkGuildBanAsync(ulong guildId,
        JSON payload, string? reason)
    {
        string data = await RequestAsync(Post, Route($"/guilds/{guildId}/bulk-ban"), payload, reason);
        var doc = JsonDocument.Parse(data);
        var bannedUsers = Gateway.Deserialize<List<ulong>>(doc.RootElement.GetProperty("banned_users"));
        var failedUsers = Gateway.Deserialize<List<ulong>>(doc.RootElement.GetProperty("failed_users"));
        return (bannedUsers, failedUsers);
    }
    
    // Returns an object with one pruned key indicating the number of members that would be removed in a prune operation.
    // Requires the MANAGE_GUILD and KICK_MEMBERS permissions.
    // 
    // By default, prune will not remove users with roles. You can optionally include specific roles in your prune by
    // providing the include_roles parameter. Any inactive user that has a subset of the provided role(s) will be counted
    // in the prune and users with additional roles will not.
    // https://discord.com/developers/docs/resources/guild#get-guild-prune-count
    internal async Task<int> GetGuildPruneCountAsync(ulong guildId, int days, IEnumerable<Role>? roles)
    {
        string route = Route($"/guilds/{guildId}/prune?days={days}");
        if (roles is not null) route += $"&include_roles={string.Join(",", roles.Select(r => r.Id))}";
        string data = await RequestAsync(Get, route);
        var payload = JsonConvert.DeserializeObject<JSON>(data)!;
        return Convert.ToInt32(payload.Values.First());
    }
    
    // Begin a prune operation. Requires the MANAGE_GUILD and KICK_MEMBERS permissions. Returns an object with one pruned
    // key indicating the number of members that were removed in the prune operation. For large guilds it's recommended
    // to set the compute_prune_count option to false, forcing pruned to null. Fires multiple Guild Member Remove
    // Gateway events.
    // 
    // By default, prune will not remove users with roles. You can optionally include specific roles in your prune by
    // providing the include_roles parameter. Any inactive user that has a subset of the provided role(s) will be
    // included in the prune and users with additional roles will not.
    // https://discord.com/developers/docs/resources/guild#begin-guild-prune
    internal async Task<int?> BeginGuildPruneAsync(ulong guildId, int days, bool computePruneCount, IEnumerable<Role>? roles,
        string? reason)
    {
        var payload = new JSON
        {
            { "days", days },
            { "compute_prune_count", computePruneCount },
            { "include_roles", roles?.Select(r => r.Id) ?? [] }
        };
        string data = await RequestAsync(Post, Route($"/guilds/{guildId}/prune"), payload, reason);
        var result = JsonConvert.DeserializeObject<JSON>(data)!;
        return computePruneCount ? Convert.ToInt32(result.Values.First()) : null;
    }
    
    // UNUSED
    // https://discord.com/developers/docs/resources/guild#get-guild-voice-regions
    
    // Returns a list of invite objects. Requires the MANAGE_GUILD or VIEW_AUDIT_LOG permission. Invite Metadata is
    // included with the MANAGE_GUILD permission.
    // https://discord.com/developers/docs/resources/guild#get-guild-invites
    internal async Task<List<Invite>> GetGuildInvitesAsync(ulong guildId)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/invites"));
        var invites = JsonConvert.DeserializeObject<List<Invite>>(data)!;
        SetInviteValues(invites);
        return invites;
    }
    
    // Returns a list of integration objects for the guild. Requires the MANAGE_GUILD permission.
    // 
    // This endpoint returns a maximum of 50 integrations. If a guild has more integrations, they cannot be accessed.
    // https://discord.com/developers/docs/resources/guild#get-guild-integrations
    internal async Task<List<Integration>> GetGuildIntegrations(ulong guildId)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/integrations"));
        var integrations = JsonConvert.DeserializeObject<List<Integration>>(data)!;
        foreach (var inte in integrations)
        {
            inte.Bot = _bot;
            inte.GuildId = guildId;
        }
        return integrations;
    }
    
    // Delete the attached integration object for the guild. Deletes any associated webhooks and kicks the associated bot
    // if there is one. Requires the MANAGE_GUILD permission. Returns a 204 empty response on success. Fires Guild
    // Integrations Update and Integration Delete Gateway events.
    // https://discord.com/developers/docs/resources/guild#delete-guild-integration
    internal async Task DeleteGuildIntegrationAsync(ulong guildId, ulong integrationId, string? reason)
    {
        await RequestAsync(Delete, Route($"/guilds/{guildId}/integrations/{integrationId}"), auditReason: reason);
    }
    
    // Returns a guild widget settings object. Requires the MANAGE_GUILD permission.
    // https://discord.com/developers/docs/resources/guild#get-guild-widget-settings
    internal async Task<WidgetSetting> GetGuildWidgetSettingsAsync(ulong guildId)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/widget"));
        return JsonConvert.DeserializeObject<WidgetSetting>(data)!;
    }
    
    // Modify a guild widget settings object for the guild. All attributes may be passed in with JSON and modified.
    // Requires the MANAGE_GUILD permission. Returns the updated guild widget settings object. Fires a Guild Update
    // Gateway event.
    // https://discord.com/developers/docs/resources/guild#modify-guild-widget
    internal async Task<WidgetSetting> ModifyGuildWidgetAsync(ulong guildId, bool enabled, ulong? channelId, string? reason)
    {
        var payload = new JSON
        {
            { "enabled", enabled },
            { "channel_id", channelId }
        };
        string data = await RequestAsync(Patch, Route($"/guilds/{guildId}/widget"), payload, reason);
        return JsonConvert.DeserializeObject<WidgetSetting>(data)!;
    }
    
    // Returns the widget for the guild. Fires an Invite Create Gateway event when an invite channel is defined and a
    // new Invite is generated.
    // https://discord.com/developers/docs/resources/guild#get-guild-widget
    internal async Task<Widget> GetGuildWidgetAsync(ulong guildId)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/widget.json"));
        return JsonConvert.DeserializeObject<Widget>(data)!;
    }
    
    // Returns a partial invite object for guilds with that feature enabled. Requires the MANAGE_GUILD permission. code
    // will be null if a vanity url for the guild is not set.
    //
    // This endpoint is required to get the usage count of the vanity invite, but the invite code can be accessed as
    // vanity_url_code in the guild object without having the MANAGE_GUILD permission.
    // https://discord.com/developers/docs/resources/guild#get-guild-vanity-url
    internal async Task<(string? code, int uses)> GetGuildVanityUrlAsync(ulong guildId)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/vanity-url"));
        var doc = JsonDocument.Parse(data);
        var codeElement = doc.RootElement.GetProperty("code"); 
        var code = codeElement.ValueKind == JsonValueKind.Null ? null : codeElement.GetString();
        var uses = doc.RootElement.GetProperty("uses").GetInt32();
        return (code, uses);
    }
    
    // Returns a PNG image widget for the guild. Requires no permissions or authentication.
    // https://discord.com/developers/docs/resources/guild#get-guild-widget-image
    internal async Task<DFile> GetGuildWidgetImageAsync(ulong guildId, WidgetStyle style)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/widget.png?style={style.GetDescription()}"));
        var bytes = Encoding.UTF8.GetBytes(data);
        return new DFile("widget.png", bytes);
    }
    
    // TODO:
    // Not used anymore? I don't see anything regarding welcome screens in the app, unless they've effectively been
    // replaced by onboarding?? Idk
    // https://discord.com/developers/docs/resources/guild#get-guild-welcome-screen
    
    // TODO:
    // See above comment.
    // https://discord.com/developers/docs/resources/guild#modify-guild-welcome-screen
    
    // Returns the Onboarding object for the guild.
    // https://discord.com/developers/docs/resources/guild#get-guild-onboarding
    internal async Task<Onboarding> GetGuildOnboardingAsync(ulong guildId)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/onboarding"));
        return JsonConvert.DeserializeObject<Onboarding>(data)!;
    }
    
    // Modifies the onboarding configuration of the guild. Returns a 200 with the Onboarding object for the guild.
    // Requires the MANAGE_GUILD and MANAGE_ROLES permissions.
    // 
    // Onboarding enforces constraints when enabled. These constraints are that there must be at least 7 Default Channels
    // and at least 5 of them must allow sending messages to the @everyone role. The mode field modifies what is considered
    // when enforcing these constraints.
    // https://discord.com/developers/docs/resources/guild#modify-guild-onboarding
    internal async Task<Onboarding> ModifyGuildOnboardingAsync(ulong guildId, JSON payload, string? reason)
    {
        string data = await RequestAsync(Put, Route($"/guilds/{guildId}/onboarding"), payload, reason);
        return JsonConvert.DeserializeObject<Onboarding>(data)!;
    }
    
    // Modifies the incident actions of the guild. Returns a 200 with the Incidents Data object for the guild. Requires
    // the MANAGE_GUILD permission.
    // 
    // Both invites_disabled_until and dms_disabled_until can be enabled for a maximal timespan of 24 hours in the future.
    // https://discord.com/developers/docs/resources/guild#modify-guild-incident-actions
    internal async Task<Incidents> ModifyGuildIncidentActions(ulong guildId, JSON payload)
    {
        string data = await RequestAsync(Put, Route($"/guilds/{guildId}/incident-actions"), payload);
        return JsonConvert.DeserializeObject<Incidents>(data)!;
    }

    #endregion

    #region INVITE

    private void SetInviteValues(IEnumerable<Invite> invites)
    {
        foreach (var invite in invites)
            invite.Bot = _bot;
    }

    // Returns an invite object for the given code.
    // https://discord.com/developers/docs/resources/invite#get-invite
    internal async Task<Invite> GetInviteAsync(string code, bool withCounts, ulong? eventId)
    {
        string route = Route($"/invites/{code}?with_counts={withCounts}");
        if (eventId is not null) route += $"&guild_scheduled_event_id={eventId.Value}";
        string data = await RequestAsync(Get, route);
        var invite = JsonConvert.DeserializeObject<Invite>(data)!;
        SetInviteValues([invite]);
        return invite;
    }
    
    // Delete an invite. Requires the MANAGE_CHANNELS permission on the channel this invite belongs to, or MANAGE_GUILD
    // to remove any invite across the guild. Returns an invite object on success. Fires an Invite Delete Gateway event.
    // https://discord.com/developers/docs/resources/invite#delete-invite
    internal async Task DeleteInviteAsync(string code, string? reason)
    {
        await RequestAsync(Delete, Route($"/invites/{code}"), auditReason: reason);
    }

    #endregion
    
    #region MESSAGE

    internal void SetMessageValues(IEnumerable<Message> message)
    {
        foreach (var m in message)
        {
            m.Bot = _bot;
            if (m.ReferencedMessage is { } rm)
                rm.Bot = _bot;
            if (m.Poll is { } poll)
            {
                poll.Bot = _bot;
                poll.Message = m;
            }
        }
    }
    
    // Retrieves the messages in a channel. Returns an array of message objects from newest to oldest on success.
    // 
    // If operating on a guild channel, this endpoint requires the current user to have the VIEW_CHANNEL permission.
    // If the channel is a voice channel, they must also have the CONNECT permission.
    // 
    // If the current user is missing the READ_MESSAGE_HISTORY permission in the channel, then no messages will be returned.
    // https://discord.com/developers/docs/resources/message#get-channel-messages
    internal async Task<List<Message>> GetChannelMessages(ulong channelId, MessageHistory history, DateTime? dt, int limit)
    {
        var dtSnowflake = Util.DateTimeToSnowflake(dt ?? DateTime.UtcNow);
        var query = history switch
        {
            MessageHistory.Before => "?before",
            MessageHistory.After => "?after",
            MessageHistory.Around => "?around"
        };
        var endpoint = $"/channels/{channelId}/messages{query}={dtSnowflake}&limit={limit}";
        string data = await RequestAsync(Get, Route(endpoint));
        var messages = JsonConvert.DeserializeObject<List<Message>>(data)!;
        SetMessageValues(messages);
        return messages;
    }
    
    // Retrieves a specific message in the channel. Returns a message object on success.
    // 
    // If operating on a guild channel, this endpoint requires the current user to have the VIEW_CHANNEL and
    // READ_MESSAGE_HISTORY permissions. If the channel is a voice channel, they must also have the CONNECT permission.
    // https://discord.com/developers/docs/resources/message#get-channel-message
    internal async Task<Message> GetChannelMessage(ulong channelId, ulong messageId)
    {
        string data = await RequestAsync(Get, Route($"/channels/{channelId}/messages/{messageId}"));
        var message = JsonConvert.DeserializeObject<Message>(data)!;
        SetMessageValues([message]);
        return message;
    }

    // https://discord.com/developers/docs/resources/message#create-message
    internal async Task<Message> CreateMessageAsync(ulong channelId, MultipartFormDataContent form)
    {
        string data = await RequestAsync(Post, Route($"/channels/{channelId}/messages"), form);
        var message = JsonConvert.DeserializeObject<Message>(data)!;
        SetMessageValues([message]);
        return message;
    }
    
    // Crosspost a message in an Announcement Channel to following channels. This endpoint requires the SEND_MESSAGES
    // permission, if the current user sent the message, or additionally the MANAGE_MESSAGES permission, for all other
    // messages, to be present for the current user.
    // 
    // Returns a message object. Fires a Message Update Gateway event.
    // https://discord.com/developers/docs/resources/message#crosspost-message
    internal async Task<Message> CrosspostMessageAsync(ulong channelId, ulong messageId)
    {
        string data = await RequestAsync(Post, Route($"/channels/{channelId}/messages/{messageId}/crosspost"));
        var message = JsonConvert.DeserializeObject<Message>(data)!;
        SetMessageValues([message]);
        return message;
    }
    
    // Delete a message. If operating on a guild channel and trying to delete a message that was not sent by the current
    // user, this endpoint requires the MANAGE_MESSAGES permission. Returns a 204 empty response on success. Fires a
    // Message Delete Gateway event.
    // https://discord.com/developers/docs/resources/message#delete-message
    internal async Task DeleteMessageAsync(ulong channelId, ulong messageId, string? reason)
    {
        await RequestAsync(Delete, Route($"/channels/{channelId}/messages/{messageId}"), auditReason: reason);
    }

    #endregion

    #region POLL

    // Get a list of users that voted for this specific answer.
    // https://discord.com/developers/docs/resources/poll#get-answer-voters
    internal async Task<List<User>> GetAnswerVotersAsync(ulong channelId, ulong messageId, int answerId, ulong after, int limit)
    {
        string query = Route($"/channels/{channelId}/polls/{messageId}/answers/{answerId}?after={after}&limit={limit}");
        var doc = JsonDocument.Parse(await RequestAsync(Get, query));
        var usersElement = doc.RootElement.GetProperty("users");
        return Gateway.Deserialize<List<User>>(usersElement);
    }
    
    // Immediately ends the poll. You cannot end polls from other users.
    // https://discord.com/developers/docs/resources/poll#end-poll
    internal async Task<Message> EndPollAsync(ulong channelId, ulong messageId)
    {
        string data = await RequestAsync(Post, Route($"/channels/{channelId}/polls/{messageId}/expire"));
        var message = JsonConvert.DeserializeObject<Message>(data)!;
        SetMessageValues([message]);
        return message;
    }

    #endregion
    
    #region STICKER

    // Returns a sticker object for the given sticker ID.
    // https://discord.com/developers/docs/resources/sticker#get-sticker
    internal async Task<Sticker> GetStickerAsync(ulong id)
    {
        string data = await RequestAsync(Get, Route($"/stickers/{id}"));
        return JsonConvert.DeserializeObject<Sticker>(data)!;
    }

    // Returns a list of available sticker packs.
    // https://discord.com/developers/docs/resources/sticker#list-sticker-packs
    internal async Task<List<StickerPack>> ListStickerPacksAsync()
    {
        string data = await RequestAsync(Get, Route($"/sticker-packs"));
        return Util.ExtractFromJson<List<StickerPack>>(data, "sticker_packs");
    }

    // Returns a sticker pack object for the given sticker pack ID.
    // https://discord.com/developers/docs/resources/sticker#get-sticker-pack
    internal async Task<StickerPack> GetStickerPackAsync(ulong id)
    {
        string data = await RequestAsync(Get, Route($"/sticker-packs/{id}"));
        return JsonConvert.DeserializeObject<StickerPack>(data)!;
    }

    // Returns an array of sticker objects for the given guild. Includes user fields if the bot has the
    // CREATE_GUILD_EXPRESSIONS or MANAGE_GUILD_EXPRESSIONS permission.
    // https://discord.com/developers/docs/resources/sticker#list-guild-stickers
    internal async Task<List<GuildSticker>> ListGuildStickersAsync(ulong guildId)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/stickers"));
        var stickers = JsonConvert.DeserializeObject<List<GuildSticker>>(data)!;
        stickers.ForEach(s => s.Bot = _bot);
        return stickers;
    }

    // Returns a sticker object for the given guild and sticker IDs. Includes the user field if the bot has the
    // CREATE_GUILD_EXPRESSIONS or MANAGE_GUILD_EXPRESSIONS permission.
    // https://discord.com/developers/docs/resources/sticker#get-guild-sticker
    internal async Task<GuildSticker> GetGuildStickerAsync(ulong guildId, ulong stickerId)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/stickers/{stickerId}"));
        var sticker = JsonConvert.DeserializeObject<GuildSticker>(data)!;
        sticker.Bot = _bot;
        return sticker;
    }

    // Create a new sticker for the guild. Send a multipart/form-data body. Requires the CREATE_GUILD_EXPRESSIONS permission.
    // Returns the new sticker object on success. Fires a Guild Stickers Update Gateway event.
    // https://discord.com/developers/docs/resources/sticker#create-guild-sticker
    internal async Task<GuildSticker> CreateGuildStickerAsync(ulong guildId, string name, string description,
        string emoji, DFile file, string? reason)
    {
        var boundary = Guid.NewGuid().ToString().Replace("-", string.Empty);

        using var form = new MultipartFormDataContent(boundary);
        form.Add(new StringContent(name), "name");
        form.Add(new StringContent(description), "description");
        form.Add(new StringContent(emoji), "tags");

        var fileContent = new ByteArrayContent(file.Bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file._mimeType);
        form.Add(fileContent, "file", file.Name);

        string data = await RequestAsync(Post, Route($"/guilds/{guildId}/stickers"), form, reason);
        var sticker = JsonConvert.DeserializeObject<GuildSticker>(data)!;
        sticker.Bot = _bot;
        return sticker;
    }

    // Modify the given sticker. For stickers created by the current user, requires either the CREATE_GUILD_EXPRESSIONS
    // or MANAGE_GUILD_EXPRESSIONS permission. For other stickers, requires the MANAGE_GUILD_EXPRESSIONS permission.
    // Returns the updated sticker object on success. Fires a Guild Stickers Update Gateway event.
    // https://discord.com/developers/docs/resources/sticker#modify-guild-sticker
    internal async Task<GuildSticker> ModifyGuildStickerAsync(ulong guildId, ulong stickerId, GuildStickerEdit edit,
        string? reason)
    {
        string data =
            await RequestAsync(Patch, Route($"/guilds/{guildId}/stickers/{stickerId}"), edit._payload, reason);
        var sticker = JsonConvert.DeserializeObject<GuildSticker>(data)!;
        sticker.Bot = _bot;
        return sticker;
    }

    // Delete the given sticker. For stickers created by the current user, requires either the CREATE_GUILD_EXPRESSIONS
    // or MANAGE_GUILD_EXPRESSIONS permission. For other stickers, requires the MANAGE_GUILD_EXPRESSIONS permission.
    // Returns 204 No Content on success. Fires a Guild Stickers Update Gateway event.
    // https://discord.com/developers/docs/resources/sticker#delete-guild-sticker
    internal async Task DeleteGuildStickerAsync(ulong guildId, ulong stickerId, string? reason) =>
        await RequestAsync(Delete, Route($"/guilds/{guildId}/stickers/{stickerId}"), auditReason: reason);

    #endregion

    #region SOUNDBOARD

    internal void SetSoundboardSoundValues(IEnumerable<SoundboardSound> sounds)
    {
        foreach (var sound in sounds)
            sound.Bot = _bot;
    }

    // Send a soundboard sound to a voice channel the user is connected to. Fires a Voice Channel Effect Send Gateway event.
    // 
    // Requires the SPEAK and USE_SOUNDBOARD permissions, and also the USE_EXTERNAL_SOUNDS permission if the sound is from
    // a different server. Additionally, requires the user to be connected to the voice channel, having a voice state
    // without deaf, self_deaf, mute, or suppress enabled.
    // https://discord.com/developers/docs/resources/soundboard#send-soundboard-sound
    internal async Task SendSoundboardSoundAsync(ulong channelId, ulong soundId, ulong? guildId)
    {
        var payload = new JSON
        {
            { "sound_id", soundId },
            { "source_guild_id", guildId },
        };
        await RequestAsync(HttpMethod.Post, Route($"/channels/{channelId}/send-soundboard-sound"), payload);
    }
    
    // Returns an array of soundboard sound objects that can be used by all users.
    // https://discord.com/developers/docs/resources/soundboard#list-default-soundboard-sounds
    internal async Task<List<SoundboardSound>> ListDefaultSoundboardSoundsAsync()
    {
        string data = await RequestAsync(Get, Route("/soundboard-default-sounds"));
        var sounds = JsonConvert.DeserializeObject<List<SoundboardSound>>(data)!;
        SetSoundboardSoundValues(sounds);
        return sounds;
    }
    
    // Returns a list of the guild's soundboard sounds. Includes user fields if the bot has the CREATE_GUILD_EXPRESSIONS
    // or MANAGE_GUILD_EXPRESSIONS permission.
    // https://discord.com/developers/docs/resources/soundboard#list-guild-soundboard-sounds
    internal async Task<List<SoundboardSound>> ListGuildSoundboardSoundsAsync(ulong guildId)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/soundboard-sounds"));
        var doc = JsonDocument.Parse(data);
        var element = doc.RootElement.GetProperty("items");
        var sounds = Gateway.Deserialize<List<SoundboardSound>>(element);
        SetSoundboardSoundValues(sounds);
        return sounds;
    }
    
    // Returns a soundboard sound object for the given sound id. Includes the user field if the bot has the
    // CREATE_GUILD_EXPRESSIONS or MANAGE_GUILD_EXPRESSIONS permission.
    // https://discord.com/developers/docs/resources/soundboard#get-guild-soundboard-sound
    internal async Task<SoundboardSound> GetGuildSoundboardSoundAsync(ulong guildId, ulong soundId)
    {
        string data = await RequestAsync(HttpMethod.Get, Route($"/guilds/{guildId}/soundboard-sounds/{soundId}"));
        var sound = JsonConvert.DeserializeObject<SoundboardSound>(data)!;
        SetSoundboardSoundValues([sound]);
        return sound;
    }
    
    // Create a new soundboard sound for the guild. Requires the CREATE_GUILD_EXPRESSIONS permission. Returns the new
    // soundboard sound object on success. Fires a Guild Soundboard Sound Create Gateway event.
    //
    // Soundboard sounds have a max file size of 512kb and a max duration of 5.2 seconds.
    // https://discord.com/developers/docs/resources/soundboard#create-guild-soundboard-sound
    internal async Task<SoundboardSound> CreateGuildSoundboardSoundAsync(ulong guildId, object payload, string? reason)
    {
        string data = await RequestAsync(Post, Route($"/guilds/{guildId}/soundboard-sounds"), payload, reason);
        var sound = JsonConvert.DeserializeObject<SoundboardSound>(data)!;
        SetSoundboardSoundValues([sound]);
        return sound;
    }

    // Modify the given soundboard sound. For sounds created by the current user, requires either the CREATE_GUILD_EXPRESSIONS
    // or MANAGE_GUILD_EXPRESSIONS permission. For other sounds, requires the MANAGE_GUILD_EXPRESSIONS permission.
    // Returns the updated soundboard sound object on success. Fires a Guild Soundboard Sound Update Gateway event.
    // https://discord.com/developers/docs/resources/soundboard#modify-guild-soundboard-sound
    internal async Task<SoundboardSound> ModifyGuildSoundboardSoundAsync(ulong guildId, ulong soundId,
        SoundboardSoundEdit edit, string? reason)
    {
        string data = await RequestAsync(Patch, Route($"/guilds/{guildId}/soundboard-sounds/{soundId}"), edit._payload,
            reason);
        var sound = JsonConvert.DeserializeObject<SoundboardSound>(data)!;
        SetSoundboardSoundValues([sound]);
        return sound;
    }
    
    // Delete the given soundboard sound. For sounds created by the current user, requires either the CREATE_GUILD_EXPRESSIONS
    // or MANAGE_GUILD_EXPRESSIONS permission. For other sounds, requires the MANAGE_GUILD_EXPRESSIONS permission.
    // Returns 204 No Content on success. Fires a Guild Soundboard Sound Delete Gateway event.
    // https://discord.com/developers/docs/resources/soundboard#delete-guild-soundboard-sound
    internal async Task DeleteGuildSoundboardSoundAsync(ulong guildId, ulong soundId, string? reason)
    {
        await RequestAsync(Delete, Route($"/guilds/{guildId}/soundboard-sounds/{soundId}"), reason);
    }

    #endregion

    #region STAGE INSTANCE

    private void SetStageInstanceValues(StageInstance stageInstance)
    {
        stageInstance.Bot = _bot;
    }

    // Creates a new Stage instance associated to a Stage channel. Returns that Stage instance. Fires a Stage Instance
    // Create Gateway event.
    // 
    // Requires the user to be a moderator of the Stage channel.
    // https://discord.com/developers/docs/resources/stage-instance#create-stage-instance
    internal async Task<StageInstance> CreateStageInstanceAsync(JSON payload, string? reason)
    {
        string data = await RequestAsync(Post, Route("/stage-instances"), payload, reason);
        var stageInstance = JsonConvert.DeserializeObject<StageInstance>(data)!;
        SetStageInstanceValues(stageInstance);
        return stageInstance;
    }
    
    // Gets the stage instance associated with the Stage channel, if it exists.
    // https://discord.com/developers/docs/resources/stage-instance#get-stage-instance
    internal async Task<StageInstance> GetStageInstanceAsync(ulong stageChannelId)
    {
        string data = await RequestAsync(Get, Route($"/stage-instances/{stageChannelId}"));
        var stageInstance = JsonConvert.DeserializeObject<StageInstance>(data)!;
        SetStageInstanceValues(stageInstance);
        return stageInstance;
    }
    
    // Updates fields of an existing Stage instance. Returns the updated Stage instance. Fires a Stage Instance Update Gateway event.
    // 
    // Requires the user to be a moderator of the Stage channel.
    // https://discord.com/developers/docs/resources/stage-instance#modify-stage-instance
    internal async Task<StageInstance> ModifyStageInstance(ulong stageChannelId, StageInstanceEdit edit, string? reason)
    {
        string data = await RequestAsync(Patch, Route($"/stage-instances/{stageChannelId}"), edit._payload, reason);
        var stageInstance = JsonConvert.DeserializeObject<StageInstance>(data)!;
        SetStageInstanceValues(stageInstance);
        return stageInstance;
    }
    
    // Deletes the Stage instance. Returns 204 No Content. Fires a Stage Instance Delete Gateway event.
    // 
    // Requires the user to be a moderator of the Stage channel.
    // https://discord.com/developers/docs/resources/stage-instance#delete-stage-instance
    internal async Task DeleteStageInstanceAsync(ulong stageChannelId, string? reason) => 
        await RequestAsync(Delete, Route($"/stage-instances/{stageChannelId}"), auditReason: reason);

    #endregion
    
    #region USER

    // Leave a guild. Returns a 204 empty response on success. Fires a Guild Delete Gateway event and a Guild Member
    // Remove Gateway event.
    // https://discord.com/developers/docs/resources/user#leave-guild
    internal async Task LeaveGuildAsync(ulong guildId)
    {
        await RequestAsync(Delete, Route($"/users/@me/guilds/{guildId}"));
    }
    
    // Create a new DM channel with a user. Returns a DM channel object (if one already exists, it will be returned instead).
    // https://discord.com/developers/docs/resources/user#create-dm
    internal async Task<DmChannel> CreateDmAsync(ulong userId)
    {
        var payload = new JSON { { "recipient_id", userId } };
        string data = await RequestAsync(Post, Route("/users/@me/channels"), payload);
        var channel = JsonConvert.DeserializeObject<DmChannel>(data)!;
        channel.Bot = _bot;
        return channel;
    }

    #endregion

    #region VOICE

    

    #endregion

    #region WEBHOOK

    private void SetWebhookValues(IEnumerable<Webhook> webhooks)
    {
        foreach (var webhook in webhooks)
            webhook.Bot = _bot;
    }
    
    // Creates a new webhook and returns a webhook object on success. Requires the MANAGE_WEBHOOKS permission. Fires a
    // Webhooks Update Gateway event.
    // 
    // An error will be returned if a webhook name (name) is not valid. A webhook name is valid if:
    // 
    // It does not contain the substrings clyde or discord (case-insensitive)
    // It follows the nickname guidelines in the Usernames and Nicknames documentation, with an exception that webhook
    // names can be up to 80 characters
    // https://discord.com/developers/docs/resources/webhook#create-webhook
    internal async Task<Webhook> CreateWebhookAsync(ulong channelId, string name, DFile? icon, string? reason)
    {
        var payload = new JSON { { "name", name } };
        if (icon != null)
            payload["avatar"] = icon._mimeTypeBase64;
        string data = await RequestAsync(Post, Route($"/channels/{channelId}/webhooks"), payload, reason);
        var webhook = JsonConvert.DeserializeObject<Webhook>(data)!;
        SetWebhookValues([webhook]);
        return webhook;
    }
    
    // Returns a list of channel webhook objects. Requires the MANAGE_WEBHOOKS permission.
    // https://discord.com/developers/docs/resources/webhook#get-channel-webhooks
    internal async Task<List<Webhook>> GetChannelWebhooksAsync(ulong channelId)
    {
        string data = await RequestAsync(Get, Route($"/channels/{channelId}/webhooks"));
        var webhooks = JsonConvert.DeserializeObject<List<Webhook>>(data)!;
        SetWebhookValues(webhooks);
        return webhooks;
    }
    
    // Returns a list of guild webhook objects. Requires the MANAGE_WEBHOOKS permission.
    // https://discord.com/developers/docs/resources/webhook#get-guild-webhooks
    internal async Task<List<Webhook>> GetGuildWebhooksAsync(ulong guildId)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/webhooks"));
        var webhooks = JsonConvert.DeserializeObject<List<Webhook>>(data)!;
        SetWebhookValues(webhooks);
        return webhooks;
    }

    // Returns the new webhook object for the given id.
    // 
    // This request requires the MANAGE_WEBHOOKS permission unless the application making the request owns the webhook.
    // https://discord.com/developers/docs/resources/webhook#get-webhook
    internal async Task<Webhook> GetWebhookAsync(ulong webhookId)
    {
        string data = await RequestAsync(Get, Route($"/webhooks/{webhookId}"));
        var webhook = JsonConvert.DeserializeObject<Webhook>(data)!;
        SetWebhookValues([webhook]);
        return webhook;
    }
    
    // Same as above, except this call does not require authentication and returns no user in the webhook object.
    // https://discord.com/developers/docs/resources/webhook#get-webhook-with-token
    internal static async Task<Webhook> GetWebhookWithTokenAsync(ulong webhookId, string webhookToken, HttpClient http)
    {
        string data = await http.GetStringAsync(Route($"/webhooks/{webhookId}/{webhookToken}"));
        var webhook = JsonConvert.DeserializeObject<Webhook>(data)!;
        return webhook;
    }
    
    // Modify a webhook. Requires the MANAGE_WEBHOOKS permission. Returns the updated webhook object on success.
    // Fires a Webhooks Update Gateway event.
    // https://discord.com/developers/docs/resources/webhook#modify-webhook
    internal async Task<Webhook> ModifyWebhookAsync(ulong webhookId, WebhookEdit edit, string? reason)
    {
        string data = await RequestAsync(Patch, Route($"/webhooks/{webhookId}"), edit._payload, reason);
        var webhook = JsonConvert.DeserializeObject<Webhook>(data)!;
        SetWebhookValues([webhook]);
        return webhook;
    }
    
    // Same as above, except this call does not require authentication, does not accept a channel_id parameter in the body,
    // and does not return a user in the webhook object.
    // https://discord.com/developers/docs/resources/webhook#modify-webhook-with-token
    internal static async Task<Webhook> ModifyWebhookWithTokenAsync(ulong webhookId, string webhookToken,
        WebhookEdit edit, HttpClient http)
    {
        var content = new StringContent(JsonConvert.SerializeObject(edit._payload));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        
        var response = await http.PatchAsync(Route($"/webhooks/{webhookId}/{webhookToken}"), content);
        var json = await response.Content.ReadAsStringAsync();
        
        if (response.IsSuccessStatusCode) return JsonConvert.DeserializeObject<Webhook>(json)!;
        var errorMessage = Convert.ToString(JsonConvert.DeserializeObject<JSON>(json)!.Values.First());
        throw new HttpRequestException(errorMessage);
    }
    
    // Delete a webhook permanently. Requires the MANAGE_WEBHOOKS permission. Returns a 204 No Content response on success.
    // Fires a Webhooks Update Gateway event.
    // https://discord.com/developers/docs/resources/webhook#delete-webhook
    internal async Task DeleteWebhookAsync(ulong webhookId, string? reason) =>
        await RequestAsync(Delete, Route($"/webhooks/{webhookId}"), auditReason: reason);
    
    // Same as above, except this call does not require authentication.
    // https://discord.com/developers/docs/resources/webhook#delete-webhook-with-token
    internal static async Task DeleteWebhookWithTokenAsync(ulong webhookId, string webhookToken, HttpClient http) =>
        await http.DeleteAsync(Route($"/webhooks/{webhookId}/{webhookToken}"));

    #endregion
    
    // Combine the base API route with the HTTP request-specific route.
    private static string Route(string endpoint, ApiRoute route = ApiRoute.Base)
    {
        if (endpoint.StartsWith('/'))
            return route.GetDescription() + endpoint;
        throw new ArgumentException("Parameter must start with '/'", nameof(endpoint));
    }

    // https://discord.com/developers/docs/events/gateway#get-gateway
    internal async Task<string> GetGatewayAsync()
    {
        string data = await RequestAsync(Get, Route("/gateway"));
        var obj = JsonConvert.DeserializeObject<JSON>(data);
        return obj["url"].ToString()!;
    }

    // https://discord.com/developers/docs/events/gateway#get-gateway-bot
    internal async Task<(string url, int shards, int sslTotal, int sslRemaining, int sslReset, int sslMax)>
        GetGatewayBotAsync()
    {
        string payload = await RequestAsync(Get, Route("/gateway/bot"));
        var data = JsonConvert.DeserializeObject<JSON>(payload);
        var sessionObj = JsonConvert.DeserializeObject<JSON>(data["session_start_limit"].ToString());
        var obj = new
        {
            url = data["url"],
            shards = Convert.ToInt32(data["shards"]),
            sessionStartLimit = new
            {
                total = Convert.ToInt32(sessionObj["total"]),
                remaining = Convert.ToInt32(sessionObj["remaining"]),
                resetAfter = Convert.ToInt32(sessionObj["reset_after"]),
                maxConcurrency = Convert.ToInt32(sessionObj["max_concurrency"])
            }
        };
        return (
            obj.url + "",
            obj.shards,
            obj.sessionStartLimit.total,
            obj.sessionStartLimit.remaining,
            obj.sessionStartLimit.resetAfter,
            obj.sessionStartLimit.maxConcurrency
        );
    }

    private class RateLimitBucket
    {
        public int Limit { get; set; }
        public int Remaining { get; set; }
        public DateTimeOffset ResetAt { get; set; }
        public Queue<(HttpRequestMessage request, TaskCompletionSource<string> tcs)> Queue { get; } = new();
        public bool IsProcessing { get; set; }
    }

    private async Task<string> RequestAsync(HttpMethod method, string route, object? data = null, string? auditReason = null)
    {
        string bucketKey = GetBucketKey(route);

        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        var request = new HttpRequestMessage(method, route);
        if (data != null)
            request.Content = data is MultipartFormDataContent form ? form : ToStringContent(data);

        if (auditReason != null)
            request.Headers.Add("X-Audit-Log-Reason", auditReason);

        RateLimitBucket bucket;
        lock (_bucketLock)
        {
            if (!_buckets.TryGetValue(bucketKey, out bucket))
            {
                bucket = new RateLimitBucket { Remaining = int.MaxValue, ResetAt = DateTimeOffset.UtcNow };
                _buckets[bucketKey] = bucket;
            }

            bucket.Queue.Enqueue((request, tcs));

            if (!bucket.IsProcessing)
            {
                bucket.IsProcessing = true;
                _ = ProcessBucket(bucketKey, bucket);
            }
        }

        return await tcs.Task;
    }

    private async Task ProcessBucket(string bucketKey, RateLimitBucket bucket)
    {
        while (true)
        {
            (HttpRequestMessage request, TaskCompletionSource<string> tcs) item;

            lock (_bucketLock)
            {
                if (bucket.Queue.Count == 0)
                {
                    bucket.IsProcessing = false;
                    return;
                }

                item = bucket.Queue.Peek();
            }

            // Global rate limit check
            await _globalSemaphore.WaitAsync();
            try
            {
                if (_globalResetAt > DateTimeOffset.UtcNow)
                {
                    var delay = _globalResetAt - DateTimeOffset.UtcNow;
                    Dev.Log($"[RateLimit] Global rate limit active, waiting {delay.TotalMilliseconds}ms");
                    await Task.Delay(delay);
                }
            }
            finally
            {
                _globalSemaphore.Release();
            }

            // Preemptive delay if bucket exhausted
            if (bucket.Remaining <= 0 && bucket.ResetAt > DateTimeOffset.UtcNow)
            {
                var delay = bucket.ResetAt - DateTimeOffset.UtcNow;
                Dev.Log($"[RateLimit] Waiting {delay.TotalMilliseconds}ms for bucket {bucketKey}");
                await Task.Delay(delay);
            }

            var payload = string.Empty;
            try
            {
                using var response = await _http.SendAsync(item.request);
                payload = await response.Content.ReadAsStringAsync();

                UpdateBucketFromHeaders(bucketKey, bucket, response.Headers);

                if (response.IsSuccessStatusCode)
                {
                    item.tcs.TrySetResult(payload);
                }
                else if ((int)response.StatusCode == 429)
                {
                    // Retry-After handling
                    var retryAfterMs = response.Headers.TryGetValues("Retry-After", out var values)
                        ? (int)(double.Parse(values.First(), System.Globalization.CultureInfo.InvariantCulture) * 1000)
                        : 1000;

                    bool isGlobal = response.Headers.TryGetValues("X-RateLimit-Global", out var globalVals)
                                    && globalVals.FirstOrDefault()
                                        ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

                    if (isGlobal)
                    {
                        await _globalSemaphore.WaitAsync();
                        try
                        {
                            _globalResetAt = DateTimeOffset.UtcNow.AddMilliseconds(retryAfterMs);
                        }
                        finally
                        {
                            _globalSemaphore.Release();
                        }

                        Dev.Log($"[RateLimit] GLOBAL 429, pausing all requests for {retryAfterMs}ms");
                    }
                    else
                    {
                        Dev.Log($"[RateLimit] 429 on {bucketKey}, retrying after {retryAfterMs}ms");
                    }

                    await Task.Delay(retryAfterMs);
                    continue; // retry same request
                }
                else
                {
                    var errorPayload = JsonConvert.DeserializeObject<JSON>(payload)!;
                    errorPayload.TryGetValue("message", out object? errorMessage);
                    var message = Convert.ToString(errorMessage) ?? "Unknown error";

                    Exception ex = (int)response.StatusCode switch
                    {
                        400 => new BadRequestException(message),
                        401 => new UnauthorizedException(message),
                        403 => new ForbiddenException(message),
                        404 => new NotFoundException(message),
                        405 => new MethodNotAllowedException(message),
                        502 => new GatewayUnavailableException(message),
                        _ => new HttpException($"Code {(int)response.StatusCode} - {message}")
                    };

                    item.tcs.TrySetException(ex);
                }
            }
            catch (Exception ex)
            {
                item.tcs.TrySetException(ex);
            }

            // Finished request → remove from queue
            lock (_bucketLock)
            {
                if (bucket.Queue.Count > 0)
                    bucket.Queue.Dequeue();
            }
        }
    }

    private void UpdateBucketFromHeaders(string bucketKey, RateLimitBucket bucket, HttpResponseHeaders headers)
    {
        if (headers.TryGetValues("X-RateLimit-Limit", out var limitVals))
            bucket.Limit = int.Parse(limitVals.First());

        if (headers.TryGetValues("X-RateLimit-Remaining", out var remVals))
            bucket.Remaining = int.Parse(remVals.First());

        if (headers.TryGetValues("X-RateLimit-Reset-After", out var resetVals))
        {
            var resetAfter = double.Parse(resetVals.First(), System.Globalization.CultureInfo.InvariantCulture);
            bucket.ResetAt = DateTimeOffset.UtcNow.AddSeconds(resetAfter);
        }

        _buckets[bucketKey] = bucket;
    }

    private static string GetBucketKey(string route)
    {
        // normalize route (replace major IDs with {id})
        return System.Text.RegularExpressions.Regex.Replace(route, @"\d{5,}", "{id}");
    }
}

internal enum ApiRoute
{
    [Description("https://discord.com/api/v10")]
    Base,

    [Description("https://cdn.discordapp.com")]
    Cdn
}
