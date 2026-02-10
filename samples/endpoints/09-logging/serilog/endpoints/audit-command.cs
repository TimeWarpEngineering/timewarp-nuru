using Microsoft.Extensions.Logging;
using Serilog;
using TimeWarp.Nuru;

[NuruRoute("audit", Description = "Log audit event with full context")]
public sealed class AuditCommand : ICommand<Unit>
{
  [Parameter] public string Action { get; set; } = "";
  [Parameter] public string Resource { get; set; } = "";

  public sealed class Handler(ILogger<AuditCommand> Logger) : ICommandHandler<AuditCommand, Unit>
  {
    public ValueTask<Unit> Handle(AuditCommand c, CancellationToken ct)
    {
      Logger.LogInformation(
        "Audit: User performed {Action} on {Resource} at {Timestamp}",
        c.Action,
        c.Resource,
        DateTime.UtcNow
      );

      Logger.LogInformation(
        "{@AuditEvent}",
        new
        {
          EventType = "Audit",
          Action = c.Action,
          Resource = c.Resource,
          Timestamp = DateTime.UtcNow,
          User = Environment.UserName
        }
      );

      return default;
    }
  }
}
