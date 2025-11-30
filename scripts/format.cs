#!/usr/bin/dotnet --
// format.cs - Check code formatting for TimeWarp.Nuru

// Change to script directory for relative paths
string scriptDir = (AppContext.GetData("EntryPointFileDirectoryPath") as string)!;
Directory.SetCurrentDirectory(scriptDir);

WriteLine("Checking code style with dotnet format...");
WriteLine($"Working from: {Directory.GetCurrentDirectory()}");

// Check code style with dotnet format
CommandResult dotnetCommandResult = Shell.Run("dotnet")
 .WithArguments("format", "../timewarp-nuru.slnx", "--verify-no-changes", "--severity", "warn", "--exclude", "**/benchmarks/**")
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