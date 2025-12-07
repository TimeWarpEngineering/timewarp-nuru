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
- **Extensible**: Easy to add custom profiles (Phase 3)

## Quick Start

### Using Default Profile

The default profile is used automatically - no configuration needed:

```csharp
NuruAppBuilder builder = NuruApp.CreateBuilder(args);
builder.Map("greet {name}", (string name) => $"Hello, {name}!");
builder.AddReplSupport();

await builder.Build().RunReplAsync();
```

### Selecting a Profile

Choose a different profile via `ReplOptions.KeyBindingProfileName`:

```csharp
NuruAppBuilder builder = NuruApp.CreateBuilder(args);
builder.Map("greet {name}", (string name) => $"Hello, {name}!");

builder.AddReplSupport(options =>
{
    options.KeyBindingProfileName = "Emacs";  // or "Vi", "VSCode", "Default"
});

await builder.Build().RunReplAsync();
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

## Usage Examples

### Interactive Application with Emacs Bindings

```csharp
#!/usr/bin/dotnet run
using TimeWarp.Nuru;

NuruAppBuilder builder = NuruApp.CreateBuilder(args);

builder.Map("add {a:int} {b:int}", (int a, int b) => a + b);
builder.Map("sub {a:int} {b:int}", (int a, int b) => a - b);
builder.Map("mul {a:int} {b:int}", (int a, int b) => a * b);

builder.AddReplSupport(options =>
{
    options.Prompt = "calc> ";
    options.KeyBindingProfileName = "Emacs";
    options.EnableColors = true;
});

return await builder.Build().RunReplAsync();
```

### Detecting User Preference

You can let users choose their profile via configuration:

```csharp
NuruAppBuilder builder = NuruApp.CreateBuilder(args);

// Read from environment or config
string profile = Environment.GetEnvironmentVariable("NURU_KEYBINDINGS") ?? "Default";

builder.AddReplSupport(options =>
{
    options.KeyBindingProfileName = profile;
});
```

### Available Profile Names

| Name | Description |
|------|-------------|
| `"Default"` | Standard readline-style bindings |
| `"Emacs"` | GNU Readline/Bash-style |
| `"Vi"` | Vi-inspired insert mode bindings |
| `"VSCode"` | Visual Studio Code-style |

Profile names are case-sensitive.

## Future Enhancements

- **Custom Key Bindings (Task 057)**: Define your own key bindings via builder API
- **Full Vi Modal Editing**: Normal, insert, and visual modes
- **Reverse History Search**: `Ctrl+R` incremental search (Emacs profile)
- **Profile Auto-Detection**: Suggest profile based on OS/shell environment
