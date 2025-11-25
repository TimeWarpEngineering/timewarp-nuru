# Add Key Binding Profiles (Emacs/Vi/VSCode Modes)

## Description

Add support for multiple key binding profiles to allow users to choose their preferred editing style (Default, Emacs, Vi, VSCode). Introduces `IKeyBindingProfile` interface and concrete implementations, making REPL key bindings customizable without breaking existing behavior.

**Goal**: Enable users to configure REPL to match their muscle memory from other editors.

## Parent

Part of 3-phase key binding evolution:
- **Task 055** (Phase 1): Action + ExitSet pattern (prerequisite)
- **Task 056** (Phase 2): IKeyBindingProfile interface (this task)
- **Task 057** (Phase 3): Custom key bindings via builder API

## Requirements

- Create `IKeyBindingProfile` interface
- Implement `DefaultKeyBindingProfile` (wraps existing bindings from Phase 1)
- Implement `EmacsKeyBindingProfile` (Emacs-style bindings)
- Implement `ViKeyBindingProfile` (Vi normal mode bindings)
- Implement `VSCodeKeyBindingProfile` (VSCode-style bindings)
- Add `KeyBindingProfile` property to `ReplOptions`
- Default to `DefaultKeyBindingProfile` for backward compatibility
- All existing tests pass
- Add tests for new profiles
- Document key binding differences

## Checklist

### Design
- [ ] Research Emacs key bindings (bash/readline standard)
- [ ] Research Vi key bindings (Vi normal mode)
- [ ] Research VSCode key bindings
- [ ] Design IKeyBindingProfile interface
- [ ] Plan profile organization structure

### Implementation - Core Interface
- [ ] Create `Source/TimeWarp.Nuru.Repl/KeyBindings/` directory
- [ ] Create `IKeyBindingProfile.cs`
  - [ ] `string Name { get; }` property
  - [ ] `Dictionary<(ConsoleKey, ConsoleModifiers), Action> GetBindings(ReplConsoleReader reader)` method
  - [ ] `HashSet<(ConsoleKey, ConsoleModifiers)> GetExitKeys()` method
  - [ ] XML documentation

### Implementation - Default Profile
- [ ] Create `DefaultKeyBindingProfile.cs`
  - [ ] Move existing bindings from ReplConsoleReader
  - [ ] Name = "Default"
  - [ ] GetBindings() returns current key mappings
  - [ ] GetExitKeys() returns Enter and Ctrl+D
  - [ ] XML documentation

### Implementation - Emacs Profile
- [ ] Create `EmacsKeyBindingProfile.cs`
  - [ ] Name = "Emacs"
  - [ ] Ctrl+A: beginning-of-line
  - [ ] Ctrl+E: end-of-line
  - [ ] Ctrl+F: forward-char
  - [ ] Ctrl+B: backward-char
  - [ ] Alt+F: forward-word
  - [ ] Alt+B: backward-word
  - [ ] Ctrl+K: kill-line (delete to end)
  - [ ] Ctrl+D: delete-char or EOF
  - [ ] Ctrl+P: previous-history
  - [ ] Ctrl+N: next-history
  - [ ] Ctrl+R: reverse-search (if implemented)
  - [ ] Tab: completion
  - [ ] XML documentation with key binding table

### Implementation - Vi Profile
- [ ] Create `ViKeyBindingProfile.cs`
  - [ ] Name = "Vi"
  - [ ] Start in insert mode by default
  - [ ] Escape: switch to normal mode (if mode system implemented)
  - [ ] h: backward-char (normal mode)
  - [ ] l: forward-char (normal mode)
  - [ ] w: forward-word (normal mode)
  - [ ] b: backward-word (normal mode)
  - [ ] 0: beginning-of-line (normal mode)
  - [ ] $: end-of-line (normal mode)
  - [ ] k: previous-history (normal mode)
  - [ ] j: next-history (normal mode)
  - [ ] i: enter insert mode
  - [ ] Note: Full Vi mode system may be future enhancement
  - [ ] XML documentation with key binding table

### Implementation - VSCode Profile
- [ ] Create `VSCodeKeyBindingProfile.cs`
  - [ ] Name = "VSCode"
  - [ ] Ctrl+Left: backward-word
  - [ ] Ctrl+Right: forward-word
  - [ ] Home: beginning-of-line
  - [ ] End: end-of-line
  - [ ] Ctrl+Home: beginning-of-history
  - [ ] Ctrl+End: end-of-history
  - [ ] Ctrl+K: delete to end of line
  - [ ] Ctrl+Backspace: delete word backward
  - [ ] Tab: completion
  - [ ] Shift+Tab: reverse completion
  - [ ] XML documentation with key binding table

### Implementation - Integration
- [ ] Update `ReplOptions.cs`
  - [ ] Add `public IKeyBindingProfile KeyBindingProfile { get; set; } = new DefaultKeyBindingProfile();`
  - [ ] XML documentation
- [ ] Update `ReplConsoleReader.cs` constructor
  - [ ] Accept profile from ReplOptions
  - [ ] Initialize KeyBindings from profile.GetBindings(this)
  - [ ] Initialize ExitKeys from profile.GetExitKeys()
- [ ] Make necessary handler methods internal/public for profile access
- [ ] Build solution and fix compilation errors

### Testing
- [ ] Create `Tests/TimeWarp.Nuru.Repl.Tests/repl-11-key-binding-profiles.cs`
  - [ ] Test DefaultKeyBindingProfile
  - [ ] Test EmacsKeyBindingProfile
  - [ ] Test ViKeyBindingProfile
  - [ ] Test VSCodeKeyBindingProfile
  - [ ] Test switching profiles
- [ ] Run all existing REPL tests (should pass with DefaultProfile)
- [ ] Manual testing with each profile
- [ ] Verify backward compatibility (no profile specified = Default)

### Documentation
- [ ] Add `documentation/user/features/repl-key-bindings.md`
  - [ ] List all profiles
  - [ ] Key binding comparison table
  - [ ] Usage examples
- [ ] Update REPL samples to show profile selection
- [ ] Update CLAUDE.md if needed

## Notes

### Profile Interface Design

```csharp
namespace TimeWarp.Nuru.Repl.KeyBindings;

/// <summary>
/// Defines a set of key bindings for REPL input handling.
/// </summary>
public interface IKeyBindingProfile
{
  /// <summary>
  /// Gets the name of this key binding profile.
  /// </summary>
  string Name { get; }

  /// <summary>
  /// Gets the key bindings for this profile.
  /// </summary>
  /// <param name="reader">The ReplConsoleReader instance for accessing handler methods.</param>
  /// <returns>Dictionary mapping key combinations to actions.</returns>
  Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Action> GetBindings(ReplConsoleReader reader);

  /// <summary>
  /// Gets the keys that should exit the read loop (typically Enter and Ctrl+D).
  /// </summary>
  /// <returns>Set of key combinations that exit the read loop.</returns>
  HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> GetExitKeys();
}
```

### Usage Example

```csharp
// User selects Emacs key bindings
var replOptions = new ReplOptions
{
  Prompt = "> ",
  EnableColors = true,
  KeyBindingProfile = new EmacsKeyBindingProfile()
};

NuruApp app = NuruApp.CreateBuilder(args)
  .AddReplSupport(replOptions)
  .Build();

await app.RunAsync();
```

### Key Binding Comparison

| Action | Default | Emacs | Vi (Normal) | VSCode |
|--------|---------|-------|-------------|--------|
| Move left | ← / Ctrl+B | Ctrl+B | h | ← |
| Move right | → / Ctrl+F | Ctrl+F | l | → |
| Word left | Ctrl+← / Alt+B | Alt+B | b | Ctrl+← |
| Word right | Ctrl+→ / Alt+F | Alt+F | w | Ctrl+→ |
| Line start | Home / Ctrl+A | Ctrl+A | 0 | Home |
| Line end | End / Ctrl+E | Ctrl+E | $ | End |
| Previous history | ↑ / Ctrl+P | Ctrl+P | k | ↑ |
| Next history | ↓ / Ctrl+N | Ctrl+N | j | ↓ |
| Delete to end | - | Ctrl+K | D | Ctrl+K |
| Completion | Tab | Tab | Tab | Tab |

### Benefits

✅ **User Choice** - Pick familiar key bindings from other tools
✅ **Community Contributions** - Easy to add new profiles
✅ **Backward Compatible** - DefaultKeyBindingProfile ensures no breaking changes
✅ **Discoverable** - `ReplOptions.KeyBindingProfile` is self-documenting
✅ **Testable** - Each profile can be tested independently

### File Organization

```
Source/TimeWarp.Nuru.Repl/
├── KeyBindings/                           (NEW DIRECTORY)
│   ├── IKeyBindingProfile.cs              (interface)
│   ├── DefaultKeyBindingProfile.cs        (current bindings)
│   ├── EmacsKeyBindingProfile.cs          (Emacs-style)
│   ├── ViKeyBindingProfile.cs             (Vi-style)
│   └── VSCodeKeyBindingProfile.cs         (VSCode-style)
├── Input/
│   ├── ReplConsoleReader.cs               (uses IKeyBindingProfile)
│   ├── CompletionState.cs
│   └── TabCompletionHandler.cs
├── ReplOptions.cs                         (add KeyBindingProfile property)
└── ...
```

### Estimated Effort

| Phase | Time |
|-------|------|
| Design & research | 1 hour |
| Create interface & Default profile | 1 hour |
| Implement Emacs profile | 1 hour |
| Implement Vi profile | 1.5 hours |
| Implement VSCode profile | 1 hour |
| Integration & testing | 1.5 hours |
| Documentation | 1 hour |
| **TOTAL** | **7-8 hours** |

**Recommendation**: Implement over 2-3 focused sessions.

### Risk Assessment

**RISK LEVEL: LOW-MEDIUM**

**Why Low**:
- Builds on Phase 1 foundation
- Additive feature (no breaking changes)
- DefaultKeyBindingProfile ensures backward compatibility

**Why Medium**:
- Requires making some ReplConsoleReader methods internal/public
- Vi mode may need modal state management (complex)
- Need to ensure profiles don't conflict with system shortcuts

### Success Criteria

- [ ] IKeyBindingProfile interface created
- [ ] All 4 profiles implemented (Default, Emacs, Vi, VSCode)
- [ ] ReplOptions.KeyBindingProfile property added
- [ ] All existing tests pass with DefaultKeyBindingProfile
- [ ] New profile tests pass
- [ ] Documentation includes key binding comparison table
- [ ] User can switch profiles via ReplOptions
- [ ] Code compiles without warnings

### Future Work

**Task 057** (Phase 3) will add custom bindings that build on this:
```csharp
// Phase 3: Start from a profile, customize specific keys
var customProfile = new CustomKeyBindingProfile(baseProfile: new EmacsKeyBindingProfile())
  .Override(ConsoleKey.K, ConsoleModifiers.Control, () => MyCustomAction())
  .Remove(ConsoleKey.D, ConsoleModifiers.Control);
```

### Open Questions

1. **Vi Mode Complexity**: Should Vi profile have full modal editing (normal/insert/visual)? 
   - Initial implementation: Basic Vi-inspired bindings only
   - Future: Full modal system if user demand exists

2. **Profile Discovery**: Should we auto-detect OS and suggest profile?
   - Windows → VSCode profile
   - macOS/Linux with bash → Emacs profile
   - Can be future enhancement

3. **Conflicting Shortcuts**: How to handle system shortcuts (Ctrl+C)?
   - Document known conflicts
   - Profiles should avoid system-reserved keys
