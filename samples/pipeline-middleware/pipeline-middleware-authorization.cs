#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-logging/timewarp-nuru-logging.csproj
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator

// ═══════════════════════════════════════════════════════════════════════════════
// AUTHORIZATION PIPELINE MIDDLEWARE - MARKER INTERFACE PATTERN
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates the marker interface pattern for selective behavior
// application. Only commands implementing IRequireAuthorization will have
// permission checks applied.
//
// MARKER INTERFACE PATTERN:
//   Behaviors are registered globally but use runtime checks to apply selectively:
//   - IRequireAuthorization: Commands requiring permission checks
//   The behavior checks: if (message is IRequireAuthorization auth) { ... }
//
// RUN THIS SAMPLE:
//   ./pipeline-middleware-authorization.cs echo "Hello"           # No auth needed
//   ./pipeline-middleware-authorization.cs admin delete-all       # Access denied
//   CLI_AUTHORIZED=1 ./pipeline-middleware-authorization.cs admin delete-all  # Success
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static System.Console;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(ConfigureServices)
  // Simple command - no authorization required
  .Map<EchoCommand>("echo {message}")
    .WithDescription("Echo a message back (no authorization required)")
  // Admin command that requires authorization (set CLI_AUTHORIZED=1 to access)
  .Map<AdminCommand>("admin {action}")
    .WithDescription("Admin operation requiring authorization (set CLI_AUTHORIZED=1)")
  .Build();

return await app.RunAsync(args);

static void ConfigureServices(IServiceCollection services)
{
  // Register Mediator with authorization behavior.
  // The behavior checks for IRequireAuthorization at runtime.
  services.AddMediator(options =>
  {
    options.PipelineBehaviors =
    [
      typeof(LoggingBehavior<,>),
      typeof(AuthorizationBehavior<,>)
    ];
  });
}

// =============================================================================
// COMMANDS
// =============================================================================

/// <summary>Simple echo command - no authorization required.</summary>
public sealed class EchoCommand : IRequest
{
  public string Message { get; set; } = string.Empty;

  public sealed class Handler : IRequestHandler<EchoCommand>
  {
    public ValueTask<Unit> Handle(EchoCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Echo: {request.Message}");
      return default;
    }
  }
}

/// <summary>
/// Admin command that requires authorization.
/// Demonstrates marker interface pattern - only commands implementing
/// IRequireAuthorization will have permission checks applied.
/// </summary>
public sealed class AdminCommand : IRequest, IRequireAuthorization
{
  public string Action { get; set; } = string.Empty;

  /// <summary>The permission required to execute this command.</summary>
  public string RequiredPermission => "admin:execute";

  public sealed class Handler : IRequestHandler<AdminCommand>
  {
    public ValueTask<Unit> Handle(AdminCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Executing admin action: {request.Action}");
      WriteLine("Admin operation completed successfully.");
      return default;
    }
  }
}

// =============================================================================
// MARKER INTERFACE
// =============================================================================

/// <summary>
/// Marker interface for commands that require authorization.
/// Only commands implementing this interface will have permission checks applied
/// by the AuthorizationBehavior.
/// </summary>
public interface IRequireAuthorization
{
  /// <summary>The permission required to execute this command.</summary>
  string RequiredPermission { get; }
}

// =============================================================================
// PIPELINE BEHAVIORS
// =============================================================================

/// <summary>
/// Simple logging behavior for observability.
/// </summary>
public sealed class LoggingBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
  where TMessage : IMessage
{
  private readonly ILogger<LoggingBehavior<TMessage, TResponse>> Logger;

  public LoggingBehavior(ILogger<LoggingBehavior<TMessage, TResponse>> logger)
  {
    Logger = logger;
  }

  public async ValueTask<TResponse> Handle
  (
    TMessage message,
    MessageHandlerDelegate<TMessage, TResponse> next,
    CancellationToken cancellationToken
  )
  {
    string requestName = typeof(TMessage).Name;
    Logger.LogInformation("[PIPELINE] Handling {RequestName}", requestName);
    TResponse response = await next(message, cancellationToken);
    Logger.LogInformation("[PIPELINE] Completed {RequestName}", requestName);
    return response;
  }
}

/// <summary>
/// Authorization behavior that checks permissions using a marker interface pattern.
/// This behavior only applies permission checks to commands that implement
/// IRequireAuthorization, demonstrating selective behavior application.
/// </summary>
/// <remarks>
/// For demonstration purposes, authorization is controlled via the CLI_AUTHORIZED
/// environment variable. In a real application, this would integrate with your
/// authentication/authorization system.
/// </remarks>
public sealed class AuthorizationBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
  where TMessage : IMessage
{
  private readonly ILogger<AuthorizationBehavior<TMessage, TResponse>> Logger;

  public AuthorizationBehavior(ILogger<AuthorizationBehavior<TMessage, TResponse>> logger)
  {
    Logger = logger;
  }

  public async ValueTask<TResponse> Handle
  (
    TMessage message,
    MessageHandlerDelegate<TMessage, TResponse> next,
    CancellationToken cancellationToken
  )
  {
    // Only check authorization for commands that require it
    if (message is IRequireAuthorization authRequest)
    {
      string permission = authRequest.RequiredPermission;
      Logger.LogInformation("[AUTH] Checking permission: {Permission}", permission);

      // Simple demo: check environment variable for authorization
      // In production, this would integrate with your auth system
      string? authorized = Environment.GetEnvironmentVariable("CLI_AUTHORIZED");
      if (string.IsNullOrEmpty(authorized) || authorized != "1")
      {
        Logger.LogWarning("[AUTH] Access denied - permission required: {Permission}", permission);
        throw new UnauthorizedAccessException
        (
          $"Access denied. Permission required: {permission}. Set CLI_AUTHORIZED=1 to authorize."
        );
      }

      Logger.LogInformation("[AUTH] Access granted for permission: {Permission}", permission);
    }

    return await next(message, cancellationToken);
  }
}
