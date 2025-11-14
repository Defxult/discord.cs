# discord.cs

<p align="center">
    <img src="https://i.imgur.com/Ga58T1q.png" width="550" height="300">
</p>

<p align="center">An in development Discord API library written in C#.</p>
<p align="center">Discord: https://discord.gg/6TNJHcGRYv</p>

<p align="center">
    <img src="https://img.shields.io/static/v1?label=version&style=for-the-badge&message=0.0.1-alpha&color=c869ff">
    <!-- <a href="https://google.com"><img src="https://img.shields.io/static/v1?label=guide&style=for-the-badge&message=gitbook&color=5865f2"></a> -->
</p>

## Basic Usage
```csharp
// The following simply brings your bot online and listens for messages.

using Discord;
using Discord.Models;

// Set your bot token.
Environment.SetEnvironmentVariable("DISCORD_BOT_TOKEN", "<token>");
var bot = new Bot(Intents.Default);

// Listen for messages.
bot.Events.OnMessageCreate += async (_, message) =>
{
    Console.WriteLine($"{message.Author.Name} said {message.Content}");
};

await bot.ConnectAsync();    
await Task.Delay(-1);
```
