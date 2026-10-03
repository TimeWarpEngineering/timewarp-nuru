# Global Key Binding Profiles

Save a key binding profile once and have every Nuru REPL load it. The file is JSON. Apps do not need to set a profile in code.

Built-in profile names (`Default`, `Emacs`, `Vi`, `VSCode`) and the programmatic `CustomKeyBindingProfile` API are described in [REPL Key Bindings](repl-key-bindings.md). The `key-bindings` command lists those built-in catalogs. It does not print the effective bindings from a JSON file loaded here.

## Search order

The REPL auto-loads a file only when `ReplOptions.KeyBindingProfile` is unset and `ReplOptions.KeyBindingProfileName` is still `"Default"`. An explicit profile instance or any other profile name wins, and the files below are not read.

When auto-load runs, the first existing file is used:

1. `NURU_KEYBINDINGS` — absolute or relative path to a JSON file
2. `./.nuru/keybindings.json` — project file, relative to the process current directory
3. `~/.nuru/keybindings.json` — user file

Missing files are skipped. A file that exists but is invalid stops startup with `KeyBindingConfigException`. The loader does not fall through to a later file after a parse error. The path that loaded is written at Debug (`KeyBindingConfigLoaded`).

`NURU_KEYBINDINGS` must be a file path. It is not a profile name. If it points at a directory, startup fails.

## Config file

```json
{
  "name": "MyGlobalProfile",
  "baseProfile": "Emacs",
  "overrides": {
    "Ctrl+K": "KillLineToRing"
  },
  "additions": {
    "Left": "BackwardChar",
    "Ctrl+Shift+Z": "Redo"
  },
  "removals": [
    "Ctrl+D"
  ],
  "exitKeys": [
    "Enter"
  ]
}
```

| Property | Required | Meaning |
|----------|----------|---------|
| `name` | no | Profile name. When omitted, the base profile name is used, or `Custom` when there is no base. |
| `baseProfile` | no | `Default`, `Emacs`, `Vi`, or `VSCode`. Names are case-sensitive. Omit it to start from an empty binding set. |
| `overrides` | no | Map of key combination to action. Replaces the same key from the base profile. |
| `additions` | no | Map applied after overrides. The same key in both maps keeps the addition. |
| `removals` | no | Key combinations to drop, including exit-key status. |
| `exitKeys` | no | Keys that end the read loop after their action runs. The key still needs a binding. |

Unknown properties are rejected. `//` comments and trailing commas are allowed.

Application order is removals, then overrides, then additions, then exit keys. A key listed in both `removals` and `additions` is added back.

## Key combination strings

Modifiers are `Ctrl` (or `Control`), `Alt`, and `Shift`, in any order, separated by `+`. The last segment is the key. Matching is case-insensitive.

| String | Parsed as |
|--------|-----------|
| `A` | `ConsoleKey.A` |
| `Ctrl+A` | `ConsoleKey.A` + Control |
| `Alt+F` | `ConsoleKey.F` + Alt |
| `Ctrl+Shift+K` | `ConsoleKey.K` + Control, Shift |
| `Ctrl+Shift+Left` | `ConsoleKey.LeftArrow` + Control, Shift |
| `Enter` | `ConsoleKey.Enter` |
| `Escape` | `ConsoleKey.Escape` |
| `Tab` | `ConsoleKey.Tab` |
| `Shift+Tab` | `ConsoleKey.Tab` + Shift |
| `Alt+.` | `ConsoleKey.OemPeriod` + Alt |
| `Alt+<` | `ConsoleKey.OemComma` + Alt, Shift |

Arrow aliases: `Left`, `Right`, `Up`, `Down` (or the `LeftArrow` enum names). Other `ConsoleKey` names work too (`F8`, `Home`, `Oem7`).

Punctuation that needs Shift on a US keyboard includes Shift in the result: `+` is `OemPlus` + Shift, `<` is `OemComma` + Shift, `_` is `OemMinus` + Shift. `=` is `OemPlus` without Shift.

The built-in Alt+= binding (possible completions) uses `ConsoleKey.Oem7`, which is the quote key on a US keyboard. To remove that binding, use `Alt+Oem7`.

## Action names

Names are case-insensitive. `TabComplete:reverse` is an alias of `TabCompleteReverse`.

### Cursor movement

`BackwardChar`, `ForwardChar`, `BackwardWord`, `ForwardWord`, `BeginningOfLine`, `EndOfLine`

### History

`PreviousHistory`, `NextHistory`, `BeginningOfHistory`, `EndOfHistory`, `HistorySearchBackward`, `HistorySearchForward`, `ReverseSearchHistory`, `ForwardSearchHistory`

### Editing

`BackwardDeleteChar`, `DeleteChar`, `DeleteCharOrExit`, `Escape`, `KillLine`, `DeleteWordBackward`, `DeleteToLineStart`, `ClearScreen`, `ToggleInsertMode`

`ToggleInsertMode` switches insert and overwrite. Typed characters then replace the character at the cursor. There is no separate overwrite action.

### Words

`UpcaseWord`, `DowncaseWord`, `CapitalizeWord`, `SwapCharacters`, `DeleteWord`, `BackwardDeleteWord`

### Selection

`SelectBackwardChar`, `SelectForwardChar`, `SelectBackwardWord`, `SelectNextWord`, `SelectBackwardsLine`, `SelectLine`, `SelectAll`, `CopyOrCancelLine`, `Cut`, `Paste`, `DeleteSelection`

### Kill ring

`KillLineToRing`, `BackwardKillInput`, `UnixWordRubout`, `KillWord`, `BackwardKillWord`, `Yank`, `YankPop`

### Yank arguments

`YankLastArg`, `YankNthArg`, `DigitArgument:0` through `DigitArgument:9`

### Undo

`Undo`, `Redo`, `RevertLine`

### Completion

`TabComplete`, `TabCompleteReverse`, `PossibleCompletions`

### Multiline and submit

`AddLine`, `Enter`

`KeyBindingActionRegistry.GetAvailableActions()` returns this list at runtime.

## Examples

Personal Emacs bindings for every app:

```bash
mkdir -p ~/.nuru
echo '{"baseProfile": "Emacs"}' > ~/.nuru/keybindings.json
```

One project, one profile:

```bash
mkdir -p .nuru
cat > .nuru/keybindings.json << 'EOF'
{
  "name": "TeamProfile",
  "baseProfile": "Default",
  "removals": ["Ctrl+D"]
}
EOF
```

Force a file for a single command:

```bash
NURU_KEYBINDINGS=./samples/configuration/emacs-enhanced.json my-nuru-app
```

Keep the built-in Default profile even when a user file exists by setting the profile in code:

```csharp
.AddRepl(options =>
{
  options.KeyBindingProfile = new DefaultKeyBindingProfile();
})
```

Or pick a built-in name, which also skips the JSON search:

```csharp
.AddRepl(options =>
{
  options.KeyBindingProfileName = "Vi";
})
```

Sample files:

- [samples/configuration/emacs-enhanced.json](../../../samples/configuration/emacs-enhanced.json) — Emacs plus arrow keys
- [samples/configuration/vi-enhanced.json](../../../samples/configuration/vi-enhanced.json) — Vi plus word movement and reverse search
- [samples/configuration/minimal.json](../../../samples/configuration/minimal.json) — a profile with no base

## Errors

Invalid documents throw `KeyBindingConfigException` before the prompt is shown. Syntax errors include a 1-based line and column. Unknown actions, unknown keys, and unknown `baseProfile` values include the line of the bad token.

```text
Key binding config '/home/me/.nuru/keybindings.json': Line 4: Unknown key binding action 'Bell'.
```
