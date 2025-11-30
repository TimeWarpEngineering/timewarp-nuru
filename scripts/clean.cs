#!/usr/bin/dotnet --
// clean.cs - Clean the TimeWarp.Nuru solution

// Change to script directory for relative paths
string scriptDir = (AppContext.GetData("EntryPointFileDirectoryPath") as string)!;
Directory.SetCurrentDirectory(scriptDir);

WriteLine("Cleaning TimeWarp.Nuru solution...");
WriteLine($"Working from: {Directory.GetCurrentDirectory()}");

// Clean the solution with minimal verbosity
try
{
    ExecutionResult result = await DotNet.Clean()
        .WithProject("../timewarp-nuru.slnx")
        .WithVerbosity("minimal")
        .ExecuteAsync();

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
    WriteLine("❌ Clean failed with exception!");
    Environment.Exit(1);
}

// Also delete obj and bin directories to ensure complete cleanup
WriteLine("\nDeleting obj and bin directories...");
try
{
    await Shell.Run("find")
        .WithArguments("..", "-type", "d", "(", "-name", "obj", "-o", "-name", "bin", ")")
        .Pipe("xargs", "rm", "-rf")
        .ExecuteAsync();
    WriteLine("✅ Deleted all obj and bin directories");
}
catch (Exception ex)
{
    WriteLine($"Warning: Could not delete some directories: {ex.Message}");
}