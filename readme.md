# TimeWarp.Nuru

<div align="center">

[![NuGet Version](https://img.shields.io/nuget/v/TimeWarp.Nuru.svg)](https://www.nuget.org/packages/TimeWarp.Nuru/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/TimeWarp.Nuru.svg)](https://www.nuget.org/packages/TimeWarp.Nuru/)
[![Build Status](https://img.shields.io/github/actions/workflow/status/TimeWarpEngineering/timewarp-nuru/build.yml?branch=master)](https://github.com/TimeWarpEngineering/timewarp-nuru/actions)
[![License](https://img.shields.io/github/license/TimeWarpEngineering/timewarp-nuru.svg)](https://github.com/TimeWarpEngineering/timewarp-nuru/blob/master/LICENSE)

**Route-based CLI framework for .NET - bringing web-style routing to command-line applications**

</div>

> **Nuru** means "light" in Swahili - illuminating the path to your commands with clarity and simplicity.

## 📦 Installation

```bash
dotnet add package TimeWarp.Nuru
```

## 🚀 Quick Start

TimeWarp.Nuru offers two first-class patterns for defining CLI commands:

### Fluent DSL

Define routes inline with a fluent builder API:

```csharp
using TimeWarp.Nuru;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("add {x:double} {y:double}")
    .WithHandler((double x, double y) => Console.WriteLine($"{x} + {y} = {x + y}"))
    .AsCommand()
    .Done()
  .Build();

return await app.RunAsync(args);
```

### Attributed Routes

Define routes as classes with `[NuruRoute]` attributes:

```csharp
using TimeWarp.Nuru;

// Program.cs - minimal setup
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Build();

return await app.RunAsync(args);

// AddCommand.cs - auto-discovered by source generator
[NuruRoute("add", Description = "Add two numbers together")]
public sealed class AddCommand : ICommand<Unit>
{
  [Parameter(Order = 0)]
  public double X { get; set; }

  [Parameter(Order = 1)]
  public double Y { get; set; }

  public sealed class Handler : ICommandHandler<AddCommand, Unit>
  {
    public ValueTask<Unit> Handle(AddCommand command, CancellationToken ct)
    {
      Console.WriteLine($"{command.X} + {command.Y} = {command.X + command.Y}");
      return default;
    }
  }
}
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
| ⚡ **Dual Approach** | Mix Direct (fast) + Mediator (structured) | [Architecture Choices](documentation/user/guides/architecture-choices.md) |
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
- **[Architecture Choices](documentation/user/guides/architecture-choices.md)** - Choose Direct, Mediator, or Mixed

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

```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("deploy {env} --version {tag?}")
    .WithHandler((string env, string? tag) => Deploy(env, tag))
    .AsCommand()
    .Done()
  .Map("backup {source} {dest?} --compress")
    .WithHandler((string source, string? dest, bool compress) => Backup(source, dest, compress))
    .AsCommand()
    .Done()
  .Build();
```

### 🔄 Progressive Enhancement
Wrap existing CLIs to add auth, logging, or validation:

```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
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
- **[01-calc-delegate.cs](samples/02-calculator/01-calc-delegate.cs)** - Fluent DSL approach (inline handlers)
- **[02-calc-commands.cs](samples/02-calculator/02-calc-commands.cs)** - Attributed routes pattern (testable, DI)
- **[03-calc-mixed.cs](samples/02-calculator/03-calc-mixed.cs)** - Mixed approach (both patterns together)

```bash
./samples/02-calculator/03-calc-mixed.cs add 10 20        # Fluent DSL: inline
./samples/02-calculator/03-calc-mixed.cs factorial 5      # Attributed: structured
```

**[AOT Example](samples/05-aot-example/)** - Native AOT compilation with source generators

## ⚡ Performance

| Implementation | Memory | Speed (37 tests) | Binary Size |
|----------------|--------|------------------|-------------|
| Direct (JIT) | ~4 KB | 2.49s | N/A |
| Direct (AOT) | ~4 KB | **0.30s** 🚀 | 3.3 MB |
| Mediator (AOT) | Moderate | **0.42s** 🚀 | 4.8 MB |

**Native AOT is 88-93% faster than JIT** → [Full Performance Benchmarks](documentation/user/reference/performance.md)

## 🤖 AI-Powered Development

Install the MCP server for AI assistance:

```bash
dotnet tool install --global TimeWarp.Nuru.Mcp
```

Get instant help in Claude Code, Roo Code, or Continue:
- Validate route patterns before writing code
- Generate handler code automatically
- Get syntax examples on demand
- Real-time error guidance

**→ [MCP Server Setup Guide](documentation/user/tools/mcp-server.md)**

## ⌨️ Shell Completion

Enable tab completion for your CLI with one line of code:

```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
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
