#!/usr/bin/dotnet --
#:property LangVersion=preview
#:property EnablePreviewFeatures=true
// Build.cs - Build the TimeWarp.Nuru library

// Change to script directory for relative paths
string scriptDir = (AppContext.GetData("EntryPointFileDirectoryPath") as string)!;
Directory.SetCurrentDirectory(scriptDir);

WriteLine("Building TimeWarp.Nuru library...");
WriteLine($"Working from: {Directory.GetCurrentDirectory()}");

// First build the parsing package so it's available for other projects
WriteLine("Building TimeWarp.Nuru.Parsing first...");
CommandResult parsingBuildResult = DotNet.Build()
  .WithProject("../Source/TimeWarp.Nuru.Parsing/TimeWarp.Nuru.Parsing.csproj")
  .WithConfiguration("Release")
  .WithVerbosity("minimal")
  .Build();

WriteLine("Running ...");
WriteLine(parsingBuildResult.ToCommandString());

if (await parsingBuildResult.RunAsync() != 0)
{
  WriteLine("❌ Failed to build TimeWarp.Nuru.Parsing!");
  Environment.Exit(1);
}

// Clear the local NuGet cache for the source-only parsing package
// This ensures the latest source files are included when building dependent projects
string localCachePath = Path.Combine("..", "LocalNuGetCache", "timewarp.nuru.parsing");
if (Directory.Exists(localCachePath))
{
  WriteLine($"Clearing local cache: {localCachePath}");
  Directory.Delete(localCachePath, recursive: true);
}

// Build each project individually to avoid framework resolution issues
string[] projectsToBuild = [
  "../Source/TimeWarp.Nuru.Parsing/TimeWarp.Nuru.Parsing.csproj",
  "../Source/TimeWarp.Nuru.Analyzers/TimeWarp.Nuru.Analyzers.csproj",
  "../Source/TimeWarp.Nuru.Logging/TimeWarp.Nuru.Logging.csproj",
  "../Source/TimeWarp.Nuru.Mcp/TimeWarp.Nuru.Mcp.csproj",
  "../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj",
  "../Benchmarks/TimeWarp.Nuru.Benchmarks/TimeWarp.Nuru.Benchmarks.csproj",
  "../Tests/TimeWarp.Nuru.TestApp.Mediator/TimeWarp.Nuru.TestApp.Mediator.csproj",
  "../Tests/TimeWarp.Nuru.TestApp.Delegates/TimeWarp.Nuru.TestApp.Delegates.csproj",
  // "../Tests/TimeWarp.Nuru.Analyzers.Tests/TimeWarp.Nuru.Analyzers.Tests.csproj",
  "../Samples/TimeWarp.Nuru.Sample/TimeWarp.Nuru.Sample.csproj"
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