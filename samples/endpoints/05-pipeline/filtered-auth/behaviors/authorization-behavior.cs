// ═══════════════════════════════════════════════════════════════════════════════
// AUTHORIZATION BEHAVIOR
// ═══════════════════════════════════════════════════════════════════════════════
// Filtered authorization - only applies to IRequireAuthorization commands.

namespace PipelineFilteredAuth.Behaviors;

using TimeWarp.Nuru;
using static System.Console;

public sealed class AuthorizationBehavior : INuruBehavior<IRequireAuthorization>
{
  public async ValueTask HandleAsync(BehaviorContext<IRequireAuthorization> context, Func<ValueTask> proceed)
  {
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
