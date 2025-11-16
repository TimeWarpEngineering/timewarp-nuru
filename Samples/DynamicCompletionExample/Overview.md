# Dynamic Completion Example

**Demonstrates**: Task #029 - `EnableDynamicCompletion()` feature
**Related**: Task #025 (Static Shell Completion), Task #028 (Type-Aware Parameter Completion)

## What is Dynamic Completion?

Dynamic completion queries your application **at Tab-press time** instead of using static completion data. This enables:

- **Runtime Data**: Complete from databases, APIs, configuration services
- **Context-Aware Suggestions**: Different completions based on previous arguments
- **Dynamic State**: Completions reflect current application state

## Comparison: Static vs Dynamic

### Static Completion (`EnableStaticCompletion()`)

```csharp
builder.EnableStaticCompletion();
// Completion script contains ALL possible values at generation time
// ✅ Fast (no app invocation)
// ❌ Cannot query runtime data
// ❌ No context awareness
```

### Dynamic Completion (`EnableDynamicCompletion()`)

```csharp
builder.EnableDynamicCompletion(configure: registry =>
{
    registry.RegisterForParameter("env", new EnvironmentCompletionSource());
});
// Completion script calls app via __complete route at Tab-press time
// ✅ Can query databases, APIs, configuration
// ✅ Context-aware (based on previous args)
// ✅ Negligible overhead with AOT (~7-10ms, imperceptible to users)
```

## Example Custom Completion Sources

This example demonstrates three types of custom completion sources:

### 1. Environment Completion (`EnvironmentCompletionSource`)

```csharp
public class EnvironmentCompletionSource : ICompletionSource
{
    public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
    {
      // In real app: query configuration service or API
      return GetEnvironments()
        .Select
        (
          env => 
            new CompletionCandidate
            (
              Value: env,
              Description: GetEnvironmentDescription(env),
              Type: CompletionType.Parameter
            )
        );
    }

    private static string[] GetEnvironments() =>
        ["production", "staging", "development", "qa", "demo"];
}
```

**Completion behavior**:
```bash
$ DynamicCompletionExample deploy <TAB>
production   (Production environment - use with caution)
staging      (Staging environment for final testing)
development  (Development environment for active work)
qa           (Quality assurance testing environment)
demo         (Demo environment for client presentations)
```

### 2. Tag Completion (`TagCompletionSource`)

```csharp
public class TagCompletionSource : ICompletionSource
{
    public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
    {
        // In real app: query Git tags or Docker registry
        return GetTags().Select(tag => new CompletionCandidate(
            Value: tag,
            Description: GetTagDescription(tag),
            Type: CompletionType.Parameter
        ));
    }

    private static string[] GetTags() =>
        ["v2.1.0", "v2.0.5", "v2.0.4", "v1.9.12", "latest"];
}
```

**Completion behavior**:
```bash
$ DynamicCompletionExample deploy production --version <TAB>
v2.1.0   (Current release - 2025-11-14)
v2.0.5   (Previous stable release)
v2.0.4   (Release v2.0.4)
v1.9.12  (Release v1.9.12)
latest   (Latest stable release)
```

### 3. Enum Completion (`EnumCompletionSource<DeploymentMode>`)

```csharp
builder.EnableDynamicCompletion(configure: registry =>
{
    registry.RegisterForType(typeof(DeploymentMode), new EnumCompletionSource<DeploymentMode>());
});

public enum DeploymentMode
{
    [Description("Fast deployment without health checks")]
    Fast,

    [Description("Standard deployment with rolling updates")]
    Standard,

    [Description("Blue-green deployment with zero downtime")]
    BlueGreen,

    [Description("Canary deployment with gradual rollout")]
    Canary
}
```

**Completion behavior**:
```bash
$ DynamicCompletionExample deploy production --mode <TAB>
Fast       (Fast deployment without health checks)
Standard   (Standard deployment with rolling updates)
BlueGreen  (Blue-green deployment with zero downtime)
Canary     (Canary deployment with gradual rollout)
```

## Quick Start

### 1. Build the Example (AOT for Best Performance)

```bash
cd Samples/DynamicCompletionExample
chmod +x DynamicCompletionExample.cs

# Option A: AOT (Native, ~7ms invocation time)
# Note: PublishDir is configured in the runfile (#:property PublishDir=../../artifacts/)
dotnet publish DynamicCompletionExample.cs -c Release -r linux-x64 -p:PublishAot=true
APP_PATH="../../artifacts/DynamicCompletionExample"

# Option B: JIT (Faster build, ~200ms invocation time)
dotnet build DynamicCompletionExample.cs
APP_PATH="./bin/Debug/net10.0/DynamicCompletionExample"
```

### 2. Install to PATH (Recommended)

For convenience, copy the executable to a location that's already in your PATH:

```bash
# Copy to ~/.local/bin (typically already in PATH)
cp ../../artifacts/DynamicCompletionExample ~/.local/bin/dynamic-completion-example

# Verify it works
dynamic-completion-example status
# Output: 📊 System Status: OK
```

**Note**: If `~/.local/bin` is not in your PATH, add it to your shell profile:
```bash
# Add to ~/.bashrc or ~/.zshrc
echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.bashrc
source ~/.bashrc
```

After installing to PATH, you can use `dynamic-completion-example` instead of the full path.

### 3. Install Completion for Your Shell

#### Bash

```bash
# Generate and source completion script (using installed command name)
source <(dynamic-completion-example --generate-completion bash)

# Permanent installation (add to ~/.bashrc):
dynamic-completion-example --generate-completion bash > ~/.bash_completion.d/dynamic-completion-example

# Or if using $APP_PATH directly without installing to PATH:
# source <($APP_PATH --generate-completion bash)
```

#### Zsh

```bash
# Generate and source completion script (using installed command name)
source <(dynamic-completion-example --generate-completion zsh)

# Permanent installation (add to ~/.zshrc):
mkdir -p ~/.zsh/completions
dynamic-completion-example --generate-completion zsh > ~/.zsh/completions/_dynamic-completion-example
# Then add to ~/.zshrc: fpath=(~/.zsh/completions $fpath) && autoload -Uz compinit && compinit
```

#### PowerShell

```powershell
# Generate and source completion script (using installed command name)
& dynamic-completion-example --generate-completion pwsh | Out-String | Invoke-Expression

# Permanent installation (add to $PROFILE):
& dynamic-completion-example --generate-completion pwsh | Out-File -Append $PROFILE
```

**Note**: PowerShell requires the executable to be in your PATH. Ensure `~/.local/bin` is in your PATH.

#### Fish

```fish
# Generate and source completion script (using installed command name)
dynamic-completion-example --generate-completion fish | source

# Permanent installation:
dynamic-completion-example --generate-completion fish > ~/.config/fish/completions/dynamic-completion-example.fish
```

### 4. Test Dynamic Completion

After installing to PATH and setting up completion scripts:

```bash
# Complete environment names
dynamic-completion-example deploy <TAB>
# → production, staging, development, qa, demo

# Complete version tags
dynamic-completion-example deploy production --version <TAB>
# → v2.1.0, v2.0.5, v2.0.4, v1.9.12, latest

# Complete deployment modes (enum values)
dynamic-completion-example deploy production --mode <TAB>
# → Fast, Standard, BlueGreen, Canary

# Complete commands (from registered routes)
dynamic-completion-example <TAB>
# → deploy, list-environments, list-tags, status

# Complete options (requires - prefix)
dynamic-completion-example deploy production -<TAB>
# → --version, --mode
```

**Alternative**: If you haven't installed to PATH, use the full path with `$APP_PATH`:
```bash
$APP_PATH deploy <TAB>
```

## How It Works

### 1. Shell Completion Script Calls Your App

When you press Tab, the shell completion script executes:

```bash
dynamic-completion-example __complete 2 dynamic-completion-example deploy
```

Where:
- `__complete` is the callback route
- `2` is the cursor position (index of word being completed)
- Remaining args are the words typed so far

### 2. App Returns Completion Candidates

The app processes the request via `DynamicCompletionHandler`:

```
production	Production environment (⚠️  use with caution)
staging	Staging environment for final testing
development	Development environment for active work
qa	Quality assurance testing environment
demo	Demo environment for client presentations
:4
```

Output format:
- One candidate per line
- Tab-separated value and description
- Final line: directive code (`:4` = NoFileComp)

### 3. Shell Displays Completions

The shell parses the output and displays the completions to the user.

## Performance

### Measured Invocation Times (AOT)

- **Cold start**: 5.71ms (minimum)
- **Average**: 7.60ms
- **p95**: 9.88ms
- **Target**: <100ms (92.4% headroom)

**Conclusion**: Dynamic completion is **highly feasible** for interactive CLIs. The 7.60ms average app invocation leaves 92.40ms for completion source logic (database queries, API calls, etc.).

### Performance Tips

1. **Use AOT compilation** for sub-10ms invocation times
2. **Cache slow completion sources** (databases, external APIs)
3. **Limit API calls** - query only what's needed for current context
4. **Return early** - if context doesn't match, return empty immediately

## Real-World Use Cases

### 1. Aspire CLI (Target Use Case for Nuru)

```csharp
// Replace System.CommandLine's dotnet-suggest with direct callback
builder.EnableDynamicCompletion(configure: registry =>
{
    registry.RegisterForParameter("projectName", new ProjectCompletionSource());
    registry.RegisterForParameter("resourceName", new ResourceCompletionSource());
});
```

### 2. Kubernetes CLI (kubectl-style)

```csharp
builder.EnableDynamicCompletion(configure: registry =>
{
    // Query cluster for pod names
    registry.RegisterForParameter("pod", new PodCompletionSource());

    // Query cluster for namespace names
    registry.RegisterForParameter("namespace", new NamespaceCompletionSource());
});
```

### 3. Cloud CLI (AWS/Azure-style)

```csharp
builder.EnableDynamicCompletion(configure: registry =>
{
    // Query cloud provider API for resource names
    registry.RegisterForParameter("instance", new InstanceCompletionSource());
    registry.RegisterForParameter("region", new RegionCompletionSource());
});
```

## Migration from Static to Dynamic

If you're currently using `EnableStaticCompletion()` and want dynamic completion:

### Before (Static)

```csharp
builder.EnableStaticCompletion();

builder.AddRoute("deploy {env}", (string env) => Deploy(env));
```

### After (Dynamic)

```csharp
builder.EnableDynamicCompletion(configure: registry =>
{
    registry.RegisterForParameter("env", new EnvironmentCompletionSource());
});

builder.AddRoute("deploy {env}", (string env) => Deploy(env));
// No changes to route patterns!
```

**Key benefits**:
- Same route patterns
- No API changes
- Progressive enhancement (start with static, upgrade to dynamic when needed)

## Implementing Custom Completion Sources

### Basic Pattern

```csharp
public class MyCompletionSource : ICompletionSource
{
    public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
    {
        // 1. Optionally inspect context for previous arguments
        string[] previousArgs = context.Args.Take(context.CursorPosition).ToArray();

        // 2. Query your data source (API, database, configuration)
        var items = GetItems(); // Your logic here

        // 3. Return completion candidates
        return items.Select(item => new CompletionCandidate(
            Value: item.Name,
            Description: item.Description,
            Type: CompletionType.Parameter
        ));
    }
}
```

### Context-Aware Pattern

```csharp
public class ContextAwareCompletionSource : ICompletionSource
{
    public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
    {
        // Check if a specific command was typed before this parameter
        if (context.Args.Contains("deploy"))
        {
            // Return deployment-specific completions
            return GetDeploymentOptions();
        }
        else if (context.Args.Contains("rollback"))
        {
            // Return rollback-specific completions
            return GetRollbackOptions();
        }

        return [];
    }
}
```

### Async Data Source Pattern (Future Enhancement)

```csharp
// Phase 4 enhancement: IAsyncCompletionSource
public class ApiCompletionSource : IAsyncCompletionSource
{
    public async Task<IEnumerable<CompletionCandidate>> GetCompletionsAsync(
        CompletionContext context,
        CancellationToken cancellationToken)
    {
        // Query external API
        var results = await httpClient.GetAsync("/api/resources", cancellationToken);
        return results.Select(r => new CompletionCandidate(r.Name, r.Description));
    }
}
```

## Troubleshooting

### Completion not working

1. **Verify executable is in PATH**:
   ```bash
   which dynamic-completion-example  # Should show ~/.local/bin/dynamic-completion-example
   ```

2. **Verify completion script is sourced**:
   ```bash
   type _dynamic_completion_example  # Should show completion function
   ```

3. **Test __complete route directly**:
   ```bash
   dynamic-completion-example __complete 2 dynamic-completion-example deploy
   # Should output: production, staging, development, etc.
   ```

4. **Check for errors in stderr**:
   ```bash
   dynamic-completion-example __complete 2 dynamic-completion-example deploy 2>&1
   # Should see "Completion ended with directive: NoFileComp"
   ```

### Slow completion

1. **Use AOT compilation** (not JIT)
2. **Profile your completion sources** - add timing logs
3. **Cache expensive operations** (database queries, API calls)
4. **Consider static completion** for infrequently-changing data

## Next Steps

- **Implement your own completion sources** for your domain data
- **Explore Phase 4 enhancements**: Async sources, caching, hybrid mode
- **Compare with Aspire CLI** to see how Nuru can replace System.CommandLine
- **Review Task #029** in Kanban for implementation details

## References

- **Task #029**: `Kanban/InProgress/029_Implement-EnableDynamicCompletion.md`
- **Core Implementation**: `Source/TimeWarp.Nuru.Completion/`
- **Static Completion Example**: `Samples/ShellCompletionExample/`
- **Industry Standard**: Cobra's `__complete` pattern (kubectl, gh, docker)
