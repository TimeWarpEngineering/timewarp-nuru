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

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("greet {name}")
    .WithHandler((string name) => Console.WriteLine($"Hello, {name}!"))
    .AsCommand()
    .Done()
  .Map("version")
    .WithHandler(() => Console.WriteLine("v1.0.0"))
    .AsQuery()
    .Done()
  .AddRepl(options =>
  {
    options.Prompt = "myapp> ";
    options.WelcomeMessage = "Welcome to MyApp REPL!";
    options.GoodbyeMessage = "Goodbye!";
  })
  .Build();

// Start REPL mode directly
await app.RunReplAsync();
```

### Command-Line Entry

Applications with REPL enabled support interactive mode flags:

```bash
# Enter REPL mode using flags
myapp --interactive
myapp -i
```

Or run the app which will process commands or enter REPL based on arguments:

```bash
# Run a single command
myapp greet Alice

# Enter interactive mode
myapp -i
```

## Basic Usage

### Starting REPL

```bash
$ myapp -i
Welcome to MyApp REPL!

myapp>
```

### Executing Commands

```bash
myapp> greet Alice
Hello, Alice!

myapp> version
v1.0.0

myapp> exit
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
myapp> greet Bob
Hello, Bob!

myapp> history
Command History:
  1: greet Bob

myapp> # Use up/down arrows to navigate history
```

### Colored Output

When colors are enabled, prompts and errors are color-coded:

```bash
# Green prompt for input
myapp> greet Charlie
Hello, Charlie!

# Red errors for failures
myapp> invalid-command
Error: No matching command found.
```

### Execution Timing

Commands show execution time when enabled:

```bash
myapp> slow-command
Command executed successfully
(150ms)
```

### Enhanced Help

The `help` command shows available application commands with descriptions:

```bash
myapp> help
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

### ReplOptions Properties

Configure REPL behavior through the `.AddRepl(options => ...)` pattern:

```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("greet {name}")
    .WithHandler((string name) => Console.WriteLine($"Hello, {name}!"))
    .AsCommand()
    .Done()
  .AddRepl(options =>
  {
    // Prompt appearance
    options.Prompt = "myapp> ";             // Prompt string (default: "> ")
    options.PromptColor = "\x1b[36m";       // ANSI color code (default: green)
    options.EnableColors = true;            // Enable colored output

    // Messages
    options.WelcomeMessage = "Welcome!";    // Displayed when REPL starts
    options.GoodbyeMessage = "Goodbye!";    // Displayed when exiting

    // History
    options.PersistHistory = true;          // Save history between sessions
    options.HistoryFilePath = "~/.myapp_history";  // Custom history file
    options.MaxHistorySize = 1000;          // Maximum history entries

    // Behavior
    options.ContinueOnError = true;         // Continue after command errors
    options.ShowExitCode = false;           // Show exit code after commands
    options.ShowTiming = true;              // Show execution time
    options.EnableArrowHistory = true;      // Arrow key history navigation

    // Key bindings
    options.KeyBindingProfileName = "Default";  // "Default", "Emacs", "Vi", "VSCode"
  })
  .Build();
```

### ANSI Color Codes

Common prompt color codes:
- `"\x1b[31m"` - Red
- `"\x1b[32m"` - Green (default)
- `"\x1b[33m"` - Yellow
- `"\x1b[34m"` - Blue
- `"\x1b[35m"` - Magenta
- `"\x1b[36m"` - Cyan

## Arrow Key History Navigation

When enabled, use arrow keys to navigate command history:

- **Up Arrow**: Previous command
- **Down Arrow**: Next command
- **Enter**: Execute current command
- **Backspace**: Edit current line
- **Left/Right Arrows**: Move cursor within line

```bash
myapp> greet Alice
Hello, Alice!

myapp> # Press up arrow to recall "greet Alice"
myapp> greet Alice  # Cursor at end, can edit

myapp> greet Bob   # Modified and executed
Hello, Bob!
```

## Error Handling

REPL continues running after command errors by default:

```bash
myapp> invalid-command
Error: No matching command found.

myapp> # REPL continues, ready for next command
myapp>
```

Configure to exit on errors:

```csharp
.AddRepl(options =>
{
  options.ContinueOnError = false;  // Exit on first error
})
```

## History Ignore Patterns

Exclude sensitive commands from history:

```csharp
.AddRepl(options =>
{
  options.HistoryIgnorePatterns = new[]
  {
    "*password*",
    "*secret*",
    "*token*",
    "*apikey*",
    "*credential*"
  };
})
```

## Integration with Completion

REPL integrates with TimeWarp.Nuru.Completion for enhanced help:

```csharp
// If CompletionProvider is available, help shows command descriptions
myapp> help
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
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("add {a:int} {b:int}")
    .WithHandler((int a, int b) => Console.WriteLine($"{a} + {b} = {a + b}"))
    .AsQuery()
    .Done()
  .Map("multiply {a:int} {b:int}")
    .WithHandler((int a, int b) => Console.WriteLine($"{a} × {b} = {a * b}"))
    .AsQuery()
    .Done()
  .AddRepl(options =>
  {
    options.Prompt = "calc> ";
    options.WelcomeMessage = "Calculator REPL - Type 'help' for operations";
  })
  .Build();

await app.RunReplAsync();
```

### CLI + REPL Dual Mode

```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("status")
    .WithHandler(() => Console.WriteLine("System status: OK"))
    .AsQuery()
    .Done()
  .Map("greet {name}")
    .WithHandler((string name) => Console.WriteLine($"Hello, {name}!"))
    .AsCommand()
    .Done()
  .AddRepl(options =>
  {
    options.Prompt = "demo> ";
    options.WelcomeMessage = "Type '--help' for commands, 'exit' to quit.";
    options.GoodbyeMessage = "Goodbye!";
  })
  .Build();

// Runs single command or enters REPL based on args
return await app.RunAsync(args);
```

See [samples/13-repl/](../../../samples/13-repl/) for complete working examples.

## Next Steps

- Explore [REPL Key Bindings](../features/repl-key-bindings.md) for customizable key binding profiles
- See [samples/13-repl/](../../../samples/13-repl/) for working examples
