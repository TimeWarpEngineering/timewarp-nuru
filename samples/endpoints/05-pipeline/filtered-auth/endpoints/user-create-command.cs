// ═══════════════════════════════════════════════════════════════════════════════
// USER CREATE COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Create a new user (admin only, requires IRequireAuthorization).

namespace PipelineFilteredAuth.Endpoints;

using PipelineFilteredAuth.Behaviors;
using TimeWarp.Nuru;

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
      Console.WriteLine($"Creating user: {command.Username} ({command.Email})");
      Console.WriteLine($"  Admin: {command.IsAdmin}");
      return default;
    }
  }
}
