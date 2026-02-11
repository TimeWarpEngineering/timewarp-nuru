#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - BUILT-IN TYPE CONVERTERS
// ═══════════════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates all built-in type converters using Endpoint DSL:
// - int, long, double, decimal, bool, DateTime, Guid, TimeSpan
// - Uri, FileInfo, DirectoryInfo, IPAddress, DateOnly, TimeOnly
//
// DSL: Endpoint with typed parameters
//
// Type converters automatically convert string arguments to the target type.
// They validate input and provide helpful error messages.
// ═══════════════════════════════════════════════════════════════════════════════════════
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
