# Implement Interactive REPL Mode with Tab Completion

## Status: IN PROGRESS (Phase 1 Complete)
## Priority: Medium
## Category: Feature Enhancement

## Problem

TimeWarp.Nuru runs commands in single-shot mode where each execution requires launching the application, parsing routes, executing, and exiting. For exploratory workflows, testing, and interactive use cases, this creates friction:

- **Slow iteration**: Each command requires full app startup overhead
- **No persistent context**: Cannot build state across commands
- **Limited discoverability**: Users must refer to documentation or help text separately
- **Poor experimentation UX**: Testing different parameter combinations requires repeated app launches

### What Is REPL Mode?

REPL (Read-Eval-Print Loop) is an interactive mode where the application:
1. **Reads** user input from a prompt
2. **Evaluates** the input by executing the corresponding route
3. **Prints** the result
4. **Loops** back to step 1

Example session:
```bash
$ myapp --repl
TimeWarp.Nuru REPL v1.0.0 - Type 'exit' to quit, 'help' for commands

> git status
On branch master
Your branch is up to date

> git commit -m "fix: update parser"
[master 1a2b3c4] fix: update parser
 1 file changed, 10 insertions(+)

> deploy staging --dry-run
Simulating deployment to staging...
✓ Build successful
✓ Tests passed
Dry run completed

> exit
$
```

### Why This Matters for TimeWarp.Nuru

1. **Developer Experience**: REPLs are familiar from Python, Node.js, F#, PowerShell - developers expect them
2. **Testing Routes**: Developers building Nuru apps can test routes interactively
3. **Exploratory Use**: Users can discover commands via tab completion without leaving the session
4. **Performance**: After initial startup, commands run immediately (no repeated parsing overhead)
5. **State Management**: Optional - REPL could maintain DI scope, configuration, or context between commands

### Precedent: Interactive Help Already Exists

The documentation already demonstrates the interactive pattern in [implementing-help.md:277-317](../documentation/developer/guides/implementing-help.md#L277-L317):

```csharp
builder.AddRoute("help --interactive", async () =>
{
    Console.WriteLine("Interactive Help - Type a command name or 'exit' to quit");

    while (true)
    {
        Console.Write("\nhelp> ");
        var input = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(input) || input == "exit")
            break;

        // Find matching commands
        var matches = endpoints.Endpoints
            .Where(e => e.RoutePattern.Contains(input, StringComparison.OrdinalIgnoreCase))
            .ToList();
        // ... show help ...
    }
});
```

This proves the pattern works. REPL mode extends this to **execute** commands instead of just showing help.

## Technical Background

### How REPL Differs from Shell Completion

This task is related but distinct from [Task 025: Shell Tab Completion](025_Implement-Shell-Tab-Completion.md):

| Feature | Shell Completion (Task 025) | REPL Mode (This Task) |
|---------|----------------------------|----------------------|
| **Scope** | System shell (bash/zsh/PowerShell) | Within Nuru application |
| **Completion Provider** | Shell scripts call app for candidates | In-process completion handler |
| **State** | Stateless (each invocation is fresh) | Optionally stateful (persistent session) |
| **Installation** | User installs shell scripts | No installation, just `--repl` flag |
| **Integration** | External (shell hooks) | Internal (application feature) |
| **Use Case** | Normal CLI usage (`myapp git <TAB>`) | Interactive exploration (`> git <TAB>`) |

**Key Synergy:** Both features can share the **same `CompletionProvider` infrastructure** developed in Task 025. The provider generates completion candidates from route metadata - REPL just consumes them via a different interface (readline library vs shell scripts).

### TimeWarp.Nuru's Architecture Advantages for REPL

Nuru's stateless design is **ideal** for REPL mode:

1. **Reusable Execution Path**: `NuruApp.RunAsync(string[] args)` is stateless and can be called repeatedly
   - File: [Source/TimeWarp.Nuru/NuruApp.cs:61-133](../../Source/TimeWarp.Nuru/NuruApp.cs#L61-L133)
   - Takes args[], routes through EndpointResolver, executes, returns exit code
   - No cleanup needed between invocations

2. **Route Metadata Accessibility**: `EndpointCollection.Endpoints` exposes all registered routes
   - File: [Source/TimeWarp.Nuru/Endpoints/EndpointCollection.cs:15](../../Source/TimeWarp.Nuru/Endpoints/EndpointCollection.cs#L15)
   - Public `IReadOnlyList<Endpoint>` property
   - Each Endpoint contains RoutePattern, CompiledRoute, Description
   - Perfect for generating completion candidates

3. **Pattern Matching Reusability**: `EndpointResolver.Resolve()` is stateless
   - File: [Source/TimeWarp.Nuru/Resolution/EndpointResolver.cs:16-54](../../Source/TimeWarp.Nuru/Resolution/EndpointResolver.cs#L16-L54)
   - Can be called in loop without side effects
   - Returns `EndpointResolutionResult` with match details

4. **Typed Metadata for Smart Completion**:
   - `ParameterMatcher`: Has Name, Constraint ("int", "FileInfo"), IsOptional
   - `OptionMatcher`: Has MatchPattern ("--force"), AlternateForm ("-f"), Description
   - `LiteralMatcher`: Has Value for command name completion
   - All available in `CompiledRoute.Segments`

### Input Library Options

For interactive line editing with tab completion, we need a library:

#### Option A: ReadLine NuGet Package
- **Package**: `ReadLine` (GNU readline-style for .NET)
- **Pros**:
  - Native tab completion support via `IAutoCompleteHandler`
  - Command history built-in
  - Cross-platform (Windows/Linux/macOS)
  - Simple API: `ReadLine.Read(prompt)`
- **Cons**:
  - Additional dependency
  - Basic styling (no colors)
- **Example**:
  ```csharp
  ReadLine.AutoCompletionHandler = new ReplCompletionHandler(endpoints);
  string? input = ReadLine.Read("> ");
  ```

#### Option B: Spectre.Console
- **Package**: `Spectre.Console` (rich console UI library)
- **Pros**:
  - Already used in some Nuru samples
  - Rich formatting, colors, tables, prompts
  - `TextPrompt<T>` with auto-complete
  - Consistent with Spectre-based samples
- **Cons**:
  - Heavier dependency
  - More complex API
  - Tab completion requires custom `ICompletionSource`
- **Example**:
  ```csharp
  var prompt = new TextPrompt<string>("> ")
      .AddCompletionSource(new NuruCompletionSource(endpoints));
  string input = AnsiConsole.Prompt(prompt);
  ```

#### Option C: Manual with Console.ReadKey()
- **Package**: None (built-in)
- **Pros**:
  - Zero dependencies
  - Full control
  - Lightweight
- **Cons**:
  - Must implement tab completion manually (100+ lines)
  - Must implement history manually
  - Must handle cursor positioning, backspace, etc.
  - High maintenance burden

**Recommendation**: **Option A (ReadLine)** for initial implementation
- Lightweight single-purpose library
- Native tab completion support
- Can upgrade to Spectre.Console later if richer UI is desired
- Keeps REPL feature focused and maintainable

## Solution Approach

### Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                    User's Terminal                       │
│  > git commit -m "test"<TAB>                            │
└──────────────────┬──────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────┐
│                  ReplMode.RunAsync()                     │
│  ┌──────────────────────────────────────────────────┐  │
│  │ while (true)                                      │  │
│  │   input = ReadLine.Read("> ")  ◄─── History      │  │
│  │   args = ParseCommandLine(input)                 │  │
│  │   await app.RunAsync(args)                       │  │
│  └──────────────────────────────────────────────────┘  │
└──────────────────┬──────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────┐
│           CompletionProvider (from Task 025)             │
│  ┌──────────────────────────────────────────────────┐  │
│  │ GetCompletions(context)                          │  │
│  │   → Analyze current args[]                       │  │
│  │   → Check position in route pattern              │  │
│  │   → Return candidates (commands/params/options)  │  │
│  └──────────────────────────────────────────────────┘  │
└──────────────────┬──────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────┐
│              EndpointCollection                          │
│  All registered routes with metadata                    │
└─────────────────────────────────────────────────────────┘
```

### Key Design Decisions

1. **Stateless vs Stateful**: Start **stateless** (each command is independent)
   - Simpler implementation
   - Matches normal CLI behavior
   - Can add stateful mode later (persistent DI scope, variables, etc.)

2. **Entry Point**: Add `--repl` flag to enter REPL mode
   ```csharp
   var app = new NuruAppBuilder()
       .AddRoute("git status", () => ...)
       .EnableRepl()  // Adds "--repl" route
       .Build();
   ```

3. **Exit Mechanism**:
   - `exit` command
   - `quit` command
   - Ctrl+C / Ctrl+D (EOF)
   - Empty input N times? (configurable)

4. **Error Handling**: Catch exceptions, print error, continue loop (don't crash REPL)

5. **Help Integration**: Special `help` command shows all routes (unless user defines custom help)

## Implementation Plan

### Phase 1: Core REPL Loop (Without Tab Completion)

**Goal**: Get basic REPL working with manual input, no completion yet.

**Files to Create:**

1. **Source/TimeWarp.Nuru/Repl/ReplMode.cs** (~150 lines)
   ```csharp
   public static class ReplMode
   {
       public static async Task<int> RunAsync(
           NuruApp app,
           EndpointCollection endpoints,
           ReplOptions? options = null)
       {
           Console.WriteLine($"TimeWarp.Nuru REPL - Type 'exit' to quit");

           while (true)
           {
               Console.Write("\n> ");
               string? input = Console.ReadLine()?.Trim();

               if (ShouldExit(input))
                   return 0;

               if (string.IsNullOrWhiteSpace(input))
                   continue;

               try
               {
                   string[] args = ParseCommandLine(input);
                   await app.RunAsync(args);
               }
               catch (Exception ex)
               {
                   Console.Error.WriteLine($"Error: {ex.Message}");
               }
           }
       }

       private static bool ShouldExit(string? input) =>
           input == "exit" || input == "quit";

       private static string[] ParseCommandLine(string input)
       {
           // Simple implementation: split on spaces
           // TODO: Handle quoted strings properly
           return input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
       }
   }
   ```

2. **Source/TimeWarp.Nuru/Repl/ReplOptions.cs** (~30 lines)
   ```csharp
   public class ReplOptions
   {
       public string Prompt { get; init; } = "> ";
       public bool ShowWelcome { get; init; } = true;
       public string? WelcomeMessage { get; init; }
       public List<string> ExitCommands { get; init; } = new() { "exit", "quit" };
   }
   ```

**Files to Update:**

1. **Source/TimeWarp.Nuru/NuruAppBuilder.cs** (add `EnableRepl()` method)
   ```csharp
   public NuruAppBuilder EnableRepl(ReplOptions? options = null)
   {
       AddRoute("--repl", async () =>
       {
           var app = new NuruApp(EndpointCollection, /* ... */);
           return await ReplMode.RunAsync(app, EndpointCollection, options);
       }, description: "Start interactive REPL mode");

       return this;
   }
   ```

**Implementation Steps:**

1. Create `ReplMode.cs` with basic loop structure
2. Implement `ParseCommandLine()` with quote handling
3. Create `ReplOptions.cs` for configuration
4. Add `EnableRepl()` extension to `NuruAppBuilder`
5. Handle Ctrl+C gracefully (register `Console.CancelKeyPress` handler)
6. Add welcome message and exit confirmation

**Tests to Add:**

Create `Tests/TimeWarp.Nuru.Tests/Repl/repl-01-basic-loop.cs`:
```csharp
#!/usr/bin/dotnet --
// Test: Basic REPL loop accepts commands and executes them
// Input: Simulated via StringReader
// Expected: Commands execute, exit works
```

Manual testing scenarios:
- Start REPL with `--repl`
- Execute simple command like `version`
- Execute parameterized command
- Type invalid command, verify error handling
- Type `exit`, verify clean shutdown

### Phase 2: Tab Completion Integration

**Goal**: Integrate with `CompletionProvider` from Task 025 to enable tab completion.

**Prerequisites**: Task 025 Phase 1 must be complete (`CompletionProvider` class exists)

**Files to Create:**

1. **Source/TimeWarp.Nuru/Repl/ReplCompletionHandler.cs** (~100 lines)
   ```csharp
   using ReadLine;

   public class ReplCompletionHandler : IAutoCompleteHandler
   {
       private readonly EndpointCollection _endpoints;
       private readonly CompletionProvider _completionProvider;

       public char[] Separators { get; set; } = new[] { ' ' };

       public string[] GetSuggestions(string text, int index)
       {
           // Parse current input
           string[] args = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

           // Build context for CompletionProvider
           var context = new CompletionContext(
               Args: args,
               CursorPosition: index,
               Endpoints: _endpoints
           );

           // Get candidates from shared completion logic
           var candidates = _completionProvider.GetCompletions(context);

           // Return just the values (ReadLine handles display)
           return candidates.Select(c => c.Value).ToArray();
       }
   }
   ```

**Files to Update:**

1. **Source/TimeWarp.Nuru/Repl/ReplMode.cs**
   - Replace `Console.ReadLine()` with `ReadLine.Read()`
   - Set `ReadLine.AutoCompletionHandler` to `ReplCompletionHandler`
   - Enable history: `ReadLine.HistoryEnabled = true`

2. **Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj**
   - Add package reference: `<PackageReference Include="ReadLine" Version="2.0.1" />`

**Implementation Steps:**

1. Add `ReadLine` package dependency
2. Create `ReplCompletionHandler` implementing `IAutoCompleteHandler`
3. Update `ReplMode.RunAsync()` to use ReadLine library
4. Wire up `CompletionProvider` instance (from Task 025)
5. Test tab completion for:
   - Command names (literal segments)
   - Parameters (with type hints)
   - Options (both `--long` and `-short` forms)
   - Enum values

**Tests to Add:**

```csharp
// Tests/TimeWarp.Nuru.Tests/Repl/repl-02-tab-completion.cs
// Simulated tests:
// - Typing "gi<TAB>" completes to "git"
// - Typing "git <TAB>" shows "status", "commit", "log"
// - Typing "git commit -<TAB>" shows "--amend", "-m", etc.
```

**Manual Testing Scenarios:**

1. Start REPL, type `gi<TAB>` → should complete to `git` if unique
2. Type `git <TAB>` → should show subcommands
3. Type `deploy <TAB>` → should show environment parameters
4. Type `build --<TAB>` → should show available options
5. Test multi-word completion (e.g., `git commit --am<TAB>` → `--amend`)

### Phase 3: Enhanced UX and History

**Goal**: Polish user experience with history, improved prompts, and quality-of-life features.

**Files to Create:**

1. **Source/TimeWarp.Nuru/Repl/ReplHistory.cs** (~80 lines)
   ```csharp
   public class ReplHistory
   {
       private readonly List<string> _commands = new();
       private readonly string? _historyFile;

       public void Add(string command) { /* ... */ }
       public void SaveToFile() { /* ... */ }
       public void LoadFromFile() { /* ... */ }
   }
   ```

**Files to Update:**

1. **Source/TimeWarp.Nuru/Repl/ReplMode.cs**
   - Add colored prompts using `AnsiConsole` (if using Spectre.Console) or ANSI codes
   - Persist history to `~/.nuru_history` file
   - Add special commands:
     - `help` → Show all available routes
     - `history` → Show command history
     - `clear` → Clear screen
   - Show execution time for each command
   - Add multi-line input support (commands ending with `\`)

2. **Source/TimeWarp.Nuru/Repl/ReplOptions.cs**
   - Add `string? HistoryFile` property
   - Add `int MaxHistorySize` property (default 1000)
   - Add `bool ShowExecutionTime` property
   - Add `bool EnableMultiline` property

**Implementation Steps:**

1. Implement history persistence (save on exit, load on start)
2. Add special REPL-only commands (`help`, `clear`, `history`)
3. Add colored/styled prompt (green `>`, red for errors, etc.)
4. Show execution time after each command
5. Handle multi-line input (line ending with `\`)
6. Add Ctrl+C handling (cancel current input, not exit REPL)

**Tests to Add:**

```csharp
// Tests/TimeWarp.Nuru.Tests/Repl/repl-03-history.cs
// - History saves to file
// - History loads on startup
// - History max size enforced
```

### Phase 4: Documentation and Samples

**Goal**: Comprehensive documentation and working examples.

**Files to Create:**

1. **documentation/user/guides/using-repl-mode.md** (~400 lines)
   - What is REPL mode and when to use it
   - How to enable REPL in your app
   - Tab completion usage
   - Special commands (`help`, `exit`, etc.)
   - Configuration options
   - Comparison to normal CLI mode
   - Troubleshooting

2. **documentation/developer/guides/implementing-repl.md** (~300 lines)
   - How to enable REPL for your Nuru app
   - Customizing prompt and behavior
   - Adding REPL-specific commands
   - Stateful vs stateless REPL
   - Integration with custom completion providers

3. **Samples/ReplDemo/** (new directory)
   - `repl-basic.cs` - Minimal REPL example
   - `repl-custom-prompt.cs` - Custom prompt styling
   - `repl-stateful.cs` - Stateful REPL with persistent variables
   - `repl-custom-commands.cs` - Adding REPL-specific commands

**Files to Update:**

1. **README.md** - Add REPL mode section in features list
2. **documentation/index.md** - Link to REPL guides
3. **CHANGELOG.md** - Document new REPL feature

**Implementation Steps:**

1. Write user-facing guide with screenshots/examples
2. Write developer implementation guide
3. Create 4 sample applications demonstrating REPL
4. Update main README with REPL feature highlight
5. Add REPL demo to quickstart guide
6. Update comparison docs (Nuru vs Cocona) showing REPL as unique feature

## Test Scenarios

### Automated Tests

**Directory**: `Tests/TimeWarp.Nuru.Tests/Repl/`

1. **repl-01-basic-loop.cs**
   - Test: REPL accepts input, executes routes, exits cleanly
   - Approach: Mock `Console.In` with `StringReader`, verify commands execute

2. **repl-02-tab-completion.cs**
   - Test: Tab completion returns correct candidates
   - Approach: Call `ReplCompletionHandler.GetSuggestions()` directly, verify output

3. **repl-03-history.cs**
   - Test: History saves and loads correctly
   - Approach: Create REPL, execute commands, verify history file contents

4. **repl-04-error-handling.cs**
   - Test: Invalid commands don't crash REPL
   - Approach: Send invalid input, verify REPL continues running

5. **repl-05-special-commands.cs**
   - Test: `help`, `history`, `clear` commands work
   - Approach: Execute special commands, verify expected behavior

6. **repl-06-multiline-input.cs**
   - Test: Commands ending with `\` continue on next line
   - Approach: Send multi-line input, verify correct parsing

### Manual Testing Checklist

- [ ] Start REPL with `myapp --repl`
- [ ] Execute simple command (e.g., `version`)
- [ ] Execute parameterized command (e.g., `greet Alice`)
- [ ] Execute command with options (e.g., `build --release`)
- [ ] Tab completion on command name
- [ ] Tab completion on parameter
- [ ] Tab completion on option
- [ ] Type invalid command, verify error message
- [ ] Type `help`, verify routes listed
- [ ] Type `history`, verify previous commands shown
- [ ] Type `clear`, verify screen cleared
- [ ] Use up/down arrows for history navigation
- [ ] Type `exit`, verify clean shutdown
- [ ] Press Ctrl+C during input, verify input canceled (not REPL exit)
- [ ] Press Ctrl+D, verify REPL exits
- [ ] Restart REPL, verify history persisted
- [ ] Test multi-line input with `\`

### Integration Testing

Run REPL mode against existing test apps:

**Delegate approach:**
```bash
./Tests/TimeWarp.Nuru.TestApp.Delegates/bin/Release/net9.0/TimeWarp.Nuru.TestApp.Delegates --repl
> git status
> git commit -m "test"
> exit
```

**Mediator approach:**
```bash
./Tests/TimeWarp.Nuru.TestApp.Mediator/bin/Release/net9.0/TimeWarp.Nuru.TestApp.Mediator --repl
> git status
> exit
```

Verify both approaches work identically in REPL mode.

## Success Criteria

### Phase 1: Core REPL Loop
- [ ] `--repl` flag enters interactive mode
- [ ] Prompt displays and accepts input
- [ ] Commands execute via existing `RunAsync()` path
- [ ] Invalid commands show error but don't crash REPL
- [ ] `exit` command exits cleanly
- [ ] Ctrl+C cancels input, Ctrl+D exits
- [ ] Works with both Delegate and Mediator approaches
- [ ] Basic automated test passes

### Phase 2: Tab Completion
- [ ] Tab key triggers completion
- [ ] Command names complete correctly
- [ ] Parameters show type hints
- [ ] Options complete (both long and short forms)
- [ ] Enum values complete for enum parameters
- [ ] File paths complete for FileInfo parameters
- [ ] Completion candidates have descriptions
- [ ] Shared `CompletionProvider` from Task 025 works
- [ ] Tab completion test suite passes

### Phase 3: Enhanced UX
- [ ] Command history persists to file
- [ ] Up/down arrows navigate history
- [ ] `help` command shows all routes
- [ ] `history` command shows previous commands
- [ ] `clear` command clears screen
- [ ] Colored/styled prompts work
- [ ] Execution time displayed after commands
- [ ] Multi-line input supported (line ending with `\`)
- [ ] History max size enforced
- [ ] All UX tests pass

### Phase 4: Documentation
- [ ] User guide complete with examples
- [ ] Developer guide complete with API docs
- [ ] 4 sample applications work
- [ ] README updated with REPL feature
- [ ] Comparison docs updated (Nuru vs others)
- [ ] CHANGELOG entry added
- [ ] Quickstart includes REPL demo

## Related Tasks

- **[Task 025: Implement Shell Tab Completion](025_Implement-Shell-Tab-Completion.md)**
  - Provides `CompletionProvider` infrastructure that REPL reuses
  - REPL should coordinate implementation after Task 025 Phase 1
  - Both features share completion logic but different integration points

- **[Task 004: Add Shell Completion Support](004_Add-Shell-Completion-Support.md)**
  - Earlier version of Task 025
  - May be superseded by Task 025

## Benefits

### For Users

1. **Faster Iteration**: Test commands immediately without repeated app startup
2. **Discoverability**: Tab completion reveals available commands and options
3. **Experimentation**: Try different parameter combinations interactively
4. **Familiar Pattern**: REPLs are common in Python, Node, PowerShell, F# - users expect them
5. **Persistent Context**: (Future) Could maintain state, variables, configuration across commands

### For Framework

1. **Competitive Feature**: Cocona doesn't have REPL - differentiates Nuru
2. **Showcases Architecture**: Demonstrates how stateless design enables reusability
3. **Testing Tool**: Developers building Nuru apps can test routes interactively
4. **Documentation Vehicle**: Interactive examples in docs can use REPL mode
5. **Completion Infrastructure**: Validates `CompletionProvider` from Task 025 works correctly

## Technical Challenges

### Challenge 1: Command Line Parsing with Quotes

**Problem**: Simple `Split(' ')` doesn't handle quoted arguments:
```bash
> git commit -m "fix: handle spaces"
# Should parse as: ["git", "commit", "-m", "fix: handle spaces"]
# Not: ["git", "commit", "-m", "\"fix:", "handle", "spaces\""]
```

**Solution**: Implement proper shell-like parsing with quote handling:
```csharp
private static string[] ParseCommandLine(string input)
{
    var args = new List<string>();
    bool inQuote = false;
    char quoteChar = '\0';
    var currentArg = new StringBuilder();

    foreach (char c in input)
    {
        if (c == '"' || c == '\'')
        {
            if (!inQuote)
            {
                inQuote = true;
                quoteChar = c;
            }
            else if (c == quoteChar)
            {
                inQuote = false;
                quoteChar = '\0';
            }
            else
            {
                currentArg.Append(c);
            }
        }
        else if (c == ' ' && !inQuote)
        {
            if (currentArg.Length > 0)
            {
                args.Add(currentArg.ToString());
                currentArg.Clear();
            }
        }
        else
        {
            currentArg.Append(c);
        }
    }

    if (currentArg.Length > 0)
        args.Add(currentArg.ToString());

    return args.ToArray();
}
```

### Challenge 2: Completion Context Ambiguity

**Problem**: Cursor position in `ReadLine` may not align with argument boundaries.

**Example**:
```bash
> git commit -m "test"
         ^--- Cursor here (position 6)
```

How do we know user wants to complete `commit` vs start a new parameter?

**Solution**: `CompletionProvider` from Task 025 handles this by analyzing:
- Number of complete arguments before cursor
- Partial argument at cursor
- Route pattern expectations at that position

Trust Task 025's design - it addresses this.

### Challenge 3: State Management

**Problem**: Should REPL maintain state between commands?

**Example**:
```bash
> set env=staging
> deploy $env  # Uses previously set variable
```

**Solution for Phase 1-3**: Keep it **stateless** (each command independent)
- Simpler implementation
- Matches normal CLI behavior
- Avoids scope/lifetime complexity

**Future Enhancement**: Add stateful mode as opt-in:
```csharp
.EnableRepl(new ReplOptions { Stateful = true })
```

This would maintain a shared `IServiceProvider` scope and variable dictionary.

### Challenge 4: Cross-Platform Console Behavior

**Problem**: Terminal behavior varies across Windows/Linux/macOS:
- Windows cmd.exe vs PowerShell vs Terminal
- Linux terminals (xterm, gnome-terminal, etc.)
- macOS Terminal.app

**Solution**: Use `ReadLine` library which abstracts cross-platform differences:
- Works on Windows, Linux, macOS
- Handles terminal capabilities detection
- Falls back to basic input if advanced features unavailable

**Testing**: Run manual tests on all three platforms before release.

## Package Considerations

### Should REPL Be Separate Package?

**Option A: Include in Core `TimeWarp.Nuru` Package**
- **Pros**: Available by default, no extra install
- **Cons**: Adds `ReadLine` dependency for all users (even if not using REPL)

**Option B: Separate `TimeWarp.Nuru.Repl` Package**
- **Pros**: Users opt-in, core stays lightweight
- **Cons**: Fragmentation, extra install step

**Recommendation**: **Option A (Include in Core)** because:
1. `ReadLine` is tiny (2.0.1 is ~50KB)
2. REPL is a first-class feature, not an add-on
3. Simplifies documentation ("just use `--repl`")
4. No version mismatch issues between packages
5. Consistent with how other frameworks handle REPL (Python, Node, etc.)

If package size becomes a concern later, can split it. Start integrated.

## Timeline Estimate

### Phase 1: Core REPL Loop
- Design and planning: 1 hour
- Implement `ReplMode.cs`: 2 hours
- Implement `ParseCommandLine()` with quotes: 1 hour
- Add `EnableRepl()` to builder: 1 hour
- Error handling and edge cases: 1 hour
- Basic tests: 1 hour
- **Subtotal: 7 hours**

### Phase 2: Tab Completion Integration
- Study Task 025's `CompletionProvider` API: 1 hour
- Implement `ReplCompletionHandler`: 2 hours
- Integrate `ReadLine` library: 1 hour
- Wire up completion provider: 1 hour
- Test completion for commands/params/options: 2 hours
- Fix issues and edge cases: 1 hour
- **Subtotal: 8 hours**

### Phase 3: Enhanced UX and History
- Implement history persistence: 2 hours
- Add special commands (help/clear/history): 1 hour
- Add colored prompts and execution time: 1 hour
- Multi-line input support: 2 hours
- Ctrl+C/Ctrl+D handling: 1 hour
- Polish and testing: 2 hours
- **Subtotal: 9 hours**

### Phase 4: Documentation and Samples
- User guide: 3 hours
- Developer guide: 2 hours
- 4 sample applications: 3 hours
- Update README and CHANGELOG: 1 hour
- **Subtotal: 9 hours**

### Testing and Polish
- Manual testing on Windows/Linux/macOS: 3 hours
- Integration testing with test apps: 2 hours
- Bug fixes from testing: 3 hours
- **Subtotal: 8 hours**

**Total Estimate: 41 hours** (~1 week of full-time work, or 2-3 weeks part-time)

**Complexity**: Medium
- Relies on existing infrastructure (Task 025 for completion)
- REPL loop pattern is straightforward
- Main complexity is input parsing and cross-platform terminal behavior

## Priority Justification

**Priority: Medium**

### Why Not High?
- Not blocking other features
- Not fixing a bug or broken functionality
- Users can still use Nuru effectively without REPL (one-shot commands work fine)

### Why Not Low?
- **Significant UX improvement** for exploratory workflows
- **Competitive differentiator**: Cocona doesn't have REPL
- **Validates architecture**: Shows stateless design enables advanced features
- **Developer tool**: Helps Nuru app developers test routes interactively
- **Synergy with Task 025**: Reuses completion infrastructure, amplifying that investment

### Suggested Sequencing
1. **Complete Task 025 Phase 1** (CompletionProvider) first
2. **Then implement this task's Phase 1** (basic REPL)
3. **Then this task's Phase 2** (integrate completion)
4. **Finish both tasks' Phase 3-4** (polish and docs)

This maximizes code reuse and validates the shared completion design.

## Notes

### Relationship to Existing Interactive Help

The [implementing-help.md](../documentation/developer/guides/implementing-help.md) guide already shows an interactive help system (lines 277-317). REPL mode is a natural extension:

**Interactive Help (Current):**
- Input loop: ✅
- Access to endpoints: ✅
- Shows help for routes: ✅
- **Executes routes**: ❌

**REPL Mode (This Task):**
- Input loop: ✅
- Access to endpoints: ✅
- Shows help for routes: ✅ (as special command)
- **Executes routes**: ✅ ← Key addition
- **Tab completion**: ✅
- **History**: ✅
- **Polish and configuration**: ✅

REPL builds directly on the proven interactive pattern.

### Future Enhancements (Out of Scope)

Features to consider for future iterations:

1. **Stateful Mode**: Maintain variables and DI scope across commands
   ```bash
   > set env=staging
   > deploy $env
   ```

2. **Scripting Support**: Read commands from file
   ```bash
   > source commands.txt
   ```

3. **Piping**: Connect command outputs
   ```bash
   > git log | grep "fix:"
   ```

4. **Syntax Highlighting**: Color keywords, parameters, options as user types

5. **Inline Help**: Show parameter hints while typing
   ```bash
   > git commit -m <message:string>
                    ^--- hint appears here
   ```

6. **Multi-User REPL**: Remote REPL over TCP/HTTP (like Python's `code.interact()` over network)

7. **REPL as Testing Tool**: Record REPL session as test scenario

Keep Phase 1-4 focused. These are valuable but can be added incrementally based on user feedback.

## Progress Updates

### 2025-11-17: Phase 1 Complete ✅

**Implementation as Separate Library** (TimeWarp.Nuru.Repl)
- Created `TimeWarp.Nuru.Repl` as separate optional package (matching pattern of `TimeWarp.Nuru.Completion`)
- Zero external dependencies - uses built-in Console APIs only
- Decided against ReadLine library (abandoned 7+ years) per user preference

**Core Files Implemented:**
- `Source/TimeWarp.Nuru.Repl/Repl/ReplMode.cs` - Main REPL loop (~290 lines)
- `Source/TimeWarp.Nuru.Repl/Repl/ReplOptions.cs` - Configuration options
- `Source/TimeWarp.Nuru.Repl/Repl/CommandLineParser.cs` - Quote-aware argument parsing
- `Source/TimeWarp.Nuru.Repl/NuruAppExtensions.cs` - Extension methods for NuruApp

**Features Delivered:**
- ✅ Interactive command loop with `Console.ReadLine()`
- ✅ Quote-aware parsing (`greet "John Doe"` → `["greet", "John Doe"]`)
- ✅ Special REPL commands: `exit`, `quit`, `q`, `help`, `?`, `history`, `clear`, `cls`, `clear-history`
- ✅ Graceful Ctrl+C handling (cancels loop, doesn't crash)
- ✅ EOF detection (Ctrl+D on Unix, Ctrl+Z on Windows)
- ✅ History persistence to `~/.nuru_history`
- ✅ Configurable prompt, welcome/goodbye messages, history size
- ✅ Error resilience (invalid commands don't crash REPL)
- ✅ Extension methods: `app.RunReplAsync()` and `app.RunWithReplSupportAsync(args)`

**Tests:**
- 19 unit tests passing (8 basic parsing + 11 quote handling)
- Tests added to `Tests/TimeWarp.Nuru.Repl.Tests/`
- Integrated into test runner (`Tests/Scripts/run-all-tests.cs`)

**Samples:**
- Created `Samples/ReplDemo/repl-basic-demo.cs` - Working demo

**Key Architectural Decisions:**
1. **Separate library** - Keeps core Nuru lightweight, REPL is opt-in
2. **No external dependencies** - Avoided abandoned ReadLine library
3. **Extension methods on NuruApp** (not NuruAppBuilder) - Simpler API
4. **Stateless execution** - Each command is independent (per plan)

**Commits:**
- `b31eb85` - chore: move Task 027 (REPL Mode) to InProgress
- `6b36274` - feat: implement REPL mode Phase 1 (Task 027)
- `d0379e1` - chore: add Repl tests to test runner

**Phase 1 Success Criteria Status:**
- [x] ~~`--repl` flag enters interactive mode~~ → Changed to extension method approach
- [x] Prompt displays and accepts input
- [x] Commands execute via existing `RunAsync()` path
- [x] Invalid commands show error but don't crash REPL
- [x] `exit` command exits cleanly
- [x] Ctrl+C cancels input, Ctrl+D exits
- [x] Works with both Delegate and Mediator approaches
- [x] Basic automated tests pass

### Next Steps (Phase 2+)

**Not Implementing Tab Completion as Originally Planned**
- ReadLine library is abandoned (7+ years old)
- Spectre.Console doesn't have native tab completion
- Manual `Console.ReadKey()` implementation would be ~200-300 lines of fragile code
- **Decision**: Tab completion remains available via shell integration (Task 025), not in-REPL

**Possible Future Enhancements:**
- Manual `Console.ReadKey()` loop for up/down arrow history navigation
- Colored prompts using ANSI codes
- Integration with `CompletionProvider` for inline hints (not full tab completion)
- Additional REPL commands (`status`, `routes`, etc.)

### 2025-11-18: Phase 2 Started - Simple AddRoute Implementation

**Problem:** REPL commands (`exit`, `help`, `history`, etc.) are currently handled as special cases in `ReplMode.cs` instead of being registered as routes using `AddRoute`.

**Current State Analysis:**
- ✅ `ReplOptions` moved to `TimeWarp.Nuru` core
- ✅ `NuruAppBuilder.ReplOptions` property exists  
- ✅ `AddReplSupport()` method exists but only stores options
- ✅ `ReplContext` provides static access to `ReplMode`
- ✅ REPL functionality works via `app.RunReplAsync()`
- ❌ REPL commands are NOT registered via `AddRoute`
- ❌ REPL commands handled directly in `ReplMode.cs:283-370`

**Simple Solution Plan:**

**The Solution:** Update `AddReplSupport()` to register REPL commands as routes:

```csharp
public NuruAppBuilder AddReplSupport(Action<ReplOptions>? configureOptions = null)
{
    var replOptions = new ReplOptions();
    configureOptions?.Invoke(replOptions);

    ReplOptions = replOptions;

    // Register REPL commands as routes
    builder.AddRoute("exit", () => ReplContext.ReplMode?.Exit(), "Exit the REPL");
    builder.AddRoute("quit", () => ReplContext.ReplMode?.Exit(), "Exit the REPL");
    builder.AddRoute("q", () => ReplContext.ReplMode?.Exit(), "Exit the REPL (shortcut)");
    builder.AddRoute("help", () => ReplContext.ReplMode?.ShowReplHelp(), "Show REPL help");
    builder.AddRoute("?", () => ReplContext.ReplMode?.ShowReplHelp(), "Show REPL help (shortcut)");
    builder.AddRoute("history", () => ReplContext.ReplMode?.ShowHistory(), "Show command history");
    builder.AddRoute("clear", () => Console.Clear(), "Clear the screen");
    builder.AddRoute("cls", () => Console.Clear(), "Clear the screen (shortcut)");
    builder.AddRoute("clear-history", () => ReplContext.ReplMode?.ClearHistory(), "Clear command history");

    return builder;
}
```

**Benefits:**
1. ✅ Uses `AddRoute` as requested
2. ✅ No static state issues (uses existing `ReplContext`)
3. ✅ Simple implementation (~10 lines)
4. ✅ REPL commands appear in help system
5. ✅ Consistent with other commands

**Implementation Steps:**
1. Move `AddReplSupport()` from core `TimeWarp.Nuru` to REPL `TimeWarp.Nuru.Repl` project
2. Remove `AddReplSupport()` method from `Source/TimeWarp.Nuru/NuruAppBuilder.cs:322-330`
3. Add `AddReplSupport()` as extension method in `Source/TimeWarp.Nuru.Repl/NuruAppExtensions.cs`
4. Register REPL commands as routes using `builder.AddRoute()` calls
5. Build and verify compilation

**Why This Simple Approach Works:**
- `ReplContext.ReplMode` is already set during REPL execution
- Route handlers can access REPL functionality via static context
- No architectural complexity needed
- Follows existing patterns in the codebase

**Next Steps:**
- Implement route registration in REPL project
- Update tests to verify REPL commands work as routes
- Update documentation if needed
