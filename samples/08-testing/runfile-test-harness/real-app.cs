#!/usr/bin/dotnet --
// real-app - A sample CLI application to demonstrate testing patterns
// This represents a "real" CLI app that a consumer would build
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#pragma warning disable CS7022 // Entry point defined in Main method is intentional for demo

using TimeWarp.Nuru;
using TimeWarp.Terminal;

return await RealApp.Main(args);

public static class RealApp
{
  public static async Task<int> Main(string[] args)
  {
    NuruCoreApp app = NuruApp.CreateBuilder(args)
      .Map("greet {name}")
        .WithHandler(Greet)
        .WithDescription("Greet someone by name")
        .AsCommand()
        .Done()
      .Map("deploy {env} --dry-run")
        .WithHandler(DeployDryRun)
        .WithDescription("Simulate deployment")
        .AsQuery()
        .Done()
      .Map("deploy {env}")
        .WithHandler(Deploy)
        .WithDescription("Deploy to environment")
        .AsCommand()
        .Done()
      .Map("version")
        .WithHandler(Version)
        .WithDescription("Show version")
        .AsQuery()
        .Done()
      .Build();

    return await app.RunAsync(args);
  }

  internal static void Greet(string name, ITerminal terminal)
    => terminal.WriteLine($"Hello, {name}!");

  internal static void DeployDryRun(string env, ITerminal terminal)
    => terminal.WriteLine($"[DRY RUN] Would deploy to {env}");

  internal static void Deploy(string env, ITerminal terminal)
    => terminal.WriteLine($"Deploying to {env}...");

  internal static void Version(ITerminal terminal)
    => terminal.WriteLine("RealApp v1.0.0");
}
