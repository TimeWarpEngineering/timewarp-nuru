#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

#pragma warning disable CA1031 // Do not catch general exception types - OK for tests

using TimeWarp.Nuru;
using static System.Console;

WriteLine
(
  """
  Testing Combined Pattern Features
  ==================================
  Complex real-world patterns using multiple features
  """
);

NuruAppBuilder builder = new();

// Complex Docker-like command
builder.AddRoute("docker run {image} {*cmd} --env {var}* --volume {vol}* --detach?",
    (string image, string[] cmd, string[] vars, string[] vols, bool detach) =>
{
    WriteLine($"✓ Docker run:");
    WriteLine($"  Image: {image}");
    WriteLine($"  Command: {string.Join(" ", cmd)}");
    WriteLine($"  Environment: {string.Join(", ", vars)}");
    WriteLine($"  Volumes: {string.Join(", ", vols)}");
    WriteLine($"  Detached: {detach}");
});

// Git with multiple optional flags and parameters
builder.AddRoute("git log {ref?} --oneline? --graph? --all? -n {count:int?}",
    (string? gitRef, bool oneline, bool graph, bool all, int? count) =>
{
    WriteLine($"✓ Git log:");
    WriteLine($"  Ref: {gitRef ?? "HEAD"}");
    WriteLine($"  Oneline: {oneline}");
    WriteLine($"  Graph: {graph}");
    WriteLine($"  All: {all}");
    WriteLine($"  Count: {count?.ToString() ?? "all"}");
});

// Kubectl with positional, options, and catch-all
builder.AddRoute("kubectl {verb} {resource} {name?} --namespace {ns?} --output {format?} -- {*extra}",
    (string verb, string resource, string? name, string? ns, string? format, string[] extra) =>
{
    WriteLine($"✓ Kubectl {verb}:");
    WriteLine($"  Resource: {resource}");
    WriteLine($"  Name: {name ?? "(all)"}");
    WriteLine($"  Namespace: {ns ?? "default"}");
    WriteLine($"  Format: {format ?? "wide"}");
    WriteLine($"  Extra args: {string.Join(" ", extra)}");
});

NuruApp app = builder.Build();

WriteLine
(
  """

  Test 1: docker run nginx --env PORT=80 --env HOST=localhost --volume /data:/data --detach
  Expected: All options parsed correctly
  """
);
try
{
    await app.RunAsync(["docker", "run", "nginx", "--env", "PORT=80", "--env", "HOST=localhost", "--volume", "/data:/data", "--detach"]);
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
    WriteLine("  Complex patterns not yet supported");
}

WriteLine
(
  """

  Test 2: git log origin/main --oneline --graph -n 10
  Expected: Parse ref, flags, and count
  """
);
try
{
    await app.RunAsync(["git", "log", "origin/main", "--oneline", "--graph", "-n", "10"]);
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
    WriteLine("  Optional parameters and flags not yet supported");
}

WriteLine
(
  """

  Test 3: kubectl get pods --namespace kube-system --output json
  Expected: Parse with optional name omitted
  """
);
try
{
    await app.RunAsync(["kubectl", "get", "pods", "--namespace", "kube-system", "--output", "json"]);
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
    WriteLine("  Complex optional patterns not yet supported");
}

WriteLine
(
  """

  Test 4: kubectl exec my-pod -- /bin/sh -c "echo hello"
  Expected: Parse with separator and catch-all
  """
);
try
{
    await app.RunAsync(["kubectl", "exec", "my-pod", "--", "/bin/sh", "-c", "echo hello"]);
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
    WriteLine("  Separator with catch-all may not work with complex patterns");
}

WriteLine
(
  """

  ========================================
  Summary:
  Real-world CLI patterns often combine:
  - Optional positional parameters
  - Optional and repeated options
  - Boolean flags (always optional)
  - Catch-all for remaining arguments
  - Type constraints on parameters
  All these features must work together seamlessly.
  """
);