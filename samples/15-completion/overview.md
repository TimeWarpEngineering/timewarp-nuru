# Shell Completion Example

**Demonstrates**: Shell tab completion with custom completion sources
**Related**: Task #340 (Shell completion architecture unification)

## What is Shell Completion?

Shell completion provides tab-completions for your CLI application. When you press Tab, the shell calls back to your application to get context-aware suggestions.

- **Dynamic Data**: Complete from databases, APIs, configuration services
- **Context-Aware**: Different completions based on previous arguments
- **AOT-Compatible**: Source-generated for native AOT compilation

## Quick Start

```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("deploy {env} --version {tag}")
    .WithHandler((string env, string tag) => Console.WriteLine($"Deploying {tag} to {env}"))
    .AsCommand()
    .Done()

  .EnableCompletion(configure: registry =>
  {
    // Register custom completion sources
    registry.RegisterForParameter("env", new EnvironmentCompletionSource());
    registry.RegisterForParameter("tag", new TagCompletionSource());
  })

  .Build();

return await app.RunAsync(args);
```

## Available Routes

When you call `.EnableCompletion()`, the following routes are source-generated:

| Route | Description |
|-------|-------------|
| `__complete {index} {*words}` | Shell callback for completions |
| `--generate-completion {shell}` | Generate completion script |
| `--install-completion {shell?}` | Install completion to shell config |
| `--install-completion --dry-run {shell?}` | Preview installation |

## Installing Completions

### Automatic Installation (Recommended)

```bash
# Install for all detected shells
./myapp --install-completion

# Install for a specific shell
./myapp --install-completion bash
./myapp --install-completion zsh
./myapp --install-completion fish
./myapp --install-completion pwsh

# Preview what will be installed
./myapp --install-completion --dry-run
```

### Manual Installation

```bash
# Bash
source <(./myapp --generate-completion bash)

# Zsh
source <(./myapp --generate-completion zsh)

# Fish
./myapp --generate-completion fish > ~/.config/fish/completions/myapp.fish

# PowerShell
./myapp --generate-completion pwsh | Out-String | Invoke-Expression
```

## Custom Completion Sources

Implement `ICompletionSource` to provide dynamic completions:

```csharp
public class EnvironmentCompletionSource : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    // Query your data source (API, database, config)
    return GetEnvironments().Select(env => new CompletionCandidate(
      Value: env,
      Description: GetDescription(env),
      Type: CompletionType.Parameter
    ));
  }

  public static string[] GetEnvironments() =>
    ["production", "staging", "development", "qa", "demo"];
}
```

## How It Works

1. **User presses Tab** in their shell
2. **Shell calls your app** with `__complete {cursor_position} {words...}`
3. **App returns completions** (one per line, tab-separated value and description)
4. **Shell displays** the completion menu

Example callback:
```bash
$ myapp __complete 1 deploy
deploy
list-environments
list-tags
status
--help	Show help for this command
:4
```

The `:4` at the end is a directive code (NoFileComp = don't fall back to file completion).

## Testing

```bash
# Run the sample
dotnet run samples/15-completion/completion-example.cs -- --help

# Test __complete callback
dotnet run samples/15-completion/completion-example.cs -- __complete 1 deploy

# Generate bash completion script
dotnet run samples/15-completion/completion-example.cs -- --generate-completion bash

# Preview installation
dotnet run samples/15-completion/completion-example.cs -- --install-completion --dry-run
```

## Manual Shell Verification

To verify completion works in each shell:

### Bash

```bash
# 1. Build and publish
dotnet publish samples/15-completion/completion-example.cs -c Release -o ~/.local/bin

# 2. Install completion
~/.local/bin/completion-example --install-completion bash

# 3. Reload shell or source completion
source ~/.local/share/bash-completion/completions/completion-example

# 4. Test
completion-example <TAB>           # Should show: deploy, list-environments, list-tags, status
completion-example deploy <TAB>    # Should show environment completions
```

### Zsh

```zsh
# 1. Build and publish
dotnet publish samples/15-completion/completion-example.cs -c Release -o ~/.local/bin

# 2. Install completion
~/.local/bin/completion-example --install-completion zsh

# 3. Add to fpath (one-time setup in ~/.zshrc)
fpath=(~/.local/share/zsh/site-functions $fpath)
autoload -Uz compinit && compinit

# 4. Reload shell and test
completion-example <TAB>
```

### Fish

```fish
# 1. Build and publish
dotnet publish samples/15-completion/completion-example.cs -c Release -o ~/.local/bin

# 2. Install completion (Fish auto-loads from this directory)
~/.local/bin/completion-example --install-completion fish

# 3. Test (no reload needed)
completion-example <TAB>
```

### PowerShell

```powershell
# 1. Build and publish
dotnet publish samples/15-completion/completion-example.cs -c Release -o $HOME/.local/bin

# 2. Install completion
& "$HOME/.local/bin/completion-example" --install-completion pwsh

# 3. Add to profile (one-time setup)
Add-Content $PROFILE ". $HOME/.local/share/nuru/completions/completion-example.ps1"

# 4. Reload profile and test
. $PROFILE
completion-example <TAB>
```

## Performance

For best performance, publish as AOT:

```bash
dotnet publish completion-example.cs -c Release -r linux-x64 -p:PublishAot=true
```

AOT-compiled apps typically respond to `__complete` in under 10ms, well within the 100ms threshold for responsive tab completion.

## References

- **Task #340**: Shell completion architecture unification
- **Task #387**: Enum option parameter support (pending)
