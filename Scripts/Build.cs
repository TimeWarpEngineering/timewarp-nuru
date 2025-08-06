#!/usr/bin/dotnet --
// Build.cs - Build the TimeWarp.Nuru library

// Change to script directory for relative paths
string scriptDir = (AppContext.GetData("EntryPointFileDirectoryPath") as string)!;
Directory.SetCurrentDirectory(scriptDir);

WriteLine("Building TimeWarp.Nuru library...");
WriteLine($"Working from: {Directory.GetCurrentDirectory()}");

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