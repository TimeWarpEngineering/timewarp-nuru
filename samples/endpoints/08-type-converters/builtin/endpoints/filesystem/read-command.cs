using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("read", Description = "Read a file")]
public sealed class ReadCommand : ICommand<Unit>
{
  [Parameter] public string File { get; set; } = ".";

  public sealed class Handler : ICommandHandler<ReadCommand, Unit>
  {
    public ValueTask<Unit> Handle(ReadCommand c, CancellationToken ct)
    {
      FileInfo file = new FileInfo(c.File);
      WriteLine($"Reading: {file.FullName}");
      WriteLine($"  Exists: {file.Exists}");
      WriteLine($"  Size: {file.Length} bytes");
      return default;
    }
  }
}
