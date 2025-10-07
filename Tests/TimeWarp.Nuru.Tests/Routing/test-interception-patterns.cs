#!/usr/bin/dotnet --

WriteLine
(
  """
  Testing Command Interception Patterns
  ======================================
  Demonstrating gradual migration from shell passthrough
  """
);

NuruAppBuilder builder = new();

// Stage 1: Pass through everything to shell
builder.AddRoute("{*args}", (string[] args) =>
{
    WriteLine($"📡 Intercepted: {string.Join(" ", args)}");

    // In real app, would check if this is a known command we're migrating
    if (args.Length > 0 && args[0] == "ls")
    {
        WriteLine("  ⚠️ This command will be migrated to native implementation soon");
    }

    // Pass through to shell
    WriteLine("  → Passing through to shell...");
    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = args[0],
            Arguments = string.Join(" ", args.Skip(1)),
            UseShellExecute = false,
            RedirectStandardOutput = true
        }
    };
    // Would execute here in real implementation
    WriteLine("  (Shell execution simulated)");
});

// Stage 2: Intercept specific commands
builder.AddRoute("ls {path?} --all?", (string? path, bool all) =>
{
    WriteLine("✓ Native ls implementation:");
    WriteLine($"  Path: {path ?? "."}");
    WriteLine($"  Show all: {all}");
    // Native implementation here
});

// Stage 3: Intercept with options
builder.AddRoute("grep {pattern} {*files} --ignore-case?", (string pattern, string[] files, bool ignoreCase) =>
{
    WriteLine("✓ Native grep implementation:");
    WriteLine($"  Pattern: {pattern}");
    WriteLine($"  Files: {string.Join(", ", files)}");
    WriteLine($"  Ignore case: {ignoreCase}");
    // Native implementation here
});

NuruApp app = builder.Build();

WriteLine
(
  """

  Test 1: ls -la /tmp
  Expected: Intercept and show migration warning
  """
);
try
{
    await app.RunAsync(["ls", "-la", "/tmp"]);
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
    WriteLine("  NuruContext required for interception pattern");
}

WriteLine
(
  """

  Test 2: ls /home --all
  Expected: Match native implementation
  """
);
try
{
    await app.RunAsync(["ls", "/home", "--all"]);
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
    WriteLine("  Optional flags not implemented yet");
}

WriteLine
(
  """

  Test 3: grep TODO *.cs --ignore-case
  Expected: Match native grep implementation
  """
);
try
{
    await app.RunAsync(["grep", "TODO", "*.cs", "--ignore-case"]);
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
    WriteLine("  Optional flags not implemented yet");
}

WriteLine
(
  """

  Test 4: Unknown command "foobar --test"
  Expected: Pass through to shell via catch-all
  """
);
try
{
    await app.RunAsync(["foobar", "--test"]);
}
catch (Exception ex)
{
    WriteLine($"✗ FAILED: {ex.Message}");
    WriteLine("  Catch-all with NuruContext not working");
}

WriteLine
(
  """

  ========================================
  Summary:
  Gradual migration strategy:
  1. Start with catch-all that passes to shell
  2. Add specific routes for commands you're migrating
  3. More specific routes take precedence
  4. Gradually move from shell passthrough to native
  5. Track usage patterns via NuruContext
  """
);