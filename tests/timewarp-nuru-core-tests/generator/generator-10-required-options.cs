#!/usr/bin/dotnet --
// Test: Required options should only match when present
// Route 1 has REQUIRED --mode option (higher specificity)
// Route 2 has no options (lower specificity)
// Input "round 2.5" should match Route 2, NOT Route 1
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;
using static System.Console;

NuruCoreApp app =
  NuruApp.CreateBuilder(args)
  .Map("round {value:double} --mode {mode}")
    .WithHandler((double value, string mode) => WriteLine($"WITH MODE: {value} -> {mode}"))
    .Done()
  .Map("round {value:double}")
    .WithHandler((double value) => WriteLine($"NO MODE: {value}"))
    .Done()
  .Build();

return await app.RunAsync(args);

// Expected output:
// ./generator-10-required-options.cs round 2.5
// NO MODE: 2.5
//
// ./generator-10-required-options.cs round 2.5 --mode up
// WITH MODE: 2.5 -> up
