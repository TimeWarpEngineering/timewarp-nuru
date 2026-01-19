#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// FILTERED AUTHORIZATION BEHAVIOR - INuruBehavior<TFilter> PATTERN
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates the new filtered behavior pattern using INuruBehavior<TFilter>.
// The authorization behavior is automatically applied ONLY to routes that implement
// IRequireAuthorization, with zero runtime overhead for non-matching routes.
//
// KEY FEATURES:
//   - Compile-time behavior filtering (no runtime type checks)
//   - Strongly-typed context.Command (no casting needed)
//   - Works with both delegate routes (.Implements<T>()) and attributed routes
//
// RUN THIS SAMPLE:
//   ./04-pipeline-middleware-filtered-auth.cs echo "Hello"           # No auth needed
//   ./04-pipeline-middleware-filtered-auth.cs admin delete-all       # Access denied
//   CLI_AUTHORIZED=1 ./04-pipeline-middleware-filtered-auth.cs admin delete-all  # Success
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using Microsoft.Extensions.Logging;
using static System.Console;

NuruCoreApp app = NuruApp.CreateBuilder(args)
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
/// The setter is used in .Implements&lt;T&gt;() expressions which the generator extracts at compile-time.
/// The generated implementation uses a read-only property with the extracted value.
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
/// Uses INuruBehavior&lt;IRequireAuthorization&gt; for compile-time filtering.
/// </summary>
/// <remarks>
/// Because this implements INuruBehavior&lt;IRequireAuthorization&gt;:
/// - It is automatically SKIPPED for routes that don't implement IRequireAuthorization
/// - context.Command is strongly typed as IRequireAuthorization (no casting needed)
/// - Zero runtime overhead for non-matching routes
/// </remarks>
public sealed class AuthorizationBehavior(ILogger<AuthorizationBehavior> logger) : INuruBehavior<IRequireAuthorization>
{
  public async ValueTask HandleAsync(BehaviorContext<IRequireAuthorization> context, Func<ValueTask> proceed)
  {
    // No casting needed - context.Command is already IRequireAuthorization
    string permission = context.Command.RequiredPermission;
    logger.LogInformation("[AUTH] Checking permission: {Permission}", permission);

    // Simple demo: check environment variable for authorization
    // In production, this would integrate with your auth system
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
