#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - CONFIGURATION VALIDATION
// ═══════════════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates fail-fast configuration validation using DataAnnotations
// and custom validation with Endpoint DSL.
//
// DSL: Endpoint with validated IOptions<T>
//
// Validators:
//   - DataAnnotations (Required, Range, StringLength, etc.)
//   - Custom validation attributes
//   - ValidateOnStart() for fail-fast behavior
// ═══════════════════════════════════════════════════════════════════════════════════════
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Microsoft.Extensions.Options
#:package Microsoft.Extensions.Options.ConfigurationExtensions
#:property EnableConfigurationBindingGenerator=true

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
