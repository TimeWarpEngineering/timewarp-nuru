#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Completion/TimeWarp.Nuru.Completion.csproj

// ============================================================================
// Shell Completion Example - Demonstrates Issue #30
// ============================================================================
// This runfile demonstrates tab completion for CLI commands.
//
// Usage:
//   1. Make executable:    chmod +x ShellCompletionExample.cs
//   2. Run directly:       ./ShellCompletionExample.cs
//   3. Generate completion: ./ShellCompletionExample.cs --generate-completion bash
//   4. Test completion:    source <(./ShellCompletionExample.cs --generate-completion bash)
//                          ./ShellCompletionExample.cs cre<TAB>  # completes to "createorder"
//
// Issue #30: User requested tab completion so typing "cre<TAB>" completes to "createorder"
// ============================================================================

using TimeWarp.Nuru;
using TimeWarp.Nuru.Completion;

var builder = new NuruAppBuilder();

// ============================================================================
// Enable Shell Completion (Auto-detects executable name)
// ============================================================================
// This adds the --generate-completion {shell} route
// The app name is auto-detected from the executable name at runtime
builder.EnableStaticCompletion();

// ============================================================================
// Sample Commands - Issue #30 Use Case
// ============================================================================

builder.AddRoute
(
  "createorder {product} {quantity:int}",
  (string product, int quantity) =>
  {
    Console.WriteLine($"✅ Creating order:");
    Console.WriteLine($"   Product: {product}");
    Console.WriteLine($"   Quantity: {quantity}");
  }
);

builder.AddRoute
(
  "create {item}",
  (string item) => Console.WriteLine($"✅ Created: {item}")
);

builder.AddRoute
(
  "status",
  () => Console.WriteLine("📊 System Status: OK")
);

builder.AddRoute
(
  "deploy {env} --version {ver}",
  (string env, string ver) => Console.WriteLine($"🚀 Deploying version {ver} to {env}")
);

builder.AddRoute
(
  "list {*items}",
  (string[] items) => Console.WriteLine($"📝 Items: {string.Join(", ", items)}")
);
// ============================================================================
// Build and Run
// ============================================================================

NuruApp app = builder.Build();
return await app.RunAsync(args);
