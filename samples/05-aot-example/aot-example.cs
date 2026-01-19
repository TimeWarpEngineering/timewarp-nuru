// AOT-Compatible TimeWarp.Nuru Example
//
// TimeWarp.Nuru is fully AOT-compatible out of the box - no special configuration needed!
// This sample demonstrates building a native AOT CLI application with zero IL2XXX/IL3XXX warnings.
//
// Build commands:
//   dotnet publish -c Release -r linux-x64    # Linux
//   dotnet publish -c Release -r osx-arm64    # macOS Apple Silicon
//   dotnet publish -c Release -r win-x64      # Windows
//
// Result: ~10 MB native binary with instant startup

using TimeWarp.Nuru;
using static System.Console;

// Create builder with full DI support
NuruAppBuilder builder = NuruApp.CreateBuilder(args);

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
builder.Map("build --release? --debug?")
  .WithHandler((bool release, bool debug) =>
  {
    string mode = release ? "Release" : (debug ? "Debug" : "default");
    WriteLine($"Building in {mode} mode");
  })
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
    WriteLine("  build [--release] [--debug] Build the project");
    WriteLine("  config --output {path}   Save configuration");
    WriteLine("  fetch {url}              Fetch a URL (async demo)");
    WriteLine("  --help                   Show this help");
  })
  .AsQuery()
  .Done();

// Build and run
NuruCoreApp app = builder.Build();
return await app.RunAsync(args);
