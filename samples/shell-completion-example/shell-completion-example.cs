#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-completion/timewarp-nuru-completion.csproj

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

NuruCoreApp app = NuruCoreApp.CreateSlimBuilder(args)
  // ============================================================================
  // Sample Commands - Issue #30 Use Case
  // ============================================================================
  .Map("createorder {product} {quantity:int}")
    .WithHandler((string product, int quantity) =>
    {
      Console.WriteLine($"✅ Creating order:");
      Console.WriteLine($"   Product: {product}");
      Console.WriteLine($"   Quantity: {quantity}");
    })
    .AsCommand()
    .Done()

  .Map("create {item}")
    .WithHandler((string item) => Console.WriteLine($"✅ Created: {item}"))
    .AsCommand()
    .Done()

  .Map("status")
    .WithHandler(() => Console.WriteLine("📊 System Status: OK"))
    .AsQuery()
    .Done()

  .Map("deploy {env} --version {ver}")
    .WithHandler((string env, string ver) => Console.WriteLine($"🚀 Deploying version {ver} to {env}"))
    .AsCommand()
    .Done()

  .Map("list {*items}")
    .WithHandler((string[] items) => Console.WriteLine($"📝 Items: {string.Join(", ", items)}"))
    .AsQuery()
    .Done()

  // ============================================================================
  // Enable Shell Completion (Auto-detects executable name)
  // ============================================================================
  // This adds the --generate-completion {shell} route
  // The app name is auto-detected from the executable name at runtime
  .EnableStaticCompletion()

  // ============================================================================
  // Build and Run
  // ============================================================================
  .Build();

return await app.RunAsync(args);
