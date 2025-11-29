#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:property UserSecretsId=nuru-user-secrets-demo

using Microsoft.Extensions.Configuration;
using TimeWarp.Nuru;

NuruCoreApp app =
  new NuruAppBuilder()
  .AddDependencyInjection()
  .AddConfiguration(args)
  .Map("show", (IConfiguration config) =>
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
  .Build();

await app.RunAsync(args);
