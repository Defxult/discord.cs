namespace Discord.Models;

// public interface IChannel
// {
//     
// }

/// <summary>
/// Represents a channel type.
/// </summary>
public enum ChannelType
{
    // DOCS: https://discord.com/developers/docs/resources/channel#channel-object-channel-types
    
    GuildText,
    
    Dm,
    
    GuildVoice,
    
    GuildCategory = 4,
    
    GuildAnnouncement,
    
    AnnouncementThread = 10,
    
    PublicThread,

    PrivateThread,
    
    GuildStageVoice = 13,
    
    GuildForum = 15,
    
    GuildMedia
}
