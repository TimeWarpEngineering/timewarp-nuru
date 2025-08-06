#!/usr/bin/dotnet --
// CheckVersion.cs - Check if NuGet packages are already published

using System.Xml.Linq;

// Change to script directory for relative paths
string scriptDir = (AppContext.GetData("EntryPointFileDirectoryPath") as string)!;
Directory.SetCurrentDirectory(scriptDir);

// Read version from Directory.Build.props
string propsPath = "../Directory.Build.props";
var doc = XDocument.Load(propsPath);
string? version = doc.Descendants("Version").FirstOrDefault()?.Value;

if (string.IsNullOrEmpty(version))
{
    WriteLine("❌ Could not find version in Directory.Build.props");
    Environment.Exit(1);
}

WriteLine($"Checking if packages with version {version} are already published on NuGet.org...");

// Packages to check
string[] packages = ["TimeWarp.Nuru", "TimeWarp.Nuru.Analyzers"];
bool anyPublished = false;

foreach (string package in packages)
{
    WriteLine($"\nChecking {package}...");

    CommandResult searchResult = DotNet.PackageSearch(package)
        .WithExactMatch()
        .WithPrerelease()
        .WithSource("https://api.nuget.org/v3/index.json")
        .Build();

    ExecutionResult result = await searchResult.ExecuteAsync();

    // Check if the version appears in the output
    if (result.StandardOutput.Contains($"| {version} |", StringComparison.Ordinal))
    {
        WriteLine($"⚠️  WARNING: {package} {version} is already published to NuGet.org");
        anyPublished = true;
    }
    else
    {
        WriteLine($"✅ {package} {version} is not yet published on NuGet.org");
    }
}

if (anyPublished)
{
    WriteLine("\n❌ One or more packages are already published. Please increment the version in Directory.Build.props");
    Environment.Exit(1);
}

WriteLine("\n✅ All packages are ready to publish!");