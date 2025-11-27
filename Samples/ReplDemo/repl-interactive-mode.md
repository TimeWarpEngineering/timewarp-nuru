# Interactive Mode Demo

Demonstrates an application that supports both CLI and REPL modes using `AddInteractiveRoute()`.

## Usage

### CLI Mode (Single Command)

Run individual commands directly:

```bash
./repl-interactive-mode.cs greet Alice
# Output: Hello, Alice!

./repl-interactive-mode.cs status
# Output: System status: OK

./repl-interactive-mode.cs add 5 3
# Output: 5 + 3 = 8
```

### Interactive Mode (REPL)

Enter interactive mode for extended use:

```bash
./repl-interactive-mode.cs --interactive
# or
./repl-interactive-mode.cs -i
```

Once in interactive mode:
```
Welcome to Interactive Mode!
Type 'help' for available commands, 'exit' to quit.
demo> greet World
Hello, World!
demo> add 10 20
10 + 20 = 30
demo> exit
Goodbye!
```

## Key Code

```csharp
var app = new NuruAppBuilder()
  // Define your commands
  .Map("greet {name}", (string name) => WriteLine($"Hello, {name}!"))
  .Map("status", () => WriteLine("OK"))

  // Add REPL support with configuration
  .AddReplSupport(options =>
  {
    options.Prompt = "demo> ";
    options.WelcomeMessage = "Welcome!";
  })

  // Add --interactive,-i route
  .AddInteractiveRoute()

  .Build();

// Single entry point handles both modes
return await app.RunAsync(args);
```

## Custom Interactive Route Patterns

You can customize the route patterns:

```csharp
// Use different patterns
.AddInteractiveRoute("--repl,-r")

// Or a single pattern
.AddInteractiveRoute("shell")
```

## How It Works

1. `AddReplSupport()` configures REPL options and registers built-in REPL commands (exit, help, history, etc.)
2. `AddInteractiveRoute()` registers routes (`--interactive`, `-i`) that start the REPL when matched
3. `app.RunAsync(args)` either:
   - Executes the matched command and exits (CLI mode)
   - Starts the REPL session if `--interactive` or `-i` is passed
