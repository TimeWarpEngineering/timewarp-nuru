// ═══════════════════════════════════════════════════════════════════════════════
// PARAMETER EXAMPLES
// ═══════════════════════════════════════════════════════════════════════════════
// Capture values from the command line using [Parameter].

namespace SyntaxExamples.Endpoints;

using TimeWarp.Nuru;

/// <summary>
/// Greet someone by name.
/// </summary>
[NuruRoute("greet", Description = "Greet someone by name")]
public sealed class GreetQuery : IQuery<Unit>
{
  [Parameter(Description = "Name of the person to greet")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler : IQueryHandler<GreetQuery, Unit>
  {
    public ValueTask<Unit> Handle(GreetQuery query, CancellationToken ct)
    {
      Console.WriteLine($"Hello {query.Name}");
      return default;
    }
  }
}

/// <summary>
/// Copy a file from source to destination.
/// </summary>
[NuruRoute("copy", Description = "Copy a file from source to destination")]
public sealed class CopyCommand : ICommand<Unit>
{
  [Parameter(Description = "Source file path")]
  public string Source { get; set; } = string.Empty;

  [Parameter(Description = "Destination file path")]
  public string Destination { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<CopyCommand, Unit>
  {
    public ValueTask<Unit> Handle(CopyCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Copying {command.Source} to {command.Destination}");
      return default;
    }
  }
}

/// <summary>
/// Move a file from source to destination.
/// </summary>
[NuruRoute("move", Description = "Move a file from source to destination")]
public sealed class MoveCommand : ICommand<Unit>
{
  [Parameter(Description = "Source file path")]
  public string Source { get; set; } = string.Empty;

  [Parameter(Description = "Destination file path")]
  public string Destination { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<MoveCommand, Unit>
  {
    public ValueTask<Unit> Handle(MoveCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Moving {command.Source} to {command.Destination}");
      return default;
    }
  }
}

/// <summary>
/// Delete a file at the specified path.
/// </summary>
[NuruRoute("delete", Description = "Delete a file")]
public sealed class DeleteCommand : ICommand<Unit>
{
  [Parameter(Description = "File path to delete")]
  public string Path { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<DeleteCommand, Unit>
  {
    public ValueTask<Unit> Handle(DeleteCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Deleting {command.Path}");
      return default;
    }
  }
}
