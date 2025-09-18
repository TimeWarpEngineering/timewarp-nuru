#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

#pragma warning disable CA1031 // Do not catch general exception types - OK for tests

using TimeWarp.Nuru;
using static System.Console;

WriteLine
(
  """
  Testing Array Parameters with Options
  ======================================
  Pattern: kubectl exec {pod} -- {*cmd}
  Expectation: Capture command array after --
  This ALREADY WORKS with catch-all syntax
  """
);

NuruAppBuilder builder = new();

// Test route with catch-all after separator
builder.AddRoute("kubectl exec {pod} -- {*cmd}", (string pod, string[] cmd) =>
{
    WriteLine($"✓ Kubectl exec on pod: {pod}");
    WriteLine($"  Command: {string.Join(" ", cmd)}");
});

NuruApp app = builder.Build();

WriteLine
(
  """

  Test 1: kubectl exec my-pod -- ls -la
  Expected: Match with pod="my-pod", cmd=["ls", "-la"]
  """
);
try
{
    await app.RunAsync(["kubectl", "exec", "my-pod", "--", "ls", "-la"]);
    WriteLine("✓ PASS: Works as expected");
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
}

WriteLine
(
  """

  Test 2: kubectl exec nginx -- /bin/bash -c "echo hello"
  Expected: Match with pod="nginx", cmd=["/bin/bash", "-c", "echo hello"]
  """
);
try
{
    await app.RunAsync(["kubectl", "exec", "nginx", "--", "/bin/bash", "-c", "echo hello"]);
    WriteLine("✓ PASS: Works as expected");
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
}

WriteLine
(
  """

  Test 3: kubectl exec db-pod -- mysql --user=root --password=secret
  Expected: Match with pod="db-pod", cmd=["mysql", "--user=root", "--password=secret"]
  """
);
try
{
    await app.RunAsync(["kubectl", "exec", "db-pod", "--", "mysql", "--user=root", "--password=secret"]);
    WriteLine("✓ PASS: Works as expected");
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
}

WriteLine
(
  """

  ========================================
  Summary:
  Catch-all parameters work with option separators.
  This pattern is useful for passing commands to subprocesses.
  The -- separator clearly delineates where the subprocess args begin.
  """
);