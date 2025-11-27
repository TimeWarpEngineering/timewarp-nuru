using Mediator;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Nuru;
using static System.Console;

NuruAppBuilder builder = new NuruAppBuilder()
    .AddDependencyInjection()
    .ConfigureServices(services => services.AddMediator());

// Test 1: Basic Commands (2)
builder.Map<StatusCommand>("status");
builder.Map<VersionCommand>("version");

// Test 2: Sub-Commands (3)
builder.Map<GitStatusCommand>("git status");
builder.Map<GitCommitCommand>("git commit");
builder.Map<GitPushCommand>("git push");

// Test 3: Option-Based Routing (2)
builder.Map<GitCommitAmendCommand>("git commit --amend");
builder.Map<GitCommitAmendNoEditCommand>("git commit --amend --no-edit");

// Test 4: Options with Values (2 - count variations)
builder.Map<GitLogCommand>("git log --max-count {count:int}");

// Test 5: Docker Pass-Through (3)
builder.Map<DockerRunEnhancedCommand>("docker run --enhance-logs {image}");
builder.Map<DockerRunCommand>("docker run {*args}");

// Test 6: Docker Build Pass-Through (2)
builder.Map<DockerBuildCommand>("docker build {*args}");
builder.Map<DockerPsCommand>("docker ps {*args}");

// Test 7: kubectl Enhancement (5)
builder.Map<KubectlGetEnhancedCommand>("kubectl get {resource} --watch --enhanced");
builder.Map<KubectlGetWatchCommand>("kubectl get {resource} --watch");
builder.Map<KubectlGetCommand>("kubectl get {resource}");
builder.Map<KubectlApplyCommand>("kubectl apply -f {file}");
builder.Map<KubectlCommand>("kubectl {*args}");

// Test 8: npm with Options (5)
builder.Map<NpmInstallDevCommand>("npm install {package} --save-dev");
builder.Map<NpmInstallSaveCommand>("npm install {package} --save");
builder.Map<NpmInstallCommand>("npm install {package}");
builder.Map<NpmRunCommand>("npm run {script}");
builder.Map<NpmCommand>("npm {*args}");

// Test 9: Option Order Independence (4)
builder.Map<GitCommitMAmendCommand>("git commit -m {message} --amend");
builder.Map<GitCommitAmendMCommand>("git commit --amend -m {message}");
builder.Map<GitCommitAmendMessageCommand>("git commit --amend --message {message}");
builder.Map<GitCommitMessageAmendCommand>("git commit --message {message} --amend");

// Test 10: Option Aliases (2)
builder.Map<GitCommitMCommand>("git commit -m {message}");
builder.Map<GitCommitMessageCommand>("git commit --message {message}");

// Test 11: Async void methods
builder.Map<AsyncTestCommand>("async-test");

// Test 15: Optional Parameters
builder.Map<DeployCommand>("deploy {env} {tag?}");
builder.Map<BackupCommand>("backup {source} {destination?}");

// Test 17: Nullable Type Parameters
builder.Map<SleepCommand>("sleep {seconds:int?}");

// Test 11: Ultimate Catch-All (2)
builder.Map<CatchAllCommand>("{*everything}");

// Test 12: Help command (1)
builder.Map<HelpCommand>("--help");

// Build and run
NuruApp app = builder.Build();
return await app.RunAsync(args).ConfigureAwait(false);

// ========== Command Definitions with Nested Handlers (44 total) ==========

// Test 1: Basic Commands
internal sealed class StatusCommand : IRequest
{
  internal sealed class Handler : IRequestHandler<StatusCommand>
  {
    public ValueTask<Unit> Handle(StatusCommand request, CancellationToken cancellationToken)
    {
      WriteLine("✓ System is running");
      return default;
    }
  }
}

internal sealed class VersionCommand : IRequest
{
  internal sealed class Handler : IRequestHandler<VersionCommand>
  {
    public ValueTask<Unit> Handle(VersionCommand request, CancellationToken cancellationToken)
    {
      WriteLine("TimeWarp.Nuru v1.0.0");
      return default;
    }
  }
}

// Test 2: Sub-Commands
internal sealed class GitStatusCommand : IRequest
{
  internal sealed class Handler : IRequestHandler<GitStatusCommand>
  {
    public ValueTask<Unit> Handle(GitStatusCommand request, CancellationToken cancellationToken)
    {
      WriteLine("On branch main\nYour branch is up to date");
      return default;
    }
  }
}

internal sealed class GitCommitCommand : IRequest
{
  internal sealed class Handler : IRequestHandler<GitCommitCommand>
  {
    public ValueTask<Unit> Handle(GitCommitCommand request, CancellationToken cancellationToken)
    {
      WriteLine("Nothing to commit, working tree clean");
      return default;
    }
  }
}

internal sealed class GitPushCommand : IRequest
{
  internal sealed class Handler : IRequestHandler<GitPushCommand>
  {
    public ValueTask<Unit> Handle(GitPushCommand request, CancellationToken cancellationToken)
    {
      WriteLine("Everything up-to-date");
      return default;
    }
  }
}

// Test 3: Option-Based Routing
internal sealed class GitCommitAmendCommand : IRequest
{
  internal sealed class Handler : IRequestHandler<GitCommitAmendCommand>
  {
    public ValueTask<Unit> Handle(GitCommitAmendCommand request, CancellationToken cancellationToken)
    {
      WriteLine("Amending previous commit");
      return default;
    }
  }
}

internal sealed class GitCommitAmendNoEditCommand : IRequest
{
  internal sealed class Handler : IRequestHandler<GitCommitAmendNoEditCommand>
  {
    public ValueTask<Unit> Handle(GitCommitAmendNoEditCommand request, CancellationToken cancellationToken)
    {
      WriteLine("Amending without editing message");
      return default;
    }
  }
}

// Test 4: Options with Values
internal sealed class GitLogCommand : IRequest
{
  public int Count { get; set; }

  internal sealed class Handler : IRequestHandler<GitLogCommand>
  {
    public ValueTask<Unit> Handle(GitLogCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Showing last {request.Count} commits");
      return default;
    }
  }
}

// Test 5: Docker Pass-Through
internal sealed class DockerRunEnhancedCommand : IRequest
{
  public string Image { get; set; } = "";

  internal sealed class Handler : IRequestHandler<DockerRunEnhancedCommand>
  {
    public ValueTask<Unit> Handle(DockerRunEnhancedCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"🚀 Running {request.Image} with enhanced logging");
      return default;
    }
  }
}

internal sealed class DockerRunCommand : IRequest
{
  public string[] Args { get; set; } = [];

  internal sealed class Handler : IRequestHandler<DockerRunCommand>
  {
    public ValueTask<Unit> Handle(DockerRunCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"docker run {string.Join(" ", request.Args)}");
      return default;
    }
  }
}

// Test 6: Docker Build Pass-Through
internal sealed class DockerBuildCommand : IRequest
{
  public string[] Args { get; set; } = [];

  internal sealed class Handler : IRequestHandler<DockerBuildCommand>
  {
    public ValueTask<Unit> Handle(DockerBuildCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"docker build {string.Join(" ", request.Args)}");
      return default;
    }
  }
}

internal sealed class DockerPsCommand : IRequest
{
  public string[] Args { get; set; } = [];

  internal sealed class Handler : IRequestHandler<DockerPsCommand>
  {
    public ValueTask<Unit> Handle(DockerPsCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"docker ps {string.Join(" ", request.Args)}");
      return default;
    }
  }
}

internal sealed class DockerCommand : IRequest
{
  public string[] Args { get; set; } = [];

  internal sealed class Handler : IRequestHandler<DockerCommand>
  {
    public ValueTask<Unit> Handle(DockerCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"docker {string.Join(" ", request.Args)}");
      return default;
    }
  }
}

// Test 7: kubectl Enhancement
internal sealed class KubectlGetEnhancedCommand : IRequest
{
  public string Resource { get; set; } = "";

  internal sealed class Handler : IRequestHandler<KubectlGetEnhancedCommand>
  {
    public ValueTask<Unit> Handle(KubectlGetEnhancedCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"⚡ Enhanced watch for {request.Resource}");
      return default;
    }
  }
}

internal sealed class KubectlGetWatchCommand : IRequest
{
  public string Resource { get; set; } = "";

  internal sealed class Handler : IRequestHandler<KubectlGetWatchCommand>
  {
    public ValueTask<Unit> Handle(KubectlGetWatchCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Watching {request.Resource}...");
      return default;
    }
  }
}

internal sealed class KubectlGetCommand : IRequest
{
  public string Resource { get; set; } = "";

  internal sealed class Handler : IRequestHandler<KubectlGetCommand>
  {
    public ValueTask<Unit> Handle(KubectlGetCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"NAME                  READY   STATUS    RESTARTS   AGE\n{request.Resource}-sample    1/1     Running   0          5m");
      return default;
    }
  }
}

internal sealed class KubectlApplyCommand : IRequest
{
  public string File { get; set; } = "";

  internal sealed class Handler : IRequestHandler<KubectlApplyCommand>
  {
    public ValueTask<Unit> Handle(KubectlApplyCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"deployment.apps/{request.File} configured");
      return default;
    }
  }
}

internal sealed class KubectlCommand : IRequest
{
  public string[] Args { get; set; } = [];

  internal sealed class Handler : IRequestHandler<KubectlCommand>
  {
    public ValueTask<Unit> Handle(KubectlCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"kubectl {string.Join(" ", request.Args)}");
      return default;
    }
  }
}

// Test 8: npm with Options
internal sealed class NpmInstallDevCommand : IRequest
{
  public string Package { get; set; } = "";

  internal sealed class Handler : IRequestHandler<NpmInstallDevCommand>
  {
    public ValueTask<Unit> Handle(NpmInstallDevCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"📦 Installing {request.Package} as dev dependency");
      return default;
    }
  }
}

internal sealed class NpmInstallSaveCommand : IRequest
{
  public string Package { get; set; } = "";

  internal sealed class Handler : IRequestHandler<NpmInstallSaveCommand>
  {
    public ValueTask<Unit> Handle(NpmInstallSaveCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"📦 Installing {request.Package} as dependency");
      return default;
    }
  }
}

internal sealed class NpmInstallCommand : IRequest
{
  public string Package { get; set; } = "";

  internal sealed class Handler : IRequestHandler<NpmInstallCommand>
  {
    public ValueTask<Unit> Handle(NpmInstallCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"📦 Installing {request.Package}");
      return default;
    }
  }
}

internal sealed class NpmRunCommand : IRequest
{
  public string Script { get; set; } = "";

  internal sealed class Handler : IRequestHandler<NpmRunCommand>
  {
    public ValueTask<Unit> Handle(NpmRunCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"🏃 Running script: {request.Script}");
      return default;
    }
  }
}

internal sealed class NpmCommand : IRequest
{
  public string[] Args { get; set; } = [];

  internal sealed class Handler : IRequestHandler<NpmCommand>
  {
    public ValueTask<Unit> Handle(NpmCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"npm {string.Join(" ", request.Args)}");
      return default;
    }
  }
}

// Test 9: Option Order Independence
internal sealed class GitCommitMAmendCommand : IRequest
{
  public string Message { get; set; } = "";

  internal sealed class Handler : IRequestHandler<GitCommitMAmendCommand>
  {
    public ValueTask<Unit> Handle(GitCommitMAmendCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Amending with message: {request.Message}");
      return default;
    }
  }
}

internal sealed class GitCommitAmendMCommand : IRequest
{
  public string Message { get; set; } = "";

  internal sealed class Handler : IRequestHandler<GitCommitAmendMCommand>
  {
    public ValueTask<Unit> Handle(GitCommitAmendMCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Amending with message: {request.Message}");
      return default;
    }
  }
}

internal sealed class GitCommitAmendMessageCommand : IRequest
{
  public string Message { get; set; } = "";

  internal sealed class Handler : IRequestHandler<GitCommitAmendMessageCommand>
  {
    public ValueTask<Unit> Handle(GitCommitAmendMessageCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Amending with message: {request.Message}");
      return default;
    }
  }
}

internal sealed class GitCommitMessageAmendCommand : IRequest
{
  public string Message { get; set; } = "";

  internal sealed class Handler : IRequestHandler<GitCommitMessageAmendCommand>
  {
    public ValueTask<Unit> Handle(GitCommitMessageAmendCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Amending with message: {request.Message}");
      return default;
    }
  }
}

// Test 10: Option Aliases
internal sealed class GitCommitMCommand : IRequest
{
  public string Message { get; set; } = "";

  internal sealed class Handler : IRequestHandler<GitCommitMCommand>
  {
    public ValueTask<Unit> Handle(GitCommitMCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Creating commit with message: {request.Message} (using -m shorthand)");
      return default;
    }
  }
}

internal sealed class GitCommitMessageCommand : IRequest
{
  public string Message { get; set; } = "";

  internal sealed class Handler : IRequestHandler<GitCommitMessageCommand>
  {
    public ValueTask<Unit> Handle(GitCommitMessageCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Creating commit with message: {request.Message} (using --message flag)");
      return default;
    }
  }
}

// Test 11: Async void methods
internal sealed class AsyncTestCommand : IRequest
{
  internal sealed class Handler : IRequestHandler<AsyncTestCommand>
  {
    public async ValueTask<Unit> Handle(AsyncTestCommand request, CancellationToken cancellationToken)
    {
      await Task.Delay(100, cancellationToken);
      WriteLine("Async operation completed");
      return Unit.Value;
    }
  }
}

// Test 15: Optional Parameters
internal sealed class DeployCommand : IRequest
{
  public string Env { get; set; } = "";
  public string? Tag { get; set; }

  internal sealed class Handler : IRequestHandler<DeployCommand>
  {
    public ValueTask<Unit> Handle(DeployCommand request, CancellationToken cancellationToken)
    {
      if (string.IsNullOrEmpty(request.Tag))
      {
        WriteLine($"Deploying to {request.Env} with latest tag");
      }
      else
      {
        WriteLine($"Deploying to {request.Env} with tag {request.Tag}");
      }

      return default;
    }
  }
}

internal sealed class BackupCommand : IRequest
{
  public string Source { get; set; } = "";
  public string? Destination { get; set; }

  internal sealed class Handler : IRequestHandler<BackupCommand>
  {
    public async ValueTask<Unit> Handle(BackupCommand request, CancellationToken cancellationToken)
    {
      await Task.Delay(50, cancellationToken);
      if (string.IsNullOrEmpty(request.Destination))
      {
        WriteLine($"Backing up {request.Source} to default location");
      }
      else
      {
        WriteLine($"Backing up {request.Source} to {request.Destination}");
      }
      return Unit.Value;
    }
  }
}

// Test 17: Nullable Type Parameters
internal sealed class SleepCommand : IRequest
{
  public int? Seconds { get; set; }

  internal sealed class Handler : IRequestHandler<SleepCommand>
  {
    public ValueTask<Unit> Handle(SleepCommand request, CancellationToken cancellationToken)
    {
      int sleepTime = request.Seconds ?? 1;
      WriteLine($"Sleeping for {sleepTime} seconds");
      return default;
    }
  }
}

// Test 11: Ultimate Catch-All
internal sealed class CatchAllCommand : IRequest
{
  public string[] Everything { get; set; } = [];

  internal sealed class Handler : IRequestHandler<CatchAllCommand>
  {
    public ValueTask<Unit> Handle(CatchAllCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Unknown command: {string.Join(" ", request.Everything)}");
      return default;
    }
  }
}

// Help command
internal sealed class HelpCommand : IRequest
{
  internal sealed class Handler : IRequestHandler<HelpCommand>
  {
    public ValueTask<Unit> Handle(HelpCommand request, CancellationToken cancellationToken)
    {
      WriteLine("TimeWarp.Nuru Integration Tests");
      WriteLine("==================================");
      WriteLine("Available test scenarios:");
      WriteLine("  status                          - Basic status command");
      WriteLine("  version                         - Show version");
      WriteLine("  git status                      - Git status");
      WriteLine("  git commit [options]            - Git commit with various options");
      WriteLine("  docker run [options] {image}    - Docker run with enhancements");
      WriteLine("  kubectl get {resource}          - Kubectl commands");
      WriteLine("  npm install {package} [options] - NPM commands");
      WriteLine("  --help                          - Show this help");
      return default;
    }
  }
}
