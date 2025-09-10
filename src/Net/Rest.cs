using System.Text;

namespace Discord.Net;
using System.Net.Http.Headers;
using System.ComponentModel;
using Newtonsoft.Json;
using Discord;
using Discord.Models;
using Discord.Utility;


public class Rest
{
    private readonly HttpClient _http;
    
    private static HttpMethod Get => HttpMethod.Get;
    private static HttpMethod Post => HttpMethod.Post;
    private static HttpMethod Delete => HttpMethod.Delete;
    private static HttpMethod Patch => HttpMethod.Patch;
    private static HttpMethod Put => HttpMethod.Put;

    internal Rest(Bot bot)
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _http.DefaultRequestHeaders.Add("User-Agent", "discord.cs (https://github.com/Defxult/discord.cs)");
        _http.DefaultRequestHeaders.Add("Authorization", $"Bot {bot.Token}");
    }
    
    // Converts the JSON object (data) to its string representation.
    internal static StringContent ToStringContent(object data)
    {
        string converted = JsonConvert.SerializeObject(data);
        return new StringContent(converted, Encoding.UTF8, "application/json");
    }

    #region Message

    // https://discord.com/developers/docs/resources/message#create-message
    public async Task<Message> CreateMessageAsync(ulong channelId, object payload)
    {
        string data = await RequestAsync(Post, Route($"/channels/{channelId}/messages"), payload);
        return JsonConvert.DeserializeObject<Message>(data)!;
    }

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
    internal async Task<(string url, int shards, int sslTotal, int sslRemaining, int sslReset, int sslMax)> GetGatewayBotAsync()
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

    private async Task<string> RequestAsync(HttpMethod method, string route, object? data  = null)
    {
        using HttpRequestMessage request = new(method, route);
        if (data != null)
            request.Content = ToStringContent(data);
        
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
