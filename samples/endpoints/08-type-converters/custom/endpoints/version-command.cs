using TimeWarp.Nuru;
using TimeWarp.Terminal;
using static System.Console;

[NuruRoute("version", Description = "Check version compatibility")]
public sealed class VersionCommand : ICommand<Unit>
{
  [Parameter] public string Current { get; set; } = "1.0.0";
  [Parameter] public string Required { get; set; } = "1.0.0";

  public sealed class Handler : ICommandHandler<VersionCommand, Unit>
  {
    public ValueTask<Unit> Handle(VersionCommand c, CancellationToken ct)
    {
      SemanticVersion current = new SemanticVersion(c.Current);
      SemanticVersion required = new SemanticVersion(c.Required);
      
      WriteLine($"Current: {current}");
      WriteLine($"Required: {required}");

      int comparison = current.CompareTo(required);

      if (comparison >= 0)
      {
        WriteLine("✓ Version requirement satisfied".Green());
      }
      else
      {
        WriteLine("✗ Version too old - update required".Red());
      }

      return default;
    }
  }
}
