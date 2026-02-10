using TimeWarp.Nuru;
using TimeWarp.Terminal;

[NuruRoute("demo", Description = "Demo command for testing output capture")]
public sealed class DemoCommand : ICommand<Unit>
{
  public sealed class Handler(ITerminal Terminal) : ICommandHandler<DemoCommand, Unit>
  {
    public ValueTask<Unit> Handle(DemoCommand command, CancellationToken ct)
    {
      // Demonstrate stdout
      Terminal.WriteLine("Hello from stdout!");
      Terminal.WriteLine("Line 1");
      Terminal.WriteLine("Line 2");
      Terminal.WriteLine("Line 3");

      // Demonstrate stderr
      Terminal.WriteErrorLine("Warning: This is a warning");

      // Demonstrate styled output
      Terminal.WriteLine("Success!".Green());

      return default;
    }
  }
}
