# Shell Completion Example

This example demonstrates how to add shell tab completion to TimeWarp.Nuru CLI applications, addressing [GitHub Issue #30](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/30).

## Files

- `Program.cs` - Sample application with multiple commands demonstrating completion scenarios
- `ShellCompletionExample.csproj` - Project file with TimeWarp.Nuru.Completion package reference

## What is Shell Completion?

Shell completion (also called tab completion) allows users to press the Tab key to:
- Complete partial command names (e.g., `cre<TAB>` → `createorder`)
- See available commands when multiple matches exist
- Discover available options (e.g., `--v<TAB>` → `--version`)
- Improve typing speed and reduce errors

## Key Features Demonstrated

### 1. Enabling Shell Completion

```csharp
var builder = new NuruAppBuilder();

// Enable shell completion with app name
builder.EnableShellCompletion("myapp");

// Register your routes as normal
builder.AddRoute("createorder {product} {quantity:int}", (string product, int quantity) =>
{
    Console.WriteLine($"✅ Creating order:");
    Console.WriteLine($"   Product: {product}");
    Console.WriteLine($"   Quantity: {quantity}");
    return 0;
});
```

The `EnableShellCompletion()` method automatically registers a `--generate-completion {shell}` route.

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

Typing `myapp deploy prod --v<TAB>` completes to `--version`.

## Generating Completion Scripts

Build and run the sample to generate shell completion scripts:

```bash
cd Samples/ShellCompletionExample
dotnet build

# Generate bash completion
dotnet run -- --generate-completion bash

# Generate zsh completion
dotnet run -- --generate-completion zsh

# Generate PowerShell completion
dotnet run -- --generate-completion pwsh

# Generate fish completion
dotnet run -- --generate-completion fish
```

## Installing Completion Scripts

### Bash

```bash
# Generate and save to bash completion directory
dotnet run -- --generate-completion bash > ~/.bash_completion.d/myapp

# Add to your ~/.bashrc
source ~/.bash_completion.d/myapp

# Or inline for current session
source <(dotnet run -- --generate-completion bash)
```

### Zsh

```bash
# Generate and save to zsh completions directory
mkdir -p ~/.zsh/completions
dotnet run -- --generate-completion zsh > ~/.zsh/completions/_myapp

# Add to your ~/.zshrc (if not already present)
fpath=(~/.zsh/completions $fpath)
autoload -Uz compinit && compinit
```

### PowerShell

```powershell
# Generate and append to your PowerShell profile
dotnet run -- --generate-completion pwsh >> $PROFILE

# Reload profile
. $PROFILE
```

### Fish

```bash
# Generate and save to fish completions directory
dotnet run -- --generate-completion fish > ~/.config/fish/completions/myapp.fish

# Fish automatically loads completions from this directory
```

## Testing the Completion

After installing the completion script, test it:

```bash
# Start typing and press Tab
myapp cre<TAB>              # Should show: create, createorder
myapp createorder <TAB>     # Shows parameter help
myapp deploy <TAB>          # Shows parameter help
myapp deploy prod --v<TAB>  # Completes to: --version
```

## Sample Commands

The example application includes these commands:

```bash
# Create order (demonstrates Issue #30 use case)
dotnet run -- createorder laptop 5

# Create generic item
dotnet run -- create project

# Check status
dotnet run -- status

# Deploy with version
dotnet run -- deploy production --version v1.2.3

# List items (catch-all parameter)
dotnet run -- list apple banana cherry
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

Shell completion is provided as a **separate optional package**:

```xml
<ItemGroup>
  <PackageReference Include="TimeWarp.Nuru" Version="2.1.0" />
  <PackageReference Include="TimeWarp.Nuru.Completion" Version="2.1.0" />
</ItemGroup>
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

# Check if profile was loaded
Test-Path $PROFILE
Get-Content $PROFILE
```

### Completion not working in Fish

```bash
# Fish automatically loads from ~/.config/fish/completions/
# Verify file exists
ls -la ~/.config/fish/completions/myapp.fish

# Reload completions
fish_update_completions
```
