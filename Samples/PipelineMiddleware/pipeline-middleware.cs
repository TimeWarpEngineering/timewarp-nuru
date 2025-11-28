#!/usr/bin/dotnet --
// pipeline-middleware - Demonstrates Mediator pipeline behaviors for cross-cutting concerns
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Logging/TimeWarp.Nuru.Logging.csproj
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator

using System.Diagnostics;
using TimeWarp.Nuru;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static System.Console;

// Pipeline Middleware Sample
// ==========================
// This sample demonstrates martinothamar/Mediator pipeline behaviors (middleware)
// for implementing cross-cutting concerns like logging, performance monitoring,
// authorization, telemetry, validation, and more.
//
// Pipeline behaviors execute in registration order, wrapping the command handler
// like layers of an onion. Each behavior can execute code before and after the
// inner handler(s).
//
// Authorization Pattern:
// The AuthorizationBehavior demonstrates the marker interface pattern. Only commands
// that implement IRequireAuthorization will have permission checks applied.
// Set CLI_AUTHORIZED=1 environment variable to grant access.

NuruApp app = new NuruAppBuilder()
  .UseConsoleLogging(LogLevel.Information)
  .AddDependencyInjection()
  .ConfigureServices
  (
    (services, config) =>
    {
      // Register Mediator - source generator discovers handlers in THIS assembly
      services.AddMediator();

      // Register pipeline behaviors in execution order (outermost to innermost)
      // The order here determines the order behaviors wrap the handler
      //
      // Note: For AOT/runfile scenarios, use explicit generic registrations rather than
      // open generic registration (typeof(IPipelineBehavior<,>)) to avoid trimmer issues.
      services.AddSingleton<IPipelineBehavior<EchoCommand, Unit>, LoggingBehavior<EchoCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<EchoCommand, Unit>, PerformanceBehavior<EchoCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<SlowCommand, Unit>, LoggingBehavior<SlowCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<SlowCommand, Unit>, PerformanceBehavior<SlowCommand, Unit>>();

      // Authorization behavior only applies to commands implementing IRequireAuthorization
      services.AddSingleton<IPipelineBehavior<AdminCommand, Unit>, LoggingBehavior<AdminCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<AdminCommand, Unit>, AuthorizationBehavior<AdminCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<AdminCommand, Unit>, PerformanceBehavior<AdminCommand, Unit>>();
    }
  )
  // Simple command to demonstrate pipeline
  .Map<EchoCommand>
  (
    pattern: "echo {message}",
    description: "Echo a message back (demonstrates pipeline)"
  )
  // Slow command to trigger performance warning
  .Map<SlowCommand>
  (
    pattern: "slow {delay:int}",
    description: "Simulate slow operation (ms) to demonstrate performance behavior"
  )
  // Admin command that requires authorization (set CLI_AUTHORIZED=1 to access)
  .Map<AdminCommand>
  (
    pattern: "admin {action}",
    description: "Admin operation requiring authorization (set CLI_AUTHORIZED=1)"
  )
  .AddAutoHelp()
  .Build();

return await app.RunAsync(args);

// =============================================================================
// COMMANDS
// =============================================================================

/// <summary>Simple echo command to demonstrate pipeline execution.</summary>
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

/// <summary>Slow command that triggers the performance warning.</summary>
public sealed class SlowCommand : IRequest
{
  public int Delay { get; set; }

  public sealed class Handler : IRequestHandler<SlowCommand>
  {
    public async ValueTask<Unit> Handle(SlowCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Starting slow operation ({request.Delay}ms)...");
      await Task.Delay(request.Delay, cancellationToken);
      WriteLine("Slow operation completed.");
      return Unit.Value;
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
// PIPELINE BEHAVIORS
// =============================================================================

/// <summary>
/// Logging behavior that logs request entry and exit.
/// This is the outermost behavior, so it wraps everything else.
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

    try
    {
      TResponse response = await next(message, cancellationToken);
      Logger.LogInformation("[PIPELINE] Completed {RequestName}", requestName);
      return response;
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "[PIPELINE] Error handling {RequestName}", requestName);
      throw;
    }
  }
}

/// <summary>
/// Performance behavior that times command execution and warns on slow commands.
/// Demonstrates cross-cutting performance monitoring.
/// </summary>
public sealed class PerformanceBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
  where TMessage : IMessage
{
  private readonly ILogger<PerformanceBehavior<TMessage, TResponse>> Logger;
  private const int SlowThresholdMs = 500;

  public PerformanceBehavior(ILogger<PerformanceBehavior<TMessage, TResponse>> logger)
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
    Stopwatch stopwatch = Stopwatch.StartNew();

    TResponse response = await next(message, cancellationToken);

    stopwatch.Stop();

    string requestName = typeof(TMessage).Name;

    if (stopwatch.ElapsedMilliseconds > SlowThresholdMs)
    {
      Logger.LogWarning
      (
        "[PERFORMANCE] {RequestName} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
        requestName,
        stopwatch.ElapsedMilliseconds,
        SlowThresholdMs
      );
    }
    else
    {
      Logger.LogInformation
      (
        "[PERFORMANCE] {RequestName} completed in {ElapsedMs}ms",
        requestName,
        stopwatch.ElapsedMilliseconds
      );
    }

    return response;
  }
}

// =============================================================================
// MARKER INTERFACES
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
// AUTHORIZATION BEHAVIOR
// =============================================================================

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
