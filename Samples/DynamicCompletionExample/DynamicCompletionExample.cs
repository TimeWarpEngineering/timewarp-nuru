#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Completion/TimeWarp.Nuru.Completion.csproj

// ============================================================================
// Dynamic Completion Example - Demonstrates Task #029
// ============================================================================
// This runfile demonstrates dynamic tab completion that queries the app
// at Tab-press time instead of using static completion data.
//
// Usage:
//   1. Make executable:    chmod +x DynamicCompletionExample.cs
//   2. Publish AOT:        dotnet publish DynamicCompletionExample.cs -c Release -r linux-x64 -p:PublishAot=true
//   3. Run AOT:            ./bin/Release/net10.0/linux-x64/publish/DynamicCompletionExample
//   4. Generate completion: ./bin/Release/net10.0/linux-x64/publish/DynamicCompletionExample --generate-completion bash
//   5. Test completion:    source <(./bin/Release/net10.0/linux-x64/publish/DynamicCompletionExample --generate-completion bash)
//                          DynamicCompletionExample deploy <TAB>  # queries app for environment list
//
// Task #029: Dynamic completion calls back to the app via __complete route
// ============================================================================

using System.ComponentModel;
using TimeWarp.Nuru;
using TimeWarp.Nuru.Completion;

var builder = new NuruAppBuilder();

// ============================================================================
// Enable Dynamic Completion with Custom Sources
// ============================================================================
// This adds both:
// - The __complete {index:int} {*words} route for dynamic callbacks
// - The --generate-completion {shell} route for shell script generation

builder.EnableDynamicCompletion(configure: registry =>
{
  // Register custom completion source for the "env" parameter
  registry.RegisterForParameter("env", new EnvironmentCompletionSource());

  // Register custom completion source for the "tag" parameter
  registry.RegisterForParameter("tag", new TagCompletionSource());

  // Register enum completion source for DeploymentMode type
  registry.RegisterForType(typeof(DeploymentMode), new EnumCompletionSource<DeploymentMode>());
});

// ============================================================================
// Sample Commands - Demonstrate Dynamic Completion
// ============================================================================

builder.AddRoute
(
  "deploy {env} --version {tag}",
  (string env, string tag) =>
  {
    Console.WriteLine($"🚀 Deploying:");
    Console.WriteLine($"   Environment: {env}");
    Console.WriteLine($"   Version: {tag}");
  },
  "Deploy a version to an environment"
);

builder.AddRoute
(
  "deploy {env} --mode {mode}",
  (string env, DeploymentMode mode) =>
  {
    Console.WriteLine($"🚀 Deploying to {env} in {mode} mode");
  },
  "Deploy with a specific mode"
);

builder.AddRoute
(
  "list-environments",
  () =>
  {
    Console.WriteLine("📋 Available Environments:");
    foreach (string env in EnvironmentCompletionSource.GetEnvironments())
    {
      Console.WriteLine($"   - {env}");
    }
  },
  "List all available environments"
);

builder.AddRoute
(
  "list-tags",
  () =>
  {
    Console.WriteLine("📋 Available Tags:");
    foreach (string tag in TagCompletionSource.GetTags())
    {
      Console.WriteLine($"   - {tag}");
    }
  },
  "List all available tags"
);

builder.AddRoute
(
  "status",
  () => Console.WriteLine("📊 System Status: OK"),
  "Check system status"
);

// ============================================================================
// Build and Run
// ============================================================================

NuruApp app = builder.Build();
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
