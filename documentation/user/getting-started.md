# Getting Started with TimeWarp.Nuru

Build your first command-line application with TimeWarp.Nuru in 5 minutes.

## Installation

Add TimeWarp.Nuru to your project:

```bash
dotnet new console -n MyCliApp
cd MyCliApp
dotnet add package TimeWarp.Nuru
```

## Your First CLI App

Let's build a simple calculator CLI that demonstrates both Direct and Mediator approaches.

### 1. Replace Program.cs

```csharp
using TimeWarp.Nuru;
using static System.Console;

NuruApp app = new NuruAppBuilder()
    .AddRoute("add {x:double} {y:double}",
        (double x, double y) => WriteLine($"{x} + {y} = {x + y}"))
    .AddRoute("multiply {x:double} {y:double}",
        (double x, double y) => WriteLine($"{x} × {y} = {x * y}"))
    .Build();

return await app.RunAsync(args);
```

### 2. Run It

```bash
dotnet run -- add 15 25
# Output: 15 + 25 = 40

dotnet run -- multiply 3 7
# Output: 3 × 7 = 21
```

🎉 **Congratulations!** You've created your first Nuru CLI app.

## Understanding the Code

### Route Pattern
```csharp
"add {x:double} {y:double}"
```

- `add` - Literal segment (must match exactly)
- `{x:double}` - Typed parameter named 'x' expecting a double
- `{y:double}` - Typed parameter named 'y' expecting a double

### Handler Delegate
```csharp
(double x, double y) => WriteLine($"{x} + {y} = {x + y}")
```

- Parameters automatically match route pattern parameters
- Type conversion happens automatically
- Return Task or void for sync operations

## Adding More Features

### Optional Parameters

```csharp
.AddRoute("greet {name} {greeting?}",
    (string name, string? greeting) =>
        WriteLine($"{greeting ?? "Hello"}, {name}!"))
```

```bash
dotnet run -- greet Alice
# Output: Hello, Alice!

dotnet run -- greet Bob "Good morning"
# Output: Good morning, Bob!
```

### Options (Flags)

```csharp
.AddRoute("list --verbose",
    () => WriteLine("Listing files with detailed information..."))
.AddRoute("list",
    () => WriteLine("Listing files..."))
```

```bash
dotnet run -- list
# Output: Listing files...

dotnet run -- list --verbose
# Output: Listing files with detailed information...
```

### Catch-All Parameters

```csharp
.AddRoute("echo {*words}",
    (string[] words) => WriteLine(string.Join(" ", words)))
```

```bash
dotnet run -- echo Hello World from Nuru!
# Output: Hello World from Nuru!
```

## Next Steps

### Add Dependency Injection

For more complex scenarios, enable DI and use the Mediator pattern:

```csharp
using TimeWarp.Nuru;
using TimeWarp.Mediator;

NuruAppBuilder builder = new();
builder.AddDependencyInjection();

// Register services
builder.Services.AddSingleton<ICalculator, Calculator>();

// Register command with handler
builder.AddRoute<FactorialCommand>("factorial {n:int}");

NuruApp app = builder.Build();
return await app.RunAsync(args);
```

See [Architecture Choices](guides/architecture-choices.md) for when to use Direct vs Mediator approaches.

### Explore Complete Examples

Check out the [Calculator Samples](../../Samples/Calculator/) for three complete implementations:
- **calc-delegate.cs** - Pure Direct approach
- **calc-mediator.cs** - Pure Mediator approach
- **calc-mixed.cs** - Mixed approach (recommended)

### Learn More

- **[Use Cases](use-cases.md)** - Greenfield apps and progressive enhancement patterns
- **[Routing Features](features/routing.md)** - Complete route pattern syntax
- **[Deployment](guides/deployment.md)** - Native AOT and .NET 10 runfiles
- **[MCP Server](tools/mcp-server.md)** - AI-assisted development with Claude

## Common Questions

### How is this different from other CLI frameworks?

TimeWarp.Nuru brings **web-style routing** to CLI applications:
- Define routes with familiar syntax (`"deploy {env} --version {tag}"`)
- Mix Direct (performance) and Mediator (architecture) approaches
- Compile-time validation with Roslyn Analyzer
- Native AOT support for fast startup

### Do I need to choose between Direct and Mediator?

No! You can mix both approaches in the same application:
- Use **Direct** for simple commands (blazing fast)
- Use **Mediator** for complex commands (testable, DI, structured)

See the [Mixed approach example](../../Samples/Calculator/calc-mixed.cs).

### What about help text?

Enable automatic help generation:

```csharp
NuruApp app = new NuruAppBuilder()
    .AddRoute("deploy {env|Target environment} {tag?|Optional version}",
        (string env, string? tag) => Deploy(env, tag))
    .AddAutoHelp()
    .Build();
```

```bash
dotnet run -- --help
dotnet run -- deploy --help
```

Learn more in [Auto-Help Features](features/auto-help.md).
