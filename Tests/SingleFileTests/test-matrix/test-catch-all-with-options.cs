#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

// Test: Catch-all Parameters with Options
// Pattern: command {*args} --flag? {value?}
// Validates that options can be parsed correctly even when mixed with catch-all parameters

using TimeWarp.Nuru;
using static System.Console;

WriteLine
(
  """
  Testing Catch-all with Options
  ===============================
  Pattern: kubectl get {*resources} --namespace? {ns?} --output? {format?}
  Expectation: Catch-all captures positional args, options parsed separately
  """
);

NuruAppBuilder builder = new();

// Test route with catch-all and optional flags
builder.AddRoute("kubectl get {*resources} --namespace? {ns?} --output? {format?}",
    (string[] resources, string? ns, string? format) =>
{
    WriteLine("✓ kubectl get:");
    WriteLine($"  Resources: [{string.Join(", ", resources)}]");
    WriteLine($"  Namespace: {ns ?? "(default)"}");
    WriteLine($"  Output: {format ?? "default"}");
});

// Another example: git add with options
builder.AddRoute("git add {*files} --force? --dry-run?",
    (string[] files, bool force, bool dryRun) =>
{
    WriteLine("✓ git add:");
    WriteLine($"  Files: [{string.Join(", ", files)}]");
    WriteLine($"  Force: {force}");
    WriteLine($"  Dry run: {dryRun}");
});

NuruApp app = builder.Build();

WriteLine
(
  """

  Test 1: kubectl get pods svc
  Expected: resources=[pods, svc], ns=null, format=null
  """
);

try
{
    await app.RunAsync(["kubectl", "get", "pods", "svc"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 2: kubectl get pods --namespace prod
  Expected: resources=[pods], ns="prod", format=null
  """
);

try
{
    await app.RunAsync(["kubectl", "get", "pods", "--namespace", "prod"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 3: kubectl get pods svc --namespace prod --output json
  Expected: resources=[pods, svc], ns="prod", format="json"
  """
);

try
{
    await app.RunAsync(["kubectl", "get", "pods", "svc", "--namespace", "prod", "--output", "json"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 4: git add file1.cs file2.cs --force
  Expected: files=[file1.cs, file2.cs], force=true, dryRun=false
  """
);

try
{
    await app.RunAsync(["git", "add", "file1.cs", "file2.cs", "--force"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 5: git add *.cs --dry-run --force
  Expected: files=[*.cs], force=true, dryRun=true
  """
);

try
{
    await app.RunAsync(["git", "add", "*.cs", "--dry-run", "--force"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  ========================================
  Options with -- prefix can be distinguished
  from positional arguments, allowing them to
  appear after catch-all parameters.
  """
);