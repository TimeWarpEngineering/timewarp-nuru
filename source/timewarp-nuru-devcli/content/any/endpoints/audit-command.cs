#region Purpose
// `dev audit`: runs this repo's fast pre-test checks (kanban task 600). `ganda repo
// audit` runs it when the repo's dev CLI has it and fails on a non-zero exit.
#endregion
#region Design
// The route lives here, in DevCli, so no repo defines `audit` itself: two endpoints
// cannot both own it. A repo adds checks through DevAuditChecks.AddRepoChecks
// (dev-audit-checks.cs). With none registered this prints that there is nothing to run
// and exits 0. DevAuditRunner does the work; any failed check sets exit code 1.
// Kanban task 601: --force and --verbose/-v reach every check through DevAuditContext,
// with the repo root (the current directory when no git root is found). The repo's
// optional pass cache (DevAuditChecks.ConfigurePassCache) skips a run whose inputs
// already passed; --force runs every check anyway.
// Never call `ganda repo audit` from here: it calls `dev audit`, so that would loop.
#endregion

namespace DevCli;

using TimeWarp.Mediator;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

/// <summary>
/// Run this repo's fast pre-test audit checks.
/// </summary>
[NuruRoute("audit", Description = "Run this repo's fast pre-test checks (called by ganda repo audit)")]
internal sealed class AuditCommand : ICommand<Unit>
{
  [Option("force", Description = "Run every check even when these inputs already passed")]
  public bool Force { get; set; }

  [Option("verbose", "v", Description = "Ask each check for more detail")]
  public bool Verbose { get; set; }

  internal sealed class Handler : IRequestHandler<AuditCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(AuditCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);

      DevAuditContext context = new(Git.FindRoot() ?? Environment.CurrentDirectory, command.Force, command.Verbose);
      DevAuditSummary summary = await DevAuditRunner
        .RunAsync(DevAuditChecks.All(), context, Terminal, DevAuditChecks.PassCache(), cancellationToken)
        .ConfigureAwait(false);

      if (summary.ExitCode != 0)
      {
        Environment.ExitCode = summary.ExitCode;
      }

      return Unit.Value;
    }
  }
}
