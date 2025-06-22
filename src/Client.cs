using Discord.Net;
using Discord.Models;
using Discord.Net;

namespace Discord;

public class Bot
{
    public readonly string Token;

    public string? SessionId => _gateway._sessionId;

    public Gateway Events => _gateway;
    
    

    private readonly Gateway _gateway;

    public Bot(string token, Intents intents)
    {
        Token = token;
        _gateway = new Gateway(this, token, intents);
    }

    public async Task RunAsync()
    {
        await _gateway.ConnectAsync(Opcode.Identify);
    }

    public async Task DisconnectAsync(bool instant = true)
    {
        await _gateway.UserDisconnectAsync(instant);
    }
}