# Using REPL Mode

REPL (Read-Eval-Print Loop) mode provides an interactive command-line experience for TimeWarp.Nuru applications, allowing you to execute commands repeatedly without restarting the application.

## When to Use REPL Mode

REPL mode is ideal for:
- **Exploratory development**: Test routes and commands interactively during development
- **Debugging**: Quickly iterate on command parameters and see immediate results
- **Learning**: Discover available commands through tab completion and help
- **Scripting**: Chain commands in a session with persistent context

## Enabling REPL Mode

### Programmatic Entry

```csharp
using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;

NuruAppBuilder builder = new NuruAppBuilder()
  .Map("greet {name}", name => Console.WriteLine($"Hello, {name}!"))
  .Map("version", () => Console.WriteLine("v1.0.0"));

NuruApp app = builder.Build();

// Start REPL mode
await app.RunReplAsync(new ReplOptions
{
  Prompt = "myapp> ",
  WelcomeMessage = "Welcome to MyApp REPL!"
});
```

### Command-Line Entry

If your application supports it, you can enter REPL mode via command line:

```bash
# If your app exposes --repl flag
myapp --repl
```

## Basic Usage

### Starting REPL

```bash
$ myapp --repl
TimeWarp.Nuru REPL Mode. Type 'help' for commands, 'exit' to quit.

>
```

### Executing Commands

```bash
> greet Alice
Hello, Alice!

> version
v1.0.0

> exit
Goodbye!
$
```

### Special REPL Commands

- `exit`, `quit`, `q` - Exit REPL mode
- `help`, `?` - Show help and available commands
- `history` - Show command history
- `clear`, `cls` - Clear the screen
- `clear-history` - Clear command history

## Features

### Command History

REPL automatically saves your command history to `~/.nuru_history`:

```bash
> greet Bob
Hello, Bob!

> history
Command History:
  1: greet Bob

> # Use up/down arrows to navigate history
```

### Colored Output

When colors are enabled, prompts and errors are color-coded:

```bash
# Green prompt for input
> greet Charlie
Hello, Charlie!

# Red errors for failures
> invalid-command
Error: No matching command found.
```

### Execution Timing

Commands show execution time when enabled:

```bash
> slow-command
Command executed successfully
(150ms)
```

### Enhanced Help

The `help` command shows available application commands with descriptions:

```bash
> help
REPL Commands:
  exit, quit, q     - Exit the REPL
  help, ?           - Show this help
  history           - Show command history
  clear, cls        - Clear the screen
  clear-history     - Clear command history

Any other input is executed as an application command.

Available Application Commands:
  greet {name} - Greet someone by name
  version - Show application version
```

## Configuration Options

### ReplOptions Class

```csharp
public class ReplOptions
{
  // Prompt appearance
  public string Prompt { get; set; } = "> ";
  public bool EnableColors { get; set; } = true;

  // Messages
  public string? WelcomeMessage { get; set; }
  public string? GoodbyeMessage { get; set; }

  // History
  public bool PersistHistory { get; set; } = true;
  public string? HistoryFilePath { get; set; }
  public int MaxHistorySize { get; set; } = 1000;

  // Behavior
  public bool ContinueOnError { get; set; } = true;
  public bool ShowExitCode { get; set; }
  public bool ShowTiming { get; set; } = true;
  public bool EnableArrowHistory { get; set; } = true;
}
```

### Example Configuration

```csharp
ReplOptions options = new()
{
  Prompt = "myapp> ",
  EnableColors = true,
  ShowTiming = true,
  PersistHistory = true,
  HistoryFilePath = "/custom/path/.myapp_history",
  WelcomeMessage = "Welcome to MyApp interactive mode!",
  GoodbyeMessage = "Thanks for using MyApp!"
};
```

## Arrow Key History Navigation

When enabled, use arrow keys to navigate command history:

- **Up Arrow**: Previous command
- **Down Arrow**: Next command
- **Enter**: Execute current command
- **Backspace**: Edit current line
- **Left/Right Arrows**: Move cursor within line

```bash
> greet Alice
Hello, Alice!

> # Press up arrow to recall "greet Alice"
> greet Alice  # Cursor at end, can edit

> greet Bob   # Modified and executed
Hello, Bob!
```

## Error Handling

REPL continues running after command errors by default:

```bash
> invalid-command
Error: No matching command found.

> # REPL continues, ready for next command
>
```

Configure to exit on errors:

```csharp
ReplOptions options = new()
{
  ContinueOnError = false  // Exit on first error
};
```

## Integration with Completion

REPL integrates with TimeWarp.Nuru.Completion for enhanced help:

```csharp
// If CompletionProvider is available, help shows command descriptions
> help
Available Application Commands:
  greet {name} - Greet someone by name
  deploy {env} {version} - Deploy to environment
  config set {key} {value} - Set configuration value
```

## Cross-Platform Considerations

REPL works on Windows, Linux, and macOS:

- **History file**: `~/.nuru_history` (respects platform conventions)
- **Colors**: ANSI escape codes (fallback to plain text if unsupported)
- **Key handling**: Console.ReadKey() (basic implementation, no advanced line editing)
- **EOF**: Ctrl+D (Unix) or Ctrl+Z (Windows)

## Troubleshooting

### Arrow Keys Not Working

- Ensure terminal supports ANSI escape sequences
- Try running in a different terminal (Windows Terminal, iTerm2, etc.)
- Arrow history can be disabled: `EnableArrowHistory = false`

### History Not Persisting

- Check write permissions to `~/.nuru_history`
- Custom path: `HistoryFilePath = "/custom/path"`
- Disable persistence: `PersistHistory = false`

### Colors Not Showing

- Terminal may not support ANSI colors
- Disable colors: `EnableColors = false`
- Some Windows consoles need `Console.OutputEncoding = Encoding.UTF8`

### Performance Issues

- Large history files slow startup: Reduce `MaxHistorySize`
- Disable timing for faster output: `ShowTiming = false`

## Examples

### Simple Calculator REPL

```csharp
NuruAppBuilder builder = new NuruAppBuilder()
  .Map("add {a:int} {b:int}", (a, b) => Console.WriteLine($"{a} + {b} = {a + b}"))
  .Map("multiply {a:int} {b:int}", (a, b) => Console.WriteLine($"{a} × {b} = {a * b}"));

NuruApp app = builder.Build();
await app.RunReplAsync(new ReplOptions
{
  Prompt = "calc> ",
  WelcomeMessage = "Calculator REPL - Type 'help' for operations"
});
```

### Git Command Wrapper

```csharp
NuruAppBuilder builder = new NuruAppBuilder()
  .Map("status", () => RunGit("status"))
  .Map("commit -m {message}", message => RunGit($"commit -m \"{message}\""))
  .Map("log --oneline", () => RunGit("log --oneline"));

NuruApp app = builder.Build();
await app.RunReplAsync(new ReplOptions
{
  Prompt = "git> ",
  EnableColors = true,
  ShowTiming = true
});
```

## Next Steps

- Explore [Implementing REPL Mode](../developer/guides/implementing-repl.md) for development details
- See [REPL Samples](../../samples/repl-demo/) for working examples
- Consider [Shell Tab Completion](../using-shell-completion.md) for external completion