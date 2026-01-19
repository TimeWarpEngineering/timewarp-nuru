#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:property UserSecretsId=nuru-user-secrets-demo

// ═══════════════════════════════════════════════════════════════════════════════
// USER SECRETS PROPERTY - RUNFILE WITH USER SECRETS
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates NuruApp.CreateBuilder(args) with user secrets via
// the #:property UserSecretsId directive in a .NET 10 runfile.
//
// User secrets are automatically loaded when:
// - DOTNET_ENVIRONMENT or ASPNETCORE_ENVIRONMENT is "Development"
// - DEBUG build configuration is used
//
// To set user secrets:
//   cd samples/09-configuration
//   dotnet user-secrets set "ApiKey" "my-secret-api-key"
//   dotnet user-secrets set "Database:ConnectionString" "Server=...;Password=secret"
//
// Examples:
//   DOTNET_ENVIRONMENT=Development dotnet run samples/09-configuration/04-user-secrets-property.cs -- show
//   dotnet run samples/09-configuration/04-user-secrets-property.cs -- show
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.Configuration;
using TimeWarp.Nuru;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("show")
    .WithHandler(Handlers.ShowSecrets)
    .WithDescription("Show configuration values including user secrets")
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args);

// ═══════════════════════════════════════════════════════════════════════════════
// HANDLERS
// ═══════════════════════════════════════════════════════════════════════════════

internal static class Handlers
{
  internal static void ShowSecrets(IConfiguration config)
  {
    string? apiKey = config["ApiKey"];
    string? dbConnection = config["Database:ConnectionString"];

    Console.WriteLine("Configuration Values:");
    Console.WriteLine($"  ApiKey: {(string.IsNullOrEmpty(apiKey) ? "(not set)" : MaskSecret(apiKey))}");
    Console.WriteLine($"  Database:ConnectionString: {(string.IsNullOrEmpty(dbConnection) ? "(not set)" : MaskSecret(dbConnection))}");
    Console.WriteLine();
    Console.WriteLine("Note: User secrets are only loaded in Development environment (DEBUG build).");
    Console.WriteLine($"Current environment: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}");
  }

  private static string MaskSecret(string value)
  {
    if (value.Length <= 4)
      return new string('*', value.Length);
    return value[..2] + new string('*', Math.Min(value.Length - 4, 8)) + value[^2..];
  }
}
