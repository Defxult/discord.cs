using System.Text.Json;

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

    #region MESSAGE

    // DOCS: https://discord.com/developers/docs/resources/message#create-message
    internal async Task<Message> CreateMessageAsync(ulong channelId, object payload)
    {
        // TODO
        // throw new NotImplementedException();
        string data = await RequestAsync(Post, Route($"/channels/{channelId}/messages"), payload);
        return JsonConvert.DeserializeObject<Message>(data)!;
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
        var emojis = DiscordGatewayClient.DeserializeWithNewtonsoft<List<Emoji>>(element);
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
    internal async Task GetGuildScheduledEventUsers(ulong guildId, ulong eventId)
    {
        // TODO
        throw new NotImplementedException();
    }

    #endregion

    #region GUILD

    private void SetGuildValues(Guild guild)
    {
        guild.Bot = _bot;
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
