# TimeWarp.Nuru

<div align="center">

[![NuGet Version](https://img.shields.io/nuget/v/TimeWarp.Nuru.svg)](https://www.nuget.org/packages/TimeWarp.Nuru/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/TimeWarp.Nuru.svg)](https://www.nuget.org/packages/TimeWarp.Nuru/)
[![Build Status](https://img.shields.io/github/actions/workflow/status/TimeWarpEngineering/timewarp-nuru/build.yml?branch=master)](https://github.com/TimeWarpEngineering/timewarp-nuru/actions)
[![License](https://img.shields.io/github/license/TimeWarpEngineering/timewarp-nuru.svg)](https://github.com/TimeWarpEngineering/timewarp-nuru/blob/master/LICENSE)

**Route-based CLI framework for .NET - bringing web-style routing to command-line applications**

</div>

> **Nuru** means "light" in Swahili - illuminating the path to your commands with clarity and simplicity.

> [!NOTE]
> **No Commercial License Required** - TimeWarp.Nuru and [TimeWarp.Mediator](https://github.com/TimeWarpEngineering/timewarp-mediator) are both released under the Unlicense. Unlike MediatR (which now requires commercial licensing), our libraries are free for any use, commercial or otherwise.

## 📦 Installation

```bash
dotnet add package TimeWarp.Nuru
```

## 🚀 Quick Start

### Delegate Approach

Maximum performance with zero overhead:

```csharp
using TimeWarp.Nuru;
using static System.Console;

NuruApp app = new NuruAppBuilder()
  .AddRoute
  (
    "add {x:double} {y:double}",
    (double x, double y) => WriteLine($"{x} + {y} = {x + y}")
  )
  .AddRoute
  (
    "multiply {x:double} {y:double}",
    (double x, double y) => WriteLine($"{x} × {y} = {x * y}")
  )
  .Build();

return await app.RunAsync(args);
```

```bash
dotnet run -- add 15 25
# Output: 15 + 25 = 40
```

### Mediator Approach

Enterprise patterns with dependency injection:

```csharp
using TimeWarp.Nuru;
using TimeWarp.Mediator;

NuruApp app = new NuruAppBuilder()
  .AddDependencyInjection()
  .ConfigureServices(services =>
  {
    services.AddSingleton<ICalculator, Calculator>();
  })
  .AddRoute<FactorialCommand>("factorial {n:int}")
  .Build();

return await app.RunAsync(args);

// Command and handler as separate classes
public sealed class FactorialCommand : IRequest
{
  public int N { get; set; }
}

public sealed class FactorialCommandHandler(ICalculator calc) : IRequestHandler<FactorialCommand>
{
  public Task Handle(FactorialCommand cmd, CancellationToken ct)
  {
      Console.WriteLine($"{cmd.N}! = {calc.Factorial(cmd.N)}");
      return Task.CompletedTask;
  }
}
```

```bash
dotnet run -- factorial 5
# Output: 5! = 120
```

> **💡 Tip:** Handlers can be nested inside commands for better organization. See [Calculator Samples](Samples/Calculator/) for nested handler examples.

**🎯 Create .NET 10 runfile executables** (requires .NET 10):
```bash
#!/usr/bin/dotnet --
#:project path/to/TimeWarp.Nuru.csproj
// Add code from above and run directly: ./mycalc add 10 20
```

> **💡 Explore complete working examples: [Calculator Samples](Samples/Calculator/) →**

## 🎯 Why TimeWarp.Nuru?

### Two Powerful Use Cases

**🆕 Greenfield CLI Applications**
Build new command-line tools from scratch with modern patterns:
```csharp
NuruApp app = new NuruAppBuilder()
  // Start with direct routes for simple commands
  .AddRoute("version", () => Console.WriteLine("v1.0.0"))
  .AddRoute("ping", () => Console.WriteLine("pong"))
  // Add DI when complexity grows
  .AddDependencyInjection()
  .ConfigureServices(services =>
  {
    services.AddSingleton<IEmployeeRepository, EmployeeRepository>();
  })
  // Use mediator for enterprise features
  .AddRoute<GetEmployee.Query>("employee get {id:int}")
  .AddRoute<AnalyzeCommand>("analyze {*files}")
  .Build();
```

**🔄 Progressive Enhancement of Existing CLIs**
Wrap existing command-line tools to add routing, validation, or special handling:
```csharp
var builder = new NuruAppBuilder();

// Intercept specific commands for special handling
builder.AddRoute("deploy prod", async () =>
{
  // Custom validation and logging
  if (!await ValidateProductionAccess()) return;
  await Shell.ExecuteAsync("existing-cli", "deploy", "prod");
});

// Pass through all other commands to existing tool
builder.AddRoute("{*args}", async (string[] args) =>
{
  await Shell.ExecuteAsync("existing-cli", args);
});
```

### Flexible Architecture

**Choose the right approach per command** - mix Direct and Mediator in the same app:

| Approach     | Best For                       | Memory   | Features                                     |
| ------------ | ------------------------------ | -------- | -------------------------------------------- |
| **Direct**   | Simple commands, maximum speed | ~4KB     | Zero overhead, inline logic                  |
| **Mediator** | Complex logic, testability     | Moderate | DI, structured handlers, enterprise patterns |
| **Mixed**    | Real applications              | Optimal  | Use Direct for simple, Mediator for complex  |

```csharp
// Mixed approach example
NuruApp app = new NuruAppBuilder()
  // Direct: Fast paths for simple operations
  .AddRoute("status", () => ShowStatus())
  .AddRoute("version", () => Console.WriteLine("1.0"))
  // Enable DI for complex operations
  .AddDependencyInjection()
  .ConfigureServices(services =>
  {
    services.AddScoped<IAnalyzer, Analyzer>();
  })
  // Mediator: Structured logic with DI
  .AddRoute<AnalyzeCommand>("analyze {*files}")
  .AddRoute<DeployCommand>("deploy {env} --dry-run")
  .Build();
```

### Web-Style Route Patterns
Bring familiar web routing syntax to the command line:
```csharp
builder.AddRoute("deploy {env} --version {tag}", (string env, string tag) => Deploy(env, tag));
builder.AddRoute("serve --port {port:int} --host {host?}", (int port, string? host) => StartServer(port, host));
builder.AddRoute("backup {*files}", (string[] files) => BackupFiles(files));
```

## 📖 Core Concepts

### Route Patterns

TimeWarp.Nuru supports intuitive route patterns:

| Pattern         | Example                 | Matches                                         |
| --------------- | ----------------------- | ----------------------------------------------- |
| Literal         | `status`                | `./cli status`                                  |
| Parameter       | `greet {name}`          | `./cli greet Alice`                             |
| Typed Parameter | `delay {ms:int}`        | `./cli delay 1000`                              |
| Optional        | `deploy {env} {tag?}`   | `./cli deploy prod` or `./cli deploy prod v1.2` |
| Options         | `build --config {mode}` | `./cli build --config Release`                  |
| Catch-all       | `docker {*args}`        | `./cli docker run -it ubuntu`                   |

### Type Safety

Parameters are automatically converted to the correct types:

```csharp
// Supports common types out of the box
.AddRoute("wait {seconds:int}", (int s) => Thread.Sleep(s * 1000))
.AddRoute("download {url:uri}", (Uri url) => Download(url))
.AddRoute("verbose {enabled:bool}", (bool v) => SetVerbose(v))
.AddRoute("process {date:datetime}", (DateTime d) => Process(d))
.AddRoute("scale {factor:double}", (double f) => Scale(f))
```

### Complex Scenarios

Build sophisticated CLIs with sub-commands and options:

```csharp
// Git-style sub-commands
builder.AddRoute("git add {*files}", (string[] files) => Git.Add(files));
builder.AddRoute("git commit -m {message}", (string msg) => Git.Commit(msg));
builder.AddRoute("git push --force", () => Git.ForcePush());

// Docker-style with options
builder.AddRoute
(
  "run {image} --port {port:int} --detach", 
  (string image, int port) => Docker.Run(image, port, detached: true)
);

// Conditional routing based on options
builder.AddRoute
(
  "deploy {app} --env {environment} --dry-run", 
  (string app, string env) => DeployDryRun(app, env)
);

builder.AddRoute
(
  "deploy {app} --env {environment}", 
  (string app, string env) => DeployReal(app, env)
);
```

### Automatic Help Generation

Enable automatic help for all your commands:

```csharp
var app = new NuruAppBuilder()
  .AddRoute
  (
    "deploy {env|Target environment} {tag?|Optional version tag}", 
    (string env, string? tag) => Deploy(env, tag)
  )
  .AddRoute
  (
    "backup {source} --compress,-c|Enable compression", 
    (string source, bool compress) => Backup(source, compress)
  )
  .AddAutoHelp()  // Generates help for all commands
  .Build();
```

This automatically creates:
- `--help` - Shows all available commands
- `deploy --help` - Shows usage for the deploy command
- Parameter and option descriptions using the `|` syntax

## 🏗️ Enterprise-Ready Patterns

Scale from simple scripts to complex applications:

```csharp
// Commands as classes with nested handlers - perfect for complex logic
public class DeployCommand : IRequest
{
  public string Environment { get; set; }
  public string? Version { get; set; }
  public bool DryRun { get; set; }
  
  // Handler nested inside command for better organization
  public sealed class Handler(IDeploymentService deployment, ILogger logger) 
    : IRequestHandler<DeployCommand>
  {
    public async Task Handle(DeployCommand cmd, CancellationToken ct)
    {
      if (cmd.DryRun)
        await deployment.ValidateAsync(cmd.Environment, cmd.Version);
      else  
        await deployment.ExecuteAsync(cmd.Environment, cmd.Version);
    }
  }
}

// Enable DI and register services
NuruApp app = new NuruAppBuilder()
  .AddDependencyInjection()
  .ConfigureServices(services =>
  {
    services.AddSingleton<IDeploymentService, DeploymentService>();
  })
  .AddRoute<DeployCommand>("deploy {environment} --version {version?} --dry-run")
  .Build();
```

## 🔧 Advanced Features

### Type Safety Built-In
```csharp
// Automatic parameter conversion
builder.AddRoute("delay {seconds:int}", (int s) => Thread.Sleep(s * 1000));
builder.AddRoute("download {url:uri}", (Uri url) => Download(url));
builder.AddRoute("process {date:datetime}", (DateTime d) => Process(d));
```

### Async Support
```csharp
// Full async support for both Direct and Mediator approaches
builder.AddRoute
(
  "fetch {url}", 
  async (string url) => 
  {
    var data = await httpClient.GetStringAsync(url);
    await File.WriteAllTextAsync("result.txt", data);
  }
);

// Async Mediator commands with nested handler
public sealed class FetchCommand : IRequest<string> 
{ 
  public string Url { get; set; }
  
  internal sealed class Handler : IRequestHandler<FetchCommand, string>
  {
    public async Task<string> Handle(FetchCommand cmd, CancellationToken ct)
    {
      using var client = new HttpClient();
      return await client.GetStringAsync(cmd.Url, ct);
    }
  }
}
```

### Output Handling & Console Streams

TimeWarp.Nuru gives you full control over output handling:

```csharp
NuruApp app = new NuruAppBuilder()
  // Simple console output (stdout)
  .AddRoute("hello", () => Console.WriteLine("Hello!"))
  // Structured data - automatically serialized to JSON (stdout)
  .AddRoute("info", () => new { Name = "MyApp", Version = "1.0" })
  // Separate concerns: diagnostics to stderr, data to stdout
  .AddRoute("process {file}", (string file) =>
  {
    Console.Error.WriteLine($"Processing {file}...");  // Progress to stderr
    Thread.Sleep(1000);
    Console.Error.WriteLine("Complete!");
    return new { File = file, Lines = 42, Status = "OK" };  // Result as JSON to stdout
  })
  // With DI and logging
  .AddDependencyInjection()
  .ConfigureServices(services =>
  {
    services.AddLogging();  // Add logging services
  })
  .AddRoute<AnalyzeCommand>("analyze {path}")
  .Build();

public sealed class AnalyzeCommand : IRequest<AnalyzeResult>
{
  public string Path { get; set; }

  public sealed class Handler(ILogger<Handler> logger) : IRequestHandler<AnalyzeCommand, AnalyzeResult>
  {
    public async Task<AnalyzeResult> Handle(AnalyzeCommand cmd, CancellationToken ct)
    {
      logger.LogInformation("Starting analysis of {Path}", cmd.Path);  // Structured logging
      var result = await AnalyzeAsync(cmd.Path);
      return result;  // Returned object → JSON to stdout
    }
  }
}
```

**Best Practices:**
- Use `Console.WriteLine()` for human-readable output to stdout
- Use `Console.Error.WriteLine()` for progress, diagnostics, and errors to stderr  
- Return objects from handlers to get automatic JSON serialization to stdout
- This separation enables piping and scripting: `./myapp analyze data.csv | jq '.summary'`

## 🚄 Native AOT Ready

Build ultra-fast native binaries with the right configuration:

**For Direct approach:**
```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

**For Mediator/Mixed approach:**
```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <TrimMode>partial</TrimMode>  <!-- Preserve reflection for command handlers -->
</PropertyGroup>
```

```bash
dotnet publish -c Release -r linux-x64
./myapp --help  # Instant startup, 3.3MB binary
```

**Plus .NET 10 RunFiles:**
```bash
#!/usr/bin/dotnet --
#:project TimeWarp.Nuru.csproj
// Write your CLI and run it directly!
```

## ⚡ Performance That Matters

TimeWarp.Nuru delivers where it counts:

- **Memory Efficient**: Only 3,992 B allocated - minimal footprint
- **Fast Execution**: 18.452 ms with highly optimized routing 
- **Native AOT**: Compile to 3.3 MB single-file binaries
- **Rich Functionality**: Route patterns, type safety, DI, mixed approaches

### Real-World Performance: 37 Integration Tests

| Implementation     | Test Results | Execution Time | Speed Improvement   |
| ------------------ | ------------ | -------------- | ------------------- |
| **Direct (JIT)**   | 37/37 ✓      | 2.49s          | Baseline            |
| **Mediator (JIT)** | 37/37 ✓      | 6.52s          | 161% slower         |
| **Direct (AOT)**   | 37/37 ✓      | **0.30s** 🚀    | 88% faster than JIT |
| **Mediator (AOT)** | 37/37 ✓      | **0.42s** 🚀    | 93% faster than JIT |

**Key Insights:**
- **AOT is ridiculously fast**: Sub-second execution for 37 complex CLI tests
- **Direct approach**: Best for maximum performance (3.3 MB binary)
- **Mediator approach**: Worth the overhead for DI/testability (4.8 MB binary)
- **Both scale beautifully**: From simple scripts to enterprise applications

## 🌟 Working Examples

Don't just read about it - **run the code**:

### [📁 Calculator Samples](Samples/Calculator/)
Three complete implementations you can run immediately:
- **calc-delegate.cs** - Pure performance (Delegate approach)
- **calc-mediator.cs** - Enterprise patterns (Mediator with DI)
- **calc-mixed.cs** - Hybrid approach (best of both)

```bash
# Try them now:
./Samples/Calculator/calc-mixed.cs add 10 20     # Direct: fast
./Samples/Calculator/calc-mixed.cs factorial 5   # Mediator: structured
./Samples/Calculator/calc-mixed.cs fibonacci 10  # Output: Fibonacci(10) = 55
```

### Key Patterns Demonstrated
- Route-based command definition
- Type-safe parameter binding  
- Mixed Direct/Mediator approaches
- Dependency injection integration
- .NET 10 single-file executables

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

## 📄 License

This project is licensed under the Unlicense - see the [LICENSE](LICENSE) file for details.

---

<div align="center">

**Ready to build powerful CLI applications?**

**Start Here:** [Calculator Samples](Samples/Calculator/) → **Working Examples You Can Run Now**

</div>