using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Nuru;
using TimeWarp.Mediator;

var builder = new AppBuilder();

// Add services
builder.Services.AddSingleton<IMediator, Mediator>();

// Test 1: Basic Commands
builder.AddRoute("status", () => Console.WriteLine("✓ System is running"));
builder.AddRoute("version", () => Console.WriteLine("TimeWarp.Nuru v1.0.0"));

// Test 2: Sub-Commands
builder.AddRoute("git status", () => Console.WriteLine("On branch main\nYour branch is up to date"));
builder.AddRoute("git commit", () => Console.WriteLine("Nothing to commit, working tree clean"));
builder.AddRoute("git push", () => Console.WriteLine("Everything up-to-date"));

// Test 3: Option-Based Routing
builder.AddRoute("git commit --amend", () => Console.WriteLine("Amending previous commit"));
builder.AddRoute("git commit --amend --no-edit", () => Console.WriteLine("Amending without editing message"));

// Test 4: Options with Values
builder.AddRoute("git log --max-count {count:int}", (int count) => 
    Console.WriteLine($"Showing last {count} commits"));

// Test 5: Docker Pass-Through
builder.AddRoute("docker run --enhance-logs {image}", (string image) => 
    Console.WriteLine($"🚀 Running {image} with enhanced logging"));
builder.AddRoute("docker run {*args}", (string[] args) => 
    Console.WriteLine($"docker run {string.Join(" ", args)}"));

// Test 6: Docker Build Pass-Through
builder.AddRoute("docker build {*args}", (string[] args) => 
    Console.WriteLine($"docker build {string.Join(" ", args)}"));
builder.AddRoute("docker ps {*args}", (string[] args) => 
    Console.WriteLine($"docker ps {string.Join(" ", args)}"));
builder.AddRoute("docker {*args}", (string[] args) => 
    Console.WriteLine($"docker {string.Join(" ", args)}"));

// Test 7: kubectl Enhancement
builder.AddRoute("kubectl get {resource} --watch --enhanced", (string resource) => 
    Console.WriteLine($"⚡ Enhanced watch for {resource}"));
builder.AddRoute("kubectl get {resource} --watch", (string resource) => 
    Console.WriteLine($"Watching {resource}..."));
builder.AddRoute("kubectl get {resource}", (string resource) => 
    Console.WriteLine($"NAME                  READY   STATUS    RESTARTS   AGE\n{resource}-sample    1/1     Running   0          5m"));
builder.AddRoute("kubectl apply -f {file}", (string file) => 
    Console.WriteLine($"deployment.apps/{file} configured"));
builder.AddRoute("kubectl {*args}", (string[] args) => 
    Console.WriteLine($"kubectl {string.Join(" ", args)}"));

// Test 8: npm with Options
builder.AddRoute("npm install {package} --save-dev", (string package) => 
    Console.WriteLine($"📦 Installing {package} as dev dependency"));
builder.AddRoute("npm install {package} --save", (string package) => 
    Console.WriteLine($"📦 Installing {package} as dependency"));
builder.AddRoute("npm install {package}", (string package) => 
    Console.WriteLine($"📦 Installing {package}"));
builder.AddRoute("npm run {script}", (string script) => 
    Console.WriteLine($"🏃 Running script: {script}"));
builder.AddRoute("npm {*args}", (string[] args) => 
    Console.WriteLine($"npm {string.Join(" ", args)}"));

// Test 9: Option Order Independence
// All these should match the amend with message handler
builder.AddRoute("git commit -m {message} --amend", (string message) => 
    Console.WriteLine($"Amending with message: {message}"));
builder.AddRoute("git commit --amend -m {message}", (string message) => 
    Console.WriteLine($"Amending with message: {message}"));
builder.AddRoute("git commit --amend --message {message}", (string message) => 
    Console.WriteLine($"Amending with message: {message}"));
builder.AddRoute("git commit --message {message} --amend", (string message) => 
    Console.WriteLine($"Amending with message: {message}"));

// Test 10: Option Aliases
builder.AddRoute("git commit -m {message}", (string message) => 
    Console.WriteLine($"Creating commit with message: {message} (using -m shorthand)"));
builder.AddRoute("git commit --message {message}", (string message) => 
    Console.WriteLine($"Creating commit with message: {message} (using --message flag)"));

// Test 11: Ultimate Catch-All
builder.AddRoute("{*everything}", (string[] everything) => 
    Console.WriteLine($"Unknown command: {string.Join(" ", everything)}"));

// Help command
builder.AddRoute("--help", () => 
{
    Console.WriteLine("TimeWarp.Nuru Integration Tests");
    Console.WriteLine("==================================");
    Console.WriteLine("Available test scenarios:");
    Console.WriteLine("  status                          - Basic status command");
    Console.WriteLine("  version                         - Show version");
    Console.WriteLine("  git status                      - Git status");
    Console.WriteLine("  git commit [options]            - Git commit with various options");
    Console.WriteLine("  docker run [options] {image}    - Docker run with enhancements");
    Console.WriteLine("  kubectl get {resource}          - Kubectl commands");
    Console.WriteLine("  npm install {package} [options] - NPM commands");
    Console.WriteLine("  --help                          - Show this help");
});

// Build and run
var app = builder.Build();
return await app.RunAsync(args);