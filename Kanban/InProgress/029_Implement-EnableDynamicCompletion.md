# Task 029: Implement EnableDynamicCompletion

**Related**: Task 028 (Type-Aware Parameter Completion), Task 025 (Shell Tab Completion)

## Status: In Progress

### Progress

- ✅ **Phase 1: Core Infrastructure** - COMPLETE (2025-11-14)
  - Created `ICompletionSource` interface for dynamic completion sources
  - Created `CompletionSourceRegistry` for parameter/type-based registration
  - Created `DynamicCompletionHandler` for `__complete` callback processing
  - Added `EnableDynamicCompletion()` method to `NuruAppBuilder`
  - `__complete` route functional (returns empty results - Phase 3 will add logic)

- ✅ **Phase 2: Dynamic Shell Templates** - COMPLETE (2025-11-14)
  - Created bash-completion-dynamic.sh template
  - Created zsh-completion-dynamic.zsh template
  - Created pwsh-completion-dynamic.ps1 template
  - Created fish-completion-dynamic.fish template
  - Created `DynamicCompletionScriptGenerator` to load templates and replace `{{APP_NAME}}`
  - Updated `EnableDynamicCompletion()` to generate dynamic scripts instead of static
  - All 4 shells now call back to `myapp __complete` at Tab-press time

- ✅ **Phase 3: Built-in Completion Sources** - COMPLETE (2025-11-14)
  - Created `DefaultCompletionSource` - analyzes registered routes for commands and options
  - Created `EnumCompletionSource<TEnum>` - automatic enum value completions with descriptions
  - Updated `DynamicCompletionHandler` to use DefaultCompletionSource
  - Dynamic completion now returns actual route-based suggestions (commands, options)
  - Registry infrastructure in place for Phase 4 parameter-specific sources

- ⏳ **Phase 4: Example and Testing** - TODO

### Performance Baseline (2025-11-14)

✅ **AOT invocation performance validated** - Dynamic completion is highly feasible:

- **Measured**: 7.60ms average (100 runs on ShellCompletionExample AOT)
- **Target**: <100ms for complete dynamic completion flow
- **Headroom**: 92.4% (92.40ms available for completion logic)
- **p95**: 9.88ms (95% of invocations complete in <10ms)
- **Tooling**: [benchmark-aot-invocation.cs](../../Tests/Scripts/benchmark-aot-invocation.cs)
- **Baseline**: [aot-invocation-baseline.json](../../Tests/benchmarks/aot-invocation-baseline.json)

**Conclusion**: Process invocation overhead is minimal. Plenty of budget for completion sources to query APIs, databases, or configuration.

## Problem

Currently, `EnableShellCompletion()` generates **static** completion scripts that contain all completion candidates at generation time. This works well for simple cases but has fundamental limitations:

1. **Cannot query runtime data** - Environment names, deployment targets, resource lists from APIs/databases
2. **No context awareness** - Cannot suggest completions based on previous arguments
3. **No dynamic state** - Cannot complete based on current application state or configuration
4. **Migration blocker for Aspire CLI** - Aspire needs dynamic completion to replace System.CommandLine and Spectre

### Static Limitation Example

```csharp
// Current: All environments must be known at script generation time
builder.AddRoute("deploy {env}", (string env) => Deploy(env));

// Desired: Query available environments at completion time
builder.EnableDynamicCompletion(configure: registry =>
{
    registry.RegisterForParameter("env", new EnvironmentCompletionSource());
});
```

### How Other CLIs Handle This

Modern CLIs (kubectl, gh, docker) use **dynamic completion via callback**:

1. Shell completion function intercepts Tab press
2. Shell calls app with `__complete <cursor_index> <word1> <word2> ...`
3. App returns completion candidates to stdout
4. Shell displays candidates to user

**Example bash flow**:
```bash
$ myapp deploy <TAB>
# Shell executes: myapp __complete 2 myapp deploy
# App outputs:
# production
# staging
# development
# :4
```

## Proposed Solution

Implement `EnableDynamicCompletion()` method that:

1. **Registers `__complete` callback route** - Receives cursor position and command line words
2. **Provides `ICompletionSource` interface** - Users implement to provide dynamic completions
3. **Generates dynamic shell scripts** - Shell scripts call back to app instead of using static data
4. **Supports all 4 shells** - bash, zsh, PowerShell, fish
5. **Mutually exclusive with EnableShellCompletion** - User opts into one or the other

### Architecture

**Core Interface**:
```csharp
public interface ICompletionSource
{
    IEnumerable<CompletionItem> GetCompletions(CompletionContext context);
}

public class CompletionContext
{
    public int CursorPosition { get; init; }
    public string[] Words { get; init; }
    public string CurrentWord { get; init; }
}

public class CompletionItem
{
    public required string Value { get; init; }
    public string? Description { get; init; }
    public CompletionDirective Directive { get; init; } = CompletionDirective.NoFileComp;
}

[Flags]
public enum CompletionDirective
{
    Default = 0,        // Normal completion
    NoFileComp = 4,     // Don't fall back to files
    NoSpace = 8,        // Don't add space after
    KeepOrder = 64      // Don't sort results
}
```

**Registration API**:
```csharp
var builder = new NuruAppBuilder();

builder.EnableDynamicCompletion(configure: registry =>
{
    // Register by parameter name
    registry.RegisterForParameter("env", new EnvironmentCompletionSource());

    // Register by type (for reusability)
    registry.RegisterForType<Environment>(new EnvironmentCompletionSource());
});

builder.AddRoute("deploy {env} --version {tag}", (string env, string tag) =>
{
    Console.WriteLine($"Deploying {tag} to {env}");
    return 0;
});
```

**Callback Protocol**:
```bash
# Input format:
myapp __complete 2 myapp deploy production

# Output format (stdout):
staging
development
:4
Completion ended with directive: NoFileComp
```

### Dynamic Template Structure

**Bash example**:
```bash
_myapp_completions()
{
    local cur prev words cword
    _init_completion || return

    # Call application for dynamic completions
    local completions
    completions=$(myapp __complete "$cword" "${words[@]}" 2>/dev/null)

    # Parse completions (one per line)
    local -a suggestions=()
    while IFS=$'\t' read -r value desc; do
        suggestions+=("$value")
    done <<< "$completions"

    COMPREPLY=($(compgen -W "${suggestions[*]}" -- "$cur"))
    return 0
}

complete -F _myapp_completions myapp
```

## Implementation Plan

### Phase 1: Core Infrastructure

**1. Create completion interfaces**
   - File: `/Source/TimeWarp.Nuru.Completion/Completion/ICompletionSource.cs`
   - Define `ICompletionSource`, `CompletionContext`, `CompletionItem`, `CompletionDirective`

**2. Create completion registry**
   - File: `/Source/TimeWarp.Nuru.Completion/Completion/CompletionSourceRegistry.cs`
   - Manages registration by parameter name or type
   - Methods: `RegisterForParameter()`, `RegisterForType()`, `GetSourceForParameter()`, `GetSourceForType()`

**3. Create dynamic completion handler**
   - File: `/Source/TimeWarp.Nuru.Completion/Completion/DynamicCompletionHandler.cs`
   - Handles `__complete` callback route
   - Steps:
     1. Parse context from cursor position and words
     2. Attempt partial route matching
     3. Find appropriate completion source
     4. Get completions from source
     5. Output to stdout (one per line, tab-separated for descriptions)
     6. Output directive code (`:4` for NoFileComp, etc.)

**4. Add EnableDynamicCompletion method**
   - File: `/Source/TimeWarp.Nuru.Completion/NuruAppBuilderExtensions.cs`
   - Signature: `EnableDynamicCompletion(string? appName, Action<CompletionSourceRegistry>? configure)`
   - Registers `__complete {index:int} {*words}` route
   - Registers `--generate-completion {shell}` route with dynamic templates

### Phase 2: Dynamic Shell Templates

**5. Create bash dynamic template**
   - File: `/Source/TimeWarp.Nuru.Completion/Completion/Templates/bash-completion-dynamic.sh`
   - Calls `myapp __complete $cword "${words[@]}"` on Tab press

**6. Create zsh dynamic template**
   - File: `/Source/TimeWarp.Nuru.Completion/Completion/Templates/zsh-completion-dynamic.zsh`
   - Uses `_describe` with `__complete` results

**7. Create PowerShell dynamic template**
   - File: `/Source/TimeWarp.Nuru.Completion/Completion/Templates/pwsh-completion-dynamic.ps1`
   - Uses `Register-ArgumentCompleter` with native callback

**8. Create fish dynamic template**
   - File: `/Source/TimeWarp.Nuru.Completion/Completion/Templates/fish-completion-dynamic.fish`
   - Uses inline command substitution

**9. Create dynamic template generator**
   - File: `/Source/TimeWarp.Nuru.Completion/Completion/DynamicCompletionScriptGenerator.cs`
   - Loads templates and replaces `{{APP_NAME}}` placeholder

### Phase 3: Built-in Completion Sources

**10. Create default completion source**
   - File: `/Source/TimeWarp.Nuru.Completion/Completion/Sources/DefaultCompletionSource.cs`
   - Provides static completions from route analysis (commands, options, enum values)
   - Used as fallback when no custom source registered

**11. Create enum completion source**
   - File: `/Source/TimeWarp.Nuru.Completion/Completion/Sources/EnumCompletionSource.cs`
   - Generic: `EnumCompletionSource<TEnum> where TEnum : struct, Enum`
   - Automatically provides all enum values with descriptions

### Phase 4: Example and Testing

**12. Create dynamic completion example**
   - File: `/Samples/DynamicCompletionExample/DynamicCompletionExample.cs`
   - Demonstrates custom `ICompletionSource` implementation
   - Shows parameter-based registration
   - Includes installation instructions for all 4 shells
   - Example: Environment completion, tag completion

**13. Create integration tests**
   - File: `/Tests/TimeWarp.Nuru.Completion.Tests/DynamicCompletionTests.cs`
   - Test `__complete` route output format
   - Test context parsing from cursor position
   - Test completion source registry lookup
   - Test directive code output
   - Test each shell template generation

**14. Update documentation**
   - Add dynamic completion section to README
   - Document migration from `EnableShellCompletion()` to `EnableDynamicCompletion()`
   - Document performance considerations (<100ms target)
   - Add troubleshooting guide for shell-specific issues

## Key Design Decisions

### 1. Callback Command Name: `__complete`
- **Rationale**: Industry standard (kubectl, gh, docker use this)
- **Alternative considered**: `--complete` (Aspire style), `--suggest` (System.CommandLine)
- **Decision**: Follow Cobra pattern for consistency with major CLIs

### 2. Mutually Exclusive Methods
- **Rationale**: Clear intent, different template generation, simpler implementation
- **Alternative considered**: `EnableShellCompletion(dynamic: true)`
- **Decision**: Separate methods - user chooses one

### 3. Performance Target: <100ms
- **Breakdown**:
  - App startup (AOT): ~30ms, JIT: ~200ms
  - Route parsing: ~5ms
  - Completion source query: <50ms (user responsibility)
  - Output formatting: ~1ms
- **Mitigation**: Document that completion sources must be fast, recommend caching for slow sources

### 4. Registration API: Multiple Patterns
- Support parameter name lookup (most common)
- Support type-based lookup (for reusability)
- Support positional lookup (for precision)
- **Rationale**: Flexibility for different use cases

## Performance Considerations

### Startup Time Optimization
- AOT compilation essential for sub-100ms target
- Minimal mode for `__complete` route (skip DI, validation, logging)
- Priority routing: check `__complete` first

### Caching (Future Enhancement)
```csharp
public interface ICompletionSource
{
    IEnumerable<CompletionItem> GetCompletions(CompletionContext context);
    TimeSpan CacheDuration { get; } // Default: 0 (no cache)
}

// Framework-provided caching decorator
builder.WithCompletionSource("env",
    new CachedCompletionSource(
        new EnvironmentCompletionSource(),
        cacheDuration: TimeSpan.FromMinutes(5)
    ));
```

### Timeout Protection
- Framework should timeout completion requests after 500ms
- Shell scripts should use `timeout` command where available

## Files to Create

### New Files
- `/Source/TimeWarp.Nuru.Completion/Completion/ICompletionSource.cs`
- `/Source/TimeWarp.Nuru.Completion/Completion/CompletionSourceRegistry.cs`
- `/Source/TimeWarp.Nuru.Completion/Completion/DynamicCompletionHandler.cs`
- `/Source/TimeWarp.Nuru.Completion/Completion/DynamicCompletionScriptGenerator.cs`
- `/Source/TimeWarp.Nuru.Completion/Completion/Templates/bash-completion-dynamic.sh`
- `/Source/TimeWarp.Nuru.Completion/Completion/Templates/zsh-completion-dynamic.zsh`
- `/Source/TimeWarp.Nuru.Completion/Completion/Templates/pwsh-completion-dynamic.ps1`
- `/Source/TimeWarp.Nuru.Completion/Completion/Templates/fish-completion-dynamic.fish`
- `/Source/TimeWarp.Nuru.Completion/Completion/Sources/DefaultCompletionSource.cs`
- `/Source/TimeWarp.Nuru.Completion/Completion/Sources/EnumCompletionSource.cs`
- `/Samples/DynamicCompletionExample/DynamicCompletionExample.cs`
- `/Samples/DynamicCompletionExample/Overview.md`
- `/Tests/TimeWarp.Nuru.Completion.Tests/DynamicCompletionTests.cs`

### Files to Modify
- `/Source/TimeWarp.Nuru.Completion/NuruAppBuilderExtensions.cs` (add `EnableDynamicCompletion` method)
- `/README.md` (add dynamic completion section)
- `/documentation/user/features/shell-completion.md` (add dynamic vs static comparison)

## Definition of Done

- [ ] Core interfaces implemented (`ICompletionSource`, `CompletionContext`, `CompletionItem`)
- [ ] Completion registry supports parameter name and type registration
- [ ] `__complete` callback route handler implemented
- [ ] Dynamic templates created for all 4 shells (bash, zsh, PowerShell, fish)
- [ ] `EnableDynamicCompletion()` method added to `NuruAppBuilder`
- [ ] `DefaultCompletionSource` provides fallback static completions
- [ ] `EnumCompletionSource<TEnum>` provides enum value completions
- [ ] Example app demonstrates dynamic completion with custom source
- [ ] Integration tests validate callback protocol and output format
- [ ] End-to-end test validates actual shell completion works
- [ ] Documentation updated with dynamic completion guide
- [ ] Performance testing shows <100ms completion response time
- [ ] README shows clear comparison: static vs dynamic completion

## Testing Scenarios

### Unit Tests
```csharp
// Test completion source invocation
var source = new EnvironmentCompletionSource();
var context = new CompletionContext { CursorPosition = 2, Words = ["myapp", "deploy"] };
var items = source.GetCompletions(context).ToList();
Assert.Contains(items, i => i.Value == "production");

// Test registry lookup
var registry = new CompletionSourceRegistry();
registry.RegisterForParameter("env", source);
var found = registry.GetSourceForParameter("env");
Assert.Same(source, found);
```

### Integration Tests
```bash
# Test __complete route output format
$ ./myapp __complete 2 myapp deploy
production
staging
development
:4

# Test with description
$ ./myapp __complete 2 myapp deploy
production	Production environment
staging	Staging environment
:4
```

### End-to-End Shell Tests
```bash
# Bash
$ source <(./myapp --generate-completion bash)
$ ./myapp deploy <TAB>
production  staging  development

# Zsh
$ source <(./myapp --generate-completion zsh)
$ ./myapp deploy <TAB>
production  -- Production environment
staging     -- Staging environment
development -- Development environment
```

## Migration Guide (for Aspire CLI)

### Before (System.CommandLine)
```csharp
var rootCommand = new RootCommand();
var deployCommand = new Command("deploy");
var envArg = new Argument<string>("environment");
envArg.AddCompletions(ctx => new[] { "production", "staging", "development" });
deployCommand.AddArgument(envArg);
```

### After (Nuru with Dynamic Completion)
```csharp
var builder = new NuruAppBuilder();

builder.EnableDynamicCompletion(configure: registry =>
{
    registry.RegisterForParameter("env", new EnvironmentCompletionSource());
});

builder.AddRoute("deploy {env}", (string env) => Deploy(env));
```

### Benefits for Aspire
- **No `dotnet-suggest` dependency** - One less global tool
- **Faster** - Direct callback vs tool intermediary
- **Better descriptions** - Tab-separated format supported
- **More control** - Full C# for completion logic
- **Simpler** - One-step installation per shell

## References

- **Industry Standard**: Cobra's `__complete` pattern (kubectl, gh, docker)
- **Current Implementation**: `EnableShellCompletion()` in `NuruAppBuilderExtensions.cs`
- **Target Use Case**: Replace System.CommandLine in Aspire CLI
- **Related Tasks**: Task 028 (Type-Aware), Task 025 (Static Completion)

## Notes

This feature is critical for positioning TimeWarp.Nuru as a **viable replacement for System.CommandLine and Spectre** in enterprise scenarios where dynamic data sources are common (databases, APIs, configuration services).

The design follows the principle of **progressive enhancement**:
1. Start with `EnableShellCompletion()` for simple cases
2. Upgrade to `EnableDynamicCompletion()` when runtime data needed
3. Both use the same route patterns - no API changes required

## Future Enhancements (Out of Scope)

- **Caching layer** - Framework-provided caching for slow sources
- **Hybrid mode** - Static for commands, dynamic for parameters
- **FileInfo/DirectoryInfo support** - Automatic file completion for file types
- **Positional registration** - `WithPositionalCompletion(index, source)`
- **Async completion sources** - `IAsyncCompletionSource` for async queries
