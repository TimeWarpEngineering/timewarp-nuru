#!/usr/bin/dotnet --

// Test: Optional Positional Parameters After Required
// Pattern: command {required} {optional?}
// Validates that optional positional parameters work correctly when placed after required ones

WriteLine
(
  """
  Testing Optional Positional After Required
  ===========================================
  Pattern: copy {source} {dest?}
  Expectation: source is required, dest is optional
  """
);

NuruAppBuilder builder = new();

// Test route with required followed by optional positional
builder.AddRoute("copy {source} {dest?}", (string source, string? dest) =>
{
    if (dest is not null)
    {
        WriteLine($"✓ Copy from {source} to {dest}");
    }
    else
    {
        WriteLine($"✓ Copy {source} to default location");
    }
});

// Another example with typed parameters
builder.AddRoute("wait {message} {seconds:int?}", (string message, int? seconds) =>
{
    if (seconds.HasValue)
    {
        WriteLine($"✓ Wait {seconds} seconds with message: {message}");
    }
    else
    {
        WriteLine($"✓ Wait indefinitely with message: {message}");
    }
});

NuruApp app = builder.Build();

WriteLine
(
  """

  Test 1: copy file1.txt file2.txt
  Expected: Match, source="file1.txt", dest="file2.txt"
  """
);

try
{
    await app.RunAsync(["copy", "file1.txt", "file2.txt"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 2: copy file1.txt
  Expected: Match, source="file1.txt", dest=null
  """
);

try
{
    await app.RunAsync(["copy", "file1.txt"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 3: copy
  Expected: No match (missing required source)
  """
);

try
{
    await app.RunAsync(["copy"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Expected error: {ex.Message}");
}

WriteLine
(
  """

  Test 4: wait "Processing..." 5
  Expected: Match, message="Processing...", seconds=5
  """
);

try
{
    await app.RunAsync(["wait", "Processing...", "5"]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  Test 5: wait "Processing..."
  Expected: Match, message="Processing...", seconds=null
  """
);

try
{
    await app.RunAsync(["wait", "Processing..."]);
}
catch (Exception ex)
{
    WriteLine($"✗ Error: {ex.Message}");
}

WriteLine
(
  """

  ========================================
  This pattern is valid and commonly used.
  Optional positional parameters MUST come after
  all required positional parameters.
  """
);