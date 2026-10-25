// Probe: logging through ConfigureServices(s => s.AddLogging(...)) (nuru-app-builder.configuration.cs,
// NuruAppBuilder.ConfigureServices). The generator turns the AddLogging lambda into the app's logger
// factory; an endpoint handler's ILogger<T> writes to the console at the configured level, so the
// Information entry appears and the Debug entry does not. AddConsole comes with the TimeWarp.Nuru
// package (Microsoft.Extensions.Logging.Console). This is the only logging setup Nuru supports
// (kanban 527 removed UseLogging and the UseConsoleLogging family).
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeWarp.Mediator;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(services => services.AddLogging(logging => logging
    .AddConsole()
    .SetMinimumLevel(LogLevel.Information)))
  .Map<WorkCommand>()
  .Build();

return await app.RunAsync(args);

[NuruRoute("work", Description = "Log through an injected ILogger<T>")]
public sealed class WorkCommand : ICommand<Unit>
{
  public sealed class Handler(ILogger<WorkCommand> logger) : ICommandHandler<WorkCommand, Unit>
  {
    public Task<Unit> Handle(WorkCommand command, CancellationToken ct)
    {
      logger.LogDebug("probe-debug-entry");
      logger.LogInformation("probe-log-entry");
      Console.WriteLine("work done");
      return Unit.Task;
    }
  }
}
