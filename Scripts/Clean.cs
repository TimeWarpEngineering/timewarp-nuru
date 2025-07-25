#!/usr/bin/dotnet --
// Clean.cs - Clean the TimeWarp.Nuru solution

// Change to script directory for relative paths
string scriptDir = (AppContext.GetData("EntryPointFileDirectoryPath") as string)!;
Directory.SetCurrentDirectory(scriptDir);

Console.WriteLine("Cleaning TimeWarp.Nuru solution...");
Console.WriteLine($"Working from: {Directory.GetCurrentDirectory()}");

// Clean the solution with minimal verbosity
try
{
    ExecutionResult result = await DotNet.Clean()
        .WithProject("../TimeWarp.Nuru.slnx")
        .WithVerbosity("minimal")
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
    Console.WriteLine("❌ Clean failed with exception!");
    Environment.Exit(1);
}

// Also delete obj and bin directories to ensure complete cleanup
Console.WriteLine("\nDeleting obj and bin directories...");
try
{
    await Shell.Run("find")
        .WithArguments("..", "-type", "d", "(", "-name", "obj", "-o", "-name", "bin", ")")
        .Pipe("xargs", "rm", "-rf")
        .ExecuteAsync();
    Console.WriteLine("✅ Deleted all obj and bin directories");
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Could not delete some directories: {ex.Message}");
}