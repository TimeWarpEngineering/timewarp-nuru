#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - CUSTOM TYPE CONVERTERS
// ═══════════════════════════════════════════════════════════════════════════════════════
// Demonstrates creating custom IRouteTypeConverter implementations.
// Shows EmailAddress, HexColor, and SemanticVersion converters.
//
// PATTERN:
//   1. Create class implementing IRouteTypeConverter (non-generic)
//   2. Implement TryConvert with validation
//   3. Register with .AddTypeConverter() in builder
// ═══════════════════════════════════════════════════════════════════════════════════════
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj

using System.Globalization;
using System.Text.RegularExpressions;
using TimeWarp.Nuru;
using TimeWarp.Terminal;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
