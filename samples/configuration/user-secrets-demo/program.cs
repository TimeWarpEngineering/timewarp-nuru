// ═══════════════════════════════════════════════════════════════════════════════
// USER SECRETS DEMO - CSPROJ-BASED PROJECT WITH USER SECRETS
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates NuruApp.CreateBuilder(args) with user secrets
// configured via the .csproj file's UserSecretsId property.
//
// Note: This is a regular .csproj project, not a runfile, so Mediator packages
// are referenced in the .csproj file.
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
      string? environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

      Console.WriteLine("=== User Secrets Demo (csproj) ===");
      Console.WriteLine($"Environment: {environment}");
      Console.WriteLine($"ApiKey: {apiKey ?? "(not set)"}");
      Console.WriteLine($"Database:ConnectionString: {dbConnection ?? "(not set)"}");
      Console.WriteLine();

      if (environment == "Development")
      {
        if (string.IsNullOrEmpty(apiKey))
        {
          Console.WriteLine("⚠️  No secrets found. Run:");
          Console.WriteLine("   dotnet user-secrets set \"ApiKey\" \"secret-123\" --project samples/configuration/user-secrets-demo");
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
