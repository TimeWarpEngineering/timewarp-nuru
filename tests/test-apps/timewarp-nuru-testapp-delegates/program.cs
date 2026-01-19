global using static System.Console;
using TimeWarp.Nuru;

NuruAppBuilder builder = NuruApp.CreateBuilder(args);

// Test 1: Basic Commands
builder.Map("status").WithHandler(() => WriteLine("✓ System is running")).AsQuery().Done();
builder.Map("version").WithHandler(() => WriteLine("TimeWarp.Nuru v1.0.0")).AsQuery().Done();

// Test 2: Sub-Commands
builder.Map("git status").WithHandler(() => WriteLine("On branch main\nYour branch is up to date")).AsQuery().Done();
builder.Map("git commit").WithHandler(() => WriteLine("Nothing to commit, working tree clean")).AsCommand().Done();
builder.Map("git push").WithHandler(() => WriteLine("Everything up-to-date")).AsCommand().Done();

// Test 3: Option-Based Routing
builder.Map("git commit --amend").WithHandler(() => WriteLine("Amending previous commit")).AsCommand().Done();
builder.Map("git commit --amend --no-edit").WithHandler(() => WriteLine("Amending without editing message")).AsCommand().Done();

// Test 4: Options with Values
builder.Map("git log --max-count {count:int}")
    .WithHandler((int count) => WriteLine($"Showing last {count} commits"))
    .AsQuery().Done();

// Test 5: Docker Pass-Through
builder.Map("docker run --enhance-logs {image}")
    .WithHandler((string image) => WriteLine($"🚀 Running {image} with enhanced logging"))
    .AsCommand().Done();
builder.Map("docker run {*args}")
    .WithHandler((string[] args) => WriteLine($"docker run {string.Join(" ", args)}"))
    .AsCommand().Done();

// Test 6: Docker Build Pass-Through
builder.Map("docker build {*args}")
    .WithHandler((string[] args) => WriteLine($"docker build {string.Join(" ", args)}"))
    .AsCommand().Done();
builder.Map("docker ps {*args}")
    .WithHandler((string[] args) => WriteLine($"docker ps {string.Join(" ", args)}"))
    .AsQuery().Done();
builder.Map("docker {*args}")
    .WithHandler((string[] args) => WriteLine($"docker {string.Join(" ", args)}"))
    .AsCommand().Done();

// Test 7: kubectl Enhancement
builder.Map("kubectl get {resource} --watch --enhanced")
    .WithHandler((string resource) => WriteLine($"⚡ Enhanced watch for {resource}"))
    .AsQuery().Done();
builder.Map("kubectl get {resource} --watch")
    .WithHandler((string resource) => WriteLine($"Watching {resource}..."))
    .AsQuery().Done();
builder.Map("kubectl get {resource}")
    .WithHandler((string resource) => WriteLine($"NAME                  READY   STATUS    RESTARTS   AGE\n{resource}-sample    1/1     Running   0          5m"))
    .AsQuery().Done();
builder.Map("kubectl apply -f {file}")
    .WithHandler((string file) => WriteLine($"deployment.apps/{file} configured"))
    .AsCommand().Done();
builder.Map("kubectl {*args}")
    .WithHandler((string[] args) => WriteLine($"kubectl {string.Join(" ", args)}"))
    .AsCommand().Done();

// Test 8: npm with Options
builder.Map("npm install {package} --save-dev")
    .WithHandler((string package) => WriteLine($"📦 Installing {package} as dev dependency"))
    .AsCommand().Done();
builder.Map("npm install {package} --save")
    .WithHandler((string package) => WriteLine($"📦 Installing {package} as dependency"))
    .AsCommand().Done();
builder.Map("npm install {package}")
    .WithHandler((string package) => WriteLine($"📦 Installing {package}"))
    .AsCommand().Done();
builder.Map("npm run {script}")
    .WithHandler((string script) => WriteLine($"🏃 Running script: {script}"))
    .AsCommand().Done();
builder.Map("npm {*args}")
    .WithHandler((string[] args) => WriteLine($"npm {string.Join(" ", args)}"))
    .AsCommand().Done();

// Test 9: Option Order Independence
// All these should match the amend with message handler
builder.Map("git commit -m {message} --amend")
    .WithHandler((string message) => WriteLine($"Amending with message: {message}"))
    .AsCommand().Done();
builder.Map("git commit --amend -m {message}")
    .WithHandler((string message) => WriteLine($"Amending with message: {message}"))
    .AsCommand().Done();
builder.Map("git commit --amend --message {message}")
    .WithHandler((string message) => WriteLine($"Amending with message: {message}"))
    .AsCommand().Done();
builder.Map("git commit --message {message} --amend")
    .WithHandler((string message) => WriteLine($"Amending with message: {message}"))
    .AsCommand().Done();

// Test 10: Option Aliases
builder.Map("git commit -m {message}")
    .WithHandler((string message) => WriteLine($"Creating commit with message: {message} (using -m shorthand)"))
    .AsCommand().Done();
builder.Map("git commit --message {message}")
    .WithHandler((string message) => WriteLine($"Creating commit with message: {message} (using --message flag)"))
    .AsCommand().Done();

// Test 11: Async void methods
builder.Map("async-test")
    .WithHandler(async () =>
    {
      await Task.Delay(10); // Simulate async work
      WriteLine("Async operation completed");
    })
    .AsCommand().Done();

// Test 12: Optional Parameters
builder.Map("deploy {env} {tag?}")
    .WithHandler((string env, string? tag) =>
    {
      if (tag is not null)
      {
        WriteLine($"Deploying to {env} with tag {tag}");
      }
      else
      {
        WriteLine($"Deploying to {env} with latest tag");
      }
    })
    .AsCommand().Done();

// Test 13: Async with Optional Parameters
builder.Map("backup {source} {destination?}")
    .WithHandler(async (string source, string? destination) =>
    {
      await Task.Delay(10); // Simulate async work
      if (destination is not null)
      {
        WriteLine($"Backing up {source} to {destination}");
      }
      else
      {
        WriteLine($"Backing up {source} to default location");
      }
    })
    .AsCommand().Done();

// Test 14: Optional Parameters with Type Constraints
builder.Map("sleep {seconds:int?}")
    .WithHandler((int? seconds) =>
    {
      int sleepTime = seconds ?? 1;
      WriteLine($"Sleeping for {sleepTime} seconds");
    })
    .AsCommand().Done();

// Test 15: Ultimate Catch-All
builder.Map("{*everything}")
    .WithHandler((string[] everything) => WriteLine($"Unknown command: {string.Join(" ", everything)}"))
    .AsQuery().Done();

// Help command
builder.Map("--help")
    .WithHandler(() =>
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
      WriteLine("  async-test                      - Test async command");
      WriteLine("  deploy {env} {tag?}             - Deploy with optional tag");
      WriteLine("  backup {source} {destination?}  - Async backup with optional destination");
      WriteLine("  sleep {seconds:int?}            - Sleep with optional seconds");
      WriteLine("  --help                          - Show this help");
    })
    .AsQuery().Done();

// Build and run
NuruCoreApp app = builder.Build();
return await app.RunAsync(args).ConfigureAwait(false);
