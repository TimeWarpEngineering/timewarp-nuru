// Probe: AddBehavior wraps every handler. The marker lines prove the behavior ran around it.
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .AddBehavior(typeof(MarkerBehavior))
  .Map("ping")
    .WithHandler(() => Console.WriteLine("pong"))
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args);

public sealed class MarkerBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    Console.WriteLine($"behavior-before {context.CommandName}");
    await proceed();
    Console.WriteLine($"behavior-after {context.CommandName}");
  }
}
