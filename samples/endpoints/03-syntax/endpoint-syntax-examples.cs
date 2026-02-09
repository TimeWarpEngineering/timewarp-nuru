#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - SYNTAX EXAMPLES ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
// This file demonstrates all route pattern syntax using the Endpoint DSL.
//
// DSL: Endpoint (class-based with [NuruRoute], [Parameter], [Option] attributes)
//
// IMPORTANT: This file is used by the TimeWarp.Nuru MCP Server
// ============================================================================
// The MCP server extracts code snippets to provide syntax documentation
// to AI assistants and IDE integrations.
//
// This file MUST compile successfully - it serves as both documentation
// and validation that all syntax examples are correct and working.
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

await app.RunAsync(args);

// =============================================================================
// LITERALS - Plain text segments that must match exactly
// =============================================================================

[NuruRoute("status", Description = "Check system status")]
public sealed class StatusQuery : IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<StatusQuery, Unit>
  {
    public ValueTask<Unit> Handle(StatusQuery query, CancellationToken ct)
    {
      Console.WriteLine("OK");
      return default;
    }
  }
}

[NuruRoute("git commit", Description = "Commit changes")]
public sealed class GitCommitCommand : ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<GitCommitCommand, Unit>
  {
    public ValueTask<Unit> Handle(GitCommitCommand command, CancellationToken ct)
    {
      Console.WriteLine("Committing...");
      return default;
    }
  }
}

[NuruRoute("version", Description = "Show version information")]
public sealed class VersionQuery : IQuery<string>
{
  public sealed class Handler : IQueryHandler<VersionQuery, string>
  {
    public ValueTask<string> Handle(VersionQuery query, CancellationToken ct)
    {
      return new ValueTask<string>("1.0.0");
    }
  }
}

// =============================================================================
// PARAMETERS - Capture values from the command line
// =============================================================================

[NuruRoute("greet", Description = "Greet someone by name")]
public sealed class GreetQuery : IQuery<Unit>
{
  [Parameter(Description = "Name of the person to greet")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler : IQueryHandler<GreetQuery, Unit>
  {
    public ValueTask<Unit> Handle(GreetQuery query, CancellationToken ct)
    {
      Console.WriteLine($"Hello {query.Name}");
      return default;
    }
  }
}

[NuruRoute("copy", Description = "Copy a file from source to destination")]
public sealed class CopyCommand : ICommand<Unit>
{
  [Parameter(Description = "Source file path")]
  public string Source { get; set; } = string.Empty;

  [Parameter(Description = "Destination file path")]
  public string Destination { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<CopyCommand, Unit>
  {
    public ValueTask<Unit> Handle(CopyCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Copying {command.Source} to {command.Destination}");
      return default;
    }
  }
}

// =============================================================================
// TYPED PARAMETERS - Type constraints using built-in converters
// =============================================================================

[NuruRoute("delay", Description = "Delay for specified milliseconds")]
public sealed class DelayCommand : ICommand<Unit>
{
  [Parameter(Description = "Milliseconds to delay")]
  public int Ms { get; set; }

  public sealed class Handler : ICommandHandler<DelayCommand, Unit>
  {
    public async ValueTask<Unit> Handle(DelayCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Delaying {command.Ms}ms");
      await Task.Delay(command.Ms, ct);
      return default;
    }
  }
}

[NuruRoute("price", Description = "Show price with formatting")]
public sealed class PriceQuery : IQuery<Unit>
{
  [Parameter(Description = "Amount to display")]
  public double Amount { get; set; }

  public sealed class Handler : IQueryHandler<PriceQuery, Unit>
  {
    public ValueTask<Unit> Handle(PriceQuery query, CancellationToken ct)
    {
      Console.WriteLine($"Price: ${query.Amount:F2}");
      return default;
    }
  }
}

[NuruRoute("enabled", Description = "Check if feature is enabled")]
public sealed class EnabledQuery : IQuery<Unit>
{
  [Parameter(Description = "Enable flag")]
  public bool Flag { get; set; }

  public sealed class Handler : IQueryHandler<EnabledQuery, Unit>
  {
    public ValueTask<Unit> Handle(EnabledQuery query, CancellationToken ct)
    {
      Console.WriteLine($"Enabled: {query.Flag}");
      return default;
    }
  }
}

[NuruRoute("schedule", Description = "Schedule an event")]
public sealed class ScheduleCommand : ICommand<Unit>
{
  [Parameter(Description = "Date and time to schedule")]
  public DateTime Date { get; set; }

  public sealed class Handler : ICommandHandler<ScheduleCommand, Unit>
  {
    public ValueTask<Unit> Handle(ScheduleCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Scheduled for {command.Date}");
      return default;
    }
  }
}

[NuruRoute("fetch", Description = "Fetch a URL")]
public sealed class FetchQuery : IQuery<Unit>
{
  [Parameter(Description = "URL to fetch")]
  public Uri Url { get; set; } = new Uri("http://localhost");

  public sealed class Handler : IQueryHandler<FetchQuery, Unit>
  {
    public ValueTask<Unit> Handle(FetchQuery query, CancellationToken ct)
    {
      Console.WriteLine($"Fetching {query.Url}");
      return default;
    }
  }
}

[NuruRoute("read", Description = "Read a file")]
public sealed class ReadQuery : IQuery<Unit>
{
  [Parameter(Description = "File to read")]
  public FileInfo File { get; set; } = new FileInfo(".");

  public sealed class Handler : IQueryHandler<ReadQuery, Unit>
  {
    public ValueTask<Unit> Handle(ReadQuery query, CancellationToken ct)
    {
      Console.WriteLine($"Reading {query.File.FullName}");
      return default;
    }
  }
}

[NuruRoute("list", Description = "List directory contents")]
public sealed class ListQuery : IQuery<Unit>
{
  [Parameter(Description = "Directory to list")]
  public DirectoryInfo Dir { get; set; } = new DirectoryInfo(".");

  public sealed class Handler : IQueryHandler<ListQuery, Unit>
  {
    public ValueTask<Unit> Handle(ListQuery query, CancellationToken ct)
    {
      Console.WriteLine($"Listing {query.Dir.FullName}");
      return default;
    }
  }
}

[NuruRoute("connect", Description = "Connect to an IP address")]
public sealed class ConnectCommand : ICommand<Unit>
{
  [Parameter(Description = "IP address to connect to")]
  public System.Net.IPAddress Addr { get; set; } = System.Net.IPAddress.Loopback;

  public sealed class Handler : ICommandHandler<ConnectCommand, Unit>
  {
    public ValueTask<Unit> Handle(ConnectCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Connecting to {command.Addr}");
      return default;
    }
  }
}

[NuruRoute("report", Description = "Generate a report for a specific date")]
public sealed class ReportQuery : IQuery<Unit>
{
  [Parameter(Description = "Date for the report")]
  public DateOnly Date { get; set; }

  public sealed class Handler : IQueryHandler<ReportQuery, Unit>
  {
    public ValueTask<Unit> Handle(ReportQuery query, CancellationToken ct)
    {
      Console.WriteLine($"Report for {query.Date}");
      return default;
    }
  }
}

[NuruRoute("alarm", Description = "Set an alarm")]
public sealed class AlarmCommand : ICommand<Unit>
{
  [Parameter(Description = "Time for the alarm")]
  public TimeOnly Time { get; set; }

  public sealed class Handler : ICommandHandler<AlarmCommand, Unit>
  {
    public ValueTask<Unit> Handle(AlarmCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Alarm set for {command.Time}");
      return default;
    }
  }
}

// =============================================================================
// OPTIONAL PARAMETERS - Make parameters optional with nullable types
// =============================================================================

[NuruRoute("deploy", Description = "Deploy to an environment")]
public sealed class DeployCommand : ICommand<Unit>
{
  [Parameter(Description = "Target environment")]
  public string Env { get; set; } = string.Empty;

  [Parameter(IsOptional = true, Description = "Optional tag to deploy")]
  public string? Tag { get; set; }

  public sealed class Handler : ICommandHandler<DeployCommand, Unit>
  {
    public ValueTask<Unit> Handle(DeployCommand command, CancellationToken ct)
    {
      string message = $"Deploying to {command.Env}";
      if (!string.IsNullOrEmpty(command.Tag))
      {
        message += $" with tag {command.Tag}";
      }
      Console.WriteLine(message);
      return default;
    }
  }
}

[NuruRoute("wait", Description = "Wait for specified seconds")]
public sealed class WaitCommand : ICommand<Unit>
{
  [Parameter(IsOptional = true, Description = "Seconds to wait (optional)")]
  public int? Seconds { get; set; }

  public sealed class Handler : ICommandHandler<WaitCommand, Unit>
  {
    public async ValueTask<Unit> Handle(WaitCommand command, CancellationToken ct)
    {
      int seconds = command.Seconds ?? 5;
      Console.WriteLine($"Waiting {seconds} seconds");
      await Task.Delay(seconds * 1000, ct);
      return default;
    }
  }
}

[NuruRoute("backup", Description = "Backup a source directory")]
public sealed class BackupCommand : ICommand<Unit>
{
  [Parameter(Description = "Source directory to backup")]
  public string Source { get; set; } = string.Empty;

  [Parameter(IsOptional = true, Description = "Optional destination path")]
  public string? Destination { get; set; }

  public sealed class Handler : ICommandHandler<BackupCommand, Unit>
  {
    public ValueTask<Unit> Handle(BackupCommand command, CancellationToken ct)
    {
      string dest = command.Destination ?? "default location";
      Console.WriteLine($"Backing up {command.Source} to {dest}");
      return default;
    }
  }
}

// =============================================================================
// CATCH-ALL PARAMETERS - Capture all remaining arguments
// =============================================================================

[NuruRoute("docker", Description = "Run docker command with arbitrary arguments")]
public sealed class DockerCommand : ICommand<Unit>
{
  [Parameter(IsCatchAll = true, Description = "Docker arguments")]
  public string[] Args { get; set; } = [];

  public sealed class Handler : ICommandHandler<DockerCommand, Unit>
  {
    public ValueTask<Unit> Handle(DockerCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Docker args: {string.Join(" ", command.Args)}");
      return default;
    }
  }
}

[NuruRoute("run", Description = "Run a script with parameters")]
public sealed class RunCommand : ICommand<Unit>
{
  [Parameter(Description = "Script to run")]
  public string Script { get; set; } = string.Empty;

  [Parameter(IsCatchAll = true, Description = "Script parameters")]
  public string[] Params { get; set; } = [];

  public sealed class Handler : ICommandHandler<RunCommand, Unit>
  {
    public ValueTask<Unit> Handle(RunCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Running {command.Script} with {command.Params.Length} parameters");
      return default;
    }
  }
}

// =============================================================================
// OPTIONS - Boolean flags and value options
// =============================================================================

[NuruRoute("build", Description = "Build the project")]
public sealed class BuildCommand : ICommand<Unit>
{
  [Option("verbose", "v", Description = "Enable verbose output")]
  public bool Verbose { get; set; }

  public sealed class Handler : ICommandHandler<BuildCommand, Unit>
  {
    public ValueTask<Unit> Handle(BuildCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Building (verbose: {command.Verbose})");
      return default;
    }
  }
}

[NuruRoute("build-config", Description = "Build with configuration mode")]
public sealed class BuildConfigCommand : ICommand<Unit>
{
  [Option("mode", "m", Description = "Build configuration mode")]
  public string Mode { get; set; } = "Debug";

  public sealed class Handler : ICommandHandler<BuildConfigCommand, Unit>
  {
    public ValueTask<Unit> Handle(BuildConfigCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Building in {command.Mode} mode");
      return default;
    }
  }
}

[NuruRoute("deploy-full", Description = "Deploy with dry-run and force options")]
public sealed class DeployFullCommand : ICommand<Unit>
{
  [Parameter(Description = "Target environment")]
  public string Env { get; set; } = string.Empty;

  [Option("dry-run", "d", Description = "Preview changes without applying")]
  public bool DryRun { get; set; }

  [Option("force", "f", Description = "Force deployment without confirmation")]
  public bool Force { get; set; }

  public sealed class Handler : ICommandHandler<DeployFullCommand, Unit>
  {
    public ValueTask<Unit> Handle(DeployFullCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Deploy to {command.Env} (dry-run: {command.DryRun}, force: {command.Force})");
      return default;
    }
  }
}

[NuruRoute("docker-env", Description = "Docker command with environment variables")]
public sealed class DockerEnvCommand : ICommand<Unit>
{
  [Option("env", "e", Description = "Environment variables", IsRepeatable = true)]
  public string[] Var { get; set; } = [];

  public sealed class Handler : ICommandHandler<DockerEnvCommand, Unit>
  {
    public ValueTask<Unit> Handle(DockerEnvCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Environment variables: {string.Join(", ", command.Var)}");
      return default;
    }
  }
}

// =============================================================================
// COMPLEX EXAMPLES - Real-world patterns combining multiple features
// =============================================================================

[NuruRoute("git", Description = "Git commit with message and options")]
public sealed class GitFullCommand : ICommand<Unit>
{
  [Parameter(Description = "Commit message")]
  public string Message { get; set; } = string.Empty;

  [Option("amend", "a", Description = "Amend previous commit")]
  public bool Amend { get; set; }

  [Option("no-verify", "n", Description = "Bypass pre-commit hooks")]
  public bool NoVerify { get; set; }

  public sealed class Handler : ICommandHandler<GitFullCommand, Unit>
  {
    public ValueTask<Unit> Handle(GitFullCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Commit: {command.Message} (amend: {command.Amend}, no-verify: {command.NoVerify})");
      return default;
    }
  }
}

[NuruRoute("docker-run", Description = "Run docker container")]
public sealed class DockerRunCommand : ICommand<Unit>
{
  [Option("env", "e", Description = "Environment variables", IsRepeatable = true)]
  public string[] E { get; set; } = [];

  [Option("port", "p", Description = "Port mappings", IsRepeatable = true)]
  public int[] Port { get; set; } = [];

  [Parameter(Description = "Container image")]
  public string Image { get; set; } = string.Empty;

  [Parameter(IsCatchAll = true, Description = "Container command")]
  public string[] Cmd { get; set; } = [];

  public sealed class Handler : ICommandHandler<DockerRunCommand, Unit>
  {
    public ValueTask<Unit> Handle(DockerRunCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Running {command.Image} with {command.E.Length} env vars, {command.Port.Length} ports");
      return default;
    }
  }
}

[NuruRoute("kubectl", Description = "Kubectl get command")]
public sealed class KubectlQuery : IQuery<Unit>
{
  [Parameter(Description = "Resource type")]
  public string Resource { get; set; } = string.Empty;

  [Option("namespace", "n", Description = "Target namespace")]
  public string? Ns { get; set; }

  [Option("output", "o", Description = "Output format")]
  public string? Format { get; set; }

  public sealed class Handler : IQueryHandler<KubectlQuery, Unit>
  {
    public ValueTask<Unit> Handle(KubectlQuery query, CancellationToken ct)
    {
      Console.WriteLine($"Get {query.Resource} in namespace {query.Ns ?? "default"}");
      return default;
    }
  }
}
