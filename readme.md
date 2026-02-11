# TimeWarp.Nuru

<div align="center">

[![NuGet Version](https://img.shields.io/nuget/v/TimeWarp.Nuru.svg)](https://www.nuget.org/packages/TimeWarp.Nuru/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/TimeWarp.Nuru.svg)](https://www.nuget.org/packages/TimeWarp.Nuru/)
[![Build Status](https://img.shields.io/github/actions/workflow/status/TimeWarpEngineering/timewarp-nuru/build.yml?branch=master)](https://github.com/TimeWarpEngineering/timewarp-nuru/actions)
[![License](https://img.shields.io/github/license/TimeWarpEngineering/timewarp-nuru.svg)](https://github.com/TimeWarpEngineering/timewarp-nuru/blob/master/LICENSE)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/TimeWarpEngineering/timewarp-nuru)

**Route-based CLI framework for .NET - bringing web-style routing to command-line applications**

</div>

> **Nuru** means "light" in Swahili - illuminating the path to your commands with clarity and simplicity.

## 📦 Installation

```bash
dotnet add package TimeWarp.Nuru
```

## 🚀 Quick Start

TimeWarp.Nuru offers two patterns for defining CLI commands.
Start with the Endpoint DSL for structured apps, or Fluent DSL for quick scripts.

### Endpoint DSL

Define routes as classes with `[NuruRoute]` attributes:

```csharp
using TimeWarp.Nuru;

[NuruRoute("add", Description = "Add two numbers together")]
public sealed class AddCommand : ICommand<Unit>
{
  [Parameter(Order = 0)] public double X { get; set; }
  [Parameter(Order = 1)] public double Y { get; set; }

  public sealed class Handler : ICommandHandler<AddCommand, Unit>
  {
    public ValueTask<Unit> Handle(AddCommand command, CancellationToken ct)
    {
      Console.WriteLine($"{command.X} + {command.Y} = {command.X + command.Y}");
      return default;
    }
  }
}

// In your main file:
NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
```

### Fluent DSL

Define routes inline with a fluent builder API:

```csharp
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .Map("add {x:double} {y:double}")
    .WithHandler((double x, double y) => Console.WriteLine($"{x} + {y} = {x + y}"))
    .AsCommand()
    .Done()
  .Build();

return await app.RunAsync(args);
```

```bash
dotnet run -- add 15 25
# Output: 15 + 25 = 40
```

**→ [Full Getting Started Guide](documentation/user/getting-started.md)**

## ✨ Key Features

| Feature | Description | Learn More |
|---------|-------------|------------|
| 🎯 **Web-Style Routing** | Familiar `"deploy {env} --version {tag}"` syntax | [Routing Guide](documentation/user/features/routing.md) |
| 📦 **Endpoint DSL** | Class-based commands with `DiscoverEndpoints()` auto-discovery | [Architecture Choices](documentation/user/guides/architecture-choices.md) |
| 🔧 **Fluent DSL** | Inline routes with `.Map().WithHandler().Done()` chain | [Architecture Choices](documentation/user/guides/architecture-choices.md) |
| 🛡️ **Roslyn Analyzer** | Catch route errors at compile-time | [Analyzer Docs](documentation/user/features/analyzer.md) |
| ⌨️ **Shell Completion** | Tab completion for bash, zsh, PowerShell, fish | [Shell Completion](#-shell-completion) |
| 🤖 **MCP Server** | AI-assisted development with Claude | [MCP Server Guide](documentation/user/tools/mcp-server.md) |
| 📊 **Logging Package** | Zero-overhead structured logging | [Logging Docs](documentation/user/features/logging.md) |
| 🚀 **Native AOT** | Zero warnings, 3.3 MB binaries, instant startup | [Deployment Guide](documentation/user/guides/deployment.md#native-aot-compilation) |
| 🔒 **Type-Safe Parameters** | Automatic type conversion and validation | [Supported Types](documentation/user/reference/supported-types.md) |
| 📖 **Auto-Help** | Generate help from route patterns | [Auto-Help Feature](documentation/user/features/auto-help.md) |
| 🎨 **Colored Output** | Fluent ANSI colors without Spectre.Console | [Terminal Abstractions](documentation/user/features/terminal-abstractions.md) |

## 📚 Documentation

### Getting Started
- **[Getting Started Guide](documentation/user/getting-started.md)** - Build your first CLI app in 5 minutes
- **[Use Cases](documentation/user/use-cases.md)** - Greenfield apps & progressive enhancement patterns
- **[Architecture Choices](documentation/user/guides/architecture-choices.md)** - Choose Endpoint DSL or Fluent DSL

### Core Features
- **[Routing Patterns](documentation/user/features/routing.md)** - Complete route syntax reference
- **[Roslyn Analyzer](documentation/user/features/analyzer.md)** - Compile-time validation
- **[Logging System](documentation/user/features/logging.md)** - Structured logging setup
- **[Auto-Help](documentation/user/features/auto-help.md)** - Automatic help generation
- **[Output Handling](documentation/user/features/output-handling.md)** - stdout/stderr best practices
- **[Terminal Abstractions](documentation/user/features/terminal-abstractions.md)** - Testable I/O & colored output

### Tools & Deployment
- **[MCP Server](documentation/user/tools/mcp-server.md)** - AI-powered development assistance
- **[Deployment Guide](documentation/user/guides/deployment.md)** - Native AOT, runfiles, distribution
- **[Best Practices](documentation/user/guides/best-practices.md)** - Patterns for maintainable CLIs

### Reference
- **[Performance Benchmarks](documentation/user/reference/performance.md)** - Detailed performance metrics
- **[Supported Types](documentation/user/reference/supported-types.md)** - Complete type reference
- **[API Documentation](documentation/user/reference/)** - Technical reference

## 🎯 Two Powerful Use Cases

### 🆕 Greenfield CLI Applications
Build modern command-line tools from scratch:

```
myapp/
├── calculator.cs       # Single runfile - just 5 lines
└── endpoints/
    ├── add-command.cs
    ├── factorial-command.cs
    └── ...
```

**Endpoint DSL approach** (class-based, organized by file):
```csharp
// In endpoints/add-command.cs
[NuruRoute("add", Description = "Add two numbers")]
public sealed class AddCommand : ICommand<Unit>
{
  [Parameter] public double X { get; set; }
  [Parameter] public double Y { get; set; }

  public sealed class Handler : ICommandHandler<AddCommand, Unit>
  {
    public ValueTask<Unit> Handle(AddCommand c, CancellationToken ct)
    {
      Console.WriteLine($"{c.X} + {c.Y} = {c.X + c.Y}");
      return default;
    }
  }
}
```

**Fluent DSL approach** (inline definitions):
```csharp
NuruApp.CreateBuilder()
  .Map("deploy {env} --version {tag?}")
    .WithHandler((string env, string? tag) => Deploy(env, tag))
    .AsCommand()
    .Done()
  .Build();
```

### 🔄 Progressive Enhancement
Wrap existing CLIs to add auth, logging, or validation:

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .Map("deploy prod")
    .WithHandler(async () =>
    {
      if (!await ValidateAccess()) return 1;
      return await Shell.ExecuteAsync("existing-cli", "deploy", "prod");
    })
    .AsCommand()
    .Done()
  .Map("{*args}")
    .WithHandler(async (string[] args) => await Shell.ExecuteAsync("existing-cli", args))
    .AsCommand()
    .Done()
  .Build();
```

**→ [Detailed Use Cases with Examples](documentation/user/use-cases.md)**

## 🌟 Working Examples

**[Calculator Samples](samples/02-calculator/)** - Three complete implementations you can run now:
- **[01-calc-endpoints.cs](samples/02-calculator/01-calc-endpoints.cs)** - Endpoint DSL pattern (testable, DI)
- **[02-calc-fluent.cs](samples/02-calculator/02-calc-fluent.cs)** - Fluent DSL approach (inline handlers)
- **[03-calc-mixed.cs](samples/02-calculator/03-calc-mixed.cs)** - Mixed approach (both patterns together)

```bash
./samples/02-calculator/01-calc-endpoints.cs add 10 20    # Endpoint DSL: structured
./samples/02-calculator/02-calc-fluent.cs factorial 5      # Fluent DSL: inline
```

**[AOT Example](samples/05-aot-example/)** - Native AOT compilation with source generators

## ⚡ Performance

| Implementation | Memory | Speed (37 tests) | Binary Size |
|----------------|--------|------------------|-------------|
| Direct (JIT) | ~4 KB | 2.49s | N/A |
| Direct (AOT) | ~4 KB | **0.30s** 🚀 | 3.3 MB |
| Endpoints (AOT) | Moderate | **0.42s** 🚀 | 4.8 MB |

**Native AOT is 88-93% faster than JIT** → [Full Performance Benchmarks](documentation/user/reference/performance.md)

## 🤖 AI-Powered Development

**For AI agents:** Load the built-in [Nuru Skill](skills/nuru/SKILL.md) for instant access to:
- Complete DSL syntax and patterns
- Testing with TestTerminal
- Route examples and type conversion

> 💡 **Tip:** No MCP installation needed - the skill provides all essential patterns.

**For MCP Server:** Install for Claude Code, Roo Code, or Continue:

```bash
dotnet tool install --global TimeWarp.Nuru.Mcp
```

Get instant help:
- Validate route patterns before writing code
- Generate handler code automatically
- Get syntax examples on demand
- Real-time error guidance

**→ [MCP Server Setup Guide](documentation/user/tools/mcp-server.md)**

## ⌨️ Shell Completion

Enable tab completion for your CLI with one line of code:

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .Map("deploy {env} --version {tag}")
    .WithHandler((string env, string tag) => Deploy(env, tag))
    .AsCommand()
    .Done()
  .Map("status")
    .WithHandler(() => ShowStatus())
    .AsQuery()
    .Done()
  .EnableStaticCompletion()  // ← Add this
  .Build();
```

Generate completion scripts for your shell:

```bash
# Bash
./myapp --generate-completion bash >> ~/.bashrc

# Zsh
./myapp --generate-completion zsh >> ~/.zshrc

# PowerShell
./myapp --generate-completion powershell >> $PROFILE

# Fish
./myapp --generate-completion fish > ~/.config/fish/completions/myapp.fish
```

**Supports:**
- ✅ Command completion (`deploy`, `status`)
- ✅ Option completion (`--version`, `--force`)
- ✅ Short option aliases (`-v`, `-f`)
- ✅ All 4 major shells (bash, zsh, PowerShell, fish)

**See [completion-example](samples/15-completion/) for a complete working example.**

## 🤝 Contributing

We welcome contributions! See [CONTRIBUTING.md](CONTRIBUTING.md) for details.

**For Contributors:**
- **[Developer Documentation](documentation/developer/overview.md)** - Architecture and design
- **[Standards](documentation/developer/standards/)** - Coding conventions

## 📄 License

This project is licensed under the Unlicense - see the [license](license) file for details.

---

<div align="center">

**Ready to build powerful CLI applications?**

**[Get Started in 5 Minutes](documentation/user/getting-started.md)** • **[View Examples](samples/02-calculator/)** • **[Read the Docs](documentation/user/overview.md)**

</div>
