#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-completion/timewarp-nuru-completion.csproj
#:property PublishDir=../../artifacts/

// ============================================================================
// Dynamic Completion Example - Demonstrates Task #029
// ============================================================================
// This runfile demonstrates dynamic tab completion that queries the app
// at Tab-press time instead of using static completion data.
//
// Usage:
//   1. Make executable:    chmod +x DynamicCompletionExample.cs
//   2. Publish AOT:        dotnet publish DynamicCompletionExample.cs -c Release -r linux-x64 -p:PublishAot=true
//   3. Run AOT:            ../../artifacts/DynamicCompletionExample
//   4. Generate completion: ../../artifacts/DynamicCompletionExample --generate-completion bash
//   5. Test completion:    source <(../../artifacts/DynamicCompletionExample --generate-completion bash)
//                          DynamicCompletionExample deploy <TAB>  # queries app for environment list
//
// Task #029: Dynamic completion calls back to the app via __complete route
// ============================================================================

using System.ComponentModel;
using TimeWarp.Nuru;

NuruCoreApp app = NuruCoreApp.CreateSlimBuilder(args)
  // ============================================================================
  // Sample Commands - Demonstrate Dynamic Completion
  // ============================================================================
  .Map("deploy {env} --version {tag}")
    .WithHandler((string env, string tag) =>
    {
      Console.WriteLine($"🚀 Deploying:");
      Console.WriteLine($"   Environment: {env}");
      Console.WriteLine($"   Version: {tag}");
    })
    .WithDescription("Deploy a version to an environment")
    .AsCommand()
    .Done()

  .Map("deploy {env} --mode {mode}")
    .WithHandler((string env, DeploymentMode mode) =>
    {
      Console.WriteLine($"🚀 Deploying to {env} in {mode} mode");
    })
    .WithDescription("Deploy with a specific mode")
    .AsCommand()
    .Done()

  .Map("list-environments")
    .WithHandler(() =>
    {
      Console.WriteLine("📋 Available Environments:");
      foreach (string env in EnvironmentCompletionSource.GetEnvironments())
      {
        Console.WriteLine($"   - {env}");
      }
    })
    .WithDescription("List all available environments")
    .AsQuery()
    .Done()

  .Map("list-tags")
    .WithHandler(() =>
    {
      Console.WriteLine("📋 Available Tags:");
      foreach (string tag in TagCompletionSource.GetTags())
      {
        Console.WriteLine($"   - {tag}");
      }
    })
    .WithDescription("List all available tags")
    .AsQuery()
    .Done()

  .Map("status")
    .WithHandler(() => Console.WriteLine("📊 System Status: OK"))
    .WithDescription("Check system status")
    .AsQuery()
    .Done()

  // ============================================================================
  // Enable Dynamic Completion with Custom Sources
  // ============================================================================
  // This adds both:
  // - The __complete {index:int} {*words} route for dynamic callbacks
  // - The --generate-completion {shell} route for shell script generation
  .EnableDynamicCompletion(configure: registry =>
  {
    // Register custom completion source for the "env" parameter
    registry.RegisterForParameter("env", new EnvironmentCompletionSource());

    // Register custom completion source for the "tag" parameter
    registry.RegisterForParameter("tag", new TagCompletionSource());

    // Register enum completion source for DeploymentMode type
    registry.RegisterForType(typeof(DeploymentMode), new EnumCompletionSource<DeploymentMode>());
  })

  // ============================================================================
  // Build and Run
  // ============================================================================
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
    // In a real app, this might query an API or configuration service
    return GetEnvironments().Select(env => new CompletionCandidate(
      Value: env,
      Description: GetEnvironmentDescription(env),
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

  private static string GetEnvironmentDescription(string env) => env switch
  {
    "production" => "Production environment (⚠️  use with caution)",
    "staging" => "Staging environment for final testing",
    "development" => "Development environment for active work",
    "qa" => "Quality assurance testing environment",
    "demo" => "Demo environment for client presentations",
    _ => env
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
    // In a real app, this might query Git tags or a Docker registry
    return GetTags().Select(tag => new CompletionCandidate(
      Value: tag,
      Description: GetTagDescription(tag),
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

  private static string GetTagDescription(string tag) => tag switch
  {
    "latest" => "Latest stable release",
    "v2.1.0" => "Current release (2025-11-14)",
    "v2.0.5" => "Previous stable release",
    _ => $"Release {tag}"
  };
}

/// <summary>
/// Example enum for deployment modes.
/// EnumCompletionSource will automatically provide all values.
/// </summary>
public enum DeploymentMode
{
  [Description("Fast deployment without health checks")]
  Fast,

  [Description("Standard deployment with rolling updates")]
  Standard,

  [Description("Blue-green deployment with zero downtime")]
  BlueGreen,

  [Description("Canary deployment with gradual rollout")]
  Canary
}
