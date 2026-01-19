#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// HELLO WORLD - METHOD REFERENCE PATTERN
// ═══════════════════════════════════════════════════════════════════════════════
// Uses .WithHandler(MethodName) with method references instead of lambdas.
// Best for: Clean separation of routing and logic, testable without full classes
// ═══════════════════════════════════════════════════════════════════════════════
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;
using TimeWarp.Terminal;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("")
    .WithHandler(Handlers.Greet)
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args);

internal static class Handlers
{
  internal static void Greet(ITerminal terminal)
    => terminal.WriteLine("Hello World");
}
