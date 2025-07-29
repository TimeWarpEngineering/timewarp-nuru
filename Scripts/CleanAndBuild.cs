#!/usr/bin/dotnet --
// CleanAndBuild.cs - Clean and build the TimeWarp.Nuru solution

// Change to script directory for relative paths
string scriptDir = (AppContext.GetData("EntryPointFileDirectoryPath") as string)!;
Directory.SetCurrentDirectory(scriptDir);

WriteLine("Running Clean and Build for TimeWarp.Nuru...");
WriteLine($"Working from: {Directory.GetCurrentDirectory()}");

// Run Clean.cs
ExecutionResult cleanResult = await Shell.Run("./Clean.cs").ExecuteAsync();
if (cleanResult.ExitCode != 0)
{
    WriteLine("❌ Clean failed!");
    Environment.Exit(1);
}

// Run Build.cs
ExecutionResult buildResult = await Shell.Run("./Build.cs").ExecuteAsync();
if (buildResult.ExitCode != 0)
{
    WriteLine("❌ Build failed!");
    Environment.Exit(1);
}

WriteLine("\n✅ Clean and Build completed successfully!");