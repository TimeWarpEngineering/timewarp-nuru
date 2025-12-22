// AOT-Compatible TimeWarp.Nuru Example
//
// This sample demonstrates how to build a fully AOT-compatible CLI application
// using TimeWarp.Nuru with zero IL2XXX/IL3XXX warnings.
//
// Build commands:
//   dotnet publish -c Release -r linux-x64    # Linux
//   dotnet publish -c Release -r osx-arm64    # macOS Apple Silicon
//   dotnet publish -c Release -r win-x64      # Windows
//
// Result: ~3-5 MB native binary with instant startup (<1ms)

using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Nuru;
using static System.Console;

// Create builder with full DI support
NuruAppBuilder builder = NuruApp.CreateBuilder(args);

// IMPORTANT: Register the source-generated Mediator for AOT compatibility
// This replaces reflection-based dependency resolution with compile-time generated code
builder.Services.AddMediator();

// Basic commands
builder.Map("hello")
  .WithHandler(() => WriteLine("Hello from AOT!"))
  .AsQuery()
  .Done();
builder.Map("version")
  .WithHandler(() => WriteLine("aot-example v1.0.0"))
  .AsQuery()
  .Done();

// Commands with typed parameters
builder.Map("greet {name}")
  .WithHandler((string name) => WriteLine($"Hello, {name}!"))
  .AsCommand()
  .Done();

builder.Map("add {x:int} {y:int}")
  .WithHandler((int x, int y) => WriteLine($"{x} + {y} = {x + y}"))
  .AsQuery()
  .Done();

builder.Map("multiply {x:double} {y:double}")
  .WithHandler((double x, double y) => WriteLine($"{x} * {y} = {x * y}"))
  .AsQuery()
  .Done();

// Optional parameters
builder.Map("deploy {env} {tag?}")
  .WithHandler((string env, string? tag) =>
  {
    string version = tag ?? "latest";
    WriteLine($"Deploying to {env} with tag: {version}");
  })
  .AsCommand()
  .Done();

// Boolean options
builder.Map("build --release")
  .WithHandler(() => WriteLine("Building in Release mode"))
  .AsCommand()
  .Done();

builder.Map("build --debug")
  .WithHandler(() => WriteLine("Building in Debug mode"))
  .AsCommand()
  .Done();

builder.Map("build")
  .WithHandler(() => WriteLine("Building in default mode"))
  .AsCommand()
  .Done();

// Options with values
builder.Map("config --output {path}")
  .WithHandler((string path) => WriteLine($"Configuration will be saved to: {path}"))
  .AsCommand()
  .Done();

// Async commands (fully AOT-compatible)
builder.Map("fetch {url}")
  .WithHandler(async (string url) =>
  {
    WriteLine($"Fetching {url}...");
    await Task.Delay(100); // Simulated network delay
    WriteLine("Done!");
  })
  .AsQuery()
  .Done();

// Catch-all for unknown commands
builder.Map("{*args}")
  .WithHandler((string[] args) =>
  {
    WriteLine($"Unknown command: {string.Join(" ", args)}");
    WriteLine("Run with --help for available commands.");
  })
  .AsQuery()
  .Done();

// Help command
builder.Map("--help")
  .WithHandler(() =>
  {
    WriteLine("AOT Example - TimeWarp.Nuru Native AOT Demo");
    WriteLine();
    WriteLine("Usage: aot-example <command> [options]");
    WriteLine();
    WriteLine("Commands:");
    WriteLine("  hello                    Print hello message");
    WriteLine("  version                  Show version");
    WriteLine("  greet {name}             Greet someone by name");
    WriteLine("  add {x} {y}              Add two integers");
    WriteLine("  multiply {x} {y}         Multiply two doubles");
    WriteLine("  deploy {env} {tag?}      Deploy to environment (tag optional)");
    WriteLine("  build [--release|--debug] Build the project");
    WriteLine("  config --output {path}   Save configuration");
    WriteLine("  fetch {url}              Fetch a URL (async demo)");
    WriteLine("  --help                   Show this help");
  })
  .AsQuery()
  .Done();

// Build and run
NuruCoreApp app = builder.Build();
return await app.RunAsync(args);
