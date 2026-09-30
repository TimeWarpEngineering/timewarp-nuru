# Global User Key Binding Profiles via JSON Config

## Description

Enable users to define key binding profiles in JSON config files that apply across all Nuru-based CLI applications. Users can save their preferred key bindings once and have them automatically loaded by any Nuru app.

**Goal**: Allow users to set "I always want Emacs bindings" or "use my custom profile" globally, without requiring each app to configure it programmatically.

## Update 2026-09-28 (triage 477)

Paths corrected: there is no `timewarp-nuru-repl` project. Key bindings live in
`source/timewarp-nuru/repl/key-bindings/` (`custom-key-binding-profile.cs`,
`key-binding-profile-factory.cs`, `key-binding-builder.cs`, default/emacs/vi/vscode profiles); REPL tests are
jaribu runfiles in `tests/timewarp-nuru-tests/repl/` (see `repl-23-key-binding-profiles.cs`,
`repl-24-custom-key-bindings.cs`). Apps enable the REPL with `NuruAppBuilder.AddRepl()` or `AddRepl(Action<ReplOptions>)` (`AddReplOptions` is obsolete);
`ReplOptions.KeyBindingProfileName` defaults to `"Default"` (not null). The handler list below is from 2025;
build the action registry from the handlers that exist today, not from this list.

## Parent

Follows Task 057 (Custom Key Bindings via Builder API) which implemented the programmatic `CustomKeyBindingProfile` API.

## Requirements

- Define JSON config file format for key bindings
- Create Action Registry mapping string names to handler methods
- Implement config file loader with search path precedence
- Auto-discover and load config files when the REPL starts (apps enable it with `AddRepl(...)`)
- Provide clear error messages for invalid configurations
- Document config format and available actions

## Checklist

### Design
- [x] Finalize JSON config schema
- [x] Define action name conventions (e.g., "BackwardChar", "ForwardWord")
- [x] Define key combination string format (e.g., "Ctrl+K", "Alt+F")
- [x] Plan config file precedence rules

### Implementation - Action Registry
- [x] Create `source/timewarp-nuru/repl/key-bindings/key-binding-action-registry.cs`
  - [x] Map action names to handler method factories
  - [x] Support all existing handlers (58 total):
    
    **Cursor Movement (6)**
    - [x] BackwardChar, ForwardChar
    - [x] BackwardWord, ForwardWord
    - [x] BeginningOfLine, EndOfLine
    
    **History (6)**
    - [x] PreviousHistory, NextHistory
    - [x] BeginningOfHistory, EndOfHistory
    - [x] HistorySearchBackward, HistorySearchForward
    
    **History Search (2)**
    - [x] ReverseSearchHistory, ForwardSearchHistory
    
    **Basic Editing (7)**
    - [x] BackwardDeleteChar, DeleteChar
    - [x] DeleteCharOrExit
    - [x] Escape, KillLine
    - [x] DeleteWordBackward, DeleteToLineStart
    
    **Advanced Editing (3)**
    - [x] ClearScreen, ToggleInsertMode
    - [x] CharacterWithOverwrite — no handler exists today (triage 477). Overwrite is ToggleInsertMode plus typed characters, so it is not a registry name.
    
    **Word Operations (6)**
    - [x] UpcaseWord, DowncaseWord, CapitalizeWord
    - [x] SwapCharacters
    - [x] DeleteWord, BackwardDeleteWord
    
    **Selection (11)**
    - [x] SelectBackwardChar, SelectForwardChar
    - [x] SelectBackwardWord, SelectNextWord
    - [x] SelectBackwardsLine, SelectLine
    - [x] SelectAll
    - [x] CopyOrCancelLine, Cut, Paste
    - [x] DeleteSelection
    
    **Kill Ring (7)**
    - [x] KillLineToRing, BackwardKillInput
    - [x] UnixWordRubout
    - [x] KillWord, BackwardKillWord
    - [x] Yank, YankPop
    
    **Yank Arg (3)**
    - [x] YankLastArg, YankNthArg
    - [x] DigitArgument (0-9)
    
    **Undo/Redo (3)**
    - [x] Undo, Redo, RevertLine
    
    **Tab Completion (2)**
    - [x] TabComplete, TabCompleteReverse
    - [x] PossibleCompletions
    
    **Multiline (1)**
    - [x] AddLine
    
    **Control (1)**
    - [x] Enter
    
  - [x] `GetAction(string name, ReplConsoleReader reader)` method
  - [x] `GetAvailableActions()` for discoverability
  - [x] XML documentation listing all actions

### Implementation - Config Model
- [x] Create `source/timewarp-nuru/repl/key-bindings/key-binding-config.cs`
  - [x] `Name` property
  - [x] `BaseProfile` property (optional: "Default", "Emacs", "Vi", "VSCode")
  - [x] `Overrides` dictionary (key combo → action name)
  - [x] `Additions` dictionary (key combo → action name)
  - [x] `Removals` list (key combos to remove)
  - [x] `ExitKeys` list (optional)

### Implementation - Config Loader
- [x] Create `source/timewarp-nuru/repl/key-bindings/key-binding-config-loader.cs`
  - [x] `LoadFromFile(string path)` method
  - [x] `LoadFromJson(string json)` method
  - [x] `LoadDefault()` method (searches standard locations)
  - [x] `TryLoadDefault(out IKeyBindingProfile?)` method
  - [x] Parse key combination strings (e.g., "Ctrl+K" → ConsoleKey.K + Control)
  - [x] Validate action names against registry
  - [x] Return `CustomKeyBindingProfile` instance
  - [x] Meaningful error messages with line numbers

### Implementation - Config Search Paths
- [x] Implement search path precedence:
  1. Environment variable: `NURU_KEYBINDINGS` (explicit path)
  2. Project-specific: `./.nuru/keybindings.json`
  3. User-specific: `~/.nuru/keybindings.json`
- [x] Skip missing files silently
- [x] Log which config file was loaded (at Debug level)

### Implementation - Auto-Discovery
- [x] Auto-load config at REPL startup if no profile was chosen by the app
- [x] Respect explicit `KeyBindingProfile` or `KeyBindingProfileName` settings
- [x] Only auto-discover when `KeyBindingProfile` is null and `KeyBindingProfileName` is still the default (`"Default"`); an explicit choice wins

### Testing
- [x] Create `tests/timewarp-nuru-tests/repl/repl-NN-key-binding-config-loader.cs`
  - [x] Test JSON parsing
  - [x] Test key combination string parsing
  - [x] Test action name resolution
  - [x] Test base profile inheritance
  - [x] Test override, add, remove operations
  - [x] Test invalid config error messages
  - [x] Test search path precedence

### Documentation
- [x] Create `documentation/user/features/global-key-binding-profiles.md`
  - [x] Config file format specification
  - [x] Available action names reference (all 58 actions)
  - [x] Search path explanation
  - [x] Example configurations
- [x] Add sample config files to `samples/configuration/`
  - [x] `emacs-enhanced.json`
  - [x] `vi-enhanced.json`
  - [x] `minimal.json`

## Notes

### JSON Config Format

```json
{
  "name": "MyGlobalProfile",
  "baseProfile": "Emacs",
  "overrides": {
    "Ctrl+U": "Escape",
    "Ctrl+K": "KillLineToRing"
  },
  "additions": {
    "Ctrl+G": "Bell",
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

### Key Combination String Format

| String | Parsed As |
|--------|-----------|
| `"A"` | ConsoleKey.A, None |
| `"Ctrl+A"` | ConsoleKey.A, Control |
| `"Alt+F"` | ConsoleKey.F, Alt |
| `"Ctrl+Shift+K"` | ConsoleKey.K, Control \| Shift |
| `"Ctrl+Shift+Left"` | ConsoleKey.LeftArrow, Control \| Shift |
| `"Enter"` | ConsoleKey.Enter, None |
| `"Escape"` | ConsoleKey.Escape, None |
| `"Tab"` | ConsoleKey.Tab, None |
| `"Shift+Tab"` | ConsoleKey.Tab, Shift |
| `"Alt+."` | ConsoleKey.OemPeriod, Alt |

### Action Name Reference

```
# Cursor Movement
BackwardChar, ForwardChar, BackwardWord, ForwardWord, BeginningOfLine, EndOfLine

# History
PreviousHistory, NextHistory, BeginningOfHistory, EndOfHistory
HistorySearchBackward, HistorySearchForward, ReverseSearchHistory, ForwardSearchHistory

# Basic Editing
BackwardDeleteChar, DeleteChar, DeleteCharOrExit, Escape, KillLine
DeleteWordBackward, DeleteToLineStart, ClearScreen, ToggleInsertMode

# Word Operations
UpcaseWord, DowncaseWord, CapitalizeWord, SwapCharacters, DeleteWord, BackwardDeleteWord

# Selection
SelectBackwardChar, SelectForwardChar, SelectBackwardWord, SelectNextWord
SelectBackwardsLine, SelectLine, SelectAll, CopyOrCancelLine, Cut, Paste, DeleteSelection

# Kill Ring
KillLineToRing, BackwardKillInput, UnixWordRubout, KillWord, BackwardKillWord, Yank, YankPop

# Yank Arg
YankLastArg, YankNthArg, DigitArgument:0-9

# Undo
Undo, Redo, RevertLine

# Tab Completion
TabComplete, TabComplete:reverse, PossibleCompletions

# Multiline
AddLine

# Control
Enter
```

### Config Precedence

```
1. ReplOptions.KeyBindingProfile (explicit instance) - highest
2. ReplOptions.KeyBindingProfileName (explicit name)
3. $NURU_KEYBINDINGS environment variable
4. ./.nuru/keybindings.json (project)
5. ~/.nuru/keybindings.json (user)
6. DefaultKeyBindingProfile - lowest (fallback)
```

### Use Cases

**Personal Global Preference**:
```bash
# Create once
echo '{"baseProfile": "Emacs"}' > ~/.nuru/keybindings.json

# All Nuru apps now use Emacs bindings
any-nuru-app
another-nuru-app
```

**Team Standard**:
```bash
# In project repo
cat > .nuru/keybindings.json << 'EOF'
{
  "name": "TeamProfile",
  "baseProfile": "Default",
  "removals": ["Ctrl+D"]
}
EOF

# All team members get same bindings
git add .nuru/keybindings.json
git commit -m "Add team key binding profile"
```

### Risk Considerations

- JSON parsing is well-understood
- Action registry is straightforward mapping
- Main risk: edge cases in key combination parsing
- Mitigation: comprehensive test coverage for parser

## Results

Nuru REPLs now load a global key binding profile from JSON when the app leaves `KeyBindingProfile` unset and `KeyBindingProfileName` at `"Default"`. An explicit profile instance or any other profile name still wins and skips the files.

Search order is `NURU_KEYBINDINGS`, then `./.nuru/keybindings.json`, then `~/.nuru/keybindings.json`. Missing files are skipped. The first file that exists is loaded, and an invalid file throws `KeyBindingConfigException` with a line number instead of falling through. The loaded path is logged at Debug.

`KeyBindingActionRegistry` maps the handlers that exist today (67 canonical names, including `DigitArgument:0` through `DigitArgument:9`). `TabComplete:reverse` is an alias of `TabCompleteReverse`. `CharacterWithOverwrite` is not a handler; overwrite remains `ToggleInsertMode` plus typed characters. The format, action list, and samples are in `documentation/user/features/global-key-binding-profiles.md` and `samples/configuration/`.

Verification:

- `dotnet tests/timewarp-nuru-tests/repl/repl-46-key-binding-config-loader.cs`: exit 0, 13 passed.
- `ganda runfile cache --clear` then `dotnet tests/ci-tests/run-ci-tests.cs`: exit 0. Multi-mode total 1812, passed 1806, skipped 6, failed 0. Standalone phase passed.
- `dotnet run tools/dev-cli/dev.cs -- verify-samples`: `64/64 samples built successfully`.
- `ganda repo audit`: "Repository passes all audit checks." Passed 29, Failed 0. (`bin/dev` is gitignored; `ganda repo audit --fix` ran `self-install`).

### How to validate

Smoke:

```bash
dotnet tests/timewarp-nuru-tests/repl/repl-46-key-binding-config-loader.cs
dotnet tests/ci-tests/run-ci-tests.cs
dotnet run tools/dev-cli/dev.cs -- verify-samples
ganda repo audit
```

Expect:

- `repl-46-key-binding-config-loader.cs` exits 0 with 13 passed. JSON parse, key strings (`Ctrl+Shift+Left`, `Alt+.`), action resolution, base/override/add/remove, line-numbered errors, and env-then-project-then-user search all pass. An explicit profile name does not read a config file. The three files in `samples/configuration/` load.
- The CI runner exits 0. Multi-mode total 1812, passed 1806, skipped 6, failed 0. Standalone tests pass.
- `verify-samples` prints `64/64 samples built successfully`.
- Audit prints "Repository passes all audit checks." Passed 29, Failed 0.
