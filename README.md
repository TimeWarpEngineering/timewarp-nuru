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

Build a powerful calculator CLI that mixes performance and flexibility:

```csharp
using TimeWarp.Nuru;
using TimeWarp.Mediator;

var builder = new AppBuilder();

// Simple operations: Direct approach (maximum speed)
builder.AddRoute("add {x:double} {y:double}", 
    (double x, double y) => Console.WriteLine($"{x} + {y} = {x + y}"));

builder.AddRoute("multiply {x:double} {y:double}", 
    (double x, double y) => Console.WriteLine($"{x} × {y} = {x * y}"));

// Complex operations: Mediator approach (testable, DI)
builder.Services.AddSingleton<ICalculator, Calculator>();
builder.AddRoute<FactorialCommand>("factorial {n:int}");
builder.AddRoute<FibonacciCommand>("fibonacci {n:int}");

var app = builder.Build();
return await app.RunAsync(args);
```

Run it:
```bash
# Fast operations
dotnet run -- add 15 25
# Output: 15 + 25 = 40

# Complex operations with full DI
dotnet run -- factorial 5  
# Output: 5! = 120
```

**🎯 Create .NET 10 single-file executables** (requires .NET 10 Preview 6+):
```bash
#!/usr/bin/dotnet --
#:project path/to/TimeWarp.Nuru.csproj
// Add your code above and run directly: ./mycalc add 10 20
```

> **💡 Explore complete working examples: [Calculator Samples](Samples/Calculator/) →**

## 🎯 Why TimeWarp.Nuru?

### Web-Style Route Patterns for CLI
```csharp
// Familiar syntax from web development
builder.AddRoute("deploy {env} --version {tag}", (string env, string tag) => Deploy(env, tag));
builder.AddRoute("serve --port {port:int} --host {host?}", (int port, string? host) => StartServer(port, host));
builder.AddRoute("backup {*files}", (string[] files) => BackupFiles(files));
```

### Flexibility Without Compromise
**Choose your approach per command** - not per application:
- Simple commands → Direct (fast)
- Complex commands → Mediator (structured)
- Mixed in the same app → Best of both

### Three Approaches - Choose Your Power Level

**🚀 Direct** - Maximum performance, zero overhead
```csharp
var app = new NuruAppBuilder()
    .AddRoute("deploy {env}", (string env) => Deploy(env))
    .Build(); // ~4KB memory, blazing fast
```

**🏗️ Mediator** - Enterprise patterns, full DI
```csharp
var builder = new AppBuilder();
builder.AddRoute<DeployCommand>("deploy {env} --strategy {strategy}");
// Testable handlers, dependency injection, complex logic
```

**⚡ Mixed** - Best of both worlds (recommended)
```csharp
// Simple commands: Direct (speed)
builder.AddRoute("status", () => ShowStatus());

// Complex commands: Mediator (structure)
builder.AddRoute<AnalyzeCommand>("analyze {*files}");
```
*Use Direct for simple operations, Mediator for complex business logic*

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
builder.AddRoute("run {image} --port {port:int} --detach", 
    (string image, int port) => Docker.Run(image, port, detached: true));

// Conditional routing based on options
builder.AddRoute("deploy {app} --env {environment} --dry-run", 
    (string app, string env) => DeployDryRun(app, env));
builder.AddRoute("deploy {app} --env {environment}", 
    (string app, string env) => DeployReal(app, env));
```

## 🏗️ Enterprise-Ready Patterns

Scale from simple scripts to complex applications:

```csharp
// Commands as classes - perfect for complex logic
public class DeployCommand : IRequest
{
    public string Environment { get; set; }
    public string? Version { get; set; }
    public bool DryRun { get; set; }
}

// Handlers with full DI support
public class DeployHandler(IDeploymentService deployment, ILogger logger) 
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

// Simple registration
builder.AddRoute<DeployCommand>("deploy {environment} --version {version?} --dry-run");
```

## 🔧 Advanced Features

### Type Safety Built-In
```csharp
// Automatic parameter conversion
builder.AddRoute("delay {seconds:int}", (int s) => Thread.Sleep(s * 1000));
builder.AddRoute("download {url:uri}", (Uri url) => Download(url));
builder.AddRoute("process {date:datetime}", (DateTime d) => Process(d));
```

### Custom Types & Async
```csharp
// Add your own type converters
builder.AddTypeConverter<GitBranch>(value => GitBranch.Parse(value));
builder.AddRoute("checkout {branch:GitBranch}", (GitBranch b) => Git.Checkout(b));

// Full async support
builder.AddRoute("fetch {url}", async (string url) => {
    var data = await httpClient.GetStringAsync(url);
    await File.WriteAllTextAsync("result.txt", data);
});
```

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

**Plus .NET 10 Script Mode:**
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

| Implementation | Test Results | Execution Time | Speed Improvement |
|----------------|--------------|----------------|-------------------|
| **Direct (JIT)** | 37/37 ✓ | 2.49s | Baseline |
| **Mediator (JIT)** | 37/37 ✓ | 6.52s | 161% slower |
| **Direct (AOT)** | 37/37 ✓ | **0.30s** 🚀 | 88% faster than JIT |
| **Mediator (AOT)** | 37/37 ✓ | **0.42s** 🚀 | 93% faster than JIT |

**Key Insights:**
- **AOT is ridiculously fast**: Sub-second execution for 37 complex CLI tests
- **Direct approach**: Best for maximum performance (3.3 MB binary)
- **Mediator approach**: Worth the overhead for DI/testability (4.8 MB binary)
- **Both scale beautifully**: From simple scripts to enterprise applications

## 🌟 Working Examples

Don't just read about it - **run the code**:

### [📁 Calculator Samples](Samples/Calculator/)
Three complete implementations you can run immediately:
- **calc-direct** - Pure performance (Direct approach)
- **calc-mediator** - Enterprise patterns (Mediator with DI)
- **calc-mixed** - Hybrid approach (best of both)

```bash
# Try them now:
./Samples/Calculator/calc-mixed add 10 20     # Direct: fast
./Samples/Calculator/calc-mixed factorial 5   # Mediator: structured  
./Samples/Calculator/calc-mixed fibonacci 10  # Output: Fibonacci(10) = 55
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