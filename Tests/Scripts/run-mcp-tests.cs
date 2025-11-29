#!/usr/bin/dotnet --
#:package TimeWarp.Amuru

using TimeWarp.Amuru;

// Get script directory to build correct paths
string scriptDir = AppContext.GetData("EntryPointFileDirectoryPath") as string
  ?? throw new InvalidOperationException("Could not get entry point directory");

// Test files are in TimeWarp.Nuru.Mcp.Tests directory
string testDir = Path.Combine(scriptDir, "..", "TimeWarp.Nuru.Mcp.Tests");

// Configure Nuru app
NuruAppBuilder builder = new();
builder.MapDefault(() => RunTests(), "Run all MCP tests");
NuruCoreApp app = builder.Build();

return await app.RunAsync(args);

async Task<int> RunTests()
{
  WriteLine("🧪 Running MCP Tests");
  WriteLine();

  // Track overall results
  int totalTests = 0;
  int passedTests = 0;

  // List of MCP test files (01-05 use Jaribu framework)
  // Note: mcp-06 (server integration test) is at Tests/Scripts/test-mcp-server.cs
  string[] testFiles = [
    Path.Combine(testDir, "mcp-01-example-retrieval.cs"),
    Path.Combine(testDir, "mcp-02-syntax-documentation.cs"),
    Path.Combine(testDir, "mcp-03-route-validation.cs"),
    Path.Combine(testDir, "mcp-04-handler-generation.cs"),
    Path.Combine(testDir, "mcp-05-error-documentation.cs"),
  ];

  foreach (string testFile in testFiles)
  {
    string fullPath = Path.GetFullPath(testFile);
    if (!File.Exists(fullPath))
    {
      WriteLine($"❌ Test file not found: {testFile}");
      continue;
    }

    string testName = Path.GetFileNameWithoutExtension(testFile);
    Write($"Running {testName}... ");

    // Run test via Shell
    CommandOutput result = await Shell.Builder(fullPath)
      .WithWorkingDirectory(Path.GetDirectoryName(fullPath)!)
      .WithNoValidation()
      .CaptureAsync();

    if (result.ExitCode == 0)
    {
      passedTests++;
      WriteLine("✅");
    }
    else
    {
      WriteLine("❌");
      WriteLine($"  Exit code: {result.ExitCode}");
      if (!string.IsNullOrEmpty(result.Stderr))
      {
        WriteLine($"  Stderr: {result.Stderr}");
      }
    }

    totalTests++;
  }

  // Summary
  WriteLine();
  WriteLine("─".PadRight(50, '─'));
  WriteLine($"Total: {totalTests} | Passed: {passedTests} | Failed: {totalTests - passedTests}");

  if (passedTests == totalTests)
  {
    WriteLine("✅ All MCP tests passed!");
    return 0;
  }
  else
  {
    WriteLine($"❌ {totalTests - passedTests} test(s) failed");
    return 1;
  }
}
