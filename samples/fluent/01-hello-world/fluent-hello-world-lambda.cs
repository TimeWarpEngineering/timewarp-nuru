#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// HELLO WORLD - LAMBDA HANDLER PATTERN (Fluent DSL)
// ═══════════════════════════════════════════════════════════════════════════════
// Simplest approach using inline lambda expressions.
// Best for: Quick scripts, simple commands, minimal boilerplate
// DSL: Fluent API (Map().WithHandler().AsQuery().Done())
// ═══════════════════════════════════════════════════════════════════════════════
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .Map("")
    .WithHandler(() => "Hello World")
    .AsQuery()
    .Done()
  .Build();

await app.RunAsync(args);
