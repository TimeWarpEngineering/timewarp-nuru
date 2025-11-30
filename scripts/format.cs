#!/usr/bin/dotnet --
// format.cs - Check code formatting for TimeWarp.Nuru

// Change to script directory for relative paths
string scriptDir = (AppContext.GetData("EntryPointFileDirectoryPath") as string)!;
Directory.SetCurrentDirectory(scriptDir);

WriteLine("Checking code style with dotnet format...");
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
 .WithArguments("format", "../timewarp-nuru.slnx", "--verify-no-changes", "--severity", "warn", "--exclude", "**/Benchmarks/**")
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

WriteLine("✅ Code style check passed!");