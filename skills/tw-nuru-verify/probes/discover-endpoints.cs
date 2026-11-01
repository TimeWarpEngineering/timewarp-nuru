// Probe: DiscoverEndpoints(). Two [NuruRoute] classes in the compilation, no Map<T>(); both
// routes must run, so a DiscoverEndpoints() the generator accepts but ignores fails here.
using TimeWarp.Mediator;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);

[NuruRoute("add", Description = "Add two numbers together")]
public sealed class AddCommand : ICommand<Unit>
{
  [Parameter(Order = 0)] public double X { get; set; }
  [Parameter(Order = 1)] public double Y { get; set; }

  public sealed class Handler : IRequestHandler<AddCommand, Unit>
  {
    public ValueTask<Unit> Handle(AddCommand command, CancellationToken ct)
    {
      Console.WriteLine($"discovered add: {command.X + command.Y}");
      return Unit.ValueTask;
    }
  }
}

[NuruRoute("subtract", Description = "Subtract two numbers")]
public sealed class SubtractCommand : ICommand<Unit>
{
  [Parameter(Order = 0)] public double X { get; set; }
  [Parameter(Order = 1)] public double Y { get; set; }

  public sealed class Handler : IRequestHandler<SubtractCommand, Unit>
  {
    public ValueTask<Unit> Handle(SubtractCommand command, CancellationToken ct)
    {
      Console.WriteLine($"discovered subtract: {command.X - command.Y}");
      return Unit.ValueTask;
    }
  }
}
