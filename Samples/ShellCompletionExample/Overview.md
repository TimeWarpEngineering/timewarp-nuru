# Shell Completion Example

This example demonstrates how to add shell tab completion to TimeWarp.Nuru CLI applications, addressing [GitHub Issue #30](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/30).

## Files

- `ShellCompletionExample.cs` - .NET 10 runfile demonstrating shell completion scenarios

## What is Shell Completion?

Shell completion (also called tab completion) allows users to press the Tab key to:
- Complete partial command names (e.g., `cre<TAB>` → `createorder`)
- See available commands when multiple matches exist
- Discover available options (e.g., `--v<TAB>` → `--version`)
- Improve typing speed and reduce errors

## Key Features Demonstrated

### 1. Enabling Shell Completion

```csharp
#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Completion/TimeWarp.Nuru.Completion.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Completion;

var builder = new NuruAppBuilder();

// Enable shell completion (auto-detects executable name)
builder.EnableShellCompletion();

// Register your routes as normal
builder.AddRoute("createorder {product} {quantity:int}", (string product, int quantity) =>
{
    Console.WriteLine($"✅ Creating order:");
    Console.WriteLine($"   Product: {product}");
    Console.WriteLine($"   Quantity: {quantity}");
    return 0;
});

NuruApp app = builder.Build();
return await app.RunAsync(args);
```

The `EnableShellCompletion()` method:
- Automatically registers a `--generate-completion {shell}` route
- Auto-detects the executable name at runtime (no hardcoded name needed)
- Works correctly with renamed executables after publishing

**Note:** This example uses .NET 10's runfile feature with `#:project` directives to reference local projects.

### 2. Supported Shells

TimeWarp.Nuru.Completion supports all major shells:

- **Bash** - Linux/macOS default, uses `complete -F` mechanism
- **Zsh** - macOS default (Catalina+), uses `_arguments` completion system
- **PowerShell** - Windows, uses `Register-ArgumentCompleter`
- **Fish** - Alternative shell, uses declarative `complete` commands

### 3. Command Name Completion

The completion system extracts command literals from your route patterns:

```csharp
builder.AddRoute("createorder {product} {quantity:int}", ...);
builder.AddRoute("create {item}", ...);
builder.AddRoute("status", ...);
builder.AddRoute("deploy {env} --version {ver}", ...);
```

Typing `cre<TAB>` will show both `create` and `createorder` as options.

### 4. Option Completion

Options are automatically extracted from route patterns:

```csharp
builder.AddRoute("deploy {env} --version {ver}", ...);
```

Typing `./ShellCompletionExample.cs deploy prod --v<TAB>` completes to `--version`.

## Publishing for Production

For production use, publish the runfile as an AOT-compiled executable:

```bash
cd Samples/ShellCompletionExample

# Publish with Native AOT
dotnet publish ShellCompletionExample.cs -c Release -r linux-x64 -p:PublishAot=true -o ./publish

# Copy to your PATH with desired name (kebab-case recommended)
cp ./publish/ShellCompletionExample ~/.local/bin/shell-completion-example

# Verify it works
shell-completion-example status
# Output: 📊 System Status: OK
```

**Auto-Detection Benefit**: The completion script will automatically use "shell-completion-example" as the command name, matching the actual executable name. No need to specify the app name in code!

### Publishing for Other Platforms

```bash
# Windows
dotnet publish ShellCompletionExample.cs -c Release -r win-x64 -p:PublishAot=true -o ./publish

# macOS (Intel)
dotnet publish ShellCompletionExample.cs -c Release -r osx-x64 -p:PublishAot=true -o ./publish

# macOS (Apple Silicon)
dotnet publish ShellCompletionExample.cs -c Release -r osx-arm64 -p:PublishAot=true -o ./publish
```

## Generating Completion Scripts

### From Runfile (Development)

```bash
cd Samples/ShellCompletionExample

# Make executable (first time only)
chmod +x ShellCompletionExample.cs

# Generate completion scripts
./ShellCompletionExample.cs --generate-completion bash
./ShellCompletionExample.cs --generate-completion zsh
./ShellCompletionExample.cs --generate-completion pwsh
./ShellCompletionExample.cs --generate-completion fish
```

### From Published Executable (Production)

```bash
# Use the published executable name (completion will match this name automatically)
shell-completion-example --generate-completion bash
shell-completion-example --generate-completion zsh
shell-completion-example --generate-completion pwsh
shell-completion-example --generate-completion fish
```

**Important**: The generated completion script uses the actual executable name. If you rename your executable to `my-tool`, the completion script will register for `my-tool` automatically!

## Installing Completion Scripts

### Bash (Development - Runfile)

```bash
# Inline for current session (temporary)
source <(./ShellCompletionExample.cs --generate-completion bash)

# Or append to ~/.bashrc (permanent)
./ShellCompletionExample.cs --generate-completion bash >> ~/.bashrc
source ~/.bashrc
```

### Bash (Production - Published Executable)

```bash
# Generate and append to ~/.bashrc
shell-completion-example --generate-completion bash >> ~/.bashrc
source ~/.bashrc

# Or save to bash completion directory
mkdir -p ~/.bash_completion.d
shell-completion-example --generate-completion bash > ~/.bash_completion.d/shell-completion-example
echo "source ~/.bash_completion.d/shell-completion-example" >> ~/.bashrc
```

### Zsh (Development - Runfile)

```bash
# Inline for current session
source <(./ShellCompletionExample.cs --generate-completion zsh)

# Or append to ~/.zshrc
./ShellCompletionExample.cs --generate-completion zsh >> ~/.zshrc
source ~/.zshrc
```

### Zsh (Production - Published Executable)

```bash
# Generate and save to zsh completions directory
mkdir -p ~/.zsh/completions
shell-completion-example --generate-completion zsh > ~/.zsh/completions/_shell-completion-example

# Add to your ~/.zshrc (if not already present)
fpath=(~/.zsh/completions $fpath)
autoload -Uz compinit && compinit
```

### PowerShell (Development - Runfile)

```powershell
# Generate and append to your PowerShell profile
./ShellCompletionExample.cs --generate-completion pwsh >> $PROFILE

# Reload profile
. $PROFILE
```

### PowerShell (Production - Published Executable)

```powershell
# Generate and append to your PowerShell profile
shell-completion-example --generate-completion pwsh >> $PROFILE

# Reload profile
. $PROFILE
```

**Note:** PowerShell's `-Native` argument completer requires the executable to be in your PATH. Make sure `~/.local/bin` (or wherever you installed the executable) is in your PATH.

### Fish (Development - Runfile)

```bash
# Inline for current session
./ShellCompletionExample.cs --generate-completion fish | source
```

### Fish (Production - Published Executable)

```bash
# Generate and save to fish completions directory
shell-completion-example --generate-completion fish > ~/.config/fish/completions/shell-completion-example.fish

# Fish automatically loads completions from this directory
```

## Testing the Completion

After installing the completion script, test it:

### With Runfile (Development)

```bash
# Start typing and press Tab
./ShellCompletionExample.cs cre<TAB>              # Should show: create, createorder
./ShellCompletionExample.cs createorder <TAB>     # Shows parameter help
./ShellCompletionExample.cs deploy <TAB>          # Shows parameter help
./ShellCompletionExample.cs deploy prod --v<TAB>  # Completes to: --version
```

### With Published Executable (Production)

```bash
# Tab completion with your published executable name
shell-completion-example st<TAB>              # Completes to: status
shell-completion-example cre<TAB>             # Should show: create, createorder
shell-completion-example createorder <TAB>    # Shows parameter help
shell-completion-example deploy prod --v<TAB> # Completes to: --version
```

## Sample Commands

The example application includes these commands:

```bash
# Create order (demonstrates Issue #30 use case)
./ShellCompletionExample.cs createorder laptop 5

# Create generic item
./ShellCompletionExample.cs create project

# Check status
./ShellCompletionExample.cs status

# Deploy with version
./ShellCompletionExample.cs deploy production --version v1.2.3

# List items (catch-all parameter)
./ShellCompletionExample.cs list apple banana cherry
```

## Implementation Details

### Static Script Generation

TimeWarp.Nuru uses **static script generation** (not dynamic/runtime queries):

- ✅ **Zero runtime overhead** - No subprocess calls when Tab is pressed
- ✅ **No additional tools** - Unlike System.CommandLine's `dotnet-suggest`
- ✅ **Simple to install** - Just source the generated script
- ✅ **Reliable** - Works even if app is slow to start

### Route Pattern Analysis

The completion system analyzes your `CompiledRoute` instances:

1. **Commands** - Extracted from `LiteralMatcher` segments
2. **Options** - Extracted from `OptionMatcher` instances (both long and short forms)
3. **Parameters** - Extracted from `ParameterMatcher` with type information

### Type-Aware Completion (Future)

The completion provider supports type-aware suggestions:

- **Enum types** - Will complete with enum values
- **File types** - Delegates to shell's file completion
- **Directory types** - Delegates to shell's directory completion
- **Custom types** - Can provide `ICompletionSource` implementations

## Package Architecture

Shell completion is provided as a **separate optional package**.

For projects, add the packages:
```xml
<ItemGroup>
  <PackageReference Include="TimeWarp.Nuru" Version="2.1.0" />
  <PackageReference Include="TimeWarp.Nuru.Completion" Version="2.1.0" />
</ItemGroup>
```

For .NET 10 runfiles, use `#:project` directives (as shown in this example):
```csharp
#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Completion/TimeWarp.Nuru.Completion.csproj
```

This keeps the core `TimeWarp.Nuru` package lightweight while allowing users to opt-in to completion support.

## Comparison to Other Frameworks

| Framework | Approach | Installation | Shells |
|-----------|----------|--------------|--------|
| **System.CommandLine** | `dotnet-suggest` global tool | Requires global tool install | bash, PowerShell |
| **Cocona** | Built-in `--completion` (disabled by default) | Source generated script | bash, zsh |
| **Spectre.Console** | Third-party package | Varies | Varies |
| **TimeWarp.Nuru** | Built-in static generation | Source generated script | bash, zsh, PowerShell, fish |

## Related Resources

- **Issue #30**: [Add "auto completion" of arguments](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/30)
- **Task 025**: Implementation details in `Kanban/InProgress/025_Implement-Shell-Tab-Completion.md`
- **Core Package**: `Source/TimeWarp.Nuru.Completion/`

## Troubleshooting

### Completion not working in Bash

```bash
# Ensure bash-completion is installed
# Ubuntu/Debian:
sudo apt-get install bash-completion

# macOS (Homebrew):
brew install bash-completion@2
```

### Completion not working in Zsh

```bash
# Ensure compinit is called in ~/.zshrc
autoload -Uz compinit && compinit

# If completions are cached, clear the cache
rm -f ~/.zcompdump && compinit
```

### Completion not working in PowerShell

```powershell
# Ensure PowerShell version 5.0 or higher
$PSVersionTable.PSVersion

# Check if the executable is in your PATH
Get-Command shell-completion-example

# Check if profile was loaded
Test-Path $PROFILE
Get-Content $PROFILE | Select-String "shell-completion-example"

# Test completion manually
TabExpansion2 -inputScript 'shell-completion-example st' -cursorColumn 27
```

**Common Issues:**
- **Executable not in PATH**: PowerShell's `-Native` completer requires the command to be accessible
- **Old buggy completion**: If you have an old version with `[CompletionResult]` (without `System.Management.Automation.`), regenerate it
- **Trailing "0" in output**: Fixed in v2.1.0-beta.32 - update to latest version

### Completion not working in Fish

```bash
# Fish automatically loads from ~/.config/fish/completions/
# Verify file exists
ls -la ~/.config/fish/completions/myapp.fish

# Reload completions
fish_update_completions
```
