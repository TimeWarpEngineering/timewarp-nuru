using TimeWarp.Nuru;
using static System.Console;

NuruAppBuilder builder = new();

// Test 1: Basic Commands
builder.AddRoute("status", () => WriteLine("✓ System is running"));
builder.AddRoute("version", () => WriteLine("TimeWarp.Nuru v1.0.0"));

// Test 2: Sub-Commands
builder.AddRoute("git status", () => WriteLine("On branch main\nYour branch is up to date"));
builder.AddRoute("git commit", () => WriteLine("Nothing to commit, working tree clean"));
builder.AddRoute("git push", () => WriteLine("Everything up-to-date"));

// Test 3: Option-Based Routing
builder.AddRoute("git commit --amend", () => WriteLine("Amending previous commit"));
builder.AddRoute("git commit --amend --no-edit", () => WriteLine("Amending without editing message"));

// Test 4: Options with Values
builder.AddRoute("git log --max-count {count:int}", (int count) =>
    WriteLine($"Showing last {count} commits"));

// Test 5: Docker Pass-Through
builder.AddRoute("docker run --enhance-logs {image}", (string image) =>
    WriteLine($"🚀 Running {image} with enhanced logging"));
builder.AddRoute("docker run {*args}", (string[] args) =>
    WriteLine($"docker run {string.Join(" ", args)}"));

// Test 6: Docker Build Pass-Through
builder.AddRoute("docker build {*args}", (string[] args) =>
    WriteLine($"docker build {string.Join(" ", args)}"));
builder.AddRoute("docker ps {*args}", (string[] args) =>
    WriteLine($"docker ps {string.Join(" ", args)}"));
builder.AddRoute("docker {*args}", (string[] args) =>
    WriteLine($"docker {string.Join(" ", args)}"));

// Test 7: kubectl Enhancement
builder.AddRoute("kubectl get {resource} --watch --enhanced", (string resource) =>
    WriteLine($"⚡ Enhanced watch for {resource}"));
builder.AddRoute("kubectl get {resource} --watch", (string resource) =>
    WriteLine($"Watching {resource}..."));
builder.AddRoute("kubectl get {resource}", (string resource) =>
    WriteLine($"NAME                  READY   STATUS    RESTARTS   AGE\n{resource}-sample    1/1     Running   0          5m"));
builder.AddRoute("kubectl apply -f {file}", (string file) =>
    WriteLine($"deployment.apps/{file} configured"));
builder.AddRoute("kubectl {*args}", (string[] args) =>
    WriteLine($"kubectl {string.Join(" ", args)}"));

// Test 8: npm with Options
builder.AddRoute("npm install {package} --save-dev", (string package) =>
    WriteLine($"📦 Installing {package} as dev dependency"));
builder.AddRoute("npm install {package} --save", (string package) =>
    WriteLine($"📦 Installing {package} as dependency"));
builder.AddRoute("npm install {package}", (string package) =>
    WriteLine($"📦 Installing {package}"));
builder.AddRoute("npm run {script}", (string script) =>
    WriteLine($"🏃 Running script: {script}"));
builder.AddRoute("npm {*args}", (string[] args) =>
    WriteLine($"npm {string.Join(" ", args)}"));

// Test 9: Option Order Independence
// All these should match the amend with message handler
builder.AddRoute("git commit -m {message} --amend", (string message) =>
    WriteLine($"Amending with message: {message}"));
builder.AddRoute("git commit --amend -m {message}", (string message) =>
    WriteLine($"Amending with message: {message}"));
builder.AddRoute("git commit --amend --message {message}", (string message) =>
    WriteLine($"Amending with message: {message}"));
builder.AddRoute("git commit --message {message} --amend", (string message) =>
    WriteLine($"Amending with message: {message}"));

// Test 10: Option Aliases
builder.AddRoute("git commit -m {message}", (string message) =>
    WriteLine($"Creating commit with message: {message} (using -m shorthand)"));
builder.AddRoute("git commit --message {message}", (string message) =>
    WriteLine($"Creating commit with message: {message} (using --message flag)"));

// Test 11: Async void methods
builder.AddRoute("async-test", async () =>
{
    await Task.Delay(10); // Simulate async work
  WriteLine("Async operation completed");
});

// Test 12: Optional Parameters
builder.AddRoute("deploy {env} {tag?}", (string env, string? tag) =>
{
    if (tag is not null)
    {
    WriteLine($"Deploying to {env} with tag {tag}");
    }
    else
    {
    WriteLine($"Deploying to {env} with latest tag");
    }
});

// Test 13: Async with Optional Parameters
builder.AddRoute("backup {source} {destination?}", async (string source, string? destination) =>
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
});

// Test 14: Optional Parameters with Type Constraints
builder.AddRoute("sleep {seconds:int?}", (int? seconds) =>
{
    int sleepTime = seconds ?? 1;
  WriteLine($"Sleeping for {sleepTime} seconds");
});

// Test 15: Ultimate Catch-All
builder.AddRoute("{*everything}", (string[] everything) =>
    WriteLine($"Unknown command: {string.Join(" ", everything)}"));

// Help command
builder.AddRoute("--help", () =>
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
});

// Build and run
NuruApp app = builder.Build();
return await app.RunAsync(args).ConfigureAwait(false);
