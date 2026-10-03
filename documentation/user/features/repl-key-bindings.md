# REPL Key Bindings

Customizable key binding profiles for the interactive REPL, allowing users to choose their preferred editing style.

## Overview

TimeWarp.Nuru REPL supports multiple key binding profiles to match your muscle memory from other editors and shells:

- **Default**: Standard readline-style bindings (arrows, Home/End, Ctrl combinations)
- **Emacs**: GNU Readline/Bash-style bindings (Ctrl+A/E, Ctrl+F/B, Alt+F/B)
- **Vi**: Vi-inspired bindings for insert mode (Ctrl+W, Ctrl+U, Ctrl+K)
- **VSCode**: Visual Studio Code-style bindings (Ctrl+Arrow for word movement)

**Key Benefits:**
- **User Choice**: Pick familiar key bindings from your preferred editor
- **Backward Compatible**: Default profile preserves existing behavior
- **Extensible**: Create custom profiles based on built-in ones

## Quick Start

### Using Default Profile

The default profile is used automatically - no configuration needed:

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .Map("greet {name}")
    .WithHandler((string name) => Console.WriteLine($"Hello, {name}!"))
    .AsCommand()
    .Done()
  .AddRepl(options =>
  {
    options.Prompt = "myapp> ";
  })
  .Build();

await app.RunReplAsync();
```

### Selecting a Profile

Choose a different profile via `KeyBindingProfileName`:

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .Map("greet {name}")
    .WithHandler((string name) => Console.WriteLine($"Hello, {name}!"))
    .AsCommand()
    .Done()
  .AddRepl(options =>
  {
    options.KeyBindingProfileName = "Emacs";  // or "Vi", "VSCode", "Default"
  })
  .Build();

await app.RunReplAsync();
```

## Key Binding Profiles

### Default Profile

Standard readline-compatible bindings that work across all platforms.

| Action | Key Binding |
|--------|-------------|
| Move left | `←` or `Ctrl+B` |
| Move right | `→` or `Ctrl+F` |
| Word left | `Ctrl+←` or `Alt+B` |
| Word right | `Ctrl+→` or `Alt+F` |
| Line start | `Home` or `Ctrl+A` |
| Line end | `End` or `Ctrl+E` |
| Delete char | `Delete` or `Ctrl+D` |
| Backspace | `Backspace` |
| Previous history | `↑` or `Ctrl+P` |
| Next history | `↓` or `Ctrl+N` |
| History start | `Alt+<` |
| History end | `Alt+>` |
| Prefix search back | `F8` |
| Prefix search forward | `Shift+F8` |
| Tab completion | `Tab` |
| Show completions | `Alt+=` |
| Clear line | `Escape` |
| Submit | `Enter` |
| EOF/Exit | `Ctrl+D` (empty line) |

### Emacs Profile

GNU Readline/Bash-style bindings for users familiar with Emacs or bash shell.

| Action | Key Binding |
|--------|-------------|
| Move left | `Ctrl+B` (backward-char) |
| Move right | `Ctrl+F` (forward-char) |
| Word left | `Alt+B` (backward-word) |
| Word right | `Alt+F` (forward-word) |
| Line start | `Ctrl+A` (beginning-of-line) |
| Line end | `Ctrl+E` (end-of-line) |
| Delete to end | `Ctrl+K` (kill-line) |
| Delete to start | `Ctrl+U` (backward-kill-line) |
| Delete char | `Ctrl+D` (delete-char) |
| Backspace | `Backspace` |
| Previous history | `Ctrl+P` (previous-history) |
| Next history | `Ctrl+N` (next-history) |
| Tab completion | `Tab` |
| Clear line | `Escape` |
| Submit | `Enter` |

**Note:** Arrow keys also work for navigation.

### Vi Profile

Vi-inspired bindings for insert mode. This profile provides Vi-style shortcuts while remaining in insert mode (full modal Vi editing is a future enhancement).

| Action | Key Binding |
|--------|-------------|
| Move left | `←` or `Ctrl+B` |
| Move right | `→` or `Ctrl+F` |
| Word left | `Ctrl+←` or `Alt+B` |
| Word right | `Ctrl+→` or `Alt+F` |
| Line start | `Home` or `Ctrl+A` |
| Line end | `End` or `Ctrl+E` |
| Delete word backward | `Ctrl+W` |
| Delete to line start | `Ctrl+U` |
| Delete to line end | `Ctrl+K` |
| Delete char | `Delete` or `Ctrl+D` |
| Backspace | `Backspace` |
| Previous history | `↑` or `Ctrl+P` |
| Next history | `↓` or `Ctrl+N` |
| Tab completion | `Tab` |
| Clear line | `Escape` |
| Submit | `Enter` |

### VSCode Profile

Visual Studio Code-style bindings for users familiar with modern editors.

| Action | Key Binding |
|--------|-------------|
| Move left | `←` |
| Move right | `→` |
| Word left | `Ctrl+←` |
| Word right | `Ctrl+→` |
| Line start | `Home` |
| Line end | `End` |
| Delete to line end | `Ctrl+K` |
| Delete word backward | `Ctrl+Backspace` |
| Delete char | `Delete` |
| Backspace | `Backspace` |
| Previous history | `↑` |
| Next history | `↓` |
| Tab completion | `Tab` |
| Reverse tab completion | `Shift+Tab` |
| Clear line | `Escape` |
| Submit | `Enter` |

## Key Binding Comparison

| Action | Default | Emacs | Vi | VSCode |
|--------|---------|-------|-----|--------|
| Move left | `←`/`Ctrl+B` | `Ctrl+B` | `←`/`Ctrl+B` | `←` |
| Move right | `→`/`Ctrl+F` | `Ctrl+F` | `→`/`Ctrl+F` | `→` |
| Word left | `Ctrl+←`/`Alt+B` | `Alt+B` | `Ctrl+←`/`Alt+B` | `Ctrl+←` |
| Word right | `Ctrl+→`/`Alt+F` | `Alt+F` | `Ctrl+→`/`Alt+F` | `Ctrl+→` |
| Line start | `Home`/`Ctrl+A` | `Ctrl+A` | `Home`/`Ctrl+A` | `Home` |
| Line end | `End`/`Ctrl+E` | `Ctrl+E` | `End`/`Ctrl+E` | `End` |
| Delete to end | - | `Ctrl+K` | `Ctrl+K` | `Ctrl+K` |
| Delete to start | - | `Ctrl+U` | `Ctrl+U` | - |
| Delete word back | - | - | `Ctrl+W` | `Ctrl+Backspace` |
| Previous history | `↑`/`Ctrl+P` | `Ctrl+P` | `↑`/`Ctrl+P` | `↑` |
| Next history | `↓`/`Ctrl+N` | `Ctrl+N` | `↓`/`Ctrl+N` | `↓` |
| Tab completion | `Tab` | `Tab` | `Tab` | `Tab` |

## Custom Key Bindings

Create personalized key bindings using `CustomKeyBindingProfile`:

```csharp
using TimeWarp.Nuru;

// Start from any built-in profile and customize
CustomKeyBindingProfile customProfile = new CustomKeyBindingProfile(new EmacsKeyBindingProfile())
  .WithName("MyCustomProfile")

  // Add a custom binding (Ctrl+G for bell)
  .Add
  (
    ConsoleKey.G,
    ConsoleModifiers.Control,
    reader => () => reader.Write("\a")
  )

  // Remove a binding (disable Ctrl+D EOF)
  .Remove(ConsoleKey.D, ConsoleModifiers.Control);

NuruApp app = NuruApp.CreateBuilder(args)
  .Map("greet {name}")
    .WithHandler((string name) => Console.WriteLine($"Hello, {name}!"))
    .AsCommand()
    .Done()
  .AddRepl(options =>
  {
    options.Prompt = "custom> ";
    options.KeyBindingProfile = customProfile;
  })
  .Build();

await app.RunReplAsync();
```

See [samples/13-repl/02-repl-custom-keys.cs](../../../samples/13-repl/02-repl-custom-keys.cs) for a complete example.

## Usage Examples

### Interactive Application with Emacs Bindings

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .Map("add {a:int} {b:int}")
    .WithHandler((int a, int b) => Console.WriteLine($"{a} + {b} = {a + b}"))
    .AsQuery()
    .Done()
  .Map("sub {a:int} {b:int}")
    .WithHandler((int a, int b) => Console.WriteLine($"{a} - {b} = {a - b}"))
    .AsQuery()
    .Done()
  .AddRepl(options =>
  {
    options.Prompt = "calc> ";
    options.KeyBindingProfileName = "Emacs";
    options.EnableColors = true;
  })
  .Build();

await app.RunReplAsync();
```

### Global JSON profiles

Users can save one profile for every Nuru app. `NURU_KEYBINDINGS` is a path to a JSON file, not a profile name. When `KeyBindingProfile` is unset and `KeyBindingProfileName` is still `"Default"`, the REPL loads the first file that exists:

1. `$NURU_KEYBINDINGS`
2. `./.nuru/keybindings.json`
3. `~/.nuru/keybindings.json`

An explicit profile name such as `"Emacs"` skips those files. See [Global Key Binding Profiles](global-key-binding-profiles.md).

### Available Profile Names

| Name | Description |
|------|-------------|
| `"Default"` | Standard readline-style bindings |
| `"Emacs"` | GNU Readline/Bash-style |
| `"Vi"` | Vi-inspired insert mode bindings |
| `"VSCode"` | Visual Studio Code-style |

Profile names are case-sensitive.

## More Examples

See [samples/13-repl/](../../../samples/13-repl/) for complete working examples:

- `01-repl-cli-dual-mode.cs` - CLI + REPL dual mode application
- `02-repl-custom-keys.cs` - Custom key binding profiles
- `03-repl-options.cs` - Comprehensive ReplOptions configuration
- `04-repl-complete.cs` - Complete REPL feature demonstration

## Listing bindings

In the REPL, `key-bindings` prints the built-in profile that is active. Bindings are grouped by category. Each table has three columns: Key, Function, and Description.

```text
key-bindings
key-bindings Emacs
key-bindings --profile Vi --detailed
key-bindings --function BeginningOfLine
key-bindings --key Ctrl+a
key-bindings --key LeftArrow
```

| Flag | Short | Meaning |
|------|-------|---------|
| `--profile` | `-p` | Built-in profile: `Default`, `Emacs`, `Vi`, or `VSCode`. A positional known name (`key-bindings Emacs`) does the same when `--profile` is absent. |
| `--key` | `-k` | A chord the parser accepts (`Ctrl+a`, `Control+a`, `Left`) matches that combination. `Ctrl+a` does not include `Ctrl+Shift+a`. Text the parser rejects (`Arrow`) is a case-insensitive substring of the displayed chord. |
| `--function` | `-f` | Case-insensitive substring of the function name. |
| `--detailed` | `-d` | One block per binding (`Function`, `Category`, `Description`) instead of the table. |

A missing flag value or an unknown argument prints usage on the error stream and leaves the REPL running. The command does not change the process exit code.

`key-bindings` lists a built-in catalog chosen by name. With no `--profile`, that name is the active profile's `Name`. `Default`, `Emacs`, `Vi`, and `VSCode` print that catalog. Any other name, including a `CustomKeyBindingProfile` or a JSON profile, prints an error and does not expand those chords. Pass `--profile Default` to print the Default catalog.

`Alt+'` is `PossibleCompletions`. That chord is Alt plus the quote key (`ConsoleKey.Oem7`).

The same listing is available outside the REPL. Running the tool with no arguments lists Default. An unknown `--profile` exits with code 1.

```bash
dotnet run tools/nuru-key-bindings/nuru-key-bindings.cs -- key-bindings
dotnet run tools/nuru-key-bindings/nuru-key-bindings.cs -- key-bindings --profile Emacs --function BackwardChar
dotnet run tools/nuru-key-bindings/nuru-key-bindings.cs -- key-bindings --key Ctrl+a --detailed
```

See [key-bindings tool](../tools/key-bindings.md).

## Future Enhancements

- **Full Vi Modal Editing**: Normal, insert, and visual modes
- **Reverse History Search**: `Ctrl+R` incremental search (Emacs profile)
- **Profile Auto-Detection**: Suggest profile based on OS/shell environment
