#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// HELLO WORLD - LAMBDA HANDLER PATTERN
// ═══════════════════════════════════════════════════════════════════════════════
// Simplest approach using inline lambda expressions.
// Best for: Quick scripts, simple commands, minimal boilerplate
// ═══════════════════════════════════════════════════════════════════════════════
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder(args)
  .Map("")
    .WithHandler(() => "Hello World")
    .AsQuery()
    .Done()
  .Build();

await app.RunAsync(args);
