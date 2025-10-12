using Discord.Utility;
using Discord.Net;
using Newtonsoft.Json;
using System.ComponentModel;

namespace Discord.Models;

/// <summary>
/// Represents a Discord application (or "apps").
/// </summary>
public class Application : IEquatable<Application>
{
    // DOCS: https://discord.com/developers/docs/resources/application#application-object
    
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

    /// <summary>
    /// List of RPC origin URLs, if RPC is enabled.
    /// </summary>
    [JsonProperty("rpc_origins")]
    public IReadOnlyCollection<string>? RpcOrigins { get; init; }

    /// <summary>
    /// When <c>false</c>, only the app owner can add the app to guilds.
    /// </summary>
    [JsonProperty("bot_public")]
    public bool IsPublic { get; init; }

    /// <summary>
    /// When <c>true</c>, the app's bot will only join upon completion of the full OAuth2 code grant flow.
    /// </summary>
    [JsonProperty("bot_require_code_grant")]
    public bool RequiresCodeGrant { get; init; }

    /// <summary>
    /// User object for the bot user associated with the app.
    /// </summary>
    [JsonProperty("bot")]
    public User? User { get; init; }

    /// <summary>
    /// URL of the app's Terms of Service.
    /// </summary>
    [JsonProperty("terms_of_service_url")]
    public string? TermsOfServiceUrl { get; init; }

    /// <summary>
    /// URL of the app's Privacy Policy.
    /// </summary>
    [JsonProperty("privacy_policy_url")]
    public string? PrivacyPolicyUrl { get; init; }

    /// <summary>
    /// User object for the owner of the app.
    /// </summary>
    [JsonProperty("owner")]
    public User? Owner { get; init; }

    /// <summary>
    /// Hex encoded key for verification in interactions and the GameSDK's <a href="https://discord.com/developers/docs/game-sdk/applications#getticket">GetTicket</a>.
    /// </summary>
    [JsonProperty("verify_key")]
    public required string VerifyKey { get; init; }

    /// <summary>
    /// Team the application is a part of.
    /// </summary>
    [JsonProperty("team")]
    public ApplicationTeam? Team { get; init; }

    /// <summary>
    /// Guild ID associated with the app.
    /// </summary>
    [JsonProperty("guild_id")]
    public ulong? GuildId { get; init; }

    /// <summary>
    /// If this app is a game sold on Discord, this field will be the ID of the "Game SKU" that is created, if it exists.
    /// </summary>
    [JsonProperty("primary_sku_id")]
    public ulong? PrimarySkuId { get; init; }

    /// <summary>
    /// If this app is a game sold on Discord, this field will be the URL slug that links to the store page.
    /// </summary>
    [JsonProperty("slug")]
    public string? Slug { get; init; }

    /// <summary>
    /// The app cover image.
    /// </summary>
    public Media? CoverImage { get; }

    /// <summary>
    /// App's public flags.
    /// </summary>
    public IReadOnlyCollection<ApplicationFlags> Flags { get; }

    /// <summary>
    /// Approximate count of guilds the app has been added to.
    /// </summary>
    [JsonProperty("approximate_guild_count")]
    public int? ApproximateGuildCount { get; init; }

    /// <summary>
    /// Redirect URIs for the app.
    /// </summary>
    [JsonProperty("redirect_uris")]
    public IReadOnlyCollection<string>? RedirectUris { get; init; }

    /// <summary>
    /// Interactions endpoint URL for the app.
    /// </summary>
    [JsonProperty("interactions_endpoint_url")]
    public string? InteractionsEndpointUrl { get; init; }

    /// <summary>
    /// Role connection verification URL for the app.
    /// </summary>
    [JsonProperty("role_connections_verification_url")]
    public string? RoleConnectionsVerificationUrl { get; init; }

    /// <summary>
    /// List of tags describing the content and functionality of the app. Max of 5 tags.
    /// </summary>
    [JsonProperty("tags")]
    public IReadOnlyCollection<string>? Tags { get; init; }

    /// <summary>
    /// Settings for the app's default in-app authorization link, if enabled.
    /// </summary>
    public InstallParams? InstallParams { get; }

    /// <summary>
    /// In preview. Default scopes and permissions for each supported installation context.
    /// </summary>
    public IReadOnlyCollection<ApplicationIntegrationTypes> IntegrationTypes { get; }
    
    /// <summary>
    /// Default custom authorization URL for the app, if enabled.
    /// </summary>
    [JsonProperty("custom_install_url")]
    public string? CustomInstallUrl { get; init; }

    #region CUSTOM
    
    /// <summary>
    /// Your bot instance.
    /// </summary>
    public Bot? Bot { get; internal set; }

    #endregion

    [JsonConstructor]
    internal Application(ulong id, string? icon, string? cover_image, int? flags, JSON? install_params, JSON? integration_types_config)
    {
        Id = id;
        if (icon != null)
            Icon = new Media(icon, $"/app-icons/{id}/{icon}");
        if (cover_image != null)
            CoverImage = new Media(cover_image, $"/app-icons/{id}/{cover_image}");
        if (flags is { } value)
            Flags = Util.FromBitfield<ApplicationFlags>(value);
        else
            Flags = [];
        InstallParams = install_params != null ? new InstallParams(install_params) : null;
        var ait = new List<ApplicationIntegrationTypes>();
        if (integration_types_config != null)
        {
            List<string> keys = [.. integration_types_config.Keys];
            foreach (var key in keys)
                ait.Add((ApplicationIntegrationTypes)int.Parse(key));
        }
        IntegrationTypes = ait;
    }
    
    public override bool Equals(object? other) => other is Application application && Equals(application);
    public bool Equals(Application? other) => Id == other?.Id;
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// Edit properties of the app associated with the requesting bot user.
    /// </summary>
    /// <param name="edit">Application edit instance.</param>
    /// <returns>The updated application.</returns>
    public async Task<Application> EditAsync(ApplicationEdit edit) =>
        await Bot!._rest.EditCurrentApplicationAsync(edit);

    /// <summary>
    /// Requests the bot's application emojis.
    /// </summary>
    /// <returns>The application emojis.</returns>
    public async Task<IReadOnlyCollection<Emoji>> EmojisAsync() =>
        await Bot!._rest.ListApplicationEmojisAsync(Id);

    /// <summary>
    /// Requests an application emoji via its ID.
    /// </summary>
    /// <param name="id">ID of the emoji.</param>
    /// <returns>The requested emoji.</returns>
    public async Task<Emoji> RequestEmojiAsync(ulong id) =>
        await Bot!._rest.GetApplicationEmojiAsync(Id, id);
    
    /// <summary>
    /// Create an application emoji.
    /// </summary>
    /// <param name="name">Emoji name.</param>
    /// <param name="image">The 128x128 emoji image.</param>
    /// <returns>The newly created application emoji.</returns>
    public async Task<Emoji> CreateEmojiAsync(string name, DFile image) =>
        await Bot!._rest.CreateApplicationEmojiAsync(Id, name, image);
}

/// <summary>
/// Represents the values that can be edited for an application via <see cref="Application.EditAsync(ApplicationEdit)"/> 
/// </summary>
public struct ApplicationEdit
{
    internal JSON _payload = [];
    
    /// <summary>
    /// Initializes a new application edit instance.
    /// </summary>
    public ApplicationEdit() { }
    
    /// <param name="url">Default custom authorization URL for the app, if enabled.</param>
    /// <returns>The edit instance.</returns>
    public ApplicationEdit SetCustomInstallUrl(string url)
    {
        _payload["custom_install_url"] = url;
        return this;
    }
    
    /// <param name="description">Description of the app.</param>
    /// <returns>The edit instance.</returns>
    public ApplicationEdit SetDescription(string description)
    {
        _payload["description"] = description;
        return this;
    }
    
    /// <param name="url">Role connection verification URL for the app.</param>
    /// <returns>The edit instance.</returns>
    public ApplicationEdit SetRoleConnectionsVerificationsUrl(string url)
    {
        _payload["role_connections_verifications_url"] = url;
        return this;
    }
    
    /// <param name="installParams">Settings for the app's default in-app authorization link, if enabled.</param>
    /// <returns>The edit instance.</returns>
    public ApplicationEdit SetInstallParams(InstallParams installParams)
    {
        
        var payload = new JSON
        {
            { "scopes", installParams.Scopes.Select(x => x.GetDescription()) },
            { "permissions", installParams.Permissions.Value.ToString() }
        };
        _payload["install_params"] = payload;
        return this;
    }
    
    //public ApplicationEdit SetFlag(HashSet<ApplicationFlags> team)
    
    /// <param name="icon">The icon file or <c>null</c> to remove it.</param>
    /// <returns>The edit instance.</returns>
    public ApplicationEdit SetIcon(DFile? icon)
    {
        _payload["icon"] = icon?._mimeTypeBase64;
        return this;
    }
    
    /// <param name="coverImage">The cover image file or <c>null</c> to remove it.</param>
    /// <returns>The edit instance.</returns>
    public ApplicationEdit SetCoverImage(DFile? coverImage)
    {
        _payload["cover_image"] = coverImage?._mimeTypeBase64;
        return this;
    }
    
    /// <param name="url">Interactions endpoint URL for the app.</param>
    /// <returns>The edit instance.</returns>
    public ApplicationEdit SetInteractionsEndpointUrl(string url)
    {
        _payload["interactions_endpoint_url"] = url;
        return this;
    }
}

/// <summary>
/// Represents where an <see cref="Application"/> can be installed.
/// </summary>
public enum ApplicationIntegrationTypes
{
    // DOCS: https://discord.com/developers/docs/resources/application#application-object-application-integration-types
    
    /// <summary>
    /// App is installable to servers.
    /// </summary>
    GuildInstall,

    /// <summary>
    /// App is installable to users.
    /// </summary>
    UserInstall
}

/// <summary>
/// Represents the install-params for an <see cref="Application"/>.
/// </summary>
public class InstallParams
{
    // DOCS: https://discord.com/developers/docs/resources/application#install-params-object
    
    /// <summary>
    /// Scopes the application has.
    /// </summary>
    public IReadOnlyCollection<OAuth2Scopes> Scopes { get; }
    
    /// <summary>
    /// Applications permissions.
    /// </summary>
    public Permissions Permissions { get; }

    /// <summary>
    /// Initializes a 
    /// </summary>
    /// <param name="scopes"></param>
    /// <param name="permissions"></param>
    public InstallParams(HashSet<OAuth2Scopes> scopes, Permissions permissions)
    {
        Scopes = scopes;
        Permissions = permissions;
    }

    internal InstallParams(JSON data) 
    {
        var permissionsValue = Convert.ToString(data["permissions"])!;
        Permissions = new Permissions(ulong.Parse(permissionsValue!));

        var scopes = JsonConvert.SerializeObject(data["scopes"]);
        var rawScopes = JsonConvert.DeserializeObject<List<string>>(scopes) ?? [];
        var ipScopes = new List<OAuth2Scopes>();
        foreach (var rs in rawScopes)
            foreach (OAuth2Scopes scope in Enum.GetValues(typeof(OAuth2Scopes)))
            {
                if (!rs.Equals(scope.GetDescription())) continue;
                ipScopes.Add(scope);
                break;
            }
        Scopes = ipScopes;
    }
}

/// <summary>
/// Represents the flags for an <see cref="Application"/>.
/// </summary>
[Flags]
public enum ApplicationFlags
{
    // DOCS: https://discord.com/developers/docs/resources/application#application-object-application-flags
    
    /// <summary>
    /// Indicates if an app uses the Auto Moderation API.
    /// </summary>
    ApplicationAutoModerationRuleCreateBadge = 1 << 6,
    
    /// <summary>
    /// Intent required for bots in <b>100 or more servers</b> to receive <see cref="DiscordGatewayClient.OnPresenceUpdate"/> events.
    /// </summary>
    GatewayPresence                          = 1 << 12,
    
    /// <summary>
    /// Intent required for bots in under 100 servers to receive <see cref="DiscordGatewayClient.OnPresenceUpdate"/> events, found on
    /// the Bot page in your app's settings.
    /// </summary>
    GatewayPresenceLimited                   = 1 << 13,
    
    /// <summary>
    /// Intent required for bots in <b>100 or more servers</b> to receive member-related events like <see cref="DiscordGatewayClient.OnGuildMemberAdd"/>.
    /// </summary>
    GatewayGuildMembers                      = 1 << 14,
    
    /// <summary>
    /// Intent required for bots in under 100 servers to receive member-related events like <see cref="DiscordGatewayClient.OnGuildMemberAdd"/>
    /// found on the Bot page in your app's settings.
    /// </summary>
    GatewayGuildMembersLimited               = 1 << 15,
    
    /// <summary>
    /// Indicates unusual growth of an app that prevents verification.
    /// </summary>
    VerificationPendingGuildLimit            = 1 << 16,
    
    /// <summary>
    /// Indicates if an app is embedded within the Discord client.
    /// </summary>
    Embedded                                 = 1 << 17,
    
    /// <summary>
    /// Intent required for bots in <b>100 or more servers</b> to receive message content.
    /// </summary>
    GatewayMessageContent                    = 1 << 18,
    
    /// <summary>
    /// Intent required for bots in under 100 servers to receive message content, found on the Bot page in your app's settings.
    /// </summary>
    GatewayMessageContentLimited             = 1 << 19,
    
    /// <summary>
    /// Indicates if an app has registered global application commands.
    /// </summary>
    ApplicationCommandBadge                  = 1 << 23
}

/// <summary>
/// Represents an <see cref="Application"/> team.
/// </summary>
public class ApplicationTeam : IEquatable<ApplicationTeam>
{
    // DOCS: https://discord.com/developers/docs/topics/teams#data-models-team-object
    
    /// <summary>
    /// Unique ID of the team.
    /// </summary>
    public ulong Id { get; }
    
    /// <summary>
    /// The team's icon.
    /// </summary>
    public Media? Icon { get; }

    /// <summary>
    /// Members of the team.
    /// </summary>
    [JsonProperty("members")]
    public IReadOnlyCollection<ApplicationTeamMember> Members { get; init; }

    /// <summary>
    /// Name of the team.
    /// </summary>
    [JsonProperty("name")]
    public required string Name { get; init; }

    /// <summary>
    /// User ID of the current team owner.
    /// </summary>
    [JsonProperty("owner_user_id")]
    public ulong OwnerUserId { get; init; }

    [JsonConstructor]
    internal ApplicationTeam(string? icon, ulong id)
    {
        Id = id;
        if (icon != null)
            Icon = new Media(icon, $"/team-icons/{id}/{icon}");
    }
    
    public override bool Equals(object? other) => other is ApplicationTeam team && Equals(team);
    public bool Equals(ApplicationTeam? other) => Id == other?.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

/// <summary>
/// Represents an <see cref="Application"/> team member.
/// </summary>
public class ApplicationTeamMember
{
    // DOCS: https://discord.com/developers/docs/topics/teams#data-models-team-member-object
    
    /// <summary>
    /// User's membership state on the team.
    /// </summary>
    [JsonProperty("membership_state")]
    public MembershipState MembershipState { get; init; }

    /// <summary>
    /// ID of the parent team of which they are a member.
    /// </summary>
    [JsonProperty("team_id")]
    public ulong TeamId { get; init; }

    /// <summary>
    /// A partial object containing their: Avatar, discriminator, ID, and username of the user.
    /// </summary>
    [JsonProperty("user")]
    public required User User { get; init; }

    /// <summary>
    /// Role of the team member.
    /// </summary>
    public TeamMemberRole Role { get; }

    [JsonConstructor]
    internal ApplicationTeamMember(string role)
    {
        var rmr = TeamMemberRole.Developer;
        foreach (TeamMemberRole item in Enum.GetValues(typeof(TeamMemberRole)))
            if (role.Equals(item.GetDescription()))
            {
                rmr = item;
                break;
            }
        Role = rmr;
    }
}

/// <summary>
/// Represents the <see cref="ApplicationTeamMember"/> state.
/// </summary>
public enum MembershipState
{
    // DOCS: https://discord.com/developers/docs/topics/teams#data-models-membership-state-enum
    
    Invited = 1,
    Accepted
}

/// <summary>
/// Represents the <see cref="ApplicationTeamMember"/> role.
/// </summary>
public enum TeamMemberRole
{
    // DOCS: https://discord.com/developers/docs/topics/teams#team-member-roles
    
    [Description("admin")]
    Admin,
    
    [Description("developer")]
    Developer,
    
    [Description("read_only")]
    ReadOnly
}

/// <summary>
/// Represents the OAuth2 scopes that Discord supports. Some scopes require approval from Discord to use. Requesting them from a user
/// without approval from Discord may cause errors or undocumented behavior in the OAuth2 flow.
/// </summary>
public enum OAuth2Scopes
{
    // DOCS: https://discord.com/developers/docs/topics/oauth2#shared-resources-oauth2-scopes
    
    /// <summary>
    /// Allows your app to fetch data from a user's "Now Playing/Recently Played" list — not currently available for apps
    /// </summary>
    [Description("activities.read")]
    ActivitiesRead,

    /// <summary>
    /// Allows your app to update a user's activity - requires Discord approval (NOT REQUIRED FOR GAMESDK ACTIVITY MANAGER)
    /// </summary>
    [Description("activities.write")]
    ActivitiesWrite,

    /// <summary>
    /// Allows your app to read build data for a user's applications.
    /// </summary>
    [Description("activities.builds.read")]
    ApplicationsBuildsRead,

    /// <summary>
    /// Allows your app to upload/update builds for a user's applications - requires Discord approval.
    /// </summary>
    [Description("applications.builds.upload")]
    ApplicationsBuildsUpload,

    /// <summary>
    /// Allows your app to use commands in a guild.
    /// </summary>
    [Description("applications.commands")]
    ApplicationsCommands,

    /// <summary>
    /// Allows your app to update its commands using a Bearer token - client credentials grant only.
    /// </summary>
    [Description("applications.commands.update")]
    ApplicationsCommandsUpdate,

    /// <summary>
    /// Allows your app to update permissions for its commands in a guild a user has permissions to.
    /// </summary>
    [Description("applications.commands.permissions.update")]
    ApplicationsCommandsPermissionsUpdate,

    /// <summary>
    /// Allows your app to read entitlements for a user's applications.
    /// </summary>
    [Description("applications.entitlements")]
    ApplicationsEntitlements,

    /// <summary>
    /// Allows your app to read and update store data (SKUs, store listings, achievements, etc.) for a user's applications.
    /// </summary>
    [Description("applications.store.update")]
    ApplicationsStoreUpdate,

    /// <summary>
    /// For oauth2 bots, this puts the bot in the user's selected guild by default.
    /// </summary>
    [Description("bot")]
    Bot,

    /// <summary>
    /// Allows /users/@me/connections to return linked third-party accounts.
    /// </summary>
    [Description("connections")]
    Connections,

    /// <summary>
    /// Allows your app to see information about the user's DMs and group DMs - requires Discord approval.
    /// </summary>
    [Description("dm_channels.read")]
    DmChannelsRead,

    /// <summary>
    /// Enables /users/@me to return an email.
    /// </summary>
    [Description("email")]
    Email,

    /// <summary>
    /// Allows your app to join users to a group dm.
    /// </summary>
    [Description("gdm.join")]
    GdmJoin,

    /// <summary>
    /// Allows /users/@me/guilds to return basic information about all of a user's guilds.
    /// </summary>
    [Description("guilds")]
    Guilds,

    /// <summary>
    /// Allows /guilds/{guild.id}/members/{user.id} to be used for joining users to a guild.
    /// </summary>
    [Description("guilds.join")]
    GuildsJoin,

    /// <summary>
    /// Allows /users/@me/guilds/{guild.id}/member to return a user's member information in a guild.
    /// </summary>
    [Description("guilds.members.read")]
    GuildsMembersRead,

    /// <summary>
    /// Allows /users/@me without email.
    /// </summary>
    [Description("identify")]
    Identify,

    /// <summary>
    /// For local RPC server API access, this allows you to read messages from all client channels (otherwise restricted to channels/guilds your app creates).
    /// </summary>
    [Description("messages.read")]
    MessagesRead,

    /// <summary>
    /// Allows your app to know a user's friends and implicit relationships - requires Discord approval.
    /// </summary>
    [Description("relationships.read")]
    RelationshipRead,

    /// <summary>
    /// Allows your app to update a user's connection and metadata for the app.
    /// </summary>
    [Description("role_connections.write")]
    RoleConnectionsWrite,

    /// <summary>
    /// For local RPC server access, this allows you to control a user's local Discord client - requires Discord approval.
    /// </summary>
    [Description("rpc")]
    Rpc,

    /// <summary>
    /// For local RPC server access, this allows you to update a user's activity - requires Discord approval.
    /// </summary>
    [Description("rpc.activities.write")]
    RpcActivitiesWrite,

    /// <summary>
    /// For local RPC server access, this allows you to receive notifications pushed out to the user - requires Discord approval.
    /// </summary>
    [Description("rpc.notifications.read")]
    RpcNotificationsRead,

    /// <summary>
    /// For local RPC server access, this allows you to read a user's voice settings and listen for voice events - requires Discord approval.
    /// </summary>
    [Description("rpc.voice.read")]
    RpcVoiceRead,

    /// <summary>
    /// For local RPC server access, this allows you to update a user's voice settings - requires Discord approval.
    /// </summary>
    [Description("rpc.voice.write")]
    RpcVoiceWrite,

    /// <summary>
    /// Allows your app to connect to voice on user's behalf and see all the voice members - requires Discord approval.
    /// </summary>
    [Description("voice")]
    Voice,

    /// <summary>
    /// This generates a webhook that is returned in the oauth token response for authorization code grants.
    /// </summary>
    [Description("webhook.incoming")]
    WebhookIncoming
}
