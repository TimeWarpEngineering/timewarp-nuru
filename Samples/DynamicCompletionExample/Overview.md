# Dynamic Completion Example

**Demonstrates**: Task #029 - `EnableDynamicCompletion()` feature
**Related**: Task #025 (Static Shell Completion), Task #028 (Type-Aware Parameter Completion)

## What is Dynamic Completion?

Dynamic completion queries your application **at Tab-press time** instead of using static completion data. This enables:

- **Runtime Data**: Complete from databases, APIs, configuration services
- **Context-Aware Suggestions**: Different completions based on previous arguments
- **Dynamic State**: Completions reflect current application state

## Comparison: Static vs Dynamic

### Static Completion (`EnableShellCompletion()`)

```csharp
builder.EnableShellCompletion();
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
// ❌ Slightly slower (app invocation overhead ~7-10ms)
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
dotnet publish DynamicCompletionExample.cs -c Release -r linux-x64 -p:PublishAot=true
APP_PATH="./bin/Release/net10.0/linux-x64/publish/DynamicCompletionExample"

# Option B: JIT (Faster build, ~200ms invocation time)
dotnet build DynamicCompletionExample.cs
APP_PATH="./bin/Debug/net10.0/DynamicCompletionExample"
```

### 2. Install Completion for Your Shell

#### Bash

```bash
# Generate and source completion script
source <($APP_PATH --generate-completion bash)

# Permanent installation (add to ~/.bashrc):
$APP_PATH --generate-completion bash > ~/.bash_completion.d/dynamic-completion-example
```

#### Zsh

```bash
# Generate and source completion script
source <($APP_PATH --generate-completion zsh)

# Permanent installation (add to ~/.zshrc):
$APP_PATH --generate-completion zsh > ~/.zsh/completions/_DynamicCompletionExample
# Then run: compinit
```

#### PowerShell

```powershell
# Generate and source completion script
& $APP_PATH --generate-completion pwsh | Out-String | Invoke-Expression

# Permanent installation (add to $PROFILE):
& $APP_PATH --generate-completion pwsh | Out-File -Append $PROFILE
```

#### Fish

```fish
# Generate and source completion script
$APP_PATH --generate-completion fish | source

# Permanent installation:
$APP_PATH --generate-completion fish > ~/.config/fish/completions/DynamicCompletionExample.fish
```

### 3. Test Dynamic Completion

```bash
# Complete environment names
DynamicCompletionExample deploy <TAB>
# → production, staging, development, qa, demo

# Complete version tags
DynamicCompletionExample deploy production --version <TAB>
# → v2.1.0, v2.0.5, v2.0.4, v1.9.12, latest

# Complete deployment modes (enum values)
DynamicCompletionExample deploy production --mode <TAB>
# → Fast, Standard, BlueGreen, Canary

# Complete commands (from registered routes)
DynamicCompletionExample <TAB>
# → deploy, list-environments, list-tags, status

# Complete options
DynamicCompletionExample deploy production <TAB>
# → --version, --mode
```

## How It Works

### 1. Shell Completion Script Calls Your App

When you press Tab, the shell completion script executes:

```bash
DynamicCompletionExample __complete 2 DynamicCompletionExample deploy
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

If you're currently using `EnableShellCompletion()` and want dynamic completion:

### Before (Static)

```csharp
builder.EnableShellCompletion();

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

1. **Verify completion script is sourced**:
   ```bash
   type _DynamicCompletionExample  # Should show completion function
   ```

2. **Test __complete route directly**:
   ```bash
   DynamicCompletionExample __complete 2 DynamicCompletionExample deploy
   # Should output: production, staging, development, etc.
   ```

3. **Check for errors in stderr**:
   ```bash
   DynamicCompletionExample __complete 2 DynamicCompletionExample deploy 2>&1
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
