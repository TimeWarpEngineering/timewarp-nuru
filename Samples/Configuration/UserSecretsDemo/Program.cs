using Microsoft.Extensions.Configuration;
using TimeWarp.Nuru;

NuruApp app =
  new NuruAppBuilder()
  .AddDependencyInjection()
  .AddConfiguration(args)
  .AddRoute("show", (IConfiguration config) =>
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
        Console.WriteLine("   dotnet user-secrets set \"ApiKey\" \"secret-123\" --project Samples/Configuration/UserSecretsDemo");
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
  .Build();

await app.RunAsync(args);
