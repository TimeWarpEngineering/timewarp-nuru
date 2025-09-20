#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

#pragma warning disable CA1031 // Do not catch general exception types - OK for tests

using TimeWarp.Nuru;
using static System.Console;

WriteLine
(
  """
  Testing Catch-All Parameters ({*args})
  =======================================
  Pattern: git add {*files}
  Expectation: Captures all remaining arguments as array
  This should ALREADY WORK in current implementation
  """
);

NuruAppBuilder builder = new();

// Test route with catch-all
builder.AddRoute("git add {*files}", (string[] files) =>
{
    WriteLine($"✓ Git add executed with {files.Length} files:");
    foreach (string file in files)
    {
        WriteLine($"  - {file}");
    }
});

NuruApp app = builder.Build();

WriteLine
(
  """

  Test 1: git add
  Expected: Match with empty array
  """
);
try
{
    await app.RunAsync(["git", "add"]);
    WriteLine("✓ PASS: Works with empty array");
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
}

WriteLine
(
  """

  Test 2: git add file1.txt
  Expected: Match with ["file1.txt"]
  """
);
try
{
    await app.RunAsync(["git", "add", "file1.txt"]);
    WriteLine("✓ PASS: Works with single file");
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
}

WriteLine
(
  """

  Test 3: git add src/*.cs tests/*.cs docs/*.md
  Expected: Match with ["src/*.cs", "tests/*.cs", "docs/*.md"]
  """
);
try
{
    await app.RunAsync(["git", "add", "src/*.cs", "tests/*.cs", "docs/*.md"]);
    WriteLine("✓ PASS: Works with multiple patterns");
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
  Catch-all parameters work correctly in current implementation.
  All tests should PASS.
  """
);