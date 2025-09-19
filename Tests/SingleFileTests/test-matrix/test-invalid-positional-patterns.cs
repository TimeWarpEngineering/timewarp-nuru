#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

// Test: Invalid Positional Parameter Patterns
// Tests patterns that should be rejected by analyzer rules NURU007 and NURU008
// These patterns create ambiguity and should not be allowed

using TimeWarp.Nuru;
using static System.Console;

WriteLine
(
  """
  Testing Invalid Positional Parameter Patterns
  ==============================================
  These patterns should fail or produce analyzer warnings
  """
);

WriteLine
(
  """

  Test 1: Consecutive Optional Parameters (NURU007)
  Pattern: deploy {env?} {version?}
  Problem: Ambiguous - can't tell which arg is which
  """
);

try
{
    NuruAppBuilder builder1 = new();
    builder1.AddRoute("deploy {env?} {version?}", (string? env, string? version) =>
    {
        WriteLine($"Deploy: env={env}, version={version}");
    });

    NuruApp app1 = builder1.Build();

    // This is ambiguous: is "v2.0" the env or the version?
    await app1.RunAsync(["deploy", "v2.0"]);
    WriteLine("✗ Should have failed - consecutive optionals create ambiguity");
}
catch (Exception ex)
{
    WriteLine($"✓ Expected failure: {ex.Message}");
}

WriteLine
(
  """

  Test 2: Optional Before Required
  Pattern: copy {source?} {dest}
  Problem: Makes no sense - can't have required after optional
  """
);

try
{
    NuruAppBuilder builder2 = new();
    builder2.AddRoute("copy {source?} {dest}", (string? source, string dest) =>
    {
        WriteLine($"Copy: source={source}, dest={dest}");
    });

    NuruApp app2 = builder2.Build();
    await app2.RunAsync(["copy", "file.txt"]);
    WriteLine("✗ Should have failed - optional before required is invalid");
}
catch (Exception ex)
{
    WriteLine($"✓ Expected failure: {ex.Message}");
}

WriteLine
(
  """

  Test 3: Catch-all with Optional Parameters (NURU008)
  Pattern: run {script?} {*args}
  Problem: Mixing optional with catch-all is ambiguous
  """
);

try
{
    NuruAppBuilder builder3 = new();
    builder3.AddRoute("run {script?} {*args}", (string? script, string[] args) =>
    {
        WriteLine($"Run: script={script}, args=[{string.Join(", ", args)}]");
    });

    NuruApp app3 = builder3.Build();
    // Ambiguous: is "test.sh" the script or part of args?
    await app3.RunAsync(["run", "test.sh", "arg1", "arg2"]);
    WriteLine("✗ Should have failed - can't mix optional with catch-all");
}
catch (Exception ex)
{
    WriteLine($"✓ Expected failure: {ex.Message}");
}

WriteLine
(
  """

  Test 4: Catch-all Not at End
  Pattern: exec {*args} {output}
  Problem: Catch-all must be last parameter
  """
);

try
{
    NuruAppBuilder builder4 = new();
    builder4.AddRoute("exec {*args} {output}", (string[] args, string output) =>
    {
        WriteLine($"Exec: args=[{string.Join(", ", args)}], output={output}");
    });

    NuruApp app4 = builder4.Build();
    await app4.RunAsync(["exec", "cmd1", "cmd2", "output.txt"]);
    WriteLine("✗ Should have failed - catch-all must be last");
}
catch (Exception ex)
{
    WriteLine($"✓ Expected failure: {ex.Message}");
}

WriteLine
(
  """

  Test 5: Multiple Catch-alls
  Pattern: merge {*files1} {*files2}
  Problem: Can't have multiple catch-alls
  """
);

try
{
    NuruAppBuilder builder5 = new();
    builder5.AddRoute("merge {*files1} {*files2}", (string[] files1, string[] files2) =>
    {
        WriteLine($"Merge: files1=[{string.Join(", ", files1)}], files2=[{string.Join(", ", files2)}]");
    });

    NuruApp app5 = builder5.Build();
    await app5.RunAsync(["merge", "a.txt", "b.txt"]);
    WriteLine("✗ Should have failed - multiple catch-alls invalid");
}
catch (Exception ex)
{
    WriteLine($"✓ Expected failure: {ex.Message}");
}

WriteLine
(
  """

  ========================================
  These patterns should be caught by:
  - NURU007: Consecutive optional parameters
  - NURU008: Mixed catch-all with optional
  - NURU005: Catch-all not at end
  - Parser validation for invalid patterns
  """
);