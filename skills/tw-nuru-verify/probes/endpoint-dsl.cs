// Probe: the README Endpoint DSL. A [NuruRoute] class with a nested handler, registered by Map<T>().
using TimeWarp.Mediator;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .Map<AddCommand>()
  .Build();

return await app.RunAsync(args);

[NuruRoute("add", Description = "Add two numbers together")]
public sealed class AddCommand : ICommand<Unit>
{
  [Parameter(Order = 0)] public double X { get; set; }
  [Parameter(Order = 1)] public double Y { get; set; }

  public sealed class Handler : ICommandHandler<AddCommand, Unit>
  {
    public Task<Unit> Handle(AddCommand command, CancellationToken ct)
    {
      Console.WriteLine($"{command.X} + {command.Y} = {command.X + command.Y}");
      return Unit.Task;
    }
  }
}
