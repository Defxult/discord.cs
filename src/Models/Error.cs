namespace Discord.Models;

/// <summary>
/// The base exception that all discord.cs errors are derived from.
/// </summary>
public class DiscordException(string message) : Exception(message) { }

#region HTTP

/// <summary>
/// Represents an error that occurred when attempting to interact with the Discord API (error code 5xx).
/// </summary>
public class HttpException(string message) : DiscordException(message) { }

/// <summary>
/// The request was improperly formatted or the server did not understand it (error code 400).
/// </summary>
public class BadRequestException(string message) : HttpException(message) { }

/// <summary>
/// The bot's authorization was not valid (error code 401).
/// </summary>
public class UnauthorizedException(string message) : HttpException(message) { }

/// <summary>
/// You don't have the proper permissions (error code 403).
/// </summary>
public class ForbiddenException(string message) : HttpException(message) { }

/// <summary>
/// The resource for the endpoint doesn't exist (error code 404).
/// </summary>
public class NotFoundException(string message) : HttpException(message) { }

/// <summary>
/// The HTTP method used is not valid for the endpoint (error code 405).
/// </summary>
public class MethodNotAllowedException(string message) : HttpException(message) { }

/// <summary>
/// There was not a gateway available to process the request. Wait a bit and retry (error code 502).
/// </summary>
public class GatewayUnavailableException(string message) : HttpException(message) { }

#endregion

#region Gateway

/// <summary>
/// Represents an error when attempting to interact with the Discord gateway (error code 4000).
/// </summary>
public class GatewayException(string message) : DiscordException(message) { }

/// <summary>
/// An invalid opcode was sent (error code 4001).
/// </summary>   
public class UnknownOpcodeException(string message) : GatewayException(message) { }

/// <summary>
/// An invalid payload was sent (error code 4002).
/// </summary>
public class DecodeErrorException(string message) : GatewayException(message) { }

/// <summary>
/// A payload was sent before identifying (error code 4003).
/// </summary>
public class NotAuthenticatedException(string message) : GatewayException(message) { }

/// <summary>
/// The token sent with the Identify payload was incorrect (error code 4004).
/// </summary>
public class AuthenticationFailedException(string message) : GatewayException(message) { }

/// <summary>
/// More than one identify payload was sent (error code 4005).
/// </summary>
public class AlreadyAuthenticatedException(string message) : GatewayException(message) { }

/// <summary>
/// The sequence sent when resuming the session was invalid (error code 4007).
/// </summary>
public class InvalidSequenceException(string message) : GatewayException(message) { }

/// <summary>
/// Too many payloads are being sent (error code 4008).
/// </summary>
public class RateLimitedException(string message) : GatewayException(message) { }

/// <summary>
/// The session has timed out (error code 4009).
/// </summary>
public class SessionTimedOutException(string message) : GatewayException(message) { }

/// <summary>
/// An invalid shard was sent when identifying (error code 4010).
/// </summary>
public class InvalidShardException(string message) : GatewayException(message) { }

/// <summary>
/// Sharding your connection is required to connect (error code 4011).
/// </summary>
public class ShardingRequiredException(string message) : GatewayException(message) { }

/// <summary>
/// An invalid version of the gateway was sent (error code 4012).
/// </summary>
public class InvalidApiVersionException(string message) : GatewayException(message) { }

/// <summary>
/// Invalid intents were sent (error code 4013).
/// </summary>
public class InvalidIntentsException(string message) : GatewayException(message) { }

/// <summary>
/// Disallowed intent was sent. Intent may have been specified that you have
/// not enabled or are not approved for. Verify your privileged intents are enabled in your developer portal (error code 4014).
/// </summary>
public class DisallowedIntentsException(string message) : GatewayException(message) { }

#endregion