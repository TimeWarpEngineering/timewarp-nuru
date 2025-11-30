#!/usr/bin/dotnet --

// build.cs - Build the TimeWarp.Nuru library

// Change to script directory for relative paths
string scriptDir = (AppContext.GetData("EntryPointFileDirectoryPath") as string)!;
Directory.SetCurrentDirectory(scriptDir);

WriteLine("Building TimeWarp.Nuru library...");
WriteLine($"Working from: {Directory.GetCurrentDirectory()}");

// Build each project individually to avoid framework resolution issues
// Note: TimeWarp.Nuru.Parsing is no longer built separately - its source is compiled directly into consuming projects
string[] projectsToBuild = [
  "../source/timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj",
  "../source/timewarp-nuru-logging/timewarp-nuru-logging.csproj",
  "../source/timewarp-nuru-mcp/timewarp-nuru-mcp.csproj",
  "../source/timewarp-nuru/timewarp-nuru.csproj",
  "../source/timewarp-nuru-completion/timewarp-nuru-completion.csproj",
  "../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj",
  "../benchmarks/timewarp-nuru-benchmarks/timewarp-nuru-benchmarks.csproj",
  "../tests/test-apps/timewarp-nuru-testapp-mediator/timewarp-nuru-testapp-mediator.csproj",
  "../tests/test-apps/timewarp-nuru-testapp-delegates/timewarp-nuru-testapp-delegates.csproj",
  // "../tests/timewarp-nuru-analyzers-tests/timewarp-nuru-analyzers-tests.csproj",
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