using TimeWarp.Nuru;
using TimeWarp.Mediator;
using static System.Console;

NuruAppBuilder builder = new NuruAppBuilder()
    .AddDependencyInjection(config => config.RegisterServicesFromAssembly(typeof(StatusCommand).Assembly));

// Test 1: Basic Commands (2)
builder.AddRoute<StatusCommand>("status");
builder.AddRoute<VersionCommand>("version");

// Test 2: Sub-Commands (3)
builder.AddRoute<GitStatusCommand>("git status");
builder.AddRoute<GitCommitCommand>("git commit");
builder.AddRoute<GitPushCommand>("git push");

// Test 3: Option-Based Routing (2)
builder.AddRoute<GitCommitAmendCommand>("git commit --amend");
builder.AddRoute<GitCommitAmendNoEditCommand>("git commit --amend --no-edit");

// Test 4: Options with Values (2 - count variations)
builder.AddRoute<GitLogCommand>("git log --max-count {count:int}");

// Test 5: Docker Pass-Through (3)
builder.AddRoute<DockerRunEnhancedCommand>("docker run --enhance-logs {image}");
builder.AddRoute<DockerRunCommand>("docker run {*args}");

// Test 6: Docker Build Pass-Through (2)
builder.AddRoute<DockerBuildCommand>("docker build {*args}");
builder.AddRoute<DockerPsCommand>("docker ps {*args}");

// Test 7: kubectl Enhancement (5)
builder.AddRoute<KubectlGetEnhancedCommand>("kubectl get {resource} --watch --enhanced");
builder.AddRoute<KubectlGetWatchCommand>("kubectl get {resource} --watch");
builder.AddRoute<KubectlGetCommand>("kubectl get {resource}");
builder.AddRoute<KubectlApplyCommand>("kubectl apply -f {file}");
builder.AddRoute<KubectlCommand>("kubectl {*args}");

// Test 8: npm with Options (5)
builder.AddRoute<NpmInstallDevCommand>("npm install {package} --save-dev");
builder.AddRoute<NpmInstallSaveCommand>("npm install {package} --save");
builder.AddRoute<NpmInstallCommand>("npm install {package}");
builder.AddRoute<NpmRunCommand>("npm run {script}");
builder.AddRoute<NpmCommand>("npm {*args}");

// Test 9: Option Order Independence (4)
builder.AddRoute<GitCommitMAmendCommand>("git commit -m {message} --amend");
builder.AddRoute<GitCommitAmendMCommand>("git commit --amend -m {message}");
builder.AddRoute<GitCommitAmendMessageCommand>("git commit --amend --message {message}");
builder.AddRoute<GitCommitMessageAmendCommand>("git commit --message {message} --amend");

// Test 10: Option Aliases (2)
builder.AddRoute<GitCommitMCommand>("git commit -m {message}");
builder.AddRoute<GitCommitMessageCommand>("git commit --message {message}");

// Test 11: Async void methods
builder.AddRoute<AsyncTestCommand>("async-test");

// Test 15: Optional Parameters
builder.AddRoute<DeployCommand>("deploy {env} {tag?}");
builder.AddRoute<BackupCommand>("backup {source} {destination?}");

// Test 17: Nullable Type Parameters
builder.AddRoute<SleepCommand>("sleep {seconds:int?}");

// Test 11: Ultimate Catch-All (2)
builder.AddRoute<CatchAllCommand>("{*everything}");

// Test 12: Help command (1)
builder.AddRoute<HelpCommand>("--help");

// Build and run
NuruApp app = builder.Build();
return await app.RunAsync(args).ConfigureAwait(false);

// ========== Command Definitions with Nested Handlers (44 total) ==========

// Test 1: Basic Commands
internal sealed class StatusCommand : IRequest
{
  internal sealed class Handler : IRequestHandler<StatusCommand>
  {
    public Task Handle(StatusCommand request, CancellationToken cancellationToken)
    {
      WriteLine("✓ System is running");
      return Task.CompletedTask;
    }
  }
}

internal sealed class VersionCommand : IRequest
{
  internal sealed class Handler : IRequestHandler<VersionCommand>
  {
    public Task Handle(VersionCommand request, CancellationToken cancellationToken)
    {
      WriteLine("TimeWarp.Nuru v1.0.0");
      return Task.CompletedTask;
    }
  }
}

// Test 2: Sub-Commands
internal sealed class GitStatusCommand : IRequest
{
  internal sealed class Handler : IRequestHandler<GitStatusCommand>
  {
    public Task Handle(GitStatusCommand request, CancellationToken cancellationToken)
    {
      WriteLine("On branch main\nYour branch is up to date");
      return Task.CompletedTask;
    }
  }
}

internal sealed class GitCommitCommand : IRequest
{
  internal sealed class Handler : IRequestHandler<GitCommitCommand>
  {
    public Task Handle(GitCommitCommand request, CancellationToken cancellationToken)
    {
      WriteLine("Nothing to commit, working tree clean");
      return Task.CompletedTask;
    }
  }
}

internal sealed class GitPushCommand : IRequest
{
  internal sealed class Handler : IRequestHandler<GitPushCommand>
  {
    public Task Handle(GitPushCommand request, CancellationToken cancellationToken)
    {
      WriteLine("Everything up-to-date");
      return Task.CompletedTask;
    }
  }
}

// Test 3: Option-Based Routing
internal sealed class GitCommitAmendCommand : IRequest
{
  internal sealed class Handler : IRequestHandler<GitCommitAmendCommand>
  {
    public Task Handle(GitCommitAmendCommand request, CancellationToken cancellationToken)
    {
      WriteLine("Amending previous commit");
      return Task.CompletedTask;
    }
  }
}

internal sealed class GitCommitAmendNoEditCommand : IRequest
{
  internal sealed class Handler : IRequestHandler<GitCommitAmendNoEditCommand>
  {
    public Task Handle(GitCommitAmendNoEditCommand request, CancellationToken cancellationToken)
    {
      WriteLine("Amending without editing message");
      return Task.CompletedTask;
    }
  }
}

// Test 4: Options with Values
internal sealed class GitLogCommand : IRequest
{
  public int Count { get; set; }

  internal sealed class Handler : IRequestHandler<GitLogCommand>
  {
    public Task Handle(GitLogCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Showing last {request.Count} commits");
      return Task.CompletedTask;
    }
  }
}

// Test 5: Docker Pass-Through
internal sealed class DockerRunEnhancedCommand : IRequest
{
  public string Image { get; set; } = "";

  internal sealed class Handler : IRequestHandler<DockerRunEnhancedCommand>
  {
    public Task Handle(DockerRunEnhancedCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"🚀 Running {request.Image} with enhanced logging");
      return Task.CompletedTask;
    }
  }
}

internal sealed class DockerRunCommand : IRequest
{
  public string[] Args { get; set; } = [];

  internal sealed class Handler : IRequestHandler<DockerRunCommand>
  {
    public Task Handle(DockerRunCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"docker run {string.Join(" ", request.Args)}");
      return Task.CompletedTask;
    }
  }
}

// Test 6: Docker Build Pass-Through
internal sealed class DockerBuildCommand : IRequest
{
  public string[] Args { get; set; } = [];

  internal sealed class Handler : IRequestHandler<DockerBuildCommand>
  {
    public Task Handle(DockerBuildCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"docker build {string.Join(" ", request.Args)}");
      return Task.CompletedTask;
    }
  }
}

internal sealed class DockerPsCommand : IRequest
{
  public string[] Args { get; set; } = [];

  internal sealed class Handler : IRequestHandler<DockerPsCommand>
  {
    public Task Handle(DockerPsCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"docker ps {string.Join(" ", request.Args)}");
      return Task.CompletedTask;
    }
  }
}

internal sealed class DockerCommand : IRequest
{
  public string[] Args { get; set; } = [];

  internal sealed class Handler : IRequestHandler<DockerCommand>
  {
    public Task Handle(DockerCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"docker {string.Join(" ", request.Args)}");
      return Task.CompletedTask;
    }
  }
}

// Test 7: kubectl Enhancement
internal sealed class KubectlGetEnhancedCommand : IRequest
{
  public string Resource { get; set; } = "";

  internal sealed class Handler : IRequestHandler<KubectlGetEnhancedCommand>
  {
    public Task Handle(KubectlGetEnhancedCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"⚡ Enhanced watch for {request.Resource}");
      return Task.CompletedTask;
    }
  }
}

internal sealed class KubectlGetWatchCommand : IRequest
{
  public string Resource { get; set; } = "";

  internal sealed class Handler : IRequestHandler<KubectlGetWatchCommand>
  {
    public Task Handle(KubectlGetWatchCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Watching {request.Resource}...");
      return Task.CompletedTask;
    }
  }
}

internal sealed class KubectlGetCommand : IRequest
{
  public string Resource { get; set; } = "";

  internal sealed class Handler : IRequestHandler<KubectlGetCommand>
  {
    public Task Handle(KubectlGetCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"NAME                  READY   STATUS    RESTARTS   AGE\n{request.Resource}-sample    1/1     Running   0          5m");
      return Task.CompletedTask;
    }
  }
}

internal sealed class KubectlApplyCommand : IRequest
{
  public string File { get; set; } = "";

  internal sealed class Handler : IRequestHandler<KubectlApplyCommand>
  {
    public Task Handle(KubectlApplyCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"deployment.apps/{request.File} configured");
      return Task.CompletedTask;
    }
  }
}

internal sealed class KubectlCommand : IRequest
{
  public string[] Args { get; set; } = [];

  internal sealed class Handler : IRequestHandler<KubectlCommand>
  {
    public Task Handle(KubectlCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"kubectl {string.Join(" ", request.Args)}");
      return Task.CompletedTask;
    }
  }
}

// Test 8: npm with Options
internal sealed class NpmInstallDevCommand : IRequest
{
  public string Package { get; set; } = "";

  internal sealed class Handler : IRequestHandler<NpmInstallDevCommand>
  {
    public Task Handle(NpmInstallDevCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"📦 Installing {request.Package} as dev dependency");
      return Task.CompletedTask;
    }
  }
}

internal sealed class NpmInstallSaveCommand : IRequest
{
  public string Package { get; set; } = "";

  internal sealed class Handler : IRequestHandler<NpmInstallSaveCommand>
  {
    public Task Handle(NpmInstallSaveCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"📦 Installing {request.Package} as dependency");
      return Task.CompletedTask;
    }
  }
}

internal sealed class NpmInstallCommand : IRequest
{
  public string Package { get; set; } = "";

  internal sealed class Handler : IRequestHandler<NpmInstallCommand>
  {
    public Task Handle(NpmInstallCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"📦 Installing {request.Package}");
      return Task.CompletedTask;
    }
  }
}

internal sealed class NpmRunCommand : IRequest
{
  public string Script { get; set; } = "";

  internal sealed class Handler : IRequestHandler<NpmRunCommand>
  {
    public Task Handle(NpmRunCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"🏃 Running script: {request.Script}");
      return Task.CompletedTask;
    }
  }
}

internal sealed class NpmCommand : IRequest
{
  public string[] Args { get; set; } = [];

  internal sealed class Handler : IRequestHandler<NpmCommand>
  {
    public Task Handle(NpmCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"npm {string.Join(" ", request.Args)}");
      return Task.CompletedTask;
    }
  }
}

// Test 9: Option Order Independence
internal sealed class GitCommitMAmendCommand : IRequest
{
  public string Message { get; set; } = "";

  internal sealed class Handler : IRequestHandler<GitCommitMAmendCommand>
  {
    public Task Handle(GitCommitMAmendCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Amending with message: {request.Message}");
      return Task.CompletedTask;
    }
  }
}

internal sealed class GitCommitAmendMCommand : IRequest
{
  public string Message { get; set; } = "";

  internal sealed class Handler : IRequestHandler<GitCommitAmendMCommand>
  {
    public Task Handle(GitCommitAmendMCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Amending with message: {request.Message}");
      return Task.CompletedTask;
    }
  }
}

internal sealed class GitCommitAmendMessageCommand : IRequest
{
  public string Message { get; set; } = "";

  internal sealed class Handler : IRequestHandler<GitCommitAmendMessageCommand>
  {
    public Task Handle(GitCommitAmendMessageCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Amending with message: {request.Message}");
      return Task.CompletedTask;
    }
  }
}

internal sealed class GitCommitMessageAmendCommand : IRequest
{
  public string Message { get; set; } = "";

  internal sealed class Handler : IRequestHandler<GitCommitMessageAmendCommand>
  {
    public Task Handle(GitCommitMessageAmendCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Amending with message: {request.Message}");
      return Task.CompletedTask;
    }
  }
}

// Test 10: Option Aliases
internal sealed class GitCommitMCommand : IRequest
{
  public string Message { get; set; } = "";

  internal sealed class Handler : IRequestHandler<GitCommitMCommand>
  {
    public Task Handle(GitCommitMCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Creating commit with message: {request.Message} (using -m shorthand)");
      return Task.CompletedTask;
    }
  }
}

internal sealed class GitCommitMessageCommand : IRequest
{
  public string Message { get; set; } = "";

  internal sealed class Handler : IRequestHandler<GitCommitMessageCommand>
  {
    public Task Handle(GitCommitMessageCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Creating commit with message: {request.Message} (using --message flag)");
      return Task.CompletedTask;
    }
  }
}

// Test 11: Async void methods
internal sealed class AsyncTestCommand : IRequest
{
  internal sealed class Handler : IRequestHandler<AsyncTestCommand>
  {
    public async Task Handle(AsyncTestCommand request, CancellationToken cancellationToken)
    {
      await Task.Delay(100, cancellationToken);
      WriteLine("Async operation completed");
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
    public Task Handle(DeployCommand request, CancellationToken cancellationToken)
    {
      if (string.IsNullOrEmpty(request.Tag))
      {
        WriteLine($"Deploying to {request.Env} with latest tag");
      }
      else
      {
        WriteLine($"Deploying to {request.Env} with tag {request.Tag}");
      }

      return Task.CompletedTask;
    }
  }
}

internal sealed class BackupCommand : IRequest
{
  public string Source { get; set; } = "";
  public string? Destination { get; set; }

  internal sealed class Handler : IRequestHandler<BackupCommand>
  {
    public async Task Handle(BackupCommand request, CancellationToken cancellationToken)
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
    }
  }
}

// Test 17: Nullable Type Parameters
internal sealed class SleepCommand : IRequest
{
  public int? Seconds { get; set; }

  internal sealed class Handler : IRequestHandler<SleepCommand>
  {
    public Task Handle(SleepCommand request, CancellationToken cancellationToken)
    {
      int sleepTime = request.Seconds ?? 1;
      WriteLine($"Sleeping for {sleepTime} seconds");
      return Task.CompletedTask;
    }
  }
}

// Test 11: Ultimate Catch-All
internal sealed class CatchAllCommand : IRequest
{
  public string[] Everything { get; set; } = [];

  internal sealed class Handler : IRequestHandler<CatchAllCommand>
  {
    public Task Handle(CatchAllCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Unknown command: {string.Join(" ", request.Everything)}");
      return Task.CompletedTask;
    }
  }
}

// Help command
internal sealed class HelpCommand : IRequest
{
  internal sealed class Handler : IRequestHandler<HelpCommand>
  {
    public Task Handle(HelpCommand request, CancellationToken cancellationToken)
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
      return Task.CompletedTask;
    }
  }
}
