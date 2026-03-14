# Add TimeWarp.Amuru dependency and migrate clipboard code

## Description

Add TimeWarp.Amuru as a dependency to timewarp-nuru and migrate the clipboard code from `System.Diagnostics.Process` to Amuru's `Shell.Builder` API.

## Checklist

- [ ] Add TimeWarp.Amuru package reference to timewarp-nuru.csproj
- [ ] Update Directory.Packages.props with Amuru version
- [ ] Migrate repl-console-reader.clipboard.cs to use Shell.Builder
- [ ] Remove direct System.Diagnostics.Process usage
- [ ] Run tests to verify clipboard functionality

## Notes

### Parent Task

#445 - Add --search and --group-filter options to --capabilities

---

## Implementation Plan

### Phase 1: Add TimeWarp.Amuru Dependency

**File:** `source/timewarp-nuru/timewarp-nuru.csproj`
- Add `<PackageReference Include="TimeWarp.Amuru" />` to existing ItemGroup

**File:** `source/timewarp-nuru/global-usings.cs` (if exists, or create)
- Add `global using TimeWarp.Amuru;`

---

### Phase 2: Refactor Key Binding System to Async

#### 2.1 Update Interface
**File:** `source/timewarp-nuru/repl/key-bindings/ikey-binding-profile.cs`
- Change `Dictionary<..., Action>` → `Dictionary<..., Func<Task>>`

#### 2.2 Update Builder
**File:** `source/timewarp-nuru/repl/key-bindings/key-binding-builder.cs`
- Change all `Action` parameters to `Func<Task>`
- Update `Bindings` dictionary type

#### 2.3 Update Interface Builder
**File:** `source/timewarp-nuru/repl/key-bindings/ikey-binding-builder.cs`
- Change all `Action` parameters to `Func<Task>`

#### 2.4 Update Nested Builder
**File:** `source/timewarp-nuru/repl/key-bindings/nested-key-binding-builder.cs`
- Change all `Action` parameters to `Func<Task>`

#### 2.5 Update All Key Binding Profiles
| File | Change |
|------|--------|
| `default-key-binding-profile.cs` | `Action` → `Func<Task>` |
| `emacs-key-binding-profile.cs` | `Action` → `Func<Task>` |
| `vi-key-binding-profile.cs` | `Action` → `Func<Task>` |
| `vscode-key-binding-profile.cs` | `Action` → `Func<Task>` |
| `custom-key-binding-profile.cs` | `Action` → `Func<Task>` |

#### 2.6 Update ReplConsoleReader Core
**File:** `source/timewarp-nuru/repl/input/repl-console-reader.cs`
- Change `KeyBindings` field type to `Dictionary<..., Func<Task>>`
- Update `ReadLine()` loop to `await handler()` instead of `handler()`

#### 2.7 Update Search Mode
**File:** `source/timewarp-nuru/repl/input/repl-console-reader.search.cs`
- Update key binding lookup to use `Func<Task>`

---

### Phase 3: Migrate Clipboard Methods to Async

**File:** `source/timewarp-nuru/repl/input/repl-console-reader.clipboard.cs`

| Method | Before | After |
|--------|--------|-------|
| `GetClipboardText()` | `string?` | `Task<string?>` |
| `SetClipboardText(string)` | `void` | `Task` |
| `GetWindowsClipboard()` | `string?` | `Task<string?>` |
| `SetWindowsClipboard(string)` | `void` | `Task` |
| `GetMacOSClipboard()` | `string?` | `Task<string?>` |
| `SetMacOSClipboard(string)` | `void` | `Task` |
| `GetLinuxClipboard()` | `string?` | `Task<string?>` |
| `SetLinuxClipboard(string)` | `void` | `Task` |
| `IsCommandAvailable(string)` | `bool` | `Task<bool>` |

#### Migration Pattern Examples

**Before (Windows Get):**
```csharp
using System.Diagnostics.Process process = new() { ... };
process.Start();
string result = process.StandardOutput.ReadToEnd();
process.WaitForExit();
return result;
```

**After (Amuru):**
```csharp
CommandOutput output = await Shell.Builder("powershell")
  .WithArguments("-command", "Get-Clipboard")
  .WithNoValidation()
  .CaptureAsync();
return output.Success ? output.Stdout.TrimEnd('\r', '\n') : null;
```

**Before (macOS Set with stdin):**
```csharp
process.StartInfo.RedirectStandardInput = true;
process.Start();
process.StandardInput.Write(text);
process.StandardInput.Close();
process.WaitForExit();
```

**After (Amuru):**
```csharp
await Shell.Builder("pbcopy")
  .WithStandardInput(text)
  .WithNoValidation()
  .RunAsync();
```

---

### Phase 4: Update Clipboard Callers

**File:** `source/timewarp-nuru/repl/input/repl-console-reader.selection.cs`

| Method | Change |
|--------|--------|
| `HandleCopyOrCancelLine()` | Add `async`, `await SetClipboardTextAsync()` |
| `HandleCut()` | Add `async`, `await SetClipboardTextAsync()` |
| `HandlePaste()` | Add `async`, `await GetClipboardTextAsync()` |

---

### Phase 5: Update All Handler Methods to Async

All handler methods in `ReplConsoleReader` partial classes need to return `Task`:

| File | Methods to Update |
|------|-------------------|
| `repl-console-reader.cursor-movement.cs` | `HandleForwardChar`, `HandleBackwardChar`, etc. |
| `repl-console-reader.history.cs` | `HandleHistoryNext`, `HandleHistoryPrevious`, etc. |
| `repl-console-reader.editing.cs` | `HandleDeleteChar`, `HandleBackspace`, etc. |
| `repl-console-reader.search.cs` | `HandleSearchForward`, `HandleSearchBackward`, etc. |
| `repl-console-reader.kill-ring.cs` | `HandleKillLine`, `HandleYank`, etc. |
| `repl-console-reader.undo.cs` | `HandleUndo`, `HandleRedo` |
| `repl-console-reader.selection.cs` | `HandleCopyOrCancelLine`, `HandleCut`, `HandlePaste` |
| `repl-console-reader.word-operations.cs` | Various word handlers |

**Note:** Most handlers don't need async (they just manipulate state). Only clipboard-related handlers need `async/await`. Others can return `Task.CompletedTask`.

---

### Phase 6: Run Tests

```bash
# Clear runfile cache (important for source generator changes)
ganda runfile cache --clear

# Run CI tests
dotnet run tests/ci-tests/run-ci-tests.cs
```

---

### Files Changed Summary

| Category | Files | Lines Changed (Est.) |
|----------|-------|----------------------|
| Package config | 1 | 1 |
| Key binding interfaces | 4 | 20 |
| Key binding profiles | 5 | 50 |
| ReplConsoleReader core | 1 | 10 |
| Clipboard implementation | 1 | 150 |
| Handler methods | 8 | 30 |
| **Total** | **20** | **~260** |

---

### Risk Assessment

| Risk | Mitigation |
|------|------------|
| Breaking existing key binding profiles | All profiles are internal, update together |
| Async handler overhead | Most handlers return `Task.CompletedTask` (no allocation) |
| Test failures | Run full CI test suite after changes |
