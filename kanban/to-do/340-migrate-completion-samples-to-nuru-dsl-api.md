# Unify Shell Completion Architecture and Migrate Samples

## Summary

Consolidate the two parallel completion systems (REPL source-generated vs shell runtime-reflected) 
into a single source-generated architecture. Remove static completion support entirely in favor of 
dynamic completion. Migrate and consolidate the two completion samples.

## Background

### Current State - Two Parallel Systems

**REPL Completion (Source Generated - Zero Runtime Cost)**
- `IReplRouteProvider` interface
- `ReplEmitter` source generator emits `GeneratedReplRouteProvider` with:
  - Static `CommandPrefixes` array baked at compile time
  - `GetCompletions()` with hardcoded logic
  - Enum values extracted via Roslyn compilation
  - Options with descriptions
  - Zero reflection, AOT-friendly

**Shell Completion (Runtime - Uses Reflection)**
- `EnableStaticCompletion()` - Generates shell scripts with embedded data (inferior)
- `EnableDynamicCompletion()` - Generates shell scripts that call `__complete` back to app
  - `DynamicCompletionHandler` processes `__complete` at runtime
  - `CompletionSourceRegistry` for custom `ICompletionSource` implementations
  - Uses reflection via `endpoint.Method.GetParameters()` (not AOT-friendly)

### Industry Standard

All major CLI tools (kubectl, gh, docker, helm, dotnet) use dynamic completion where shell 
scripts call back to the app. No one uses truly static pre-baked completion files because:
- Commands/options change between versions
- Dynamic data (branches, containers, packages) can't be pre-baked
- User must regenerate scripts on every app update

### Decision

- **Remove `EnableStaticCompletion()`** - Inferior approach, no one would choose it
- **Rename `EnableDynamicCompletion()` → `EnableCompletion()`** - Simpler API
- **Source generate the `__complete` handler** - Reuse REPL's static data extraction
- **Keep `ICompletionSource` for runtime extensions** - Only invoked if user registers custom sources

## Architecture

```
Source Generator (compile time)
├── Extracts: commands, options, enums, descriptions (already done in ReplEmitter)
├── Emits: GeneratedCompletionProvider (shared by REPL and shell)
└── Emits: __complete handler with static lookup tables

Runtime (only if user registered custom sources)
├── ICompletionSource interface (keep)
├── CompletionSourceRegistry (keep)  
└── Custom sources for DB/API/dynamic data
```

## API Changes

```csharp
// Before (confusing - two similar options)
.EnableStaticCompletion()                    // REMOVE
.EnableDynamicCompletion(configure: ...)     // RENAME

// After (simple - one option)
.EnableCompletion()                          // Basic - all static data source-generated
.EnableCompletion(configure: registry =>     // With custom runtime sources
{
    registry.RegisterForParameter("env", new MyApiCompletionSource());
})
```

## Checklist

### Phase 1: Remove Static Completion
- [ ] Remove `EnableStaticCompletion()` method
- [ ] Remove `CompletionScriptGenerator` (static version)
- [ ] Update any tests that use `EnableStaticCompletion()`

### Phase 2: Source Generate __complete Handler  
- [ ] Extract completion data extraction from `ReplEmitter` into shared helper
- [ ] Create `CompletionEmitter` that emits `__complete` handler with static lookup
- [ ] Emit shell script generators that call `__complete`
- [ ] Remove reflection from `DynamicCompletionHandler` (use generated static data)

### Phase 3: API Rename
- [ ] Rename `EnableDynamicCompletion()` → `EnableCompletion()`
- [ ] Keep `EnableDynamicCompletion()` as obsolete alias for backward compatibility (optional)
- [ ] Update XML docs

### Phase 4: Consolidate Samples
- [ ] Delete `samples/_shell-completion-example/` (static - no longer supported)
- [ ] Rename `samples/_dynamic-completion-example/` → `samples/15-completion/`
- [ ] Update sample to demonstrate:
  - Basic completion (commands, options, enums - all source-generated)
  - Custom `ICompletionSource` for runtime data (optional advanced use)
- [ ] Verify completion works in bash, zsh, fish, PowerShell

### Phase 5: Cleanup
- [ ] Remove dead code from `timewarp-nuru-completion` if any
- [ ] Update MCP server examples if needed
- [ ] Run all completion tests

## Notes

The REPL already has all the static completion data extraction working in `ReplEmitter`:
- `ExtractCommandPrefixes()` - Leading literal segments
- `ExtractOptions()` - All options with long/short forms and descriptions
- `ExtractRouteOptions()` - Context-aware options per route
- `ExtractParameters()` - Parameter names and types
- `ExtractEnumParameters()` - Enum values via Roslyn compilation

This same data should be reused for shell completion's `__complete` handler instead of 
using runtime reflection.

## Files to Modify

- `source/timewarp-nuru/completion/nuru-app-builder-extensions.cs` - Remove static, rename dynamic
- `source/timewarp-nuru/completion/completion/completion-script-generator.cs` - Remove (static version)
- `source/timewarp-nuru/completion/completion/dynamic-completion-handler.cs` - Remove reflection
- `source/timewarp-nuru-analyzers/generators/emitters/repl-emitter.cs` - Extract shared helpers
- `source/timewarp-nuru-analyzers/generators/emitters/completion-emitter.cs` - New file
- `samples/_dynamic-completion-example/` - Migrate and rename
- `samples/_shell-completion-example/` - Delete
