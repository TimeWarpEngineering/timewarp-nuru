#!/usr/bin/dotnet --

// Test: Invalid Positional Parameter Patterns
// Tests patterns that should be rejected by analyzer rules NURU007 and NURU008
// These patterns create ambiguity and should not be allowed

using TimeWarp.Nuru;

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

NuruAppBuilder builder1 = new();
ArgumentException ex1 =
  Should.Throw<ArgumentException>
  (
    () =>
      builder1.AddRoute
      (
        "deploy {env?} {version?}",
        (string? env, string? version) =>
          WriteLine($"Deploy: env={env}, version={version}")
      )
  );

ex1.Message.ShouldContain("Multiple consecutive optional positional parameters");
WriteLine($"✓ Expected failure: {ex1.Message}");

WriteLine
(
  """

  Test 2: Optional Before Required
  Pattern: copy {source?} {dest}
  Problem: Makes no sense - can't have required after optional
  """
);

NuruAppBuilder builder2 = new();
ArgumentException ex2 =
  Should.Throw<ArgumentException>
  (
    () =>
      builder2.AddRoute
      (
        "copy {source?} {dest}",
        (string? source, string dest) =>
          WriteLine($"Copy: source={source}, dest={dest}")
      )
  );

ex2.Message.ShouldContain("Optional parameter 'source?' cannot appear before required parameter");
WriteLine($"✓ Expected failure: {ex2.Message}");

WriteLine
(
  """

  Test 3: Catch-all with Optional Parameters (NURU008)
  Pattern: run {script?} {*args}
  Problem: Mixing optional with catch-all is ambiguous
  """
);

NuruAppBuilder builder3 = new();
ArgumentException ex3 =
  Should.Throw<ArgumentException>
  (
    () =>
      builder3.AddRoute
      (
        "run {script?} {*args}",
        (string? script, string[] args) =>
          WriteLine($"Run: script={script}, args=[{string.Join(", ", args)}]")
      )
  );

ex3.Message.ShouldContain("Cannot mix optional parameters with catch-all");
WriteLine($"✓ Expected failure: {ex3.Message}");

WriteLine
(
  """

  Test 4: Catch-all Not at End
  Pattern: exec {*args} {output}
  Problem: Catch-all must be last parameter
  """
);

NuruAppBuilder builder4 = new();
ArgumentException ex4 =
  Should.Throw<ArgumentException>
  (
    () =>
      builder4.AddRoute
      (
        "exec {*args} {output}",
        (string[] args, string output) =>
          WriteLine($"Exec: args=[{string.Join(", ", args)}], output={output}")
      )
  );

ex4.Message.ShouldContain("Catch-all parameter 'args' must be the last positional segment");
WriteLine($"✓ Expected failure: {ex4.Message}");

WriteLine
(
  """

  Test 5: Multiple Catch-alls
  Pattern: merge {*files1} {*files2}
  Problem: Can't have multiple catch-alls
  """
);

NuruAppBuilder builder5 = new();
ArgumentException ex5 =
  Should.Throw<ArgumentException>
  (
    () =>
      builder5.AddRoute
      (
        "merge {*files1} {*files2}",
        (string[] files1, string[] files2) =>
          WriteLine($"Merge: files1=[{string.Join(", ", files1)}], files2=[{string.Join(", ", files2)}]")
      )
  );

ex5.Message.ShouldContain("Catch-all parameter 'files1' must be the last positional segment");
WriteLine($"✓ Expected failure: {ex5.Message}");

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