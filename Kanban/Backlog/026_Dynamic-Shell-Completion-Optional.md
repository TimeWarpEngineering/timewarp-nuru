# Dynamic Shell Completion (Optional Enhancement)

## Related Issue: [#30](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/30)
## Depends On: Task 025 (Shell Tab Completion)

## Overview

This task implements **Phase 3** dynamic completion for TimeWarp.Nuru - an optional enhancement that enables runtime-computed completion suggestions. This is ONLY needed for applications that require context-aware completions that cannot be determined statically.

**IMPORTANT:** This is an optional enhancement. Static completion (Phase 1 & 2, completed in Task 025) covers 90%+ of use cases and is production-ready. Only implement this if users specifically request dynamic completion capabilities.

## Why This Is Separate and Optional

### Static Completion (Already Complete) Handles:
- ✅ Command name completion
- ✅ Option name completion (--long, -short)
- ✅ Enum value completion (all values known at build time)
- ✅ File/directory path completion (delegated to shell)
- ✅ Simple parameter completion

### Dynamic Completion Would Add:
- Runtime-computed suggestions (database queries, API calls, etc.)
- Context-aware completions based on previous arguments
- User-specific or environment-specific suggestions
- Complex business logic in completion

**Trade-offs:**
- **Performance Cost**: Every Tab press = subprocess call to your app
- **Complexity**: Requires app to handle special completion requests
- **Maintenance**: Shell script templates become more complex
- **User Experience**: Slower than static (subprocess overhead)

**When You Might Need This:**
```bash
# Example: Environment names from a configuration service
./myapp deploy <TAB>  # Queries service, returns: production, staging, dev

# Example: Recent order IDs from database
./myapp order status <TAB>  # Queries DB, returns: 12345, 12346, 12347

# Example: Context-aware suggestions
./myapp set-permission <user> <TAB>  # Returns permissions valid for that specific user
```

**When Static Is Sufficient (99% of cases):**
```bash
# Static enum completion
./myapp log --level <TAB>  # Returns: Debug, Info, Warning, Error

# Static command completion
./myapp cre<TAB>  # Completes to: createorder

# Shell-provided file completion
./myapp process --file <TAB>  # Shell handles file listing
```

## Problem

Some advanced CLI applications need completion candidates that cannot be determined at build time. For example:
- Environment names from a configuration service
- Available database records for an ID parameter
- User names from an auth system
- Context-dependent options based on previous arguments

Static completion cannot handle these scenarios because the data is only available at runtime.

## Proposed Solution

### Architecture: App Callback Mechanism

When user presses Tab, shell completion function calls back to the application with special arguments:

```bash
# User types: myapp deploy <TAB>
# Shell calls: myapp --complete 1 deploy
# App returns (to stdout):
production     Deploy to production environment
staging        Deploy to staging environment
preview        Deploy to preview environment
```

### Core Components

#### 1. ICompletionSource Interface

```csharp
public interface ICompletionSource
{
    IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context);
}
```

**Example Implementation:**
```csharp
public class EnvironmentCompletionSource : ICompletionSource
{
    private readonly IConfigurationService _configService;

    public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
    {
        // Query configuration service at runtime
        var environments = _configService.GetAvailableEnvironments();

        return environments.Select(env => new CompletionCandidate(
            env.Name,
            env.Description,
            CompletionType.Parameter
        ));
    }
}
```

#### 2. Registration API

```csharp
var app = new NuruAppBuilder()
    .AddRoute("deploy {env} --version {ver}", (string env, string ver) => Deploy(env, ver))
    .WithCompletionSource("env", new EnvironmentCompletionSource())
    .EnableStaticCompletion()  // Now includes dynamic support
    .Build();
```

#### 3. Hidden Completion Route

Auto-registered when dynamic completion sources are present:

```csharp
// Internal route: myapp --complete {index} {*words}
.AddRoute("--complete {index:int} {*words}", (int index, string[] words) => {
    // index: which argument position needs completion
    // words: all words typed so far

    // Find matching completion source
    // Call GetCompletions()
    // Output candidates to stdout (one per line)
    // Format: "value\tdescription" or just "value"
});
```

#### 4. Updated Shell Templates

Shell completion scripts need to call back to app for dynamic completions:

**Bash Example:**
```bash
_myapp_completion() {
    local cur="${COMP_WORDS[COMP_CWORD]}"
    local prev="${COMP_WORDS[COMP_CWORD-1]}"

    # Try static completions first (fast)
    local static_completions="createorder status version help"
    COMPREPLY=($(compgen -W "${static_completions}" -- "$cur"))

    # If no static match and app supports dynamic, call it
    if [ ${#COMPREPLY[@]} -eq 0 ]; then
        local completions=$(./myapp --complete $COMP_CWORD "${COMP_WORDS[@]}" 2>/dev/null)
        COMPREPLY=($(compgen -W "${completions}" -- "$cur"))
    fi
}
```

## Implementation Plan

### Phase 3a: Core Dynamic Infrastructure

**Files to Create:**
- `/Source/TimeWarp.Nuru.Completion/Completion/ICompletionSource.cs`
- `/Source/TimeWarp.Nuru.Completion/Completion/CompletionSourceRegistry.cs`
- `/Source/TimeWarp.Nuru.Completion/Completion/DynamicCompletionHandler.cs`

**Files to Update:**
- `/Source/TimeWarp.Nuru.Completion/NuruAppBuilderExtensions.cs` - Add `.WithCompletionSource()` API
- `/Source/TimeWarp.Nuru.Completion/Completion/CompletionScriptGenerator.cs` - Add dynamic support flag

**Implementation:**
1. Create `ICompletionSource` interface
2. Create registry to map parameter names to completion sources
3. Implement `--complete {index} {*words}` hidden route
4. Add `.WithCompletionSource(paramName, source)` API
5. Update `EnableStaticCompletion()` to detect if any dynamic sources registered

### Phase 3b: Shell Script Updates

**Files to Update:**
- `/Source/TimeWarp.Nuru.Completion/Completion/Templates/bash-completion.sh`
- `/Source/TimeWarp.Nuru.Completion/Completion/Templates/zsh-completion.zsh`
- `/Source/TimeWarp.Nuru.Completion/Completion/Templates/pwsh-completion.ps1`
- `/Source/TimeWarp.Nuru.Completion/Completion/Templates/fish-completion.fish`

**Implementation:**
1. Add fallback logic: try static first, then dynamic
2. Implement app callback mechanism for each shell
3. Handle subprocess errors gracefully (fall back to static)
4. Add timeout protection (don't hang on slow completions)

### Phase 3c: Testing & Documentation

**Tests to Add:**
- Test `ICompletionSource` registration and lookup
- Test `--complete` route with various scenarios
- Test shell script fallback behavior
- Performance test: measure completion latency

**Documentation to Create:**
- `/documentation/user/guide/dynamic-completion.md`
- Add section to shell completion guide
- Sample app demonstrating dynamic completion

**Sample Application:**
```csharp
// DynamicCompletionExample
public class EnvironmentSource : ICompletionSource
{
    public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
    {
        // Simulate querying a configuration service
        yield return new CompletionCandidate("production", "Production environment", CompletionType.Parameter);
        yield return new CompletionCandidate("staging", "Staging environment", CompletionType.Parameter);
        yield return new CompletionCandidate("preview", "Preview environment", CompletionType.Parameter);
    }
}

var app = new NuruAppBuilder()
    .AddRoute("deploy {env}", (string env) => Deploy(env))
    .WithCompletionSource("env", new EnvironmentSource())
    .EnableStaticCompletion()
    .Build();
```

## Performance Considerations

### Static vs Dynamic Performance

| Approach | Latency | Subprocess Calls | User Experience |
|----------|---------|------------------|-----------------|
| **Static** | ~0ms | 0 | Instant |
| **Dynamic** | 50-500ms+ | 1 per Tab press | Noticeable delay |

### Optimization Strategies

**1. Cache at Source Level:**
```csharp
public class CachedEnvironmentSource : ICompletionSource
{
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);
    private CachedResult? _cache;

    public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
    {
        if (_cache != null && DateTime.UtcNow - _cache.Timestamp < _cacheDuration)
            return _cache.Candidates;

        var candidates = FetchFromService().ToList();
        _cache = new CachedResult(candidates, DateTime.UtcNow);
        return candidates;
    }
}
```

**2. Timeout Protection:**
```bash
# In shell script, timeout the subprocess
local completions=$(timeout 0.5s ./myapp --complete $COMP_CWORD "${COMP_WORDS[@]}" 2>/dev/null)
```

**3. Background Refresh:**
- Pre-populate cache on app startup
- Refresh cache in background thread
- Tab completion uses cached data (always fast)

## Decision Criteria: When to Implement

**Implement if:**
- [ ] Multiple users request dynamic completion capability
- [ ] Use cases cannot be solved with static completion
- [ ] Users willing to accept performance trade-off
- [ ] Clear examples of runtime-computed completions needed

**Don't implement if:**
- [x] Static completion meets all known use cases
- [x] No user requests for dynamic completion
- [ ] Performance requirements are strict (sub-10ms)
- [ ] Maintenance bandwidth is limited

**Current Recommendation:** **DEFER** until user demand is clear. Static completion is working well and covers vast majority of scenarios.

## Success Criteria

### Phase 3a (Core Infrastructure)
- [ ] `ICompletionSource` interface defined and documented
- [ ] `.WithCompletionSource(paramName, source)` API implemented
- [ ] Completion source registry tracks all registered sources
- [ ] `--complete {index} {*words}` hidden route works correctly
- [ ] Route calls appropriate completion source based on parameter position
- [ ] Output format is shell-compatible (value\tdescription per line)

### Phase 3b (Shell Integration)
- [ ] Bash script updated with dynamic callback mechanism
- [ ] Zsh script updated with dynamic callback mechanism
- [ ] PowerShell script updated with dynamic callback mechanism
- [ ] Fish script updated with dynamic callback mechanism
- [ ] All shells fall back to static if dynamic fails
- [ ] Timeout protection prevents hanging
- [ ] Error messages are suppressed (no stderr to user)

### Phase 3c (Testing & Documentation)
- [ ] Unit tests cover completion source registration
- [ ] Integration tests verify end-to-end dynamic completion
- [ ] Performance benchmarks show acceptable latency (<500ms)
- [ ] Documentation explains when to use dynamic vs static
- [ ] Sample app demonstrates dynamic completion patterns
- [ ] Caching strategies documented with examples

## Alternatives Considered

### Alternative 1: No Dynamic Completion
**Pros:**
- Simpler codebase
- Better performance
- Lower maintenance burden
- Covers 90%+ of use cases

**Cons:**
- Cannot handle runtime-computed completions
- Less flexible for advanced scenarios

**Status:** Currently implemented in Task 025

### Alternative 2: Shell-Side Scripts Only
Users write their own shell completion scripts that query services.

**Pros:**
- No framework changes needed
- Maximum flexibility
- Users control performance/caching

**Cons:**
- No integration with route definitions
- Users must maintain separate shell scripts
- Less discoverable

### Alternative 3: Pre-Generated Completion Files
App generates static completion files at runtime, shell reloads them.

**Pros:**
- Still fast (no subprocess on Tab)
- Can include runtime data

**Cons:**
- Requires file system access
- Complex reload mechanism
- Stale data between regenerations

## Related Tasks and Issues

- **Task 025**: Implement-Shell-Tab-Completion (Phase 1 & 2 - Complete)
- **Issue #30**: Command argument completion (original request)

## Breaking Changes

**None** - This is purely additive:
- Existing static completion continues to work
- `.WithCompletionSource()` is optional
- Shell scripts gracefully degrade if app doesn't support dynamic
- No changes to existing route APIs

## Timeline Estimate

- **Phase 3a (Core Infrastructure)**: 6-8 hours
- **Phase 3b (Shell Integration)**: 8-10 hours
- **Phase 3c (Testing & Documentation)**: 6-8 hours

**Total Estimate**: 20-26 hours

**Complexity**: Medium-High
- Shell script coordination is complex
- Performance optimization is tricky
- Testing requires real shell environments
- Documentation must clearly explain trade-offs

## Priority Justification

**Low Priority** because:

1. **Not Requested**: No users asking for dynamic completion yet
2. **Static Sufficient**: Current implementation handles vast majority of use cases
3. **Performance Cost**: Dynamic completion is noticeably slower
4. **Complexity**: Significant implementation and maintenance burden
5. **Optional**: Nice-to-have enhancement, not core functionality
6. **Other Priorities**: Core framework features more important
7. **Can Wait**: Easy to add later if demand materializes

**Recommendation:** Keep in backlog until clear user demand emerges. Static completion (Phase 1 & 2) is production-ready and sufficient for most applications.
