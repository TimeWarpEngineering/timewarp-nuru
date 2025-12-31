#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator
#:property UserSecretsId=nuru-user-secrets-demo

// ═══════════════════════════════════════════════════════════════════════════════
// USER SECRETS PROPERTY - RUNFILE WITH USER SECRETS
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates NuruApp.CreateBuilder(args) with user secrets via
// the #:property UserSecretsId directive in a .NET 10 runfile.
//
// REQUIRED PACKAGES:
//   #:package Mediator.Abstractions    - Required by NuruApp.CreateBuilder
//   #:package Mediator.SourceGenerator - Generates AddMediator() in YOUR assembly
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Nuru;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(services => services.AddMediator())
  .Map("show")
    .WithHandler((IConfiguration config) =>
    {
      string? apiKey = config["ApiKey"];
      string? dbConnection = config["Database:ConnectionString"];

      Console.WriteLine("Configuration Values:");
      Console.WriteLine($"  ApiKey: {apiKey ?? "(not set)"}");
      Console.WriteLine($"  Database:ConnectionString: {dbConnection ?? "(not set)"}");
      Console.WriteLine();
      Console.WriteLine("Note: User secrets are only loaded in Development environment.");
      Console.WriteLine($"Current environment: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}");
    })
    .AsQuery()
    .Done()
  .Build();

await app.RunAsync(args);
