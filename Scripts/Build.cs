#!/usr/bin/dotnet --
// Build.cs - Build the TimeWarp.Nuru library

// Change to script directory for relative paths
string scriptDir = (AppContext.GetData("EntryPointFileDirectoryPath") as string)!;
Directory.SetCurrentDirectory(scriptDir);

WriteLine("Building TimeWarp.Nuru library...");
WriteLine($"Working from: {Directory.GetCurrentDirectory()}");

// First build the parsing package so it's available for format restore
WriteLine("Building TimeWarp.Nuru.Parsing first...");
CommandResult parsingBuildResult = DotNet.Build()
  .WithProject("../Source/TimeWarp.Nuru.Parsing/TimeWarp.Nuru.Parsing.csproj")
  .WithConfiguration("Release")
  .WithVerbosity("minimal")
  .Build();

WriteLine("Running ...");
WriteLine(parsingBuildResult.ToCommandString());

ExecutionResult parsingResult = await parsingBuildResult.ExecuteAsync();
parsingResult.WriteToConsole();

if (!parsingResult.IsSuccess)
{
  WriteLine("❌ Failed to build TimeWarp.Nuru.Parsing!");
  Environment.Exit(1);
}

// Now check code style with dotnet format
WriteLine("Checking code style with dotnet format...");
CommandResult dotnetCommandResult = Shell.Run("dotnet")
 .WithArguments("format", "../TimeWarp.Nuru.slnx", "--verify-no-changes", "--severity", "warn", "--exclude", "**/Benchmarks/**")
 .Build();

WriteLine("Running ...");
WriteLine(dotnetCommandResult.ToCommandString());

ExecutionResult formatResult = await dotnetCommandResult.ExecuteAsync();
formatResult.WriteToConsole();

if (!formatResult.IsSuccess)
{
  WriteLine("❌ Code style violations found! Run 'dotnet format' to fix them.");
  Environment.Exit(1);
}

// Build the solution
try
{
  CommandResult buildCommandResult = DotNet.Build()
    .WithProject("../TimeWarp.Nuru.slnx")
    .WithConfiguration("Release")
    .WithVerbosity("minimal")
    .WithNoIncremental()
    .WithNoValidation()
    .Build();

  WriteLine("Running ...");
  WriteLine(buildCommandResult.ToCommandString());

  ExecutionResult result = await buildCommandResult.ExecuteAsync();
  result.WriteToConsole();
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