#!/usr/bin/env dotnet run
// Temporary test file for V2 generator development
// Single route, single test - minimal case to get V2 working end-to-end
//
// Reference: .agent/workspace/2024-12-25T01-00-00_v2-generator-architecture.md
// Reference: sandbox/experiments/manual-runtime-construction.cs

using TestTerminal terminal = new();
NuruCoreApp app = NuruApp.CreateSlimBuilder()
  .UseTerminal(terminal)
  .Map("status").WithHandler(() => "healthy").AsQuery().Done()
  .Build();

int exitCode = await app.RunAsync(["status"]);

WriteLine($"Exit code: {exitCode}");
WriteLine($"Terminal output: {terminal.GetAllOutput()}");

exitCode.ShouldBe(0);
terminal.OutputContains("healthy").ShouldBeTrue();

WriteLine("✓ V2 single route test passed");
return 0;
