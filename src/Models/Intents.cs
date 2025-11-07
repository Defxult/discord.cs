namespace Discord.Models;


/// <summary>
/// Represents Discords gateway intents.
/// </summary>
[Flags]
public enum Intents : long
{
    None = 0,
    Guilds = 1L << 0,
    GuildMembers = 1L << 1,
    GuildModeration = 1L << 2,
    GuildExpressions = 1L << 3,
    GuildIntegrations = 1L << 4,
    GuildWebhooks = 1L << 5,
    GuildInvites = 1L << 6,
    GuildVoiceStates = 1L << 7,
    GuildPresences = 1L << 8,
    GuildMessages = 1L << 9,
    GuildMessageReactions = 1L << 10,
    GuildMessageTyping = 1L << 11,
    DmMessages = 1L << 12,
    DmReactions = 1L << 13,
    DmTyping = 1L << 14,
    MessageContent = 1L << 15,
    GuildScheduledEvents = 1L << 16,
    AutoModerationConfiguration = 1L << 20,
    AutoModerationExecution = 1L << 21,
    GuildMessagePolls = 1L << 24,
    DirectMessagePolls = 1L << 25,

    /// <summary>
    /// Enables all intents <b>except</b> <see cref="GuildPresences"/>, <see cref="GuildMembers"/>, and <see cref="MessageContent"/>.
    /// </summary>
    Unprivileged = Guilds | GuildModeration | GuildExpressions | GuildIntegrations |
        GuildWebhooks | GuildInvites | GuildVoiceStates | GuildMessages |
        GuildMessageReactions | GuildMessageTyping | DmMessages | DmReactions |
        DmTyping | GuildScheduledEvents | AutoModerationConfiguration | AutoModerationExecution |
        GuildMessagePolls | DirectMessagePolls,

    /// <summary>
    /// Enables all intents <b>except</b> <see cref="Intents.GuildPresences"/>, <see cref="Intents.GuildMessageTyping"/>, and <see cref="Intents.DmTyping"/>  
    /// </summary>
    Default = Guilds | GuildMembers | GuildModeration | GuildExpressions |
        GuildIntegrations | GuildWebhooks | GuildInvites | GuildVoiceStates |
        GuildMessages | GuildMessageReactions | DmMessages | DmReactions |
        MessageContent | GuildScheduledEvents | AutoModerationConfiguration | AutoModerationExecution |
        GuildMessagePolls | DirectMessagePolls,

    /// <summary>
    /// Enables all intents.
    /// </summary>
    All = Unprivileged | GuildPresences | GuildMembers | MessageContent
}
