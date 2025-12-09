#!/usr/bin/dotnet --
// real-app - A sample CLI application to demonstrate testing patterns
// This represents a "real" CLI app that a consumer would build
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;

return await RealApp.Main(args);

public static class RealApp
{
  public static async Task<int> Main(string[] args)
  {
    NuruCoreApp app = NuruCoreApp.CreateSlimBuilder()
      .AddAutoHelp()
      .Map("greet {name}", Greet, "Greet someone by name")
      .Map("deploy {env} --dry-run", DeployDryRun, "Simulate deployment")
      .Map("deploy {env}", Deploy, "Deploy to environment")
      .Map("version", Version, "Show version")
      .Build();

    return await app.RunAsync(args);
  }

  private static void Greet(string name, ITerminal terminal)
    => terminal.WriteLine($"Hello, {name}!");

  private static void DeployDryRun(string env, ITerminal terminal)
    => terminal.WriteLine($"[DRY RUN] Would deploy to {env}");

  private static void Deploy(string env, ITerminal terminal)
    => terminal.WriteLine($"Deploying to {env}...");

  private static void Version(ITerminal terminal)
    => terminal.WriteLine("RealApp v1.0.0");
}
