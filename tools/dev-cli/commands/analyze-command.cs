// ═══════════════════════════════════════════════════════════════════════════════
// ANALYZE COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Run Roslynator analysis and fixes on all projects.

namespace DevCli.Commands;

using System.Xml.Linq;

/// <summary>
/// Run Roslynator analysis and fixes.
/// </summary>
[NuruRoute("analyze", Description = "Run Roslynator analysis and fixes")]
internal sealed class AnalyzeCommand : ICommand<Unit>
{
  [Option("diagnostic", "d", Description = "Only fix specific diagnostic ID (e.g., RCS1036)")]
  public string? Diagnostic { get; set; }

  [Option("verbose", "v", Description = "Verbose output")]
  public bool Verbose { get; set; }

  internal sealed class Handler : ICommandHandler<AnalyzeCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(AnalyzeCommand command, CancellationToken ct)
    {
      // Get repo root using Git.FindRoot
      string? repoRoot = Git.FindRoot();

      if (repoRoot is null)
      {
        throw new InvalidOperationException("Could not find git repository root (.git not found)");
      }

      // Verify we're in the right place
      if (!File.Exists(Path.Combine(repoRoot, "timewarp-nuru.slnx")))
      {
        throw new InvalidOperationException("Could not find repository root (timewarp-nuru.slnx not found)");
      }

      Terminal.WriteLine("Running Roslynator analysis and fixes...");
      Terminal.WriteLine($"Working from: {repoRoot}");

      // Read the .slnx file to get all project paths
      string slnxPath = Path.Combine(repoRoot, "timewarp-nuru.slnx");
      List<string> projects = [];

      XDocument doc = XDocument.Load(slnxPath);
      foreach (XElement project in doc.Descendants("Project"))
      {
        string? projectPath = project.Attribute("Path")?.Value;
        if (!string.IsNullOrEmpty(projectPath))
        {
          projects.Add(Path.Combine(repoRoot, projectPath));
        }
      }

      Terminal.WriteLine($"Found {projects.Count} projects to analyze");

      bool hasErrors = false;

      foreach (string project in projects)
      {
        Terminal.WriteLine($"\nAnalyzing project: {Path.GetFileName(project)}");

        List<string> roslynatorArgs = ["roslynator", "fix", project];

        if (!string.IsNullOrEmpty(command.Diagnostic))
        {
          roslynatorArgs.Add("--supported-diagnostics");
          roslynatorArgs.Add(command.Diagnostic);
          Terminal.WriteLine($"  Fixing diagnostic: {command.Diagnostic}");
        }

        CommandResult analyzeResult = Shell.Builder("dotnet")
          .WithArguments([.. roslynatorArgs])
          .Build();

        if (command.Verbose)
        {
          Terminal.WriteLine(analyzeResult.ToCommandString());
        }

        int exitCode = await analyzeResult.RunAsync();

        if (exitCode == 0)
        {
          Terminal.WriteLine("  ✅ Success");
        }
        else
        {
          Terminal.WriteLine($"  ❌ Failed with exit code {exitCode}");
          hasErrors = true;
        }
      }

      if (hasErrors)
      {
        throw new InvalidOperationException("Analysis completed with errors!");
      }

      Terminal.WriteLine("\n✅ Analysis complete!");
      return Unit.Value;
    }
  }
}
