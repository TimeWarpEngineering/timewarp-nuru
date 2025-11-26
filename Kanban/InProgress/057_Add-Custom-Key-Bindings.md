# Add Custom Key Bindings via Builder API

## Description

Enable power users to create fully customized key binding configurations using a fluent builder API. Users can start from any existing profile (Default, Emacs, Vi, VSCode) and override, add, or remove specific key bindings. Supports both programmatic configuration and loading from JSON config files.

**Goal**: Provide maximum flexibility for users who want complete control over their REPL key bindings.

## Parent

Part of 3-phase key binding evolution:
- **Task 055** (Phase 1): Action + ExitSet pattern (prerequisite)
- **Task 056** (Phase 2): IKeyBindingProfile interface (prerequisite)
- **Task 057** (Phase 3): Custom key bindings (this task)

## Requirements

- Create `KeyBindingBuilder` class with fluent API
- Create `CustomKeyBindingProfile` class implementing `IKeyBindingProfile`
- Support starting from any base profile (Default, Emacs, Vi, VSCode)
- Support override, add, and remove operations
- Support loading from JSON configuration file
- Provide clear error messages for invalid configurations
- All existing tests pass
- Add tests for custom binding scenarios
- Document builder API and config file format

## Checklist

### Design
- [x] Design KeyBindingBuilder fluent API
- [x] Design CustomKeyBindingProfile architecture
- [ ] Design JSON config file format (deferred to future iteration)
- [x] Plan validation and error handling
- [ ] Consider security implications of custom actions (deferred - only affects JSON config)

### Implementation - KeyBindingBuilder
- [x] Create `Source/TimeWarp.Nuru.Repl/KeyBindings/KeyBindingBuilder.cs`
  - [x] Private dictionary for storing bindings
  - [x] Private HashSet for exit keys
  - [x] `Bind(ConsoleKey key, Action action)` method
  - [x] `Bind(ConsoleKey key, ConsoleModifiers modifiers, Action action)` method
  - [x] `BindExit(ConsoleKey key, ConsoleModifiers modifiers, Action action)` method
  - [x] `Remove(ConsoleKey key, ConsoleModifiers modifiers)` method
  - [x] `Clear()` method to reset all bindings
  - [x] `LoadFrom(IKeyBindingProfile profile)` method to start from base
  - [x] `Build()` method returning (bindings, exitKeys) tuple
  - [x] XML documentation for all public methods
  - [x] Fluent interface (return `this` from all methods)

### Implementation - CustomKeyBindingProfile
- [x] Create `Source/TimeWarp.Nuru.Repl/KeyBindings/CustomKeyBindingProfile.cs`
  - [x] Implement `IKeyBindingProfile`
  - [x] Constructor accepting optional base profile
  - [x] `Override(ConsoleKey, Action)` fluent method
  - [x] `Override(ConsoleKey, ConsoleModifiers, Action)` fluent method
  - [x] `Add(ConsoleKey, Action)` fluent method
  - [x] `Add(ConsoleKey, ConsoleModifiers, Action)` fluent method
  - [x] `Remove(ConsoleKey, ConsoleModifiers)` fluent method
  - [x] `Name` property returning "Custom" or user-specified name
  - [x] GetBindings() returning merged bindings
  - [x] GetExitKeys() returning exit keys
  - [x] XML documentation

### Implementation - Config File Support
- [ ] Design JSON config schema
  - [ ] Profile name
  - [ ] Base profile reference (optional)
  - [ ] Overrides section (key combinations → action names)
  - [ ] Additions section
  - [ ] Removals section
- [ ] Create `KeyBindingConfig.cs` model class
- [ ] Create `KeyBindingConfigLoader.cs`
  - [ ] `LoadFromFile(string path)` method
  - [ ] `LoadFromJson(string json)` method
  - [ ] Parse key combinations (e.g., "Ctrl+K", "Alt+F")
  - [ ] Map action names to methods
  - [ ] Validate configuration
  - [ ] Return CustomKeyBindingProfile
  - [ ] Error handling and meaningful error messages
- [ ] Add config file search paths
  - [ ] `~/.nuru/keybindings.json` (user-specific)
  - [ ] `./.nuru/keybindings.json` (project-specific)
  - [ ] Environment variable: `NURU_KEYBINDINGS`

### Implementation - Action Registry
- [ ] Create `KeyBindingActionRegistry.cs`
  - [ ] Map action names to methods
  - [ ] "BackwardChar" → HandleBackwardChar
  - [ ] "ForwardChar" → HandleForwardChar
  - [ ] "TabComplete" → HandleTabCompletion
  - [ ] etc. for all available actions
  - [ ] Support parameterized actions (e.g., "TabComplete:reverse")
  - [ ] XML documentation with all available actions

### Implementation - Validation
- [ ] Validate key combinations don't conflict with system shortcuts
- [ ] Warn about overriding exit keys (Enter, Ctrl+D)
- [ ] Validate action names exist in registry
- [ ] Provide helpful error messages
- [ ] Log warnings for questionable configurations

### Sample Application
- [x] Create `Samples/ReplDemo/repl-custom-keybindings.cs`
  - [x] Demonstrate starting from existing profile (Emacs)
  - [x] Demonstrate Override, Add, Remove operations
  - [x] Demonstrate using CustomKeyBindingProfile with ReplOptions
  - [x] Include helpful comments explaining each feature
- [x] Create `Samples/ReplDemo/repl-custom-keybindings.md` documentation

### Testing
- [x] Create `Tests/TimeWarp.Nuru.Repl.Tests/repl-24-custom-key-bindings.cs`
  - [x] Test CustomKeyBindingProfile programmatic API
  - [x] Test KeyBindingBuilder fluent API
  - [x] Test starting from different base profiles
  - [x] Test override, add, remove operations
  - [ ] Test loading from JSON config (deferred - JSON config not yet implemented)
  - [x] Test validation and error cases
  - [ ] Test conflicting bindings (deferred)
- [ ] Create sample config files in `Samples/` (deferred - JSON config not yet implemented)
- [ ] Manual testing with custom configurations (deferred - interactive REPL required)

### Documentation
- [ ] Create `documentation/user/features/custom-key-bindings.md`
  - [ ] Builder API examples
  - [ ] Config file format specification
  - [ ] Available action names reference
  - [ ] Common customization recipes
- [ ] Update `documentation/user/guides/repl-configuration.md`
- [ ] Add sample config files to `Samples/Configuration/`
- [ ] Update CLAUDE.md if needed

## Notes

### Builder API Design

```csharp
// Programmatic customization
var customBindings = new CustomKeyBindingProfile(baseProfile: new EmacsKeyBindingProfile())
  .Override(ConsoleKey.K, ConsoleModifiers.Control, () => ClearToEnd())
  .Override(ConsoleKey.R, ConsoleModifiers.Control, () => ReverseSearch())
  .Add(ConsoleKey.T, ConsoleModifiers.Control, () => TransposeCharacters())
  .Remove(ConsoleKey.D, ConsoleModifiers.Control); // Unbind Ctrl+D

var replOptions = new ReplOptions
{
  KeyBindingProfile = customBindings
};
```

### KeyBindingBuilder Usage

```csharp
// Start from scratch
var builder = new KeyBindingBuilder()
  .Bind(ConsoleKey.LeftArrow, () => MoveLeft())
  .Bind(ConsoleKey.RightArrow, () => MoveRight())
  .BindExit(ConsoleKey.Enter, ConsoleModifiers.None, () => Submit())
  .BindExit(ConsoleKey.Escape, ConsoleModifiers.None, () => Cancel());

var (bindings, exitKeys) = builder.Build();

// Or start from existing profile
var builder = new KeyBindingBuilder()
  .LoadFrom(new DefaultKeyBindingProfile())
  .Remove(ConsoleKey.D, ConsoleModifiers.Control)
  .Bind(ConsoleKey.Q, ConsoleModifiers.Control, () => Quit());
```

### JSON Config Format

```json
{
  "name": "MyCustomProfile",
  "baseProfile": "Emacs",
  "overrides": {
    "Ctrl+K": "ClearToEnd",
    "Ctrl+R": "ReverseHistorySearch",
    "Ctrl+T": "TransposeCharacters"
  },
  "additions": {
    "Ctrl+X Ctrl+S": "SaveHistory",
    "Alt+.": "InsertLastArgument"
  },
  "removals": [
    "Ctrl+D"
  ],
  "exitKeys": [
    "Enter",
    "Ctrl+C"
  ]
}
```

### Config File Loading

```csharp
// Automatic loading from default locations
var profile = KeyBindingConfigLoader.LoadDefault();

// Or explicit path
var profile = KeyBindingConfigLoader.LoadFromFile("~/.nuru/keybindings.json");

// Or from JSON string
string json = File.ReadAllText("config.json");
var profile = KeyBindingConfigLoader.LoadFromJson(json);

var replOptions = new ReplOptions
{
  KeyBindingProfile = profile
};
```

### Action Registry Example

```csharp
public static class KeyBindingActionRegistry
{
  public static Dictionary<string, Func<ReplConsoleReader, Action>> Actions = new()
  {
    ["BackwardChar"] = reader => reader.HandleBackwardChar,
    ["ForwardChar"] = reader => reader.HandleForwardChar,
    ["BackwardWord"] = reader => reader.HandleBackwardWord,
    ["ForwardWord"] = reader => reader.HandleForwardWord,
    ["BeginningOfLine"] = reader => reader.HandleBeginningOfLine,
    ["EndOfLine"] = reader => reader.HandleEndOfLine,
    ["TabComplete"] = reader => () => reader.HandleTabCompletion(reverse: false),
    ["TabComplete:reverse"] = reader => () => reader.HandleTabCompletion(reverse: true),
    ["PreviousHistory"] = reader => reader.HandlePreviousHistory,
    ["NextHistory"] = reader => reader.HandleNextHistory,
    ["ClearToEnd"] = reader => reader.HandleClearToEnd, // Future feature
    // ... etc
  };
  
  public static Action GetAction(string actionName, ReplConsoleReader reader)
  {
    if (!Actions.TryGetValue(actionName, out var factory))
      throw new KeyBindingException($"Unknown action: {actionName}");
    
    return factory(reader);
  }
}
```

### Benefits

✅ **Maximum Flexibility** - Users control every key binding
✅ **Start from Base** - Build on existing profiles instead of starting from scratch
✅ **Config File Support** - Team-wide or project-specific configurations
✅ **Discoverable** - Action registry documents available actions
✅ **Validation** - Catch configuration errors early with helpful messages
✅ **Backward Compatible** - Optional feature, doesn't affect existing users

### File Organization

```
Source/TimeWarp.Nuru.Repl/
├── KeyBindings/
│   ├── IKeyBindingProfile.cs
│   ├── DefaultKeyBindingProfile.cs
│   ├── EmacsKeyBindingProfile.cs
│   ├── ViKeyBindingProfile.cs
│   ├── VSCodeKeyBindingProfile.cs
│   ├── CustomKeyBindingProfile.cs           (NEW)
│   ├── KeyBindingBuilder.cs                 (NEW)
│   ├── KeyBindingConfig.cs                  (NEW - model, deferred)
│   ├── KeyBindingConfigLoader.cs            (NEW - loader, deferred)
│   ├── KeyBindingActionRegistry.cs          (NEW - action mapping, deferred)
│   └── KeyBindingException.cs               (NEW - custom exception, deferred)
├── Input/
│   └── ReplConsoleReader.cs
└── ReplOptions.cs

Samples/ReplDemo/
├── repl-custom-keybindings.cs               (NEW - sample demonstrating custom key bindings)
└── repl-custom-keybindings.md               (NEW - documentation for sample)

Samples/Configuration/
├── emacs-enhanced.json                       (NEW - sample config, deferred)
├── vi-enhanced.json                          (NEW - sample config, deferred)
└── team-bindings.json                        (NEW - sample config, deferred)
```

### Estimated Effort

| Phase | Time |
|-------|------|
| Design API & config format | 1.5 hours |
| Implement KeyBindingBuilder | 2 hours |
| Implement CustomKeyBindingProfile | 1.5 hours |
| Implement config file loading | 2 hours |
| Implement action registry | 1.5 hours |
| Validation & error handling | 1.5 hours |
| Testing | 2 hours |
| Documentation & samples | 2 hours |
| **TOTAL** | **14-16 hours** |

**Recommendation**: Implement over 4-5 focused sessions.

### Risk Assessment

**RISK LEVEL: MEDIUM**

**Why Medium**:
- More complex than previous phases
- Config file parsing can have edge cases
- Security concern: users could bind dangerous actions
- Need robust validation to prevent bad configurations

**Mitigation**:
- Comprehensive validation with clear error messages
- Sandbox available actions (don't allow arbitrary code execution)
- Thorough testing of edge cases
- Document security considerations

### Success Criteria

- [x] KeyBindingBuilder fluent API works
- [x] CustomKeyBindingProfile implements IKeyBindingProfile
- [x] Can start from any base profile
- [x] Override, add, and remove operations work correctly
- [ ] JSON config file loading works (deferred)
- [ ] Action registry maps all available actions (deferred)
- [ ] Validation catches invalid configurations (deferred - part of JSON config)
- [x] All tests pass (existing + new)
- [x] Sample application demonstrates custom key bindings
- [ ] Documentation includes examples and action reference (deferred)
- [ ] Sample config files provided (deferred)
- [x] Code compiles without warnings

### Security Considerations

1. **Action Sandboxing**: Only allow predefined actions from registry, no arbitrary code execution
2. **File Validation**: Validate JSON schema before loading
3. **Path Restrictions**: Only load from known safe locations
4. **Error Handling**: Don't expose internal paths or sensitive info in error messages

### Use Cases

**Power User** - Complete customization:
```csharp
var profile = new CustomKeyBindingProfile()
  .Override(ConsoleKey.K, ConsoleModifiers.Control, ClearToEnd)
  .Override(ConsoleKey.U, ConsoleModifiers.Control, ClearLine)
  .Add(ConsoleKey.Y, ConsoleModifiers.Control, Yank);
```

**Team Standard** - Load from shared config:
```bash
# Team repo includes .nuru/keybindings.json
git clone company/project
cd project
dotnet run  # Automatically loads .nuru/keybindings.json
```

**Personal Preference** - Start from Emacs, tweak a few keys:
```csharp
var profile = new CustomKeyBindingProfile(new EmacsKeyBindingProfile())
  .Override(ConsoleKey.P, ConsoleModifiers.Control, FuzzySearch)
  .Remove(ConsoleKey.S, ConsoleModifiers.Control); // Don't use Ctrl+S
```

### Known Limitations

1. **Internal Handler Methods**: `ReplConsoleReader` handler methods (e.g., `HandleBackwardChar`, `HandleEscape`) are `internal`, so external code cannot reference them in custom bindings. Custom actions from outside the assembly can only perform independent operations (write output, play sounds, etc.) but cannot call existing handlers.

2. **Solution**: The planned Action Registry feature would expose handlers by name, allowing external code to reference them via strings like `"BackwardChar"` in JSON config files.

### Open Questions

1. **Chord Support**: Should we support multi-key chords (e.g., "Ctrl+X Ctrl+S")?
   - Initial: No, single key combinations only
   - Future: Add if user demand exists

2. **Action Parameters**: How to pass parameters to actions in config?
   - Syntax: `"actionName:param1:param2"`
   - Example: `"TabComplete:reverse"` → `HandleTabCompletion(reverse: true)`

3. **Config Merging**: Should project config merge with user config?
   - Initial: Project config overrides user config
   - Future: Add merge strategies if needed

4. **Live Reload**: Should config changes hot-reload during REPL session?
   - Initial: No, requires restart
   - Future: Add file watcher if requested
