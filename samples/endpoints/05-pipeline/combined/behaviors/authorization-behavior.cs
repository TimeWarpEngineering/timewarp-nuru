// ═══════════════════════════════════════════════════════════════════════════════
// AUTHORIZATION BEHAVIOR
// ═══════════════════════════════════════════════════════════════════════════════
// Filtered authorization - only applies to IRequireAuthorization commands.

namespace PipelineCombined.Behaviors;

using TimeWarp.Nuru;
using static System.Console;

public sealed class AuthorizationBehavior : INuruBehavior<IRequireAuthorization>
{
  public async ValueTask HandleAsync(BehaviorContext<IRequireAuthorization> context, Func<ValueTask> proceed)
  {
    string? role = Environment.GetEnvironmentVariable("USER_ROLE") ?? "user";
    if (role != "admin")
      throw new UnauthorizedAccessException($"Role '{role}' cannot execute {context.CommandName}");
    WriteLine($"[AUTH] ✓ Admin access granted");
    await proceed();
  }
}
