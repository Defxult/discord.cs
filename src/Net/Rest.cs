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

    internal void SetEmojiValues(Emoji e, ulong? guildId)
    {
        e.GuildId = guildId;
        e.Bot = _bot;
    }

    // Returns a list of emoji objects for the given guild. Includes user fields if the bot has the CREATE_GUILD_EXPRESSIONS
    // or MANAGE_GUILD_EXPRESSIONS permission.
    // https://discord.com/developers/docs/resources/emoji#list-guild-emojis
    internal async Task<List<Emoji>> ListGuildEmojisAsync(ulong guildId)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/emojis"));
        var emojis = JsonConvert.DeserializeObject<List<Emoji>>(data)!;
        emojis.ForEach(e => { SetEmojiValues(e, guildId); });
        return emojis;
    }
    
    // Returns an emoji object for the given guild and emoji IDs. Includes the user field if the bot has the
    // MANAGE_GUILD_EXPRESSIONS permission, or if the bot created the emoji and has the CREATE_GUILD_EXPRESSIONS permission.
    // https://discord.com/developers/docs/resources/emoji#get-guild-emoji
    internal async Task<Emoji> GetGuildEmojiAsync(ulong guildId, ulong emojiId)
    {
        string data = await RequestAsync(Get, Route($"/guilds/{guildId}/emojis/{emojiId}"));
        var emoji = JsonConvert.DeserializeObject<Emoji>(data)!;
        SetEmojiValues(emoji, guildId);
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
        SetEmojiValues(emoji, guildId);
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
        SetEmojiValues(emoji, guildId);
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
        emojis.ForEach(e => { SetEmojiValues(e, null); });
        return emojis;
    }
    
    // Returns an emoji object for the given application and emoji IDs. Includes the user field.
    // https://discord.com/developers/docs/resources/emoji#get-application-emoji
    internal async Task<Emoji> GetApplicationEmojiAsync(ulong applicationId, ulong emojiId)
    {
        string data = await RequestAsync(Get, Route($"/applications/{applicationId}/emojis/{emojiId}"));
        var emoji = JsonConvert.DeserializeObject<Emoji>(data)!;
        SetEmojiValues(emoji, null);
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
        SetEmojiValues(emoji, null);
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
        SetEmojiValues(emoji, null);
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
        await RequestAsync(Delete, Route($"/guilds/{guildId}/stickers/{stickerId}"), reason);

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
        Console.WriteLine(obj);
        return (
            obj.url + "", 
            obj.shards, 
            obj.sessionStartLimit.total,
            obj.sessionStartLimit.remaining,
            obj.sessionStartLimit.resetAfter, 
            obj.sessionStartLimit.maxConcurrency
            );
    }

    private async Task<string> RequestAsync(HttpMethod method, string route, object? data  = null, string? auditReason = null)
    {
        using HttpRequestMessage request = new(method, route);
        if (data != null)
            request.Content = data is MultipartFormDataContent form ? form: ToStringContent(data);
        
        if  (auditReason != null)
            request.Headers.Add("X-Audit-Log-Reason", auditReason);
        
        using HttpResponseMessage response = await _http.SendAsync(request);
        
        // Convert the received data into something we can read.
        var payload = await response.Content.ReadAsStringAsync();
        
        // Verify the status code. If OK, return the response. Otherwise, throw the appropriate error.
        if (response.IsSuccessStatusCode)
            return payload;
        
        var errorPayload = JsonConvert.DeserializeObject<JSON>(payload)!;
        errorPayload.TryGetValue("message", out object? errorMessage);
        var message = Convert.ToString(errorMessage)!;

        throw (int)response.StatusCode switch
        {
            400 => new BadRequestException(message),
            401 => new UnauthorizedException(message),
            403 => new ForbiddenException(message),
            404 => new NotFoundException(message),
            405 => new MethodNotAllowedException(message),
            429 => new HttpException($"Error 429 TODO ({(int)response.StatusCode}) - {message}"),
            502 => new GatewayUnavailableException(message),
            _ => new HttpException($"Code {(int)response.StatusCode} - {message}")
        };
    }
}

internal enum ApiRoute
{
    [Description("https://discord.com/api/v10")]
    Base,

    [Description("https://cdn.discordapp.com")]
    Cdn
}
