#!/usr/bin/dotnet --

WriteLine("Testing option parameter matching:");
WriteLine();

var builder = new NuruAppBuilder();

// Add test routes
builder.AddRoute("git commit -m {message}", (string message) =>
  WriteLine($"✓ Creating commit with message: {message} (using -m shorthand)"));

builder.AddRoute("kubectl apply -f {file}", (string file) =>
  WriteLine($"✓ deployment.apps/{file} configured"));

NuruApp app = builder.Build();

// Test 1: What the test suite is passing
WriteLine("Test 1: Args as test suite passes them (space-separated):");
string[] test1Args = ["git", "commit", "-m", "Test", "message"];
WriteLine($"  Args: [{string.Join(", ", test1Args.Select(a => $"'{a}'"))}]");
try
{
  await app.RunAsync(test1Args);
}
catch (Exception ex)
{
  WriteLine($"  ERROR: {ex.Message}");
}

// Test 2: What we expect (message as single arg)
WriteLine("\nTest 2: Args with message as single argument:");
string[] test2Args = ["git", "commit", "-m", "Test message"];
WriteLine($"  Args: [{string.Join(", ", test2Args.Select(a => $"'{a}'"))}]");
try
{
  await app.RunAsync(test2Args);
}
catch (Exception ex)
{
  WriteLine($"  ERROR: {ex.Message}");
}

// Test 3: kubectl example
WriteLine("\nTest 3: kubectl with filename:");
string[] test3Args = ["kubectl", "apply", "-f", "deployment.yaml"];
WriteLine($"  Args: [{string.Join(", ", test3Args.Select(a => $"'{a}'"))}]");
try
{
  await app.RunAsync(test3Args);
}
catch (Exception ex)
{
  WriteLine($"  ERROR: {ex.Message}");
}

// Test 4: Show what routes are registered
WriteLine("\nRegistered routes:");
builder.AddRoute("--help", () =>
{
  WriteLine("  git commit -m {message}");
  WriteLine("  kubectl apply -f {file}");
});
await app.RunAsync(["--help"]);