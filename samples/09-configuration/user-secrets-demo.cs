#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:property UserSecretsId=nuru-csproj-user-secrets-demo

// ═══════════════════════════════════════════════════════════════════════════════
// USER SECRETS DEMO - RUNFILE WITH USER SECRETS
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates NuruApp.CreateBuilder() with user secrets
// configured via the #:property directive.
//
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.Configuration;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .Map("show")
    .WithHandler((IConfiguration config) =>
    {
      string? apiKey = config["ApiKey"];
      string? dbConnection = config["Database:ConnectionString"];
      string? environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

      Console.WriteLine("=== User Secrets Demo ===");
      Console.WriteLine($"Environment: {environment}");
      Console.WriteLine($"ApiKey: {apiKey ?? "(not set)"}");
      Console.WriteLine($"Database:ConnectionString: {dbConnection ?? "(not set)"}");
      Console.WriteLine();

      if (environment == "Development")
      {
        if (string.IsNullOrEmpty(apiKey))
        {
          Console.WriteLine("⚠️  No secrets found. Run:");
          Console.WriteLine("   dotnet user-secrets set \"ApiKey\" \"secret-123\" --id nuru-csproj-user-secrets-demo");
        }
        else
        {
          Console.WriteLine("✅ User secrets loaded successfully!");
        }
      }
      else
      {
        Console.WriteLine("ℹ️  User secrets only load in Development environment");
      }
    })
    .AsQuery()
    .Done()
  .Build();

await app.RunAsync(args);
