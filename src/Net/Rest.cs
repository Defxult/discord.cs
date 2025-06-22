namespace Discord.Net;
using System.Net.Http.Headers;
using System.ComponentModel;
using Newtonsoft.Json;
using Discord;
using Discord.Models;
using Discord.Utility;


internal class Rest
{
    private readonly HttpClient _http;

    internal Rest(Bot bot)
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _http.DefaultRequestHeaders.Add("User-Agent", "discord.cs (https://github.com/Defxult/discord.cs)");
        _http.DefaultRequestHeaders.Add("Authorization", $"Bot {bot.Token}");
    }
    
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
        string data = await RequestAsync(HttpMethod.Get, Route("/gateway"));
        var obj = JsonConvert.DeserializeObject<JSON>(data);
        return obj["url"].ToString()!;
    }

    internal async Task<string> RequestAsync(HttpMethod method, string route)
    {
        using HttpRequestMessage request = new(method, route);
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
            429 => new HttpException("Error 429 - TODO"),
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
