#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// FLUENT DSL - FILTERED AUTHORIZATION BEHAVIOR
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates the filtered behavior pattern using INuruBehavior<TFilter>
// with Fluent DSL. The authorization behavior is automatically applied ONLY to
// routes that implement IRequireAuthorization.
//
// DSL: Fluent API with .Implements<T>() marker interface
//
// KEY FEATURES:
//   - Compile-time behavior filtering (no runtime type checks)
//   - Strongly-typed context.Command (no casting needed)
//   - Works with Fluent DSL via .Implements<T>() method
//
// RUN THIS SAMPLE:
//   ./fluent-pipeline-filtered-auth.cs echo "Hello"           # No auth needed
//   ./fluent-pipeline-filtered-auth.cs admin delete-all     # Access denied
//   CLI_AUTHORIZED=1 ./fluent-pipeline-filtered-auth.cs admin delete-all  # Success
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using Microsoft.Extensions.Logging;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  // Register behaviors - filtered behavior only applies to matching routes
  .AddBehavior(typeof(LoggingBehavior))
  .AddBehavior(typeof(AuthorizationBehavior))
  // Simple command - no authorization required (AuthorizationBehavior is NOT applied)
  .Map("echo {message}")
    .WithDescription("Echo a message back (no authorization required)")
    .WithHandler((string message) => WriteLine($"Echo: {message}"))
    .Done()
  // Admin command with authorization via .Implements<T>()
  .Map("admin {action}")
    .Implements<IRequireAuthorization>(x => x.RequiredPermission = "admin:execute")
    .WithDescription("Admin operation requiring authorization (set CLI_AUTHORIZED=1)")
    .WithHandler((string action) =>
    {
      WriteLine($"Executing admin action: {action}");
      WriteLine("Admin operation completed successfully.");
    })
    .Done()
  .Build();

return await app.RunAsync(args);

// =============================================================================
// MARKER INTERFACE
// =============================================================================

/// <summary>
/// Contract interface for commands that require authorization.
/// Only routes implementing this interface will have AuthorizationBehavior applied.
/// </summary>
public interface IRequireAuthorization
{
  /// <summary>The permission required to execute this command.</summary>
  string RequiredPermission { get; set; }
}

// =============================================================================
// PIPELINE BEHAVIORS
// =============================================================================

/// <summary>
/// Global logging behavior - applies to ALL routes.
/// Uses INuruBehavior (no filter).
/// </summary>
public sealed class LoggingBehavior(ILogger<LoggingBehavior> logger) : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    logger.LogInformation("[PIPELINE] Handling {CommandName}", context.CommandName);
    await proceed();
    logger.LogInformation("[PIPELINE] Completed {CommandName}", context.CommandName);
  }
}

/// <summary>
/// Filtered authorization behavior - only applies to routes implementing IRequireAuthorization.
/// Uses INuruBehavior<IRequireAuthorization> for compile-time filtering.
/// </summary>
public sealed class AuthorizationBehavior(ILogger<AuthorizationBehavior> logger) : INuruBehavior<IRequireAuthorization>
{
  public async ValueTask HandleAsync(BehaviorContext<IRequireAuthorization> context, Func<ValueTask> proceed)
  {
    // No casting needed - context.Command is already IRequireAuthorization
    string permission = context.Command.RequiredPermission;
    logger.LogInformation("[AUTH] Checking permission: {Permission}", permission);

    // Simple demo: check environment variable for authorization
    string? authorized = Environment.GetEnvironmentVariable("CLI_AUTHORIZED");
    if (string.IsNullOrEmpty(authorized) || authorized != "1")
    {
      logger.LogWarning("[AUTH] Access denied - permission required: {Permission}", permission);
      throw new UnauthorizedAccessException(
        $"Access denied. Permission required: {permission}. Set CLI_AUTHORIZED=1 to authorize.");
    }

    logger.LogInformation("[AUTH] Access granted for permission: {Permission}", permission);
    await proceed();
  }
}
