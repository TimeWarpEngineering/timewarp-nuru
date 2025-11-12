// Shell Completion Example
// Demonstrates Issue #30: Tab completion for commands like createorder

using TimeWarp.Nuru;
using TimeWarp.Nuru.Completion;

var builder = new NuruAppBuilder();

// ============================================================================
// Enable Shell Completion
// ============================================================================
// This adds the --generate-completion {shell} route
builder.EnableShellCompletion("myapp");

// ============================================================================
// Sample Commands - Issue #30 Use Case
// ============================================================================

builder.AddRoute("createorder {product} {quantity:int}", (string product, int quantity) =>
{
  Console.WriteLine($"✅ Creating order:");
  Console.WriteLine($"   Product: {product}");
  Console.WriteLine($"   Quantity: {quantity}");
  return 0;
});

builder.AddRoute("create {item}", (string item) =>
{
  Console.WriteLine($"✅ Created: {item}");
  return 0;
});

builder.AddRoute("status", () =>
{
  Console.WriteLine("📊 System Status: OK");
  return 0;
});

builder.AddRoute("deploy {env} --version {ver}", (string env, string ver) =>
{
  Console.WriteLine($"🚀 Deploying version {ver} to {env}");
  return 0;
});

builder.AddRoute("list {*items}", (string[] items) =>
{
  Console.WriteLine($"📝 Items: {string.Join(", ", items)}");
  return 0;
});

// ============================================================================
// Build and Run
// ============================================================================

NuruApp app = builder.Build();
return await app.RunAsync(args);
