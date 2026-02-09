#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Microsoft.Extensions.DependencyInjection

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - RUNTIME DI ADVANCED ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// Advanced runtime DI scenarios: decorators, factories, keyed services.
//
// DSL: Endpoint with advanced DI patterns
//
// PATTERNS:
//   - Decorator pattern with Scrutor-like decoration
//   - Named/keyed service resolution
//   - Factory-based service creation
//   - Scoped lifetime management
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(services =>
  {
    // Base services
    services.AddSingleton<ILogger, ConsoleLogger>();

    // Repository with caching wrapper (manual decoration pattern)
    services.AddScoped<IRepository>(provider =>
      new CachedRepository(
        new DatabaseRepository(),
        provider.GetRequiredService<ILogger>()));

    // Both processor implementations
    services.AddSingleton<FastProcessor>();
    services.AddSingleton<ThoroughProcessor>();

    // Factory registration
    services.AddTransient<Func<string, IAnalyzer>>(provider =>
      name => new DataAnalyzer(name, provider.GetRequiredService<ILogger>()));

    // Scoped service with disposal tracking
    services.AddScoped<ISession, UserSession>();
  })
  .UseMicrosoftDependencyInjection()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);

// =============================================================================
// SERVICE DEFINITIONS
// =============================================================================

public interface ILogger
{
  void Log(string message);
}

public class ConsoleLogger : ILogger
{
  public void Log(string message) => WriteLine($"[LOG] {message}");
}

public interface IRepository
{
  Task<string> GetAsync(string key);
}

public class DatabaseRepository : IRepository
{
  public async Task<string> GetAsync(string key)
  {
    await Task.Delay(50); // Simulate DB access
    return $"DB value for {key}";
  }
}

// Decorator adds caching
public class CachedRepository(IRepository Inner, ILogger Logger) : IRepository
{
  private readonly Dictionary<string, string> Cache = new();

  public async Task<string> GetAsync(string key)
  {
    if (Cache.TryGetValue(key, out string? cached))
    {
      Logger.Log($"Cache hit for {key}");
      return cached;
    }

    Logger.Log($"Cache miss for {key}");
    string value = await Inner.GetAsync(key);
    Cache[key] = value;
    return value;
  }
}

public interface IProcessor
{
  Task<string> ProcessAsync(string input);
}

public class FastProcessor : IProcessor
{
  public Task<string> ProcessAsync(string input) =>
    Task.FromResult($"Fast result for {input}");
}

public class ThoroughProcessor : IProcessor
{
  public async Task<string> ProcessAsync(string input)
  {
    await Task.Delay(200); // Simulate thorough processing
    return $"Thorough analysis of {input}";
  }
}

public interface IAnalyzer
{
  string Analyze(string data);
}

public class DataAnalyzer(string Name, ILogger Logger) : IAnalyzer
{
  public string Analyze(string data)
  {
    Logger.Log($"{Name} analyzing: {data}");
    return $"Analysis by {Name}: {data.Length} chars";
  }
}

public interface ISession : IDisposable
{
  string SessionId { get; }
  DateTime CreatedAt { get; }
}

public class UserSession : ISession
{
  public string SessionId { get; } = Guid.NewGuid().ToString()[..8];
  public DateTime CreatedAt { get; } = DateTime.UtcNow;

  public void Dispose()
  {
    WriteLine($"[SESSION] Disposing session {SessionId}");
  }
}

// =============================================================================
// ENDPOINT DEFINITIONS
// =============================================================================

[NuruRoute("get", Description = "Get value (uses decorated repository with caching)")]
public sealed class GetCommand : ICommand<string>
{
  [Parameter] public string Key { get; set; } = "";

  public sealed class Handler(IRepository Repo) : ICommandHandler<GetCommand, string>
  {
    public async ValueTask<string> Handle(GetCommand c, CancellationToken ct)
    {
      WriteLine("=== First call (cache miss expected) ===");
      string result1 = await Repo.GetAsync(c.Key);

      WriteLine("\n=== Second call (cache hit expected) ===");
      string result2 = await Repo.GetAsync(c.Key);

      return result2;
    }
  }
}

[NuruRoute("process", Description = "Process with selected mode")]
public sealed class ProcessCommand : ICommand<string>
{
  [Parameter] public string Mode { get; set; } = "fast";
  [Parameter] public string Input { get; set; } = "";

  public sealed class Handler(FastProcessor Fast, ThoroughProcessor Thorough) : ICommandHandler<ProcessCommand, string>
  {
    public async ValueTask<string> Handle(ProcessCommand c, CancellationToken ct)
    {
      IProcessor processor = c.Mode.ToLower() switch
      {
        "thorough" => Thorough,
        _ => Fast
      };

      WriteLine($"Using {processor.GetType().Name}");
      return await processor.ProcessAsync(c.Input);
    }
  }
}

[NuruRoute("analyze", Description = "Analyze using factory-created analyzer")]
public sealed class AnalyzeCommand : ICommand<string>
{
  [Parameter] public string Data { get; set; } = "";

  public sealed class Handler(Func<string, IAnalyzer> AnalyzerFactory) : ICommandHandler<AnalyzeCommand, string>
  {
    public ValueTask<string> Handle(AnalyzeCommand c, CancellationToken ct)
    {
      // Create analyzer using factory
      IAnalyzer analyzer = AnalyzerFactory("Smart");
      string result = analyzer.Analyze(c.Data);
      return new ValueTask<string>(result);
    }
  }
}

[NuruRoute("session", Description = "Get current session info")]
public sealed class SessionQuery : IQuery<Unit>
{
  public sealed class Handler(ISession Session) : IQueryHandler<SessionQuery, Unit>
  {
    public ValueTask<Unit> Handle(SessionQuery q, CancellationToken ct)
    {
      WriteLine($"Session ID: {Session.SessionId}");
      WriteLine($"Created: {Session.CreatedAt:HH:mm:ss} UTC");
      return default;
    }
  }
}
