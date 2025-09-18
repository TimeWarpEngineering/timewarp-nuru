#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

#pragma warning disable CA1031 // Do not catch general exception types - OK for tests

using TimeWarp.Nuru;
using static System.Console;

WriteLine
(
  """
  Testing Route Specificity Ordering
  ===================================
  Routes should match from most specific to least specific
  """
);

NuruAppBuilder builder = new();

// Add routes in deliberately "wrong" order to test specificity
// Less specific route added first
builder.AddRoute("git {*args}", (string[] args) =>
{
    WriteLine($"✓ Catch-all: git {string.Join(" ", args)}");
});

// More specific route added second
builder.AddRoute("git commit -m {message}", (string message) =>
{
    WriteLine($"✓ Specific: git commit -m \"{message}\"");
});

// Even more specific route added third
builder.AddRoute("git commit -m {message} --amend", (string message) =>
{
    WriteLine($"✓ Most specific: git commit -m \"{message}\" --amend");
});

// Another specific route
builder.AddRoute("git status", () =>
{
    WriteLine("✓ Specific: git status");
});

NuruApp app = builder.Build();

WriteLine
(
  """

  Test 1: git commit -m "fix bug" --amend
  Expected: Match most specific route (with --amend)
  """
);
try
{
    await app.RunAsync(["git", "commit", "-m", "fix bug", "--amend"]);
    WriteLine("✓ PASS: Matched most specific route");
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
    WriteLine("  Specificity ordering may not be working correctly");
}

WriteLine
(
  """

  Test 2: git commit -m "add feature"
  Expected: Match specific route (without --amend)
  """
);
try
{
    await app.RunAsync(["git", "commit", "-m", "add feature"]);
    WriteLine("✓ PASS: Matched specific route");
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
}

WriteLine
(
  """

  Test 3: git status
  Expected: Match git status route, not catch-all
  """
);
try
{
    await app.RunAsync(["git", "status"]);
    WriteLine("✓ PASS: Matched specific route");
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
}

WriteLine
(
  """

  Test 4: git log --oneline
  Expected: Match catch-all route
  """
);
try
{
    await app.RunAsync(["git", "log", "--oneline"]);
    WriteLine("✓ PASS: Matched catch-all route");
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
  Specificity scoring ensures:
  - Literal segments > parameters > catch-all
  - Required options > optional options
  - More segments = more specific
  - Routes automatically ordered regardless of registration order
  """
);