# Custom Key Bindings Demo

Demonstrates how to create personalized REPL key bindings using `CustomKeyBindingProfile`. Start from any built-in profile and customize it to your preferences.

## Running the Demo

```bash
cd samples/repl-demo
./repl-custom-keybindings.cs
```

Or:

```bash
dotnet run samples/repl-demo/repl-custom-keybindings.cs
```

## Key Concepts

### Built-in Profiles

TimeWarp.Nuru includes four key binding profiles:

| Profile | Style | Description |
|---------|-------|-------------|
| `Default` | PSReadLine | Windows PowerShell-compatible bindings |
| `Emacs` | Emacs/Bash | Traditional readline/bash key bindings |
| `Vi` | Vi/Vim | Modal editing (insert/command modes) |
| `VSCode` | Modern IDE | Contemporary editor key bindings |

### Using a Built-in Profile

```csharp
// Simply set the profile name
ReplOptions options = new ReplOptions
{
  KeyBindingProfileName = "Emacs"
};
```

### Creating a Custom Profile

Use `CustomKeyBindingProfile` to modify an existing profile:

```csharp
// Start from Emacs and customize
CustomKeyBindingProfile customProfile = new CustomKeyBindingProfile(new EmacsKeyBindingProfile())
  .WithName("MyProfile")
  .Override(ConsoleKey.U, ConsoleModifiers.Control, reader => reader.HandleEscape)
  .Add(ConsoleKey.G, ConsoleModifiers.Control, _ => () => Console.Write("\a"))
  .Remove(ConsoleKey.D, ConsoleModifiers.Control);

ReplOptions options = new ReplOptions
{
  KeyBindingProfile = customProfile
};
```

## CustomKeyBindingProfile API

### Constructor

```csharp
// Start with no bindings
new CustomKeyBindingProfile()

// Start from an existing profile
new CustomKeyBindingProfile(new EmacsKeyBindingProfile())
```

### Methods

| Method | Description |
|--------|-------------|
| `.WithName(string)` | Set a custom name for the profile |
| `.Override(key, action)` | Replace or add a binding |
| `.Override(key, modifiers, action)` | Replace or add a binding with modifiers |
| `.Add(key, action)` | Alias for Override (adds new binding) |
| `.Add(key, modifiers, action)` | Alias for Override with modifiers |
| `.Remove(key)` | Remove a binding |
| `.Remove(key, modifiers)` | Remove a binding with modifiers |
| `.AddExitKey(key, action)` | Add an exit key (like Enter) |
| `.RemoveExitKey(key)` | Remove an exit key status |

### Action Factory

Actions are provided as factories that receive the `ReplConsoleReader`:

```csharp
// Use a reader method
.Override(ConsoleKey.A, ConsoleModifiers.Control, reader => reader.HandleBeginningOfLine)

// Use a custom action
.Add(ConsoleKey.G, ConsoleModifiers.Control, _ => () => Console.Write("\a"))

// Combine reader method with custom logic
.Override(ConsoleKey.U, ConsoleModifiers.Control, reader => () =>
{
  Console.Write("[Custom!] ");
  reader.HandleEscape();
})
```

### Available Reader Methods

The `ReplConsoleReader` provides these handler methods for use in custom bindings:

| Method | Description |
|--------|-------------|
| `HandleBackwardChar` | Move cursor left one character |
| `HandleForwardChar` | Move cursor right one character |
| `HandleBackwardWord` | Move cursor left one word |
| `HandleForwardWord` | Move cursor right one word |
| `HandleBeginningOfLine` | Move cursor to start of line |
| `HandleEndOfLine` | Move cursor to end of line |
| `HandleBackwardDeleteChar` | Delete character before cursor |
| `HandleDeleteChar` | Delete character at cursor |
| `HandlePreviousHistory` | Navigate to previous history entry |
| `HandleNextHistory` | Navigate to next history entry |
| `HandleBeginningOfHistory` | Navigate to oldest history entry |
| `HandleEndOfHistory` | Navigate to newest history entry |
| `HandleHistorySearchBackward` | Search history backwards |
| `HandleHistorySearchForward` | Search history forwards |
| `HandleEscape` | Clear current input |
| `HandleEnter` | Submit current input |
| `HandleTabCompletion(reverse)` | Trigger tab completion |
| `HandlePossibleCompletions` | Show all possible completions |

## Demo Modifications

This demo creates an "EmacsCustomized" profile with these changes:

| Change | Key | Description |
|--------|-----|-------------|
| Added | Ctrl+G | Plays bell/ding sound |
| Overridden | Ctrl+U | Shows message then clears line |
| Removed | Ctrl+D | No longer exits (EOF disabled) |

## Use Cases

### Power User Customization

```csharp
CustomKeyBindingProfile profile = new CustomKeyBindingProfile(new EmacsKeyBindingProfile())
  .WithName("PowerUser")
  .Override(ConsoleKey.K, ConsoleModifiers.Control, reader => reader.HandleDeleteToEnd)
  .Add(ConsoleKey.Y, ConsoleModifiers.Control, reader => reader.HandleYank);
```

### Disable Accidental Exits

```csharp
CustomKeyBindingProfile profile = new CustomKeyBindingProfile(new DefaultKeyBindingProfile())
  .Remove(ConsoleKey.D, ConsoleModifiers.Control)  // No Ctrl+D exit
  .RemoveExitKey(ConsoleKey.D, ConsoleModifiers.Control);
```

### Start from Scratch

```csharp
CustomKeyBindingProfile profile = new CustomKeyBindingProfile()
  .WithName("Minimal")
  .Add(ConsoleKey.LeftArrow, reader => reader.HandleBackwardChar)
  .Add(ConsoleKey.RightArrow, reader => reader.HandleForwardChar)
  .AddExitKey(ConsoleKey.Enter, reader => reader.HandleEnter);
```

## Related

- [repl-basic-demo.cs](repl-basic-demo.cs) - Basic REPL demonstration
- [repl-options-showcase.cs](repl-options-showcase.cs) - All ReplOptions configuration
- [Key Binding Profiles](/documentation/user/features/key-binding-profiles.md) - Full reference
