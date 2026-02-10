using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("list", Description = "List directory contents")]
public sealed class ListCommand : ICommand<Unit>
{
  [Parameter] public string Dir { get; set; } = ".";

  public sealed class Handler : ICommandHandler<ListCommand, Unit>
  {
    public ValueTask<Unit> Handle(ListCommand c, CancellationToken ct)
    {
      DirectoryInfo dir = new DirectoryInfo(c.Dir);
      WriteLine($"Listing: {dir.FullName}");
      WriteLine($"  Exists: {dir.Exists}");

      if (dir.Exists)
      {
        WriteLine($"  Files: {dir.GetFiles().Length}");
        WriteLine($"  Subdirectories: {dir.GetDirectories().Length}");
      }

      return default;
    }
  }
}
