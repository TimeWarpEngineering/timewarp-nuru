#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// GANDA FULL EDITION
// ═══════════════════════════════════════════════════════════════════════════════
// Full CLI with all commands: kanban, git, and all groups.
// Build: dotnet run ganda.cs
// Usage: ./ganda kanban add "Task 1"
//        ./ganda git commit -m "message"
// ═══════════════════════════════════════════════════════════════════════════════
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;
using TimeWarp.Terminal;

// Full edition - no filtering, includes all endpoints
NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);

// ═══════════════════════════════════════════════════════════════════════════════
// SHARED COMMAND DEFINITIONS
// ═══════════════════════════════════════════════════════════════════════════════

[NuruRouteGroup("ganda")]
public abstract class GandaGroup;

[NuruRouteGroup("kanban")]
public abstract class KanbanGroup : GandaGroup;

[NuruRouteGroup("git")]
public abstract class GitGroup : GandaGroup;

[NuruRoute("add", Description = "Add a kanban task")]
public sealed class KanbanAddCommand : KanbanGroup, ICommand<Unit>
{
  [Parameter(Description = "Task name")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler(ITerminal terminal) : ICommandHandler<KanbanAddCommand, Unit>
  {
    public ValueTask<Unit> Handle(KanbanAddCommand command, CancellationToken ct)
    {
      terminal.WriteLine($"[KANBAN] Added task: {command.Name}");
      return default;
    }
  }
}

[NuruRoute("list", Description = "List kanban tasks")]
public sealed class KanbanListCommand : KanbanGroup, ICommand<Unit>
{
  public sealed class Handler(ITerminal terminal) : ICommandHandler<KanbanListCommand, Unit>
  {
    public ValueTask<Unit> Handle(KanbanListCommand command, CancellationToken ct)
    {
      terminal.WriteLine("[KANBAN] Tasks: (none yet)");
      return default;
    }
  }
}

[NuruRoute("commit", Description = "Commit changes")]
public sealed class GitCommitCommand : GitGroup, ICommand<Unit>
{
  [Option("message", "m", Description = "Commit message")]
  public string Message { get; set; } = string.Empty;

  public sealed class Handler(ITerminal terminal) : ICommandHandler<GitCommitCommand, Unit>
  {
    public ValueTask<Unit> Handle(GitCommitCommand command, CancellationToken ct)
    {
      terminal.WriteLine($"[GIT] Committed: {command.Message}");
      return default;
    }
  }
}

[NuruRoute("status", Description = "Show git status")]
public sealed class GitStatusCommand : GitGroup, ICommand<Unit>
{
  public sealed class Handler(ITerminal terminal) : ICommandHandler<GitStatusCommand, Unit>
  {
    public ValueTask<Unit> Handle(GitStatusCommand command, CancellationToken ct)
    {
      terminal.WriteLine("[GIT] Status: working tree clean");
      return default;
    }
  }
}
