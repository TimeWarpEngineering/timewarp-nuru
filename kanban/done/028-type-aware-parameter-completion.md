# Task 028: Type-Aware Parameter Completion

**Related**: Task 025 (Shell Tab Completion)

## Resolution: SUPERSEDED by Task 029 (EnableDynamicCompletion)

**Resolution**: Dynamic completion (Task 029) solves this problem more elegantly:
- `DynamicCompletionHandler` defaults to `CompletionDirective.NoFileComp`
- String parameters no longer get file completion in dynamic mode
- Custom `ICompletionSource` implementations provide type-aware completions
- `EnumCompletionSource<T>` automatically completes enum values with descriptions

Users should use `EnableDynamicCompletion()` instead of `EnableStaticCompletion()` for type-aware completion behavior.

---

## Original Problem

Currently, shell completion scripts default to **file completion** for all parameters after the command. This is problematic because:

1. **String parameters get file completion** - Users typing product names, environment names, etc. don't want file suggestions
2. **Dedicated types exist** - We have `FileInfo` and `DirectoryInfo` types specifically for file/directory paths
3. **Inconsistent with type system** - The completion behavior doesn't respect the parameter type declarations

### Current Behavior

```bash
shell-completion-example createorder <TAB>
# Shows: .bashrc  .config/  Documents/  ...
# Expected: No completion (user types freely)
```

### Problem Code

**bash-completion.sh (line 27-28)**:
```bash
# Default: file completion
_filedir
return 0
```

All four shell templates have this issue:
- Bash: `_filedir`
- Zsh: `_files`
- PowerShell: Native file completion in PSReadLine
- Fish: Default file completion

## Proposed Solution

### Option 1: Remove Default File Completion (Simple)

**Change**: Remove the default `_filedir` fallback entirely.

**Bash template**:
```bash
# If we're completing the first argument, suggest commands
if [[ $cword -eq 1 ]]; then
    COMPREPLY=( $(compgen -W "$commands" -- "$cur") )
    return 0
fi

# If current word starts with -, suggest options
if [[ "$cur" == -* ]]; then
    COMPREPLY=( $(compgen -W "$options" -- "$cur") )
    return 0
fi

# No completion for parameters - let user type freely
return 0
```

**Pros**:
- ✅ Simple - just remove 2 lines
- ✅ Consistent with type system
- ✅ Works for all shells

**Cons**:
- ❌ FileInfo/DirectoryInfo parameters won't get file completion

### Option 2: Type-Aware Completion (Advanced)

**Change**: Generate completion logic based on parameter types at each position.

**Requires**:
1. Parse route patterns to extract parameter positions and types
2. Generate position-aware completion logic
3. Add file completion only for FileInfo/DirectoryInfo parameters

**Example generated code**:
```bash
# Completion for: deploy {env} {config:fileinfo}
case "${words[1]}" in
    deploy)
        case $cword in
            2) # {env} - string, no completion
                return 0
                ;;
            3) # {config:fileinfo} - file completion
                _filedir
                return 0
                ;;
        esac
        ;;
esac
```

**Pros**:
- ✅ Respects type system completely
- ✅ FileInfo/DirectoryInfo get proper completion
- ✅ Enum types could be completed with values

**Cons**:
- ❌ Complex implementation
- ❌ Larger generated scripts
- ❌ Needs parameter extraction from CompiledRoute

## Recommendation

Start with **Option 1** (remove default file completion):
1. Quick win - fixes the immediate problem
2. Aligns with type system philosophy
3. Can be enhanced to Option 2 later (non-breaking)

## Implementation Steps

### Phase 1: Remove Default File Completion

1. **Update bash-completion.sh template**
   - Remove `_filedir` call
   - Add comment explaining no default completion

2. **Update zsh-completion.sh template**
   - Remove `_files` call

3. **Update pwsh-completion.ps1 template**
   - PowerShell already doesn't have default file completion (only for commands)

4. **Update fish-completion.fish template**
   - Fish already only completes commands/options

5. **Update documentation**
   - Explain that string parameters don't get file completion
   - Document that FileInfo/DirectoryInfo should be used for paths
   - Update getting-started.md (remove "File paths for string parameters")

6. **Update tests**
   - Test that string parameters have no completion
   - Test that commands still complete

### Phase 2: Add FileInfo/DirectoryInfo Completion (Future)

This would be a separate task (Task 029) to implement type-aware completion for FileInfo/DirectoryInfo parameters specifically.

## Files to Modify

### Templates
- `Source/TimeWarp.Nuru.Completion/Completion/Templates/bash-completion.sh`
- `Source/TimeWarp.Nuru.Completion/Completion/Templates/zsh-completion.sh`
- `Source/TimeWarp.Nuru.Completion/Completion/Templates/fish-completion.fish`

### Documentation
- `documentation/user/getting-started.md` (line 175)
- `documentation/user/features/shell-completion.md`
- `samples/shell-completion-example/Overview.md`

### Tests
- Add test to verify string parameters don't trigger file completion
- Verify command completion still works

## Definition of Done

- [ ] Bash template no longer defaults to file completion
- [ ] Zsh template no longer defaults to file completion
- [ ] Fish template verified to not default to file completion
- [ ] PowerShell template verified (already correct)
- [ ] Documentation updated to remove "File paths for string parameters"
- [ ] Documentation explains FileInfo/DirectoryInfo for path parameters
- [ ] Tests pass confirming command completion works
- [ ] BuiltInTypesExample.cs demonstrates FileInfo/DirectoryInfo usage
- [ ] Version bumped to beta.33

## Testing Scenarios

```bash
# After fix, these should NOT show file completion
shell-completion-example createorder <TAB>  # No completion
shell-completion-example deploy <TAB>       # No completion

# These should still work
shell-completion-example cre<TAB>           # Completes: create, createorder
shell-completion-example status <TAB>       # No completion (no more parameters)

# FileInfo parameters (future - Task 029)
myapp process {file:fileinfo} <TAB>         # Would show file completion
myapp backup {dir:directoryinfo} <TAB>      # Would show directory completion
```

## References

- Issue: Raised during testing of Task 025
- Type System: `Source/TimeWarp.Nuru/TypeConversion/DefaultTypeConverters.cs`
- FileInfo/DirectoryInfo converters already exist
- Related: Getting Started docs claim "File paths for string parameters" (incorrect)

## Notes

This aligns TimeWarp.Nuru with the principle of **explicit over implicit**. If a user wants file path completion, they should declare the parameter as `FileInfo` or `DirectoryInfo`, not rely on implicit behavior for `string` parameters.
