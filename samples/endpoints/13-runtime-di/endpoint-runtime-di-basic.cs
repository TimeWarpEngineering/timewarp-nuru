#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Microsoft.Extensions.DependencyInjection

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - RUNTIME DI BASICS ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// Runtime DI with full Microsoft DI container for complex dependency chains.
//
// DSL: Endpoint with constructor injection and runtime DI
//
// PATTERN:
//   - Use .UseMicrosoftDependencyInjection() for runtime DI
//   - Supports complex service graphs
//   - Opt-in when source-gen DI is insufficient
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(services =>
  {
    services.AddSingleton<IConfigService, ConfigService>();
    services.AddScoped<IDataService, DataService>();
    services.AddTransient<IProcessingService, ProcessingService>();
  })
  .UseMicrosoftDependencyInjection() // Enable runtime DI
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);

// =============================================================================
// SERVICE INTERFACES AND IMPLEMENTATIONS
// =============================================================================

public interface IConfigService
{
  string GetSetting(string key);
}

public class ConfigService : IConfigService
{
  private readonly Dictionary<string, string> Settings = new()
  {
    ["api.url"] = "https://api.example.com",
    ["timeout"] = "30",
    ["retries"] = "3"
  };

  public string GetSetting(string key) => Settings.TryGetValue(key, out string? value) ? value : "";
}

public interface IDataService
{
  Task<string[]> GetDataAsync(string query);
}

public class DataService(IConfigService Config) : IDataService
{
  public async Task<string[]> GetDataAsync(string query)
  {
    WriteLine($"DataService querying: {query}");
    WriteLine($"  Using API: {Config.GetSetting("api.url")}");
    await Task.Delay(100); // Simulate DB call
    return [$"Result 1 for {query}", $"Result 2 for {query}"];
  }
}

public interface IProcessingService
{
  Task<string> ProcessAsync(string input);
}

public class ProcessingService(IDataService Data, IConfigService Config) : IProcessingService
{
  public async Task<string> ProcessAsync(string input)
  {
    WriteLine($"ProcessingService processing: {input}");
    WriteLine($"  Timeout: {Config.GetSetting("timeout")}s");

    string[] data = await Data.GetDataAsync(input);
    return $"Processed: {string.Join(", ", data)}";
  }
}

// =============================================================================
// ENDPOINT DEFINITIONS
// =============================================================================

[NuruRoute("process", Description = "Process data through service chain")]
public sealed class ProcessCommand : ICommand<string>
{
  [Parameter] public string Input { get; set; } = "";

  public sealed class Handler(IProcessingService Processor) : ICommandHandler<ProcessCommand, string>
  {
    public async ValueTask<string> Handle(ProcessCommand c, CancellationToken ct)
    {
      WriteLine("=== Process Command ===");
      string result = await Processor.ProcessAsync(c.Input);
      WriteLine($"Result: {result}");
      return result;
    }
  }
}

[NuruRoute("query", Description = "Query data from service")]
public sealed class QueryCommand : IQuery<string[]>
{
  [Parameter] public string Search { get; set; } = "";

  public sealed class Handler(IDataService Data) : IQueryHandler<QueryCommand, string[]>
  {
    public async ValueTask<string[]> Handle(QueryCommand q, CancellationToken ct)
    {
      WriteLine("=== Query Command ===");
      return await Data.GetDataAsync(q.Search);
    }
  }
}

[NuruRoute("config", Description = "Read configuration value")]
public sealed class ConfigQuery : IQuery<string>
{
  [Parameter] public string Key { get; set; } = "";

  public sealed class Handler(IConfigService Config) : IQueryHandler<ConfigQuery, string>
  {
    public ValueTask<string> Handle(ConfigQuery q, CancellationToken ct)
    {
      string value = Config.GetSetting(q.Key);
      WriteLine($"{q.Key} = {value}");
      return new ValueTask<string>(value);
    }
  }
}
