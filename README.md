# discord.cs

<p align="center">
    <img src="https://i.imgur.com/Ga58T1q.png" width="550" height="300">
</p>

<p align="center">An in development Discord API library written in C#.</p>

<p align="center">
    <img src="https://img.shields.io/static/v1?label=version&style=for-the-badge&message=0.0.3-alpha&color=c869ff">
    <!-- <a href="https://google.com"><img src="https://img.shields.io/static/v1?label=guide&style=for-the-badge&message=gitbook&color=5865f2"></a> -->
</p>

## Discord
Join the official [Discord server](https://discord.gg/6TNJHcGRYv) for discord.cs! Get support, updates/announcements, and contribute to development.

## NuGet
Discord.cs is on [NuGet](https://www.nuget.org/packages/Discord.cs/). In the early stages of development, any help testing the library is appreciated!

## Basic Usage
```csharp
// The following simply brings your bot online and responds to a message.

using Discord;
using Discord.Models;

// Set your bot token.
Environment.SetEnvironmentVariable("DISCORD_BOT_TOKEN", "<token>");
var bot = new Bot(Intents.Default);

// Listen for messages.
bot.Events.OnMessageCreate += async (_, message) =>
{
    // Don't respond to bot messages (ourselves) to avoid a loop.
    if (message.Author.IsBot)
        return;
    
    if (message.Content == "hi")
        await message.Channel.SendAsync("Hello! 👋")
};

await bot.ConnectAsync();    
await Task.Delay(-1);
```


