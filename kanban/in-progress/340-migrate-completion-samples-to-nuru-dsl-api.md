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

- [x] Delete obsolete completion tests (tested runtime-based system)
- [x] Rename `samples/_dynamic-completion-example/` → `samples/15-completion/`
- [x] Add retained completion tests to CI suite (Directory.Build.props)
- [x] Fix completion registry callback bug (configure callback was ignored)
- [x] Fix completion handlers to use `ITerminal` for testable output
- [x] Add automated endpoint protocol tests (`completion-27-endpoint-protocol.cs`)
- [x] Move completion tests from `dynamic/` to `completion/`
- [x] Add REPL mode to sample for demos
- [ ] Verify shell TAB completion works in bash, zsh, fish, PowerShell (manual testing only)

### Tests in CI Suite

Working completion unit tests (49 total, don't need source generation):
- `completion-15-completion-registry.cs` - 14 tests
- `completion-17-enum-source.cs` - 10 tests  
- `completion-20-dynamic-script-gen.cs` - 12 tests
- `completion-27-endpoint-protocol.cs` - 13 tests (NEW: `__complete`, `--generate-completion`, `--install-completion`)

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
- `tests/timewarp-nuru-tests/completion/dynamic/completion-22-callback-protocol.cs`
- `tests/timewarp-nuru-tests/completion/dynamic/completion-23-custom-sources.cs`
- `tests/timewarp-nuru-tests/completion/dynamic/completion-24-context-aware.cs`
- `tests/timewarp-nuru-tests/completion/dynamic/completion-25-output-format.cs`
- `tests/timewarp-nuru-tests/completion/dynamic/completion-26-enum-partial-filtering.cs`
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
- `samples/15-completion/completion-example.cs` - updated for current API, added REPL mode
- `source/timewarp-nuru/completion/completion/install-completion-handler.cs` - refactored to use ITerminal
- `tests/timewarp-nuru-tests/completion/completion-27-endpoint-protocol.cs` - NEW: 13 automated endpoint tests
