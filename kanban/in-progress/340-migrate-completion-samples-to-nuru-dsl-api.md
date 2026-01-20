# Unify Shell Completion Architecture and Migrate Samples

## Summary

Consolidate the two parallel completion systems (REPL source-generated vs shell runtime-reflected) 
into a single source-generated architecture. Remove static completion support entirely in favor of 
dynamic completion. Migrate and consolidate the two completion samples.

## Progress

### Completed

- [x] Remove `EnableStaticCompletion()` method and static completion infrastructure
- [x] Rename `EnableDynamicCompletion()` → `EnableCompletion()`
- [x] Add `EnableCompletion` to DSL interpreter dispatch table
- [x] Create `CompletionEmitter` that emits `GeneratedShellCompletionProvider`
- [x] Create `CompletionDataExtractor` (shared by ReplEmitter and CompletionEmitter)
- [x] Create `IShellCompletionProvider` interface
- [x] Remove `EndpointCollection` and `IEndpointCollectionBuilder`
- [x] Remove `Endpoint` class
- [x] Remove old completion engine (`completion/engine/` folder)
- [x] Remove `DefaultCompletionSource`, `CompletionProvider`
- [x] Remove dead `HelpProvider` class
- [x] Simplify `CompletionContext` (removed `Endpoints` field)
- [x] Update `DynamicCompletionHandler` to use `IShellCompletionProvider`
- [x] Simplify `EnableCompletion()` (routes now source-generated)
- [x] Delete tests for removed code
- [x] Delete duplicate reference-only REPL tests
- [x] Add `HasCompletion` to `AppModel` to track `EnableCompletion()` calls
- [x] Make completion code emission conditional on `HasCompletion || HasRepl`
- [x] Source generate `__complete` route handler in `InterceptorEmitter`
- [x] Source generate `--generate-completion` route
- [x] Source generate `--install-completion` route (with `--dry-run` support)
- [x] Add `CompletionSourceRegistry` property to `NuruCoreApp`
- [x] Make `DynamicCompletionHandler`, `DynamicCompletionScriptGenerator`, `EmptyShellCompletionProvider` public
- [x] Fix embedded resource paths for completion templates
- [x] Update sample to work with current API
- [x] Delete `MessageTypeFluentApiTests` (used deleted `Endpoint` class)

**~21,000 lines of dead code removed**

### Remaining

- [ ] Add completion tests that use source-generated approach
- [x] Rename `samples/_dynamic-completion-example/` → `samples/15-completion/`
- [ ] Verify completion works in bash, zsh, fish, PowerShell (manual testing)

## Related Issues

- **#387** - Enum option parameters (`--option {enumParam}`) don't generate conversion code

## Architecture (Final)

```
Source Generator (compile time)
├── CompletionDataExtractor - extracts commands, options, enums from routes
├── CompletionEmitter - emits GeneratedShellCompletionProvider class
└── InterceptorEmitter - emits completion routes when HasCompletion is true
    ├── __complete {index} {*words} - shell callback
    ├── --generate-completion {shell} - script generation
    └── --install-completion {shell?} [--dry-run] - installation

Runtime
├── IShellCompletionProvider - interface for source-generated completions
├── DynamicCompletionHandler - uses provider for static completions
├── DynamicCompletionScriptGenerator - generates bash/zsh/fish/pwsh scripts
├── InstallCompletionHandler - installs scripts to shell config
├── CompletionSourceRegistry - custom ICompletionSource for runtime data
└── ICompletionSource - interface for DB/API completion sources
```

## Files Modified/Deleted

### Deleted (dead code)
- `source/timewarp-nuru/endpoints/endpoint-collection.cs`
- `source/timewarp-nuru/endpoints/iendpoint-collection-builder.cs`
- `source/timewarp-nuru/endpoints/endpoint.cs`
- `source/timewarp-nuru/completion/completion/engine/` (entire folder)
- `source/timewarp-nuru/completion/completion/completion-provider.cs`
- `source/timewarp-nuru/completion/completion/sources/default-completion-source.cs`
- `source/timewarp-nuru/help/help-provider.ansi.cs`
- `source/timewarp-nuru/help/help-provider.filtering.cs`
- `source/timewarp-nuru/help/help-provider.formatting.cs`
- `tests/timewarp-nuru-tests/completion/engine/` (engine tests)
- `tests/timewarp-nuru-tests/completion/dynamic/` (most files)
- `tests/timewarp-nuru-repl-tests-reference-only/` (entire folder)
- `tests/timewarp-nuru-tests/message-type/message-type-01-fluent-api.cs`
- `samples/_shell-completion-example/` (static completion)

### Created
- `source/timewarp-nuru/completion/ishell-completion-provider.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/completion-emitter.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/completion-data-extractor.cs`

### Modified
- `source/timewarp-nuru-analyzers/generators/models/app-model.cs` - added `HasCompletion`
- `source/timewarp-nuru-analyzers/generators/ir-builders/abstractions/iir-app-builder.cs` - added `EnableCompletion()`
- `source/timewarp-nuru-analyzers/generators/ir-builders/ir-app-builder.cs` - implemented `EnableCompletion()`
- `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs` - updated `DispatchEnableCompletion`
- `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` - conditional completion emission + route handlers
- `source/timewarp-nuru/nuru-core-app.cs` - added `CompletionSourceRegistry`
- `source/timewarp-nuru/completion/completion/dynamic-completion-handler.cs` - made public, added null checks
- `source/timewarp-nuru/completion/completion/dynamic-completion-script-generator.cs` - made public, fixed resource paths
- `source/timewarp-nuru/completion/ishell-completion-provider.cs` - made `EmptyShellCompletionProvider` public
- `samples/_dynamic-completion-example/dynamic-completion-example.cs` - updated for current API
