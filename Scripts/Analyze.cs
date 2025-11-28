#!/usr/bin/dotnet --
using System.Xml.Linq;

// Change to script directory for relative paths
string scriptDir = AppContext.GetData("EntryPointFileDirectoryPath") as string ?? throw new InvalidOperationException("Could not get entry point directory");
Directory.SetCurrentDirectory(scriptDir);

WriteLine("Running Roslynator analysis and fixes...");
WriteLine($"Working from: {Directory.GetCurrentDirectory()}");

// Read the .slnx file to get all project paths
const string SolutionFileName = "TimeWarp.Nuru.slnx";
string slnxPath = Path.Combine("..", SolutionFileName);
if (!File.Exists(slnxPath))
{
  WriteLine($"Error: Solution file not found at {slnxPath}");
  Environment.Exit(1);
}

List<string> projects = [];
var doc = XDocument.Load(slnxPath);
foreach (XElement project in doc.Descendants("Project"))
{
  string? projectPath = project.Attribute("Path")?.Value;
  if (!string.IsNullOrEmpty(projectPath))
  {
    projects.Add(Path.Combine("..", projectPath));
  }
}

WriteLine($"Found {projects.Count} projects to analyze");

// Get command line arguments
string[] commandArgs = Environment.GetCommandLineArgs();
string? diagnosticId = commandArgs.Length > 1 ? commandArgs[1] : null;

bool hasErrors = false;

foreach (string project in projects)
{
  WriteLine($"\nAnalyzing project: {Path.GetFileName(project)}");

  try
  {
    RunBuilder roslynatorCommand = Shell.Run("dotnet")
      .WithArguments("roslynator", "fix", project);

    if (!string.IsNullOrEmpty(diagnosticId))
    {
      roslynatorCommand = roslynatorCommand.WithArguments("--supported-diagnostics", diagnosticId);
      WriteLine($"  Fixing diagnostic: {diagnosticId}");
    }

    ExecutionResult result = await roslynatorCommand.ExecuteAsync().ConfigureAwait(false);

    if (result.ExitCode == 0)
    {
      WriteLine("  ✅ Success");
    }
    else
    {
      WriteLine($"  ❌ Failed with exit code {result.ExitCode}");
      hasErrors = true;
    }

    result.WriteToConsole();
  }
  catch (InvalidOperationException ex)
  {
    WriteLine($"  ❌ Error: {ex.Message}");
    hasErrors = true;
  }
}

WriteLine(hasErrors ? "\n❌ Analysis completed with errors!" : "\n✅ Analysis complete!");
Environment.Exit(hasErrors ? 1 : 0);