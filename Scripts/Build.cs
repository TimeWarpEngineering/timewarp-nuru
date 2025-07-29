#!/usr/bin/dotnet --
// Build.cs - Build the TimeWarp.Nuru library

// Change to script directory for relative paths
string scriptDir = (AppContext.GetData("EntryPointFileDirectoryPath") as string)!;
Directory.SetCurrentDirectory(scriptDir);

Console.WriteLine("Building TimeWarp.Nuru library...");
Console.WriteLine($"Working from: {Directory.GetCurrentDirectory()}");

// First check code style with dotnet format
Console.WriteLine("Checking code style with dotnet format...");
CommandResult dotnetCommandResult = Shell.Run("dotnet")
 .WithArguments("format", "../TimeWarp.Nuru.slnx", "--verify-no-changes", "--severity", "warn", "--exclude", "**/Benchmarks/**")
 .Build();

Console.WriteLine("Running ...");
Console.WriteLine(dotnetCommandResult.ToCommandString());

ExecutionResult formatResult = await dotnetCommandResult.ExecuteAsync();
formatResult.WriteToConsole();

if (!formatResult.IsSuccess)
{
  Console.WriteLine("❌ Code style violations found! Run 'dotnet format' to fix them.");
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

  Console.WriteLine("Running ...");
  Console.WriteLine(buildCommandResult.ToCommandString());

  ExecutionResult result = await buildCommandResult.ExecuteAsync();
  result.WriteToConsole();
}
catch (Exception ex)
{
  Console.WriteLine("=== Exception Details ===");
  Console.WriteLine($"Exception type: {ex.GetType().Name}");
  Console.WriteLine($"Message: {ex.Message}");

  if (ex.InnerException is not null)
  {
    Console.WriteLine($"Inner exception type: {ex.InnerException.GetType().Name}");
    Console.WriteLine($"Inner exception message: {ex.InnerException.Message}");
  }

  Console.WriteLine($"Stack trace: {ex.StackTrace}");
  Console.WriteLine("❌ Build failed with exception!");
  Environment.Exit(1);
}