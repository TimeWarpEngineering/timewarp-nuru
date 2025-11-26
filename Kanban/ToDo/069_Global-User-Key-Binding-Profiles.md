# Global User Key Binding Profiles via JSON Config

## Description

Enable users to define key binding profiles in JSON config files that apply across all Nuru-based CLI applications. Users can save their preferred key bindings once and have them automatically loaded by any Nuru app.

**Goal**: Allow users to set "I always want Emacs bindings" or "use my custom profile" globally, without requiring each app to configure it programmatically.

## Parent

Follows Task 057 (Custom Key Bindings via Builder API) which implemented the programmatic `CustomKeyBindingProfile` API.

## Requirements

- Define JSON config file format for key bindings
- Create Action Registry mapping string names to handler methods
- Implement config file loader with search path precedence
- Auto-discover and load config files in `AddReplSupport()`
- Provide clear error messages for invalid configurations
- Document config format and available actions

## Checklist

### Design
- [ ] Finalize JSON config schema
- [ ] Define action name conventions (e.g., "BackwardChar", "ForwardWord")
- [ ] Define key combination string format (e.g., "Ctrl+K", "Alt+F")
- [ ] Plan config file precedence rules

### Implementation - Action Registry
- [ ] Create `Source/TimeWarp.Nuru.Repl/KeyBindings/KeyBindingActionRegistry.cs`
  - [ ] Map action names to handler method factories
  - [ ] Support all existing handlers:
    - [ ] BackwardChar, ForwardChar
    - [ ] BackwardWord, ForwardWord
    - [ ] BeginningOfLine, EndOfLine
    - [ ] BackwardDeleteChar, DeleteChar
    - [ ] PreviousHistory, NextHistory
    - [ ] BeginningOfHistory, EndOfHistory
    - [ ] HistorySearchBackward, HistorySearchForward
    - [ ] TabComplete, TabComplete:reverse
    - [ ] PossibleCompletions
    - [ ] Enter, Escape
  - [ ] `GetAction(string name, ReplConsoleReader reader)` method
  - [ ] `GetAvailableActions()` for discoverability
  - [ ] XML documentation listing all actions

### Implementation - Config Model
- [ ] Create `Source/TimeWarp.Nuru.Repl/KeyBindings/KeyBindingConfig.cs`
  - [ ] `Name` property
  - [ ] `BaseProfile` property (optional: "Default", "Emacs", "Vi", "VSCode")
  - [ ] `Overrides` dictionary (key combo → action name)
  - [ ] `Additions` dictionary (key combo → action name)
  - [ ] `Removals` list (key combos to remove)
  - [ ] `ExitKeys` list (optional)

### Implementation - Config Loader
- [ ] Create `Source/TimeWarp.Nuru.Repl/KeyBindings/KeyBindingConfigLoader.cs`
  - [ ] `LoadFromFile(string path)` method
  - [ ] `LoadFromJson(string json)` method
  - [ ] `LoadDefault()` method (searches standard locations)
  - [ ] `TryLoadDefault(out IKeyBindingProfile?)` method
  - [ ] Parse key combination strings (e.g., "Ctrl+K" → ConsoleKey.K + Control)
  - [ ] Validate action names against registry
  - [ ] Return `CustomKeyBindingProfile` instance
  - [ ] Meaningful error messages with line numbers

### Implementation - Config Search Paths
- [ ] Implement search path precedence:
  1. Environment variable: `NURU_KEYBINDINGS` (explicit path)
  2. Project-specific: `./.nuru/keybindings.json`
  3. User-specific: `~/.nuru/keybindings.json`
- [ ] Skip missing files silently
- [ ] Log which config file was loaded (at Debug level)

### Implementation - Auto-Discovery
- [ ] Update `AddReplSupport()` to auto-load config if no profile specified
- [ ] Respect explicit `KeyBindingProfile` or `KeyBindingProfileName` settings
- [ ] Only auto-discover if neither is set

### Testing
- [ ] Create `Tests/TimeWarp.Nuru.Repl.Tests/KeyBindings/config-loader-tests.cs`
  - [ ] Test JSON parsing
  - [ ] Test key combination string parsing
  - [ ] Test action name resolution
  - [ ] Test base profile inheritance
  - [ ] Test override, add, remove operations
  - [ ] Test invalid config error messages
  - [ ] Test search path precedence

### Documentation
- [ ] Create `documentation/user/features/global-key-binding-profiles.md`
  - [ ] Config file format specification
  - [ ] Available action names reference
  - [ ] Search path explanation
  - [ ] Example configurations
- [ ] Add sample config files to `Samples/Configuration/`
  - [ ] `emacs-enhanced.json`
  - [ ] `vi-enhanced.json`
  - [ ] `minimal.json`

## Notes

### JSON Config Format

```json
{
  "name": "MyGlobalProfile",
  "baseProfile": "Emacs",
  "overrides": {
    "Ctrl+U": "Escape",
    "Ctrl+K": "DeleteToEnd"
  },
  "additions": {
    "Ctrl+G": "Bell"
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
| `"Enter"` | ConsoleKey.Enter, None |
| `"Escape"` | ConsoleKey.Escape, None |
| `"Tab"` | ConsoleKey.Tab, None |
| `"Shift+Tab"` | ConsoleKey.Tab, Shift |

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
