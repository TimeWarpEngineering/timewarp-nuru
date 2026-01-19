#!/usr/bin/dotnet --
// publish-dev.cs - Build AOT binary for dev CLI
//
// Publishes the dev CLI as a native AOT binary to the repository root.
// The resulting binary can be run as: ./dev <command>

// Change to script directory for relative paths
string scriptDir = (AppContext.GetData("EntryPointFileDirectoryPath") as string)!;
Directory.SetCurrentDirectory(scriptDir);

string repoRoot = Path.GetFullPath("..");
string devCliSource = Path.Combine(repoRoot, "tools", "dev-cli", "dev.cs");
string outputPath = repoRoot;

WriteLine("Publishing dev CLI as AOT binary...");
WriteLine($"Source: {devCliSource}");
WriteLine($"Output: {outputPath}/dev");

// Detect runtime identifier
string rid = GetRuntimeIdentifier();
WriteLine($"Runtime: {rid}");

// Build the AOT binary
CommandResult publishResult = DotNet.Publish()
    .WithProject(devCliSource)
    .WithConfiguration("Release")
    .WithRuntime(rid)
    .WithSelfContained()
    .WithOutput(outputPath)
    .Build();

WriteLine($"\nRunning: {publishResult.ToCommandString()}");

int exitCode = await publishResult.RunAsync();

if (exitCode != 0)
{
    WriteLine("\n❌ AOT publish failed!");
    Environment.Exit(1);
}

// Verify the binary was created
string binaryName = rid.StartsWith("win", StringComparison.OrdinalIgnoreCase) ? "dev.exe" : "dev";
string binaryPath = Path.Combine(outputPath, binaryName);

if (File.Exists(binaryPath))
{
    FileInfo info = new(binaryPath);
    WriteLine($"\n✅ AOT binary created: {binaryPath}");
    WriteLine($"   Size: {info.Length / 1024.0 / 1024.0:F1} MB");
}
else
{
    WriteLine($"\n❌ Binary not found at expected location: {binaryPath}");
    Environment.Exit(1);
}

static string GetRuntimeIdentifier()
{
    // Detect OS and architecture
    if (OperatingSystem.IsWindows())
    {
        return Environment.Is64BitOperatingSystem ? "win-x64" : "win-x86";
    }
    else if (OperatingSystem.IsMacOS())
    {
        return System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture ==
            System.Runtime.InteropServices.Architecture.Arm64 ? "osx-arm64" : "osx-x64";
    }
    else if (OperatingSystem.IsLinux())
    {
        return System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture ==
            System.Runtime.InteropServices.Architecture.Arm64 ? "linux-arm64" : "linux-x64";
    }

    // Fallback
    return "linux-x64";
}
