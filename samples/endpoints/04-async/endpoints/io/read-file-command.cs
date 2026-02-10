// ═══════════════════════════════════════════════════════════════════════════════
// READ-FILE COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Simulate async file read operation.

namespace AsyncExamples.Endpoints.IO;

using TimeWarp.Nuru;

[NuruRoute("read-file", Description = "Simulate async file read")]
public sealed class ReadFileCommand : ICommand<string>
{
  [Parameter(Description = "File path to read")]
  public string Path { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<ReadFileCommand, string>
  {
    public async ValueTask<string> Handle(ReadFileCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Reading file: {command.Path}");
      await Task.Delay(50, ct); // Simulate I/O
      return $"Contents of {command.Path}";
    }
  }
}
