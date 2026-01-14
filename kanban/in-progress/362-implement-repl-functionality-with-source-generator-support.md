# Implement REPL functionality with source generator support

## Description

Implement REPL (Read-Eval-Print-Loop) functionality directly in `timewarp-nuru` with full source generator support. This replaces the old `timewarp-nuru-repl` package which relied on runtime endpoint discovery.

The new implementation should:
- Work with the V2 source generator architecture
- Generate REPL completion/highlighting data at compile time
- Keep full feature parity: tab completion, syntax highlighting, key bindings, history
- AOT tree-shaking removes REPL code if not used

## Reference Implementation

The old REPL code is preserved for reference:
- `source/timewarp-nuru-repl-reference-only/`
- `tests/timewarp-nuru-repl-tests-reference-only/`

## Architecture

### Key Insight

Almost all REPL code is **runtime** (ReplConsoleReader, key bindings, history, etc.). The only route-dependent parts are:
- **Tab completion** - needs to know what commands/parameters/options exist
- **Syntax highlighting** - needs to know if a token is a valid command

### Solution

1. **Copy reference implementation** into `timewarp-nuru/repl/` (~25 files, ~4000 lines)
2. **Replace `EndpointCollection`** dependency with new `IReplRouteProvider` interface
3. **Generator emits** `GeneratedReplRouteProvider` with compile-time route data
4. **Generator emits** `--interactive`/`-i` route and `RunReplAsync` interceptor

### IReplRouteProvider Interface

```csharp
public interface IReplRouteProvider
{
    /// <summary>Gets all known command prefixes for completion.</summary>
    IReadOnlyList<string> GetCommandPrefixes();
    
    /// <summary>Gets completion candidates for the current input.</summary>
    IEnumerable<CompletionCandidate> GetCompletions(string[] args, bool hasTrailingSpace);
    
    /// <summary>Checks if a token is a known command/subcommand.</summary>
    bool IsKnownCommand(string token);
}
```

### REPL Built-in Commands

Handled directly in `ReplSession` loop (not through route matcher):
- `exit`, `quit`, `q` - exit REPL
- `clear`, `cls` - clear screen
- `history` - show command history
- `clear-history` - clear history

### Exit Mechanism

REPL built-ins are handled *before* calling the command executor:
```csharp
while (Running)
{
    string[] args = CommandLineParser.Parse(ReadCommandInput());
    
    // Handle REPL built-ins directly
    if (args is ["exit"] or ["quit"] or ["q"]) break;
    if (args is ["clear"] or ["cls"]) { Terminal.Clear(); continue; }
    // ...
    
    // Execute user command through generated route matcher
    int exitCode = await executeCommand(app, args, cancellationToken);
}
```

## Checklist

### Phase 1: Runtime Code Integration ✅ COMPLETED

- [x] Copy `input/` folder (ReplConsoleReader + all partials) to `timewarp-nuru/repl/input/`
- [x] Copy `key-bindings/` folder to `timewarp-nuru/repl/key-bindings/`
- [x] Copy `display/` folder to `timewarp-nuru/repl/display/`
- [x] Copy `repl/` core files (ReplSession, ReplHistory, CommandLineParser) to `timewarp-nuru/repl/`
- [x] Create `IReplRouteProvider` interface in `timewarp-nuru/repl/`
- [x] Refactor `ReplSession` to use `IReplRouteProvider` + command execution delegate
- [x] Refactor `TabCompletionHandler` to use `IReplRouteProvider`
- [x] Refactor `SyntaxHighlighter` to use `IReplRouteProvider`
- [x] Update namespaces to `TimeWarp.Nuru` (flat)
- [x] Verify build compiles (533/533 tests pass)

### Phase 2: Generator Additions ✅ COMPLETED

- [x] Update `InterceptorEmitter` to emit `--interactive`/`-i` route when `HasRepl=true`
- [x] Create `ReplEmitter` to emit `GeneratedReplRouteProvider` class
- [x] Update `InterceptorEmitter` to call `ReplEmitter.Emit()` when `HasRepl=true`
- [x] Extract completion data from routes (command prefixes, parameters, options)
- [x] Add `ReplOptions` and `LoggerFactory` properties to `NuruCoreApp`
- [x] Update `NuruCoreAppBuilder.Build()` to pass properties to app
- [x] Add `AddRepl()` methods with null validation

### Phase 3: Testing & Validation

- [ ] Add REPL unit tests using TestTerminal
- [ ] Test interactive mode entry/exit
- [ ] Test command execution in REPL
- [ ] Test tab completion
- [ ] Test history navigation
- [ ] Verify `samples/_repl-demo/` work with new implementation

## File Structure After Integration

```
source/timewarp-nuru/
├── repl/
│   ├── input/
│   │   ├── repl-console-reader.cs
│   │   ├── repl-console-reader.basic-editing.cs
│   │   ├── repl-console-reader.clipboard.cs
│   │   ├── repl-console-reader.cursor-movement.cs
│   │   ├── repl-console-reader.editing.cs
│   │   ├── repl-console-reader.history.cs
│   │   ├── repl-console-reader.kill-ring.cs
│   │   ├── repl-console-reader.multiline.cs
│   │   ├── repl-console-reader.search.cs
│   │   ├── repl-console-reader.selection.cs
│   │   ├── repl-console-reader.undo.cs
│   │   ├── repl-console-reader.word-operations.cs
│   │   ├── repl-console-reader.yank-arg.cs
│   │   ├── tab-completion-handler.cs
│   │   ├── syntax-highlighter.cs
│   │   ├── command-line-token.cs
│   │   ├── completion-state.cs
│   │   ├── edit-mode.cs
│   │   ├── kill-ring.cs
│   │   ├── multiline-buffer.cs
│   │   ├── selection.cs
│   │   ├── token-type.cs
│   │   └── undo-stack.cs
│   ├── key-bindings/
│   │   ├── ikey-binding-profile.cs
│   │   ├── ikey-binding-builder.cs
│   │   ├── key-binding-builder.cs
│   │   ├── key-binding-result.cs
│   │   ├── key-binding-profile-factory.cs
│   │   ├── nested-key-binding-builder.cs
│   │   ├── custom-key-binding-profile.cs
│   │   ├── default-key-binding-profile.cs
│   │   ├── emacs-key-binding-profile.cs
│   │   ├── vi-key-binding-profile.cs
│   │   └── vscode-key-binding-profile.cs
│   ├── display/
│   │   ├── prompt-formatter.cs
│   │   └── syntax-colors.cs
│   ├── repl-session.cs
│   ├── repl-history.cs
│   ├── repl-commands.cs
│   ├── command-line-parser.cs
│   └── irepl-route-provider.cs
├── options/
│   └── repl-options.cs (already exists)
└── ...
```

## Generator Output (when HasRepl=true)

### 1. GeneratedReplRouteProvider

```csharp
file sealed class GeneratedReplRouteProvider : IReplRouteProvider
{
    private static readonly string[] CommandPrefixes = [
        "greet", "status", "git commit", "git status", // ... from routes
    ];
    
    public IReadOnlyList<string> GetCommandPrefixes() => CommandPrefixes;
    
    public IEnumerable<CompletionCandidate> GetCompletions(string[] args, bool hasTrailingSpace)
    {
        // Generated completion logic - knows parameters, options, enums
    }
    
    public bool IsKnownCommand(string token) => 
        CommandPrefixes.Any(p => p.StartsWith(token, StringComparison.OrdinalIgnoreCase));
}
```

### 2. Interactive Mode Route

```csharp
// In route matcher, before user routes:
if (routeArgs is ["--interactive"] or ["-i"])
{
    await app.RunReplAsync().ConfigureAwait(false);
    return 0;
}
```

### 3. RunReplAsync Interceptor

```csharp
[InterceptsLocation(...)]
public static async Task RunReplAsync_Intercepted(
    this NuruCoreApp app,
    ReplOptions? options = null,
    CancellationToken cancellationToken = default)
{
    ReplOptions replOptions = options ?? app.ReplOptions ?? new();
    IReplRouteProvider routeProvider = new GeneratedReplRouteProvider();
    
    await ReplSession.RunAsync(
        app,
        replOptions,
        routeProvider,
        static (app, args, ct) => GeneratedInterceptor.ExecuteRouteAsync(app, args, ct),
        app.LoggerFactory,
        cancellationToken
    ).ConfigureAwait(false);
}
```

## Notes

- Namespace: `TimeWarp.Nuru` (flat, no sub-namespace)
- Tab completion supports: command prefixes, parameters with types, options, enum values
- Syntax highlighting colors known commands differently
- AOT: Unused REPL code should be tree-shaken if `AddRepl()` not called
- `AddRepl()` DSL method already recognized by interpreter (sets `HasRepl=true`)

## Dependencies

- Blocks #338 (Migrate REPL demo samples) - samples need working REPL to migrate to
