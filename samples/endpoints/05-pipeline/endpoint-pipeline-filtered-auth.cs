#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - FILTERED AUTHORIZATION PIPELINE ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates filtered behaviors using INuruBehavior<TFilter>.
// Only commands implementing IRequireAuthorization are protected.
//
// DSL: Endpoint with filtered AuthorizationBehavior registered via .AddBehavior()
//
// PATTERN DEMONSTRATED:
//   - Marker interface (IRequireAuthorization) for opt-in behavior
//   - Type-safe filtered behavior with INuruBehavior<TFilter>
//   - Authorization checks before command execution
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .AddBehavior(typeof(AuthorizationBehavior))
  .DiscoverEndpoints()
  .Build();

await app.RunAsync(args);

// =============================================================================
// MARKER INTERFACE - Commands opt-in by implementing this
// =============================================================================

/// <summary>
/// Marker interface for commands that require authorization.
/// Commands implement this to opt-in to authorization checks.
/// </summary>
public interface IRequireAuthorization { }

// =============================================================================
// FILTERED AUTHORIZATION BEHAVIOR
// =============================================================================

/// <summary>
/// Authorization behavior that only applies to commands implementing
/// IRequireAuthorization. Uses INuruBehavior<TFilter> for type-safe filtering.
/// </summary>
public sealed class AuthorizationBehavior : INuruBehavior<IRequireAuthorization>
{
  public async ValueTask HandleAsync(BehaviorContext<IRequireAuthorization> context, Func<ValueTask> proceed)
  {
    // Simulate authorization check
    string? userRole = Environment.GetEnvironmentVariable("USER_ROLE");

    if (string.IsNullOrEmpty(userRole))
    {
      WriteLine("[AUTH] ⚠️  No USER_ROLE set, using default 'user' role");
      userRole = "user";
    }

    if (userRole == "admin")
    {
      WriteLine($"[AUTH] ✓ Admin access granted for {context.CommandName}");
      await proceed();
    }
    else
    {
      WriteLine($"[AUTH] ✗ Access denied for {context.CommandName}. Admin role required.");
      throw new UnauthorizedAccessException($"User with role '{userRole}' cannot execute {context.CommandName}");
    }
  }
}

// =============================================================================
// ENDPOINT DEFINITIONS
// =============================================================================

/// <summary>
/// Public endpoint - NO authorization required (does NOT implement IRequireAuthorization)
/// </summary>
[NuruRoute("status", Description = "Check system status (public, no auth required)")]
public sealed class StatusQuery : IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<StatusQuery, Unit>
  {
    public ValueTask<Unit> Handle(StatusQuery query, CancellationToken ct)
    {
      WriteLine("System status: ✓ OK");
      return default;
    }
  }
}

/// <summary>
/// Public endpoint - list items (no auth required)
/// </summary>
[NuruRoute("list", Description = "List items (public, no auth required)")]
public sealed class ListQuery : IQuery<string[]>
{
  public sealed class Handler : IQueryHandler<ListQuery, string[]>
  {
    public ValueTask<string[]> Handle(ListQuery query, CancellationToken ct)
    {
      return new ValueTask<string[]>(["Item 1", "Item 2", "Item 3"]);
    }
  }
}

/// <summary>
/// Protected endpoint - REQUIRES authorization (implements IRequireAuthorization)
/// </summary>
[NuruRoute("delete", Description = "Delete an item (admin only)")]
public sealed class DeleteCommand : ICommand<Unit>, IRequireAuthorization
{
  [Parameter(Description = "Item ID to delete")]
  public string Id { get; set; } = string.Empty;

  [Option("force", "f", Description = "Force deletion without confirmation")]
  public bool Force { get; set; }

  public sealed class Handler : ICommandHandler<DeleteCommand, Unit>
  {
    public ValueTask<Unit> Handle(DeleteCommand command, CancellationToken ct)
    {
      WriteLine($"Deleting item {command.Id} (force: {command.Force})");
      return default;
    }
  }
}

/// <summary>
/// Protected endpoint - REQUIRES authorization (implements IRequireAuthorization)
/// </summary>
[NuruRoute("config-set", Description = "Set configuration value (admin only)")]
public sealed class ConfigSetCommand : ICommand<Unit>, IRequireAuthorization
{
  [Parameter(Description = "Configuration key")]
  public string Key { get; set; } = string.Empty;

  [Parameter(Description = "Configuration value")]
  public string Value { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<ConfigSetCommand, Unit>
  {
    public ValueTask<Unit> Handle(ConfigSetCommand command, CancellationToken ct)
    {
      WriteLine($"Setting {command.Key} = {command.Value}");
      return default;
    }
  }
}

/// <summary>
/// Protected endpoint - REQUIRES authorization (implements IRequireAuthorization)
/// </summary>
[NuruRoute("user-create", Description = "Create a new user (admin only)")]
public sealed class UserCreateCommand : ICommand<Unit>, IRequireAuthorization
{
  [Parameter(Description = "Username")]
  public string Username { get; set; } = string.Empty;

  [Parameter(Description = "Email address")]
  public string Email { get; set; } = string.Empty;

  [Option("admin", "a", Description = "Grant admin privileges")]
  public bool IsAdmin { get; set; }

  public sealed class Handler : ICommandHandler<UserCreateCommand, Unit>
  {
    public ValueTask<Unit> Handle(UserCreateCommand command, CancellationToken ct)
    {
      WriteLine($"Creating user: {command.Username} ({command.Email})");
      WriteLine($"  Admin: {command.IsAdmin}");
      return default;
    }
  }
}
