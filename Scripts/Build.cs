#!/usr/bin/dotnet --
// Build.cs - Build the TimeWarp.Nuru library

// Change to script directory for relative paths
string scriptDir = (AppContext.GetData("EntryPointFileDirectoryPath") as string)!;
Directory.SetCurrentDirectory(scriptDir);

Console.WriteLine("Building TimeWarp.Nuru library...");
Console.WriteLine($"Working from: {Directory.GetCurrentDirectory()}");

// Build the project with minimal verbosity to see errors
try
{
    ExecutionResult result = await DotNet.Build()
        .WithProject("../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj")
        .WithConfiguration("Release")
        .WithVerbosity("minimal")
        .WithNoValidation()
        .ExecuteAsync();

    result.WriteToConsole(); 
}
catch (Exception ex)
{
    Console.WriteLine($"=== Exception Details ===");
    Console.WriteLine($"Exception type: {ex.GetType().Name}");
    Console.WriteLine($"Message: {ex.Message}");
    
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner exception type: {ex.InnerException.GetType().Name}");
        Console.WriteLine($"Inner exception message: {ex.InnerException.Message}");
    }
    
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    Console.WriteLine("❌ Build failed with exception!");
    Environment.Exit(1);
}