# Shell Completion

Automatic tab completion for your CLI applications across bash, zsh, PowerShell, and fish shells.

## Overview

TimeWarp.Nuru.Completion generates shell-specific completion scripts from your route definitions, providing intelligent tab completion for:

- Command names
- Parameters (with type-aware hints)
- Options and flags (`--long`, `-short`)
- Enum values (all possible values)
- File and directory paths

**Key Benefits:**
- ⚡ **One line to enable**: `.EnableStaticCompletion()`
- 🎯 **Automatic**: Generates from your existing route definitions
- 🌐 **Cross-platform**: Supports 4 major shells
- 📦 **Static**: No runtime overhead, completions computed at build time
- 🔒 **Type-aware**: Knows parameter types and suggests appropriately

## Quick Start

### 1. Install the Package

```bash
dotnet add package TimeWarp.Nuru.Completion
```

### 2. Enable Shell Completion

```csharp
using TimeWarp.Nuru;
using TimeWarp.Nuru.Completion;

NuruApp app = new NuruAppBuilder()
    .Map("deploy {env} --version {tag}", (string env, string tag) => Deploy(env, tag))
    .Map("status", () => ShowStatus())
    .EnableStaticCompletion()  // ← Add this one line
    .Build();

return await app.RunAsync(args);
```

### 3. Generate Completion Script

After building your application:

```bash
# Generate completion for your shell
./myapp --generate-completion bash   # For bash
./myapp --generate-completion zsh    # For zsh
./myapp --generate-completion pwsh   # For PowerShell
./myapp --generate-completion fish   # For fish
```

### 4. Install the Script

#### Bash (Linux/macOS)

```bash
# Append to your .bashrc
./myapp --generate-completion bash >> ~/.bashrc
source ~/.bashrc
```

#### Zsh (macOS/Linux)

```bash
# Append to your .zshrc
./myapp --generate-completion zsh >> ~/.zshrc
source ~/.zshrc
```

#### PowerShell (Windows)

```powershell
# Append to your PowerShell profile
./myapp --generate-completion pwsh >> $PROFILE
. $PROFILE
```

#### Fish (Linux/macOS)

```bash
# Create completion file in fish config
./myapp --generate-completion fish > ~/.config/fish/completions/myapp.fish
```

## What Gets Completed

### Command Names

All root-level literal routes become completable commands:

```csharp
builder.Map("deploy {env}", (string env) => Deploy(env));
builder.Map("status", () => Status());
builder.Map("version", () => Version());
```

```bash
$ ./myapp <TAB>
deploy    status    version
```

### Parameters

Parameter names are shown as completion hints:

```csharp
builder.Map("deploy {env}", (string env) => Deploy(env));
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
builder.Map("build --config {mode} --verbose", (string mode, bool verbose) => Build(mode, verbose));
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

builder.Map("log --level {level}", (LogLevel level) => SetLogLevel(level));
```

```bash
$ ./myapp log --level <TAB>
Debug    Info    Warning    Error
```

### Catch-All Parameters

Catch-all parameters trigger file/directory completion:

```csharp
builder.Map("echo {*words}", (string[] words) => Echo(words));
```

```bash
$ ./myapp echo <TAB>
# Shell's native file completion
```

## Examples

### Simple Commands

```csharp
using TimeWarp.Nuru;
using TimeWarp.Nuru.Completion;

NuruAppBuilder builder = new();

builder.Map("createorder {product} {quantity:int}",
    (string product, int quantity) => CreateOrder(product, quantity));

builder.Map("status", () => ShowStatus());

builder.EnableStaticCompletion();

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
builder.Map("git commit -m {message} --amend?", (string message, bool amend) => Commit(message, amend));
builder.Map("git status --short?", (bool short) => Status(short));
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

builder.Map("deploy {env}", (Environment env) => Deploy(env));
builder.EnableStaticCompletion();
```

**Tab completion behavior:**

```bash
$ ./myapp deploy <TAB>
Development    Staging    Production
```

### Complex Routes

```csharp
builder.Map("docker run {image} {*args}", (string image, string[] args) => DockerRun(image, args));
builder.Map("docker ps --all?", (bool all) => DockerPs(all));
builder.Map("docker logs {container} --follow?", (string container, bool follow) => DockerLogs(container, follow));
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

## Static vs Dynamic Completion

TimeWarp.Nuru.Completion provides **static completion** by default:

### Static Completion (Current Implementation)

**All completion candidates are known at build time:**
- Command names from route definitions
- Option names from route patterns
- Enum values from parameter types
- File paths (delegated to shell)

**Advantages:**
- ⚡ Instant (0ms latency)
- 🔒 No runtime dependencies
- 🎯 Works offline
- ✅ Covers 90%+ of use cases

**Example:**

```csharp
public enum LogLevel { Debug, Info, Warning, Error }

builder.Map("log --level {level}", (LogLevel level) => SetLogLevel(level));
builder.EnableStaticCompletion();
```

The completion script contains: `Debug Info Warning Error`

### Dynamic Completion (Optional Enhancement)

**Runtime-computed completion candidates:**
- Environment names from a configuration service
- Database record IDs from a query
- Context-aware suggestions based on previous arguments

**Trade-offs:**
- ⏱️ Slower (subprocess call on every Tab press)
- 🔄 Requires app to be executable at completion time
- 🌐 May need network/database access
- 🛠️ More complex to implement

**Status:** Optional feature, see [Task 026](../../../kanban/backlog/026-dynamic-shell-completion-optional.md) if your use case requires runtime-computed completions.

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

Static completion has **zero runtime overhead**:

- **Bash/Zsh/Fish**: <1ms (native shell lookup)
- **PowerShell**: <5ms (PSReadLine integration)

All completions are pre-computed and embedded in the shell script.

## Best Practices

### 1. Descriptive Parameter Names

Use clear parameter names that make sense as completion hints:

```csharp
// ✅ Good
builder.Map("deploy {environment}", (string environment) => Deploy(environment));

// ❌ Less helpful
builder.Map("deploy {env}", (string env) => Deploy(env));
```

### 2. Use Enums for Fixed Value Sets

Convert string parameters with known values to enums:

```csharp
// ✅ Good - automatic completion of all values
public enum Environment { Dev, Staging, Prod }
builder.Map("deploy {env}", (Environment env) => Deploy(env));

// ❌ Missed opportunity
builder.Map("deploy {env}", (string env) => Deploy(env));
```

### 3. Group Related Commands

Use consistent command prefixes for related functionality:

```csharp
builder.Map("docker run {image}", (string image) => DockerRun(image));
builder.Map("docker ps", () => DockerPs());
builder.Map("docker logs {container}", (string container) => DockerLogs(container));

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
builder.Map("build --verbose", (bool verbose) => Build(verbose));
builder.Map("test --verbose", (bool verbose) => Test(verbose));
builder.Map("deploy --verbose", (bool verbose) => Deploy(verbose));
```

## API Reference

### EnableStaticCompletion()

Enables shell completion support by registering the `--generate-completion` route:

```csharp
public NuruAppBuilder EnableStaticCompletion(string? appName = null)
```

**Parameters:**
- `appName` (optional): The application name used in completion functions. If not provided, uses the executable name.

**Returns:** The builder instance for method chaining.

**Example:**

```csharp
NuruAppBuilder builder = new();
builder.EnableStaticCompletion();  // Uses executable name
// or
builder.EnableStaticCompletion("myapp");  // Explicit name
```

### Generated Route

When you call `EnableStaticCompletion()`, it registers this hidden route:

```csharp
"--generate-completion {shell}"
```

**Shell parameter values:** `bash`, `zsh`, `pwsh`, `fish`

**Output:** Complete shell completion script to stdout

## Related Documentation

- **[Getting Started](../getting-started.md#shell-completion-tab-completion)** - Quick setup guide
- **[Shell Completion Example](../../../samples/shell-completion-example/)** - Complete working example
- **[Task 026: Dynamic Completion](../../../kanban/backlog/026-dynamic-shell-completion-optional.md)** - Optional runtime-computed completions

## Learn More

- **Sample Application:** [samples/shell-completion-example/](../../../samples/shell-completion-example/)
- **Test Coverage:** [Tests/TimeWarp.Nuru.Completion.Tests/](../../../Tests/TimeWarp.Nuru.Completion.Tests/) (135 tests)
- **Source Code:** [Source/TimeWarp.Nuru.Completion/](../../../Source/TimeWarp.Nuru.Completion/)
