#!/usr/bin/dotnet --
// clean-and-build.cs - Clean and build the TimeWarp.Nuru solution

// Change to script directory for relative paths
string scriptDir = (AppContext.GetData("EntryPointFileDirectoryPath") as string)!;
Directory.SetCurrentDirectory(scriptDir);

WriteLine("Running Clean and Build for TimeWarp.Nuru...");
WriteLine($"Working from: {Directory.GetCurrentDirectory()}");

// Run clean.cs
int cleanExitCode = await Shell.Builder("dotnet")
    .WithArguments("./clean.cs")
    .RunAsync();

if (cleanExitCode != 0)
{
    WriteLine("❌ Clean failed!");
    Environment.Exit(1);
}

// Run build.cs
int buildExitCode = await Shell.Builder("dotnet")
    .WithArguments("./build.cs")
    .RunAsync();

if (buildExitCode != 0)
{
    WriteLine("❌ Build failed!");
    Environment.Exit(1);
}

WriteLine("\n✅ Clean and Build completed successfully!");
