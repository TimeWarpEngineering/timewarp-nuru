#!/usr/bin/dotnet --

// This runfile tests NURU_D001: MediatorDependencyAnalyzer
// Expected: COMPILE SUCCESS - Should NOT report NURU_D001 error
//
// The non-generic Map() method does NOT require Mediator packages,
// so the analyzer should NOT report any error even without Mediator.Abstractions.

WriteLine("Testing NURU_D001: Non-generic Map() without Mediator packages");
WriteLine("Expected: This should compile successfully - no analyzer error");
WriteLine();

// Use NuruCoreApp.CreateSlimBuilder for delegate-based routing (no DI, no Mediator required)
var app = NuruCoreApp.CreateSlimBuilder(args)
  // This should NOT trigger NURU_D001 because:
  // 1. We're calling Map() (NON-generic form with delegate)
  // 2. Non-generic Map() does not require Mediator
  // 3. The analyzer only checks for Map<T>() generic calls
  .Map("ping", () => WriteLine("Pong!"))
  .Map("greet {name}", (string name) => WriteLine($"Hello, {name}!"))
  .Map("add {a:int} {b:int}", (int a, int b) => WriteLine($"Result: {a + b}"))
  .Build();

int result = await app.RunAsync(args);

WriteLine();
WriteLine("SUCCESS: Compiled and ran without NURU_D001 error");

return result;
