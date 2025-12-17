#!/usr/bin/dotnet --
// clean-and-build.cs - Clean and build the TimeWarp.Nuru solution

// Change to script directory for relative paths
string scriptDir = (AppContext.GetData("EntryPointFileDirectoryPath") as string)!;
Directory.SetCurrentDirectory(scriptDir);

WriteLine("Running Clean and Build for TimeWarp.Nuru...");
WriteLine($"Working from: {Directory.GetCurrentDirectory()}");

// Run clean.cs
ExecutionResult cleanResult = await Shell.Run("./clean.cs").ExecuteAsync();
if (cleanResult.ExitCode != 0)
{
    WriteLine("❌ Clean failed!");
    Environment.Exit(1);
}

// Run build.cs
ExecutionResult buildResult = await Shell.Run("./build.cs").ExecuteAsync();
if (buildResult.ExitCode != 0)
{
    WriteLine("❌ Build failed!");
    Environment.Exit(1);
}

WriteLine("\n✅ Clean and Build completed successfully!");