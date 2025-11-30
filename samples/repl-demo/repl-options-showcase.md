# ReplOptions Comprehensive Showcase

This demo demonstrates **all** `ReplOptions` configuration properties available in TimeWarp.Nuru's REPL mode.

## Running the Demo

```bash
cd samples/repl-demo
./repl-options-showcase.cs
```

Or:

```bash
dotnet run samples/repl-demo/repl-options-showcase.cs
```

## ReplOptions Properties Demonstrated

### Prompt Customization

| Property | Demo Value | Default | Description |
|----------|------------|---------|-------------|
| `Prompt` | `"showcase> "` | `"> "` | Text displayed before user input |
| `PromptColor` | `"\x1b[36m"` (Cyan) | `"\x1b[32m"` (Green) | ANSI color code for prompt |
| `EnableColors` | `true` | `true` | Enable/disable colored output |

### Messages

| Property | Demo Value | Default | Description |
|----------|------------|---------|-------------|
| `WelcomeMessage` | Custom | `"TimeWarp.Nuru REPL Mode..."` | Shown when REPL starts |
| `GoodbyeMessage` | Custom | `"Goodbye!"` | Shown when REPL exits |

### History Configuration

| Property | Demo Value | Default | Description |
|----------|------------|---------|-------------|
| `PersistHistory` | `true` | `true` | Save history between sessions |
| `HistoryFilePath` | `"./repl-showcase-history.txt"` | `~/.nuru_history` | History file location |
| `MaxHistorySize` | `50` | `1000` | Maximum entries to keep |
| `EnableArrowHistory` | `true` | `true` | Arrow key navigation |
| `HistoryIgnorePatterns` | See below | Common sensitive patterns | Commands to exclude from history |

### Error Handling

| Property | Demo Value | Default | Description |
|----------|------------|---------|-------------|
| `ContinueOnError` | `false` | `true` | Whether REPL continues after command failure |

### Display Options

| Property | Demo Value | Default | Description |
|----------|------------|---------|-------------|
| `ShowExitCode` | `true` | `false` | Display exit code after each command |
| `ShowTiming` | `true` | `true` | Display execution time |

## Demo Commands

| Command | Purpose |
|---------|---------|
| `config` | Display all current ReplOptions settings |
| `success` | Returns exit code 0 (demonstrates ShowExitCode) |
| `fail` | Returns exit code 1 (demonstrates ContinueOnError=false) |
| `exitcode {n}` | Returns custom exit code |
| `slow {ms}` | Delays execution (demonstrates ShowTiming) |
| `set-password {value}` | Excluded from history (*password* pattern) |
| `set-token {value}` | Excluded from history (*token* pattern) |
| `my-secret-command` | Excluded from history (*secret* pattern) |
| `echo {message}` | Simple echo command |
| `history` | View command history (built-in) |
| `help` | List available commands (built-in) |
| `exit` | Exit REPL (built-in) |

## HistoryIgnorePatterns

The demo configures these patterns to exclude sensitive commands from history:

```csharp
options.HistoryIgnorePatterns =
[
  "*password*",   // Excludes: set-password, change-password, etc.
  "*secret*",     // Excludes: my-secret-command, show-secret, etc.
  "*token*",      // Excludes: set-token, refresh-token, etc.
  "*apikey*",     // Excludes: set-apikey, show-apikey, etc.
  "*credential*", // Excludes: store-credential, etc.
  "clear-history" // Don't record history management commands
];
```

Wildcards supported:
- `*` matches any characters
- `?` matches a single character

## ANSI Color Codes Reference

```csharp
"\x1b[31m" // Red
"\x1b[32m" // Green (default)
"\x1b[33m" // Yellow
"\x1b[34m" // Blue
"\x1b[35m" // Magenta
"\x1b[36m" // Cyan (used in demo)
"\x1b[37m" // White
```

## Key Behaviors to Observe

1. **Cyan Prompt**: Notice the prompt is cyan, not the default green
2. **Exit Codes**: After each command, see `[Exit code: X]` displayed
3. **Timing**: Execution time shown after commands (try `slow 500`)
4. **History Filtering**: Run `set-password test`, then `history` - password command not recorded
5. **ContinueOnError**: Run `fail` command - REPL will exit immediately

## Related

- [repl-basic-demo.cs](repl-basic-demo.cs) - Simple REPL demo with logging
- [ReplOptions.cs](/Source/TimeWarp.Nuru/ReplOptions.cs) - Full property documentation
