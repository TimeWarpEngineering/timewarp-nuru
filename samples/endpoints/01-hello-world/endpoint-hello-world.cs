#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// HELLO WORLD - ENDPOINT DSL ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
// Simplest endpoint approach using [NuruRoute] attribute.
// Best for: Testable commands, dependency injection, separation of concerns
// DSL: Endpoint (class-based with [NuruRoute], [Parameter], nested Handler)
// ═══════════════════════════════════════════════════════════════════════════════
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;

// DiscoverEndpoints() enables automatic discovery of [NuruRoute] classes
// No external packages needed - TimeWarp.Nuru provides ICommand<T>, IQuery<T>, handlers, and Unit
NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);

// =============================================================================
// ENDPOINT DEFINITIONS
// =============================================================================

/// <summary>
/// Simple greeting endpoint - demonstrates the Endpoint DSL pattern.
/// </summary>
[NuruRoute("", Description = "Say hello world")]
public sealed class HelloWorldQuery : IQuery<string>
{
  public sealed class Handler : IQueryHandler<HelloWorldQuery, string>
  {
    public ValueTask<string> Handle(HelloWorldQuery query, CancellationToken ct)
    {
      return new ValueTask<string>("Hello World");
    }
  }
}

/// <summary>
/// Personalized greeting endpoint with parameter.
/// </summary>
[NuruRoute("greet", Description = "Greet someone by name")]
public sealed class GreetQuery : IQuery<Unit>
{
  [Parameter(Description = "Name of the person to greet")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler : IQueryHandler<GreetQuery, Unit>
  {
    public ValueTask<Unit> Handle(GreetQuery query, CancellationToken ct)
    {
      Console.WriteLine($"Hello, {query.Name}!");
      return default;
    }
  }
}
