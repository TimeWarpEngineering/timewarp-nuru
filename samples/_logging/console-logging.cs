#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Microsoft.Extensions.Logging
#:package Microsoft.Extensions.Logging.Console

// ═══════════════════════════════════════════════════════════════════════════════
// CONSOLE LOGGING - MICROSOFT.EXTENSIONS.LOGGING WITH ATTRIBUTED ROUTES
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates NuruApp.CreateBuilder(args) with console logging
// and attributed routes. ILogger<T> is injected via DI into handlers.
//
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

NuruCoreApp app = NuruApp.CreateBuilder(args)
    .ConfigureServices(services => services.AddLogging(builder => builder.AddConsole()))
    .Build();

return await app.RunAsync(args);

/// <summary>Test query demonstrating all log levels.</summary>
[NuruRoute("test", Description = "Test all logging levels")]
public sealed class TestQuery : IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<TestQuery, Unit>
  {
    private readonly ILogger<Handler> Logger;

    public Handler(ILogger<Handler> logger)
    {
      Logger = logger;
    }

    public ValueTask<Unit> Handle(TestQuery query, CancellationToken ct)
    {
      Logger.LogTrace("This is a TRACE message (very detailed)");
      Logger.LogDebug("This is a DEBUG message (detailed)");
      Logger.LogInformation("This is an INFORMATION message - Test command executed!");
      Logger.LogWarning("This is a WARNING message");
      Logger.LogError("This is an ERROR message");
      Console.WriteLine("✓ Test query completed");
      return default;
    }
  }
}

/// <summary>Greet query with ILogger injection.</summary>
[NuruRoute("greet", Description = "Greet someone by name")]
public sealed class GreetQuery : IQuery<Unit>
{
  [Parameter(Description = "Name of the person to greet")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler : IQueryHandler<GreetQuery, Unit>
  {
    private readonly ILogger<Handler> Logger;

    public Handler(ILogger<Handler> logger)
    {
      Logger = logger;
    }

    public ValueTask<Unit> Handle(GreetQuery query, CancellationToken ct)
    {
      Logger.LogInformation("Greeting user: {Name}", query.Name);
      Console.WriteLine($"Hello, {query.Name}!");
      return default;
    }
  }
}
