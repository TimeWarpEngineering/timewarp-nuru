#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:property PublishDir=../../artifacts/

// ============================================================================
// Dynamic Completion Example - Demonstrates Shell Completion
// ============================================================================
// This runfile demonstrates shell tab completion that queries the app
// at Tab-press time for dynamic completions.
//
// Usage:
//   1. Run with --help:           dotnet run dynamic-completion-example.cs -- --help
//   2. Generate completion:       dotnet run dynamic-completion-example.cs -- --generate-completion bash
//   3. Test __complete callback:  dotnet run dynamic-completion-example.cs -- __complete 1 deploy
//
// Task #340: Shell completion architecture unification
// ============================================================================

using TimeWarp.Nuru;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("deploy {env} --version {tag}")
    .WithHandler((string env, string tag) =>
    {
      Console.WriteLine($"Deploying version {tag} to {env}");
    })
    .WithDescription("Deploy a version to an environment")
    .AsCommand()
    .Done()

  .Map("list-environments")
    .WithHandler(() =>
    {
      Console.WriteLine("Available Environments:");
      Console.WriteLine("  - production");
      Console.WriteLine("  - staging");
      Console.WriteLine("  - development");
      Console.WriteLine("  - qa");
      Console.WriteLine("  - demo");
    })
    .WithDescription("List all available environments")
    .AsQuery()
    .Done()

  .Map("list-tags")
    .WithHandler(() =>
    {
      Console.WriteLine("Available Tags:");
      Console.WriteLine("  - v2.1.0");
      Console.WriteLine("  - v2.0.5");
      Console.WriteLine("  - v2.0.4");
      Console.WriteLine("  - v1.9.12");
      Console.WriteLine("  - latest");
    })
    .WithDescription("List all available tags")
    .AsQuery()
    .Done()

  .Map("status")
    .WithHandler(() => Console.WriteLine("System Status: OK"))
    .WithDescription("Check system status")
    .AsQuery()
    .Done()

  .EnableCompletion(configure: registry =>
  {
    // Register custom completion sources for dynamic completions
    registry.RegisterForParameter("env", new EnvironmentCompletionSource());
    registry.RegisterForParameter("tag", new TagCompletionSource());
  })

  .Build();

return await app.RunAsync(args);

// ============================================================================
// Custom Completion Sources
// ============================================================================

/// <summary>
/// Provides dynamic completion for environment names.
/// In a real application, this might query a configuration service or API.
/// </summary>
public class EnvironmentCompletionSource : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    return GetEnvironments().Select(e => new CompletionCandidate(
      Value: e,
      Description: GetEnvironmentDescription(e),
      Type: CompletionType.Parameter
    ));
  }

  public static string[] GetEnvironments() =>
  [
    "production",
    "staging",
    "development",
    "qa",
    "demo"
  ];

  private static string GetEnvironmentDescription(string e) => e switch
  {
    "production" => "Production environment",
    "staging" => "Staging environment",
    "development" => "Development environment",
    "qa" => "QA environment",
    "demo" => "Demo environment",
    _ => e
  };
}

/// <summary>
/// Provides dynamic completion for version tags.
/// In a real application, this might query a Git repository or artifact registry.
/// </summary>
public class TagCompletionSource : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    return GetTags().Select(t => new CompletionCandidate(
      Value: t,
      Description: GetTagDescription(t),
      Type: CompletionType.Parameter
    ));
  }

  public static string[] GetTags() =>
  [
    "v2.1.0",
    "v2.0.5",
    "v2.0.4",
    "v1.9.12",
    "latest"
  ];

  private static string GetTagDescription(string t) => t switch
  {
    "latest" => "Latest stable release",
    "v2.1.0" => "Current release",
    "v2.0.5" => "Previous release",
    _ => $"Release {t}"
  };
}
