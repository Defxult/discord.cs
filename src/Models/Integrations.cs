using Discord.Utility;
using Newtonsoft.Json;

namespace Discord.Models;

/// <summary>
/// Represents a <see cref="Guild"/> integration.
/// </summary>
public class Integration
{
    /// <summary>
    /// Integration ID.
    /// </summary>
    [JsonProperty("id")]
    public ulong Id { get; init; }
    
    /// <summary>
    /// Integration name.
    /// </summary>
    [JsonProperty("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Integration type.
    /// </summary>
    [JsonProperty("type")]
    public required string Type { get; init; }
    
    /// <summary>
    /// Is this integration enabled.
    /// </summary>
    [JsonProperty("enabled")]
    public bool IsEnabled { get; init; }
    
    /// <summary>
    /// Is this integration syncing. Will be <c>null</c> for Discord bot integrations.
    /// </summary>
    [JsonProperty("syncing")]
    public bool? IsSyncing { get; init; }
    
    /// <summary>
    /// ID that this integration uses for "subscribers". Will be <c>null</c> for Discord bot integrations.
    /// </summary>
    [JsonProperty("role_id")]
    public ulong? RoleId { get; init; }
    
    /// <summary>
    /// Whether emoticons should be synced for this integration (twitch only currently). Will be <c>null</c> for Discord
    /// bot integrations.
    /// </summary>
    [JsonProperty("enable_emoticons")]
    public bool? EmoticonsEnabled { get; init; }
    
    /// <summary>
    /// The behavior of expiring subscribers. Will be <c>null</c> for Discord bot integrations.
    /// </summary>
    [JsonProperty("expire_behavior")]
    public IntegrationExpireBehavior? ExpireBehavior { get; init; }
    
    /// <summary>
    /// The grace period (in days) before expiring subscribers. Will be <c>null</c> for Discord bot integrations.
    /// </summary>
    [JsonProperty("expire_grace_period")]
    public int? ExpireGracePeriod { get; init; }
    
    /// <summary>
    /// User for this integration.
    /// </summary>
    [JsonProperty("user")]
    public User? User { get; init; }
    
    /// <summary>
    /// Integration account information.
    /// </summary>
    [JsonProperty("account")]
    public required IntegrationAccount Account { get; init; }
    
    /// <summary>
    /// When this integration was last synced. Will be <c>null</c> for Discord bot integrations.
    /// </summary>
    [JsonProperty("synced_at")]
    public DateTime? SyncedAt { get; init; }
    
    /// <summary>
    /// How many subscribers this integration has. Will be <c>null</c> for Discord bot integrations.
    /// </summary>
    [JsonProperty("subscriber_count")]
    public int? SubscriberCount { get; init; }
    
    /// <summary>
    /// Hhas this integration been revoked. Will be <c>null</c> for Discord bot integrations.
    /// </summary>
    [JsonProperty("revoked")]
    public bool? IsRevoked { get; init; }
    
    /// <summary>
    /// The bot/OAuth2 application for Discord integrations.
    /// </summary>
    [JsonProperty("application")]
    public IntegrationApplication? Application { get; init; }

    /// <summary>
    /// The scopes the application has been authorized for.
    /// </summary>
    public IReadOnlyCollection<OAuth2Scopes> Scopes
    {
        get
        {
            var scopes = new List<OAuth2Scopes>();
            foreach (var rs in _scopes ?? [])
                foreach (OAuth2Scopes scope in Enum.GetValues(typeof(OAuth2Scopes)))
                {
                    if (!rs.Equals(scope.GetDescription())) continue;
                    scopes.Add(scope);
                    break;
                }
            return scopes;
        }
    }
    [JsonProperty("scopes")] private List<string>? _scopes;

    #region CUSTOM

    /// <summary>
    /// Your bot instance.
    /// </summary>
    public Bot Bot { get; internal set; }
    
    /// <summary>
    /// Guild ID this integration belongs to.
    /// </summary>
    public ulong GuildId { get; internal set; }

    #endregion
    
    private Integration() { }

    /// <summary>
    /// Delete the integration. This also deletes any associated webhooks and kicks the associated bot if there is one.
    /// </summary>
    /// <param name="reason">The reason for deleting the integration. This is displayed in the audit-log.</param>
    /// <remarks>Requires <see cref="Permission.ManageGuild"/>.</remarks>
    public async Task DeleteAsync(string? reason = null)
    {
        await Bot._rest.DeleteGuildIntegrationAsync(GuildId, Id, reason);
    }
}

/// <summary>
/// Represents an <see cref="Integration"/> application.
/// </summary>
public record IntegrationApplication
{
    /// <summary>
    /// ID of the app.
    /// </summary>
    public ulong Id { get; }
    
    /// <summary>
    /// Name of the app.
    /// </summary>
    [JsonProperty("name")]
    public required string Name { get; init; }
    
    /// <summary>
    /// The application icon.
    /// </summary>
    public Media? Icon { get; }
    
    /// <summary>
    /// Description of the app.
    /// </summary>
    [JsonProperty("description")]
    public required string Description { get; init; }

    [JsonConstructor]
    private IntegrationApplication(ulong id, string? icon)
    {
        Id = id;
        if (icon != null)
            Icon = new Media(icon, $"/app-icons/{id}/{icon}");
    }
}

/// <summary>
/// Represents an <see cref="Integration"/> account.
/// </summary>
public record IntegrationAccount
{
    /// <summary>
    /// ID of the account.
    /// </summary>
    [JsonProperty("id")]
    public required string Id { get; init; }
    
    /// <summary>
    /// Name of the account.
    /// </summary>
    [JsonProperty("name")]
    public required string Name { get; init; }
    
    private IntegrationAccount() { }
}

/// <summary>
/// Represents am <see cref="Integration"/> expire behaviour.
/// </summary>
public enum IntegrationExpireBehavior
{
    RemoveRole,
    Kick
}
