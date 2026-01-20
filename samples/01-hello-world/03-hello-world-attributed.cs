#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// HELLO WORLD - ATTRIBUTED ROUTE PATTERN
// ═══════════════════════════════════════════════════════════════════════════════
// Uses [NuruRoute] attribute for auto-discovery. No Map() calls needed.
// Best for: Large applications, testable handlers, DI integration
// ═══════════════════════════════════════════════════════════════════════════════
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;
using TimeWarp.Terminal;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();
return await app.RunAsync(args);

[NuruRoute("", Description = "Greet the world")]
public sealed class HelloWorldQuery : IQuery<Unit>
{
  public sealed class Handler(ITerminal terminal) : IQueryHandler<HelloWorldQuery, Unit>
  {
    public ValueTask<Unit> Handle(HelloWorldQuery query, CancellationToken ct)
    {
      terminal.WriteLine("Hello World");
      return default;
    }
  }
}
