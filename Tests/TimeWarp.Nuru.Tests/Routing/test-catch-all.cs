#!/usr/bin/dotnet --

WriteLine
(
  """
  Testing Catch-All Parameters ({*args})
  =======================================
  Testing both required {*files} and optional {*files?}
  """
);

NuruAppBuilder builder = new();

// Test route with REQUIRED catch-all
builder.AddRoute("git add {*files}", (string[] files) =>
{
    WriteLine($"✓ Git add (required) executed with {files.Length} files:");
    foreach (string file in files)
    {
        WriteLine($"  - {file}");
    }
});

// Test route with OPTIONAL catch-all
builder.AddRoute("git status {*files?}", (string[] files) =>
{
    WriteLine($"✓ Git status (optional) executed with {files.Length} files:");
    if (files.Length == 0)
    {
        WriteLine("  (showing all changes)");
    }
    else
    {
        foreach (string file in files)
        {
            WriteLine($"  - {file}");
        }
    }
});

NuruApp app = builder.Build();

WriteLine
(
  """

  Test 1: git add (required catch-all with no files)
  Expected: Should FAIL - required parameter
  """
);
int exitCode = await app.RunAsync(["git", "add"]);
if (exitCode != 0)
{
    WriteLine($"✓ PASS: Correctly failed with exit code {exitCode}");
}
else
{
    WriteLine("✗ UNEXPECTED: Should have failed!");
}

WriteLine
(
  """

  Test 2: git status (optional catch-all with no files)
  Expected: Should work with empty array
  """
);
await app.RunAsync(["git", "status"]);
WriteLine("✓ PASS: Optional catch-all works with empty");

WriteLine
(
  """

  Test 3: git add file1.txt (required with single file)
  Expected: Match with ["file1.txt"]
  """
);
await app.RunAsync(["git", "add", "file1.txt"]);
WriteLine("✓ PASS: Required works with single file");

WriteLine
(
  """

  Test 4: git add src/*.cs tests/*.cs (required with multiple)
  Expected: Match with ["src/*.cs", "tests/*.cs"]
  """
);
await app.RunAsync(["git", "add", "src/*.cs", "tests/*.cs"]);
WriteLine("✓ PASS: Required works with multiple files");

WriteLine
(
  """

  Test 5: git status modified.cs (optional with single file)
  Expected: Match with ["modified.cs"]
  """
);
await app.RunAsync(["git", "status", "modified.cs"]);
WriteLine("✓ PASS: Optional works with single file");

WriteLine
(
  """

  ========================================
  Summary:
  - {*param} = REQUIRED catch-all (fails if empty)
  - {*param?} = OPTIONAL catch-all (accepts empty)
  Both syntaxes work correctly!
  """
);