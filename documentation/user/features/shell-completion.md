# Shell Completion

Automatic tab completion for your CLI applications across bash, zsh, PowerShell, and fish shells.

## Overview

Nuru generates shell-specific completion scripts from your route definitions, providing intelligent tab completion for:

- Command names
- Parameters (with type-aware hints)
- Options and flags (`--long`, `-short`)
- Enum values (all possible values)
- File and directory paths

**Key Benefits:**
- ⚡ **One call to enable**: `.EnableCompletion()` on the builder
- 🎯 **Automatic**: Generates from your existing route definitions
- 🌐 **Cross-platform**: Supports 4 major shells
- 🔄 **Dynamic**: Runtime-computed completions for databases, APIs, config
- 🔒 **Type-aware**: Knows parameter types and suggests appropriately

## Quick Start

### Enabling Completion

Call `.EnableCompletion()` on the builder to enable dynamic shell completion:

```csharp
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .Map("deploy {env} --version {tag}")
    .WithHandler((string env, string tag) => Deploy(env, tag))
    .AsCommand()
    .Done()
  .Map("status")
    .WithHandler(() => ShowStatus())
    .AsQuery()
    .Done()
  .EnableCompletion()
  .Build();

return await app.RunAsync(args);
```

`EnableCompletion()` registers these routes:
- `__complete {index:int} {*words}` - Dynamic completion callback for shells
- `--generate-completion {shell}` - Generate shell-specific completion scripts
- `--install-completion {shell?}` - Install completion to shell config files
- `--install-completion --dry-run {shell?}` - Preview installation

### Customizing Completion Sources

Pass a `configure` callback to `EnableCompletion` to register custom completion sources:

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .Map("deploy {env} --version {tag}")
    .WithHandler((string env, string tag) => Deploy(env, tag))
    .AsCommand()
    .Done()
  .EnableCompletion(configure: registry =>
  {
    // Complete "env" parameter from a list
    registry.RegisterForParameter("env", new StaticCompletionSource("dev", "staging", "prod"));

    // Complete "tag" parameter dynamically (could query Git, Docker registry, etc.)
    registry.RegisterForParameter("tag", new TagCompletionSource());
  })
  .Build();
```

### 3. Install Shell Completion

#### Automatic Installation (Recommended)

The easiest way to install completion - single command, auto-detects your shell:

```bash
# Auto-detect shell and install
./myapp --install-completion

# Or specify shell explicitly
./myapp --install-completion bash
./myapp --install-completion zsh
./myapp --install-completion fish
./myapp --install-completion pwsh

# Preview what will be installed (dry-run)
./myapp --install-completion --dry-run
```

**Installation Paths:**

| Shell | Path | Auto-loads? |
|-------|------|-------------|
| **Bash** | `~/.local/share/bash-completion/completions/<appname>` | ✅ Yes |
| **Fish** | `~/.config/fish/completions/<appname>.fish` | ✅ Yes |
| **Zsh** | `~/.local/share/zsh/site-functions/_<appname>` | ⚠️ One-time fpath setup |
| **PowerShell** | `~/.local/share/nuru/completions/<appname>.ps1` | ⚠️ One-time profile setup |

#### Manual Installation (Alternative)

If you prefer manual control, generate and install scripts yourself:

##### Bash (Linux/macOS)

```bash
# Option 1: Source directly (temporary)
source <(./myapp --generate-completion bash)

# Option 2: Append to profile (permanent)
./myapp --generate-completion bash >> ~/.bashrc
source ~/.bashrc
```

##### Zsh (macOS/Linux)

```bash
# Option 1: Source directly (temporary)
source <(./myapp --generate-completion zsh)

# Option 2: Write to file (permanent)
mkdir -p ~/.zsh/completions
./myapp --generate-completion zsh > ~/.zsh/completions/_myapp
# Add to ~/.zshrc: fpath=(~/.zsh/completions $fpath) && autoload -Uz compinit && compinit
```

##### PowerShell (Windows/macOS/Linux)

```powershell
# Option 1: Invoke directly (temporary)
& ./myapp --generate-completion pwsh | Out-String | Invoke-Expression

# Option 2: Append to profile (permanent)
./myapp --generate-completion pwsh >> $PROFILE
. $PROFILE
```

##### Fish (Linux/macOS)

```bash
# Fish auto-loads from this directory
mkdir -p ~/.config/fish/completions
./myapp --generate-completion fish > ~/.config/fish/completions/myapp.fish
```

## What Gets Completed

### Command Names

All root-level literal routes become completable commands:

```csharp
builder.Map("deploy {env}")
  .WithHandler((string env) => Deploy(env))
  .AsCommand()
  .Done();
builder.Map("status")
  .WithHandler(() => Status())
  .AsQuery()
  .Done();
builder.Map("version")
  .WithHandler(() => Version())
  .AsQuery()
  .Done();
```

```bash
$ ./myapp <TAB>
deploy    status    version
```

### Parameters

Parameter names are shown as completion hints:

```csharp
builder.Map("deploy {env}")
  .WithHandler((string env) => Deploy(env))
  .AsCommand()
  .Done();
```

```bash
$ ./myapp deploy <TAB>
{env}
```

For string parameters, the shell's native file completion activates:

```bash
$ ./myapp process --file <TAB>
config.json    data.xml    settings.toml
```

### Options (Flags)

All options defined in routes are completable:

```csharp
builder.Map("build --config {mode} --verbose")
  .WithHandler((string mode, bool verbose) => Build(mode, verbose))
  .AsCommand()
  .Done();
```

```bash
$ ./myapp build --<TAB>
--config    --verbose

$ ./myapp build -<TAB>
--config    --verbose
```

### Enum Values

Enum parameters automatically complete with all possible values:

```csharp
public enum LogLevel { Debug, Info, Warning, Error }

builder.Map("log --level {level}")
  .WithHandler((LogLevel level) => SetLogLevel(level))
  .AsCommand()
  .Done();
```

```bash
$ ./myapp log --level <TAB>
Debug    Info    Warning    Error
```

### Catch-All Parameters

Catch-all parameters trigger file/directory completion:

```csharp
builder.Map("echo {*words}")
  .WithHandler((string[] words) => Echo(words))
  .AsQuery()
  .Done();
```

```bash
$ ./myapp echo <TAB>
# Shell's native file completion
```

## Examples

### Simple Commands

```csharp
using TimeWarp.Nuru;

NuruAppBuilder builder = NuruApp.CreateBuilder();

builder.Map("createorder {product} {quantity:int}")
  .WithHandler((string product, int quantity) => CreateOrder(product, quantity))
  .AsCommand()
  .Done();

builder.Map("status")
  .WithHandler(() => ShowStatus())
  .AsQuery()
  .Done();

builder.EnableCompletion();

NuruApp app = builder.Build();
return await app.RunAsync(args);
```

**Tab completion behavior:**

```bash
$ ./myapp cre<TAB>
createorder

$ ./myapp createorder <TAB>
{product}

$ ./myapp createorder laptop <TAB>
{quantity:int}

$ ./myapp st<TAB>
status
```

### With Options

```csharp
builder.Map("git commit -m {message} --amend?")
  .WithHandler((string message, bool amend) => Commit(message, amend))
  .AsCommand()
  .Done();
builder.Map("git status --short?")
  .WithHandler((bool short) => Status(short))
  .AsCommand()
  .Done();
```

**Tab completion behavior:**

```bash
$ ./myapp git <TAB>
commit    status

$ ./myapp git commit -<TAB>
-m    --amend

$ ./myapp git status --<TAB>
--short
```

### With Enums

```csharp
public enum Environment { Development, Staging, Production }

builder.Map("deploy {env}")
  .WithHandler((Environment env) => Deploy(env))
  .AsCommand()
  .Done();
builder.EnableCompletion();
```

**Tab completion behavior:**

```bash
$ ./myapp deploy <TAB>
Development    Staging    Production
```

### Complex Routes

```csharp
builder.Map("docker run {image} {*args}")
  .WithHandler((string image, string[] args) => DockerRun(image, args))
  .AsCommand()
  .Done();
builder.Map("docker ps --all?")
  .WithHandler((bool all) => DockerPs(all))
  .AsCommand()
  .Done();
builder.Map("docker logs {container} --follow?")
  .WithHandler((string container, bool follow) => DockerLogs(container, follow))
  .AsCommand()
  .Done();
```

**Tab completion behavior:**

```bash
$ ./myapp docker <TAB>
run    ps    logs

$ ./myapp docker run <TAB>
{image}

$ ./myapp docker ps --<TAB>
--all

$ ./myapp docker logs <TAB>
{container}

$ ./myapp docker logs mycontainer --<TAB>
--follow
```

## Shell-Specific Details

### Bash

Bash completion uses `compgen` and the `COMPREPLY` array:

- Completes commands from the function list
- Uses `compgen -W` for word list completion
- Uses `compgen -f` for file completion
- Respects `COMP_CWORD` for cursor position

**Compatibility:** Bash 4.0+ recommended (3.2+ supported)

### Zsh

Zsh completion uses the powerful `_arguments` function:

- Supports command descriptions
- Handles options with `-` and `--` prefixes
- Integrates with zsh's completion menu
- Supports option descriptions

**Compatibility:** Zsh 5.0+

### PowerShell

PowerShell completion uses `Register-ArgumentCompleter`:

- Completes based on `CommandAst` parsing
- Supports option descriptions in the completion tooltip
- Uses `CompletionResult` objects
- Integrates with PSReadLine menu completion

**Compatibility:** PowerShell 5.1+ (Core 7.0+ recommended)

### Fish

Fish completion uses the declarative `complete` command:

- Uses `--no-files` for command completion
- Uses `--require-parameter` for options requiring values
- Supports completion descriptions
- Integrates with fish's completion pager

**Compatibility:** Fish 3.0+

## How Completion Works

`EnableCompletion()` enables **dynamic completion**: the shell calls your app via `__complete` at Tab-press time, and the app computes candidates at runtime.

**Runtime-computed completion candidates:**
- Query databases, APIs, configuration services at Tab-press time
- Context-aware suggestions based on previous arguments
- Custom completion sources for any parameter
- Command names, option names, and enum values come from your route definitions

**Advantages:**
- 🔄 **Live data**: Completions reflect current state
- 🎯 **Context-aware**: Different suggestions based on previous args
- 🛠️ **Extensible**: Register custom completion sources

**Performance:** AOT-compiled apps have ~7-10ms invocation time, imperceptible to users.

**Example:**

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .Map("deploy {env}")
    .WithHandler((string env) => Deploy(env))
    .AsCommand()
    .Done()
  .EnableCompletion(configure: registry =>
  {
    // Register custom completion source for environments
    registry.RegisterForParameter("env", new EnvironmentCompletionSource());
  })
  .Build();
```

When you press Tab, the shell calls your app via `__complete`, which queries your completion source.

## Troubleshooting

### Completion Not Working

**1. Verify completion script was installed:**

```bash
# Bash/Zsh
type _myapp_completion
# Should show the function definition

# PowerShell
Get-ArgumentCompleter -CommandName myapp
# Should show the completer registration

# Fish
complete -C myapp
# Should show completion commands
```

**2. Reload your shell configuration:**

```bash
# Bash
source ~/.bashrc

# Zsh
source ~/.zshrc

# PowerShell
. $PROFILE

# Fish (no reload needed - completions are auto-loaded)
```

**3. Check for syntax errors in generated script:**

```bash
# Generate to a file first
./myapp --generate-completion bash > /tmp/completion-test.sh

# Check for errors
bash -n /tmp/completion-test.sh
```

### Completions Not Updating

Regenerate and reinstall the completion script after modifying routes:

```bash
# Remove old completion from config file
# Then regenerate and append
./myapp --generate-completion bash >> ~/.bashrc
source ~/.bashrc
```

### File Completion Not Working

Ensure string parameters don't have restrictive patterns that prevent file completion. The shell provides file completion for string parameters automatically.

## Performance

### Generation Time

Completion script generation happens once, typically during installation:

- **Small CLI (5-10 routes)**: <1ms
- **Medium CLI (50 routes)**: <5ms
- **Large CLI (200+ routes)**: <20ms

### Completion Latency

Dynamic completion invokes your app on each Tab press. AOT-compiled apps respond in ~7-10ms, which is imperceptible to users. Non-AOT apps pay the normal JIT startup cost per Tab press.

## Best Practices

### 1. Descriptive Parameter Names

Use clear parameter names that make sense as completion hints:

```csharp
// ✅ Good
builder.Map("deploy {environment}")
  .WithHandler((string environment) => Deploy(environment))
  .AsCommand()
  .Done();

// ❌ Less helpful
builder.Map("deploy {env}")
  .WithHandler((string env) => Deploy(env))
  .AsCommand()
  .Done();
```

### 2. Use Enums for Fixed Value Sets

Convert string parameters with known values to enums:

```csharp
// ✅ Good - automatic completion of all values
public enum Environment { Dev, Staging, Prod }
builder.Map("deploy {env}")
  .WithHandler((Environment env) => Deploy(env))
  .AsCommand()
  .Done();

// ❌ Missed opportunity
builder.Map("deploy {env}")
  .WithHandler((string env) => Deploy(env))
  .AsCommand()
  .Done();
```

### 3. Group Related Commands

Use consistent command prefixes for related functionality:

```csharp
builder.Map("docker run {image}")
  .WithHandler((string image) => DockerRun(image))
  .AsCommand()
  .Done();
builder.Map("docker ps")
  .WithHandler(() => DockerPs())
  .AsCommand()
  .Done();
builder.Map("docker logs {container}")
  .WithHandler((string container) => DockerLogs(container))
  .AsCommand()
  .Done();

// Tab: ./myapp docker <TAB> → run, ps, logs
```

### 4. Document Installation in Your README

Include shell completion setup in your project's installation instructions:

```markdown
## Installation

1. Install the application
2. Enable tab completion:

   ```bash
   # Bash
   ./myapp --generate-completion bash >> ~/.bashrc
   source ~/.bashrc
   ```
```

### 5. Consistent Option Naming

Use standard option names for common functionality:

```csharp
builder.Map("build --verbose")
  .WithHandler((bool verbose) => Build(verbose))
  .AsCommand()
  .Done();
builder.Map("test --verbose")
  .WithHandler((bool verbose) => Test(verbose))
  .AsCommand()
  .Done();
builder.Map("deploy --verbose")
  .WithHandler((bool verbose) => Deploy(verbose))
  .AsCommand()
  .Done();
```

## API Reference

### EnableCompletion()

Enable dynamic completion and optionally register custom completion sources:

```csharp
public static TBuilder EnableCompletion<TBuilder>(
    this TBuilder builder,
    string? appName = null,
    Action<CompletionSourceRegistry>? configure = null)
    where TBuilder : NuruAppBuilder
```

**Parameters:**
- `appName` (optional): Application name for completion functions. Defaults to executable name.
- `configure` (optional): Action to register custom completion sources.

**Example:**

```csharp
NuruApp.CreateBuilder()
  .EnableCompletion(configure: registry =>
  {
    registry.RegisterForParameter("env", new StaticCompletionSource("dev", "staging", "prod"));
    registry.RegisterForType(typeof(MyEnum), new EnumCompletionSource<MyEnum>());
  });
```

**Registers these routes:**
- `__complete {index:int} {*words}` - Callback for dynamic completion
- `--generate-completion {shell}` - Generate shell completion scripts
- `--install-completion {shell?}` - Install completion to shell config
- `--install-completion --dry-run {shell?}` - Preview installation

**Shell parameter values:** `bash`, `zsh`, `pwsh`, `fish`

## Related Documentation

- **[Getting Started](../getting-started.md#shell-completion-tab-completion)** - Quick setup guide
- **[Shell Completion Example](../../../samples/shell-completion-example/)** - Complete working example
- **[Task 026: Dynamic Completion](../../../kanban/backlog/026-dynamic-shell-completion-optional.md)** - Optional runtime-computed completions

## Learn More

- **Sample Application:** [samples/shell-completion-example/](../../../samples/shell-completion-example/)
