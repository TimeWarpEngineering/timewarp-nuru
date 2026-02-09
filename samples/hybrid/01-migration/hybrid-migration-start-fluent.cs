#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// HYBRID - MIGRATION: START WITH FLUENT DSL ⚠️ EDGE CASE
// ═══════════════════════════════════════════════════════════════════════════════
//
// Step 1 of migration: Pure Fluent DSL implementation.
// This is the starting point before introducing Endpoint patterns.
//
// DSL: Fluent (Map().WithHandler().Done())
//
// This sample demonstrates a calculator app using pure Fluent DSL.
// See hybrid-migration-add-endpoint.cs for the next step.
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  // All routes use Fluent DSL
  .Map("add {x:double} {y:double}")
    .WithHandler((double x, double y) => WriteLine($"{x} + {y} = {x + y}"))
    .AsQuery()
    .Done()
  .Map("subtract {x:double} {y:double}")
    .WithHandler((double x, double y) => WriteLine($"{x} - {y} = {x - y}"))
    .AsQuery()
    .Done()
  .Map("multiply {x:double} {y:double}")
    .WithHandler((double x, double y) => WriteLine($"{x} × {y} = {x * y}"))
    .AsQuery()
    .Done()
  .Map("divide {x:double} {y:double}")
    .WithHandler((double x, double y) =>
    {
      if (y == 0) { WriteLine("Error: Division by zero"); return; }
      WriteLine($"{x} ÷ {y} = {x / y}");
    })
    .AsQuery()
    .Done()
  .Build();

WriteLine("=== Hybrid Migration Demo: Step 1 - Fluent DSL Only ===\n");
WriteLine("This app uses only Fluent DSL patterns.");
WriteLine("Next step: hybrid-migration-add-endpoint.cs adds Endpoint patterns.\n");

return await app.RunAsync(args);
