using System.Text.Json;
using Discord.Channels.Models;
using Discord.Models;
using Discord.Net;
using Discord.Utility;
using Newtonsoft.Json;
namespace Discord.Channels.Abstractions;

/// <summary>
/// Represents a basic channel.
/// </summary>
public interface IChannel
{
    /// <summary>
    /// Channel ID.
    /// </summary>
    public ulong Id { get; }
    
    /// <summary>
    /// Channel type.
    /// </summary>
    public ChannelType Type { get; }
    
    /// <summary>
    /// Your bot instance.
    /// </summary>
    public Bot Bot { get; }

    /// <summary>
    /// Delete the channel.
    /// </summary>
    /// <param name="reason">Reason for editing the channel. This is displayed in the audit-log.</param>
    public Task DeleteAsync(string? reason = null);
}

/// <summary>
/// Represents a channel that belongs to a <see cref="Guild"/>.
/// </summary>
public abstract class GuildChannel : IChannel
{
    /// <inheritdoc/>
    [JsonProperty("id")]
    public ulong Id { get; init; }
    
    /// <inheritdoc/>
    [JsonProperty("type")]
    public ChannelType Type { get; init; }

    /// <inheritdoc/>
    public Bot Bot { get; internal set; } = null!;
    
    /// <summary>
    /// Permission overwrites for the channel.
    /// </summary>
    public IReadOnlyCollection<PermissionOverwrites> Overwrites => PermissionOverwrites.Parse(_permissionOverwrites ?? []);
    [JsonProperty("permission_overwrites")] internal List<JSON>? _permissionOverwrites;

    /// <summary>
    /// Channel name.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; internal set; } = string.Empty;

    /// <summary>
    /// Guild the channel belongs to.
    /// </summary>
    public Guild Guild { get; internal set; } = null!;

    /// <summary>
    /// ID of the parent channel.
    /// </summary>
    [JsonProperty("parent_id")]
    public ulong? ParentId { get; internal set; }

    /// <summary>
    /// Channel topic. 0-4096 characters for <see cref="ForumChannel"/> and <see cref="MediaChannel"/>, 0-1024 characters
    /// for all others.
    /// </summary>
    [JsonProperty("topic")]
    public string? Topic { get; internal set; }
    
    /// <summary>
    /// Sorting position of the channel.
    /// </summary>
    [JsonProperty("position")]
    public int Position { get; internal set; }
    
    /// <summary>
    /// Whether the channel is NSFW (age restricted).
    /// </summary>
    [JsonProperty("nsfw")]
    public bool? IsNsfw { get; internal set; }
    
    /// <summary>
    /// ID of the last message that was sent in this channel.
    /// </summary>
    [JsonProperty("last_message_id")]
    public ulong? LastMessageId { get; internal set; }
    
    /// <summary>
    /// When the last message pinned message was pinned. Can be <c>null</c> if initially accessing from event
    /// <see cref="DiscordGatewayClient.OnGuildCreate"/>.
    /// </summary>
    [JsonProperty("last_pin_timestamp")]
    public DateTime? LastPinned { get; internal set; }
    
    /// <summary>
    /// Amount of seconds a user has to wait before sending another message (0-21600); bots, as well as users with
    /// <see cref="Permission.ManageMessages"/> or <see cref="Permission.ManageChannels"/> , are unaffected.
    /// </summary>
    [JsonProperty("rate_limit_per_user")]
    public int? SlowModeSeconds { get; internal set; }

    #region CUSTOM

    /// <summary>
    /// Mention the channel.
    /// </summary>
    public string Mention => Markdown.MentionChannel(Id);

    #endregion
    
    /// <inheritdoc/>
    public async Task DeleteAsync(string? reason = null) =>
        await Bot._rest.DeleteCloseChannelAsync(Id, reason);
    
    /// <summary>
    /// Edit the channel.
    /// </summary>
    /// <param name="edit">Channel edit instance.</param>
    /// <param name="reason">Reason for editing the channel. This is displayed in the audit-log.</param>
    /// <returns>The updated channel.</returns>
    public async Task<GuildChannel> EditAsync(GuildChannelEdit edit, string? reason = null)
    {
        var updated = await Bot._rest.ModifyChannelAsync(edit, this, reason);
        FillSelf(updated);
        return updated;
    }
    
    private void FillSelf(GuildChannel channel)
    {
        channel.Bot = Bot;
        channel.Guild = Guild;
    }

    internal static List<GuildChannel> ParseChannels(IEnumerable<JSON> channelObjs)
    {
        var channels = new List<GuildChannel>();
        var doc = JsonDocument.Parse(JsonConvert.SerializeObject(channelObjs));
        
        foreach (var channelElement in doc.RootElement.EnumerateArray())
        {
            var value = Convert.ToInt32(channelElement.GetProperty("type").ToString());
            switch ((ChannelType)value)
            {
                case ChannelType.GuildText:
                    var text = DiscordGatewayClient.Deserialize<TextChannel>(channelElement);
                    channels.Add(text);
                    break;
                case ChannelType.GuildVoice:
                    var voice = DiscordGatewayClient.Deserialize<VoiceChannel>(channelElement);
                    channels.Add(voice);
                    break;
                case ChannelType.GuildCategory:
                    var cat = DiscordGatewayClient.Deserialize<CategoryChannel>(channelElement);
                    channels.Add(cat);
                    break;
                case ChannelType.GuildAnnouncement:
                    var announcement = DiscordGatewayClient.Deserialize<AnnouncementChannel>(channelElement);
                    channels.Add(announcement);
                    break;
                case ChannelType.GuildStageVoice:
                    var stage = DiscordGatewayClient.Deserialize<StageChannel>(channelElement);
                    channels.Add(stage);
                    break;
                case ChannelType.GuildForum:
                    var forum = DiscordGatewayClient.Deserialize<ForumChannel>(channelElement);
                    channels.Add(forum);
                    break;
                case ChannelType.GuildMedia:
                    var media = DiscordGatewayClient.Deserialize<MediaChannel>(channelElement);
                    channels.Add(media);
                    break;
                default:
                    continue;
            }
        }
        return channels;
    }
    
    internal static List<ThreadChannel> ParseThreads(IEnumerable<JSON> threadObjs)
    {
        var threads = new List<ThreadChannel>();
        var docT = JsonDocument.Parse(JsonConvert.SerializeObject(threadObjs));
        foreach (var threadElement in docT.RootElement.EnumerateArray())
        {
            var thread = DiscordGatewayClient.Deserialize<ThreadChannel>(threadElement);
            threads.Add(thread);
        }
        return threads;
    }
}

/// <summary>
/// Represents a <see cref="GuildChannel"/> that can have its permissions edited.
/// </summary>
public interface IPermissionEditable
{
    /// <summary>
    /// Edit the channel permission overwrites.
    /// </summary>
    /// <param name="overwrites">Permissions to overwrite.</param>
    /// <param name="reason">Reason for editing the channel permission overwrites. This is displayed in the audit-log.</param>
    public Task EditPermissionsAsync(PermissionOverwrites overwrites, string? reason = null);

    /// <summary>
    /// Delete the channels permissions overwrites.
    /// </summary>
    /// <param name="id">ID of the <see cref="User"/> or <see cref="Role"/> of permissions to delete.</param>
    /// <param name="reason">Reason for deleting the channel permission overwrites. This is displayed in the audit-log.</param>
    public Task DeletePermissionsAsync(ulong id, string? reason = null);
}

/// <summary>
/// Represents a channel that's capable of having messages sent to it.
/// </summary>
public interface IMessageable : IChannel
{
    /// <summary>
    /// Request messages.
    /// </summary>
    /// <param name="history">Timestamp label for the messages to return.</param>
    /// <param name="dt">Date/time of the messages to look for.</param>
    /// <param name="limit">Maximum amount of messages to return (1-100).</param>
    /// <returns>The requested messages.</returns>
    /// <remarks>Unlike <see cref="Bot.Messages"/> this is an API call.</remarks>
    public Task<IReadOnlyCollection<Message>> RequestMessages(MessageHistory history = MessageHistory.Before,
        DateTime? dt = null, int limit = 50);
    
    /// <summary>
    /// Request a message.
    /// </summary>
    /// <param name="id">ID of the message.</param>
    /// <returns>The requested message.</returns>
    /// <remarks>Unlike <see cref="Bot.GetMessage"/> this is an API call.</remarks>
    public Task<Message> RequestMessage(ulong id);
    
    /// <summary>
    /// Send a message to the channel.
    /// </summary>
    /// <param name="content">Message contents (up to 2000 characters).</param>
    /// <param name="tts">Whether this is a TTS message.</param>
    /// <param name="embeds">Embeds (max 10), up to 6000 characters total.</param>
    /// <param name="allowedMentions">Allowed mentions for the message.</param>
    /// <param name="stickers">Up to 3 stickers to send in the message.</param>
    /// <param name="poll">A poll.</param>
    /// <param name="files">Files to upload with the message.</param>
    /// <returns>The message that was sent.</returns>
    /// <remarks>Polls and files cannot be in the same message.</remarks>
    public Task<Message> SendAsync(string? content = null, bool tts = false, IEnumerable<Embed>? embeds = null,
        AllowedMentions? allowedMentions = null, IEnumerable<GuildSticker>? stickers = null, Poll? poll = null,
        ICollection<DFile>? files = null);

    /// <summary>
    /// Trigger the typing indicator.
    /// </summary>
    /// <param name="func">If provided, the typing indicator will continuously be triggered until
    /// the function completes and a message is sent in the channel. If <c>null</c>, the typing indicator will only be
    /// triggered once. Each trigger lasts approximately 10 seconds.
    /// </param>
    /// <param name="ct">A cancellation token.</param>
    public Task TriggerTypingAsync(Func<Task>? func = null, CancellationToken ct = default);
}

public interface ICoreGuildChannel
{
    /// <summary>
    /// Create a webhook.
    /// </summary>
    /// <param name="name">Name of the webhook (1-80 characters). Cannot contain the substrings "clyde" or "discord"
    ///     (case-insensitive).</param>
    /// <param name="avatar">Webhook avatar.</param>
    /// <param name="reason">Reason for creating the webhook. This is displayed in the audit-log.</param>
    /// <returns>The created webhook.</returns>
    /// <remarks>Requires <see cref="Permission.ManageWebhooks"/>.</remarks>
    public Task<Webhook> CreateWebhookAsync(string name, DFile? avatar = null, string? reason = null);

    /// <summary>
    /// Requests all webhooks for the channel.
    /// </summary>
    /// <returns>All webhooks for the channel.</returns>
    /// <remarks>Requires <see cref="Permission.ManageWebhooks"/>.</remarks>
    public Task<IReadOnlyCollection<Webhook>> WebhooksAsync();
}

/// <summary>
/// Represents a <see cref="GuildChannel"/> where a <see cref="ThreadChannel"/> can be created.
/// </summary>
public interface IThreadable
{
    /// <summary>
    /// When threads will stop showing in the channel list after the specified period of inactivity.
    /// </summary>
    public ThreadArchiveDuration? DefaultAutoArchiveDuration { get; }

    // public Task<ICollection<ThreadChannel>> PublicThreadsAsync();
    //
    // public Task<ICollection<ThreadChannel>> PrivateThreadsAsync();
}

/// <summary>
/// Represents a <see cref="GuildChannel"/> that can have an <see cref="Invite"/> created for it.
/// </summary>
public interface IInvitable
{
    /// <summary>
    /// Invites for the channel.
    /// </summary>
    /// <returns>All invites for this channel.</returns>
    /// <remarks>Requires <see cref="Permission.ManageChannels"/>.</remarks>
    public Task<IReadOnlyCollection<Invite>> InvitesAsync();
    
    /// <summary>
    /// Create an invite.
    /// </summary>
    /// <param name="maxAge">Duration of invite in seconds before expiry, or <c>null</c> for never. Between 1 and 604800
    /// (7 days), defaults to 86400 (24 hours).</param>
    /// <param name="maxUses">Max number of uses or <c>null</c> for unlimited. Between 1 and 100.</param>
    /// <param name="temporary">Whether this invite only grants temporary membership.</param>
    /// <param name="unique">If <c>true</c>, don't try to reuse a similar invite (useful for creating many unique one
    /// time use invites).</param>
    /// <param name="targetType">The type of target for this voice channel invite.</param>
    /// <param name="targetUserId">ID of the user whose stream to display for this invite, required if <paramref name="targetType"/>,
    /// is <see cref="InviteTargetType.Stream"/>, the user must be streaming in the channel.</param>
    /// <param name="targetApplicationId">ID of the embedded application to open for this invite, required if <paramref name="targetType"/>,
    /// is <see cref="InviteTargetType.EmbeddedApplication"/>, the application must have the <see cref="ApplicationFlags.Embedded"/>.</param>
    /// <param name="reason">Reason for creating the invite. This is displayed in the audit-log.</param>
    /// <returns>The invite the was created.</returns>
    /// <remarks>Requires <see cref="Permission.CreateInstantInvite"/>.</remarks>
    public Task<Invite> CreateInviteAsync(int? maxAge = 86400, int? maxUses = null, bool temporary = false,
        bool unique = false, InviteTargetType? targetType = null, ulong? targetUserId = null,
        ulong? targetApplicationId = null, string? reason = null);
}

/// <summary>
/// Represents a <see cref="GuildChannel"/> that has voice capabilities.
/// </summary>
public interface IVoiceChannel
{
    /// <summary>
    /// The bitrate (in bits).
    /// </summary>
    public int Bitrate { get; }
    
    /// <summary>
    /// The user limit. If 0 it has no user limit.
    /// </summary>
    public int UserLimit { get; }
}

/// <summary>
/// Represents a channel type.
/// </summary>
public enum ChannelType
{
    // DOCS: https://discord.com/developers/docs/resources/channel#channel-object-channel-types
    
    /// <summary>
    /// A text channel within a guild.
    /// </summary>
    GuildText,
    
    /// <summary>
    /// A direct message between users.
    /// </summary>
    Dm,
    
    /// <summary>
    /// A voice channel within a guild.
    /// </summary>
    GuildVoice,
    
    /// <summary>
    /// An organizational category that contains up to 50 channels.
    /// </summary>
    GuildCategory = 4,
    
    /// <summary>
    /// A channel that users can follow and crosspost into their own guild.
    /// </summary>
    GuildAnnouncement,
    
    /// <summary>
    /// A temporary sub-channel within a <see cref="GuildAnnouncement"/> channel.
    /// </summary>
    AnnouncementThread = 10,
    
    /// <summary>
    /// A temporary sub-channel within a <see cref="GuildText"/> or <see cref="GuildForum"/> channel.
    /// </summary>
    PublicThread,

    /// <summary>
    /// A temporary sub-channel within a <see cref="GuildText"/> channel that is only viewable by those invited and those
    /// with <see cref="Permission.ManageThreads"/>.
    /// </summary>
    PrivateThread,
    
    /// <summary>
    /// A voice channel for hosting events with an audience.
    /// </summary>
    GuildStageVoice = 13,
    
    /// <summary>
    /// Channel that can only contain threads.
    /// </summary>
    GuildForum = 15,
    
    /// <summary>
    /// Similar to a <see cref="GuildForum"/>.
    /// </summary>
    GuildMedia
}

/// <summary>
/// Represents a channel's flags.
/// </summary>
[Flags]
public enum ChannelFlags
{
    // DOCS: https://discord.com/developers/docs/resources/channel#channel-object-channel-flags
    
    /// <summary>
    /// A thread is pinned to the top of its parent <see cref="ForumChannel"/> or <see cref="MediaChannel"/>.
    /// </summary>
    Pinned                   = 1 << 1,
    
    /// <summary>
    /// Whether a tag is required to be specified when creating a thread in a <see cref="ForumChannel"/> or
    /// <see cref="MediaChannel"/>. Tags are specified in <see cref="ForumChannel.AppliedTags"/>.
    /// </summary>
    RequireTag               = 1 << 4,
    
    /// <summary>
    /// When set, hides the embedded media download options (only for <see cref="MediaChannel"/>).
    /// </summary>
    HideMediaDownloadOptions = 1 << 15
}
