#!/usr/bin/dotnet --

// Build.cs - Build the TimeWarp.Nuru library

// Change to script directory for relative paths
string scriptDir = (AppContext.GetData("EntryPointFileDirectoryPath") as string)!;
Directory.SetCurrentDirectory(scriptDir);

WriteLine("Building TimeWarp.Nuru library...");
WriteLine($"Working from: {Directory.GetCurrentDirectory()}");

// Build each project individually to avoid framework resolution issues
// Note: TimeWarp.Nuru.Parsing is no longer built separately - its source is compiled directly into consuming projects
string[] projectsToBuild = [
  "../Source/TimeWarp.Nuru.Analyzers/TimeWarp.Nuru.Analyzers.csproj",
  "../Source/TimeWarp.Nuru.Logging/TimeWarp.Nuru.Logging.csproj",
  "../Source/TimeWarp.Nuru.Mcp/TimeWarp.Nuru.Mcp.csproj",
  "../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj",
  "../Source/TimeWarp.Nuru.Completion/TimeWarp.Nuru.Completion.csproj",
  "../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj",
  "../Benchmarks/TimeWarp.Nuru.Benchmarks/TimeWarp.Nuru.Benchmarks.csproj",
  "../Tests/TestApps/TimeWarp.Nuru.TestApp.Mediator/TimeWarp.Nuru.TestApp.Mediator.csproj",
  "../Tests/TestApps/TimeWarp.Nuru.TestApp.Delegates/TimeWarp.Nuru.TestApp.Delegates.csproj",
  // "../Tests/TimeWarp.Nuru.Analyzers.Tests/TimeWarp.Nuru.Analyzers.Tests.csproj",
  "../samples/timewarp-nuru-sample/timewarp-nuru-sample.csproj"
];

try
{
  foreach (string projectPath in projectsToBuild)
  {
    WriteLine($"Building {projectPath}...");
    CommandResult buildCommandResult = DotNet.Build()
      .WithProject(projectPath)
      .WithConfiguration("Release")
      .WithVerbosity("minimal")
      .Build();

    WriteLine("Running ...");
    WriteLine(buildCommandResult.ToCommandString());

    if (await buildCommandResult.RunAsync() != 0)
    {
      WriteLine($"❌ Failed to build {projectPath}!");
      Environment.Exit(1);
    }
  }
}
catch (Exception ex)
{
  WriteLine("=== Exception Details ===");
  WriteLine($"Exception type: {ex.GetType().Name}");
  WriteLine($"Message: {ex.Message}");

  if (ex.InnerException is not null)
  {
    WriteLine($"Inner exception type: {ex.InnerException.GetType().Name}");
    WriteLine($"Inner exception message: {ex.InnerException.Message}");
  }

  WriteLine($"Stack trace: {ex.StackTrace}");
  WriteLine("❌ Build failed with exception!");
  Environment.Exit(1);
}