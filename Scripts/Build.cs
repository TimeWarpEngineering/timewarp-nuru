#!/usr/bin/dotnet run
// Build.cs - Build the TimeWarp.Nuru library
#pragma warning disable IDE0005 // Using directive is unnecessary
#pragma warning restore IDE0005

// Get script directory using CallerFilePath (C# equivalent of PowerShell's $PSScriptRoot)
static string GetScriptDirectory([CallerFilePath] string scriptPath = "")
{
  return Path.GetDirectoryName(scriptPath) ?? "";
}

// Push current directory, change to script directory for relative paths
string originalDirectory = Directory.GetCurrentDirectory();
string scriptDir = GetScriptDirectory();
Directory.SetCurrentDirectory(scriptDir);

Console.WriteLine("Building TimeWarp.Nuru library...");
Console.WriteLine($"Script directory: {scriptDir}");
Console.WriteLine($"Working from: {Directory.GetCurrentDirectory()}");

try
{
    try
    {
        // Build the project using simple relative path
        var buildProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build ../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj --configuration Release",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        buildProcess.Start();
        string output = await buildProcess.StandardOutput.ReadToEndAsync();
        string error = await buildProcess.StandardError.ReadToEndAsync();
        await buildProcess.WaitForExitAsync();

        Console.WriteLine(output);
        if (!string.IsNullOrEmpty(error))
        {
            Console.WriteLine($"Errors: {error}");
        }

        if (buildProcess.ExitCode == 0)
        {
            Console.WriteLine("✅ Build completed successfully!");
        }
        else
        {
            Console.WriteLine($"❌ Build failed with exit code {buildProcess.ExitCode}");
            Environment.Exit(buildProcess.ExitCode);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ An error occurred: {ex.Message}");
        Environment.Exit(1);
    }
}
finally
{
    // Pop - restore original working directory
    Directory.SetCurrentDirectory(originalDirectory);
}