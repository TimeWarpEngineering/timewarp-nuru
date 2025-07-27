#!/usr/bin/dotnet --
// Build.cs - Build the TimeWarp.Nuru library
#pragma warning disable CA1014 // Mark assemblies with CLSCompliant
#pragma warning disable CA1031 // Do not catch general exception types
#pragma warning disable CA1303 // Do not pass literals as localized parameters
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

// Change to script directory for relative paths
string scriptDir = (AppContext.GetData("EntryPointFileDirectoryPath") as string)!;
Directory.SetCurrentDirectory(scriptDir);

Console.WriteLine("Building TimeWarp.Nuru library...");
Console.WriteLine($"Working from: {Directory.GetCurrentDirectory()}");

// First check code style with dotnet format
Console.WriteLine("Checking code style with dotnet format...");
ExecutionResult formatResult = await Shell.Run("dotnet")
    .WithArguments("format", "../TimeWarp.Nuru.slnx", "--verify-no-changes", "--severity", "warn", "--exclude", "**/Benchmarks/**")
    .ExecuteAsync();

if (!formatResult.IsSuccess)
{
    Console.WriteLine("❌ Code style violations found! Run 'dotnet format' to fix them.");
    formatResult.WriteToConsole();
    Environment.Exit(1);
}

// Build the solution
try
{
    ExecutionResult result = await DotNet.Build()
        .WithProject("../TimeWarp.Nuru.slnx")
        .WithConfiguration("Release")
        .WithVerbosity("normal")
        .WithNoIncremental()
        .WithNoValidation()
        .ExecuteAsync();

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