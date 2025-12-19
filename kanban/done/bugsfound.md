# Bugs Found During Tab Completion Testing

**Task**: 061 Implement Comprehensive Tab Completion Tests  
**Date**: 2025-11-26  
**Total Bugs Found**: 18  
**Test Coverage**: 134 tests (116 passing, 18 documenting bugs)

---

## Bug Categories

### Category 1: Option Discovery Failures (3 bugs)

**Severity**: HIGH - Users cannot discover available options

#### Bug #1: Option list not shown after simple commands
**Test**: `Should_show_verbose_options_after_build_space`  
**File**: `repl-23-tab-options.cs:55`

**Steps to Reproduce**:
1. Type `build ` (with trailing space)
2. Press Tab
3. **Expected**: Show `--verbose, -v` options
4. **Actual**: No options shown

**Impact**: Users must memorize that `build` accepts `--verbose/-v` option

---

#### Bug #2: Option list not shown after required parameter
**Test**: `Should_show_limit_option_after_search_query`  
**File**: `repl-23-tab-options.cs:132`

**Steps to Reproduce**:
1. Type `search foo ` (with trailing space)
2. Press Tab
3. **Expected**: Show `--limit, -l` options
4. **Actual**: No options shown

**Impact**: Users must memorize that `search` accepts `--limit/-l` option

---

#### Bug #3: Option list not shown after multiple options
**Test**: `Should_show_compress_and_output_options_after_backup_source`  
**File**: `repl-23-tab-options.cs:182`

**Steps to Reproduce**:
1. Type `backup data ` (with trailing space)
2. Press Tab
3. **Expected**: Show `--compress, -c, --output, -o` options
4. **Actual**: No options shown

**Impact**: Users cannot discover multiple available options

---

### Category 2: Partial Option Completion Failures (7 bugs)

**Severity**: HIGH - Users must type exact option names

#### Bug #4: Cannot complete --l to --limit
**Test**: `Should_complete_search_foo_dash_dash_l_to_limit`  
**File**: `repl-23-tab-options.cs:163`

**Steps to Reproduce**:
1. Type `search foo --l`
2. Press Tab
3. **Expected**: Complete to `search foo --limit`
4. **Actual**: No completion happens

---

#### Bug #5: Cannot complete --lim to --limit
**Test**: `Should_complete_search_foo_dash_dash_lim_to_limit`  
**File**: `repl-23-tab-options.cs:177`

**Steps to Reproduce**:
1. Type `search foo --lim`
2. Press Tab
3. **Expected**: Complete to `search foo --limit`
4. **Actual**: No completion happens

---

#### Bug #6: Cannot complete --c to --compress
**Test**: `Should_complete_backup_data_dash_dash_c_to_compress`  
**File**: `repl-23-tab-options.cs:238`

**Steps to Reproduce**:
1. Type `backup data --c`
2. Press Tab
3. **Expected**: Complete to `backup data --compress`
4. **Actual**: No completion happens

---

#### Bug #7: Cannot complete --o to --output
**Test**: `Should_complete_backup_data_dash_dash_o_to_output`  
**File**: `repl-23-tab-options.cs:252`

**Steps to Reproduce**:
1. Type `backup data --o`
2. Press Tab
3. **Expected**: Complete to `backup data --output`
4. **Actual**: No completion happens

---

#### Bug #8: Cannot complete --com to --compress
**Test**: `Should_complete_backup_data_dash_dash_com_to_compress`  
**File**: `repl-23-tab-options.cs:266`

**Steps to Reproduce**:
1. Type `backup data --com`
2. Press Tab
3. **Expected**: Complete to `backup data --compress`
4. **Actual**: No completion happens

---

#### Bug #9: Cannot complete --out to --output
**Test**: `Should_complete_backup_data_dash_dash_out_to_output`  
**File**: `repl-23-tab-options.cs:280`

**Steps to Reproduce**:
1. Type `backup data --out`
2. Press Tab
3. **Expected**: Complete to `backup data --output`
4. **Actual**: No completion happens

---

#### Bug #10: Remaining options not shown after using one option
**Test**: `Should_show_output_option_after_backup_data_compress`  
**File**: `repl-23-tab-options.cs:201`

**Steps to Reproduce**:
1. Type `backup data -c ` (with trailing space)
2. Press Tab
3. **Expected**: Show remaining `--output, -o` options
4. **Actual**: No options shown

**Impact**: Users cannot discover additional options after using one

---

### Category 3: Case Sensitivity Issues (1 bug)

**Severity**: MEDIUM - Inconsistent behavior

#### Bug #11: Case insensitive option completion inconsistent
**Test**: `Should_complete_search_foo_dash_dash_L_to_limit_case_insensitive`  
**File**: `repl-23-tab-options.cs:310`

**Steps to Reproduce**:
1. Type `search foo --L` (uppercase L)
2. Press Tab
3. **Expected**: Complete to `search foo --limit`
4. **Actual**: No completion happens

**Note**: `build --V` DOES complete to `build --verbose`, so case-insensitive matching works for some options but not others.

**Impact**: Unpredictable behavior - users don't know which options are case-sensitive

---

### Category 4: State Management Issues (1 bug)

**Severity**: MEDIUM - Confusing UX

#### Bug #12: Completion state leaks between attempts
**Test**: `Should_clear_state_between_completion_attempts`  
**File**: `repl-25-tab-state-management.cs:111`

**Steps to Reproduce**:
1. Type `s`
2. Press Tab (shows status, search)
3. Press Escape
4. Press Backspace (remove 's')
5. Type `g`
6. Press Tab
7. **Expected**: Show only `git, greet`
8. **Actual**: Still shows `status` from first attempt

**Impact**: Stale completions appear, confusing users

---

### Category 5: Help Option Discovery Failures (6 bugs)

**Severity**: MEDIUM - Help system not discoverable

#### Bug #13: --help not shown after simple commands (status)
**Test**: `Should_show_help_option_after_status_command`  
**File**: `repl-27-tab-help-option.cs:56`

**Steps to Reproduce**:
1. Type `status ` (with trailing space)
2. Press Tab
3. **Expected**: Show `--help` option
4. **Actual**: No options shown

---

#### Bug #14: --help not shown after simple commands (time)
**Test**: `Should_show_help_option_after_time_command`  
**File**: `repl-27-tab-help-option.cs:70`

**Steps to Reproduce**:
1. Type `time ` (with trailing space)
2. Press Tab
3. **Expected**: Show `--help` option
4. **Actual**: No options shown

---

#### Bug #15: --help not shown for commands with parameters
**Test**: `Should_show_help_option_after_greet_command`  
**File**: `repl-27-tab-help-option.cs:84`

**Steps to Reproduce**:
1. Type `greet ` (with trailing space)
2. Press Tab
3. **Expected**: Show `--help` option
4. **Actual**: No options shown

---

#### Bug #16: --help not shown with enum values
**Test**: `Should_show_help_option_with_deploy_enum_values`  
**File**: `repl-27-tab-help-option.cs:98`

**Steps to Reproduce**:
1. Type `deploy ` (with trailing space)
2. Press Tab
3. **Expected**: Show `--help` along with `Dev, Staging, Prod`
4. **Actual**: Only enum values shown, no `--help`

---

#### Bug #17: --help not shown with build options
**Test**: `Should_show_help_with_build_options`  
**File**: `repl-27-tab-help-option.cs:142`

**Steps to Reproduce**:
1. Type `build ` (with trailing space)
2. Press Tab
3. **Expected**: Show `--help` along with `--verbose, -v`
4. **Actual**: No options shown (see Bug #1)

---

#### Bug #18: --help not shown with search options
**Test**: `Should_show_help_with_search_options`  
**File**: `repl-27-tab-help-option.cs:156`

**Steps to Reproduce**:
1. Type `search foo ` (with trailing space)
2. Press Tab
3. **Expected**: Show `--help` along with `--limit, -l`
4. **Actual**: No options shown (see Bug #2)

**Note**: Bugs #17 and #18 are related to Bugs #1 and #2 - if option lists were shown, --help should be included.

---

## Summary by Severity

### HIGH (11 bugs) - Critical UX Problems
- **Category 1**: Option discovery failures (3 bugs)
- **Category 2**: Partial option completion failures (7 bugs)
- **Root Cause**: Option completion infrastructure incomplete/broken

**User Impact**: Must memorize exact option names and syntax. Cannot explore available options.

### MEDIUM (7 bugs) - Confusing Behavior
- **Category 3**: Case sensitivity inconsistent (1 bug)
- **Category 4**: State management leaks (1 bug)
- **Category 5**: Help option not discoverable (5 bugs)

**User Impact**: Unpredictable behavior, help system hidden

---

## Recommended Fix Priority

### Priority 1: Fix Option Discovery (Bugs #1-3)
Enable showing option lists after commands. This is the foundation for all other option-related features.

**Files to investigate**:
- `Source/TimeWarp.Nuru.Repl/Input/ReplConsoleReader.cs`
- `Source/TimeWarp.Nuru.Completion/` (completion provider logic)

### Priority 2: Fix Partial Option Completion (Bugs #4-10)
Enable completing partial option prefixes (--l → --limit).

### Priority 3: Fix Help Option Availability (Bugs #13-18)
Ensure --help appears in all completion contexts.

### Priority 4: Fix Case Sensitivity (Bug #11)
Make option completion consistently case-insensitive.

### Priority 5: Fix State Management (Bug #12)
Clear completion state properly between attempts.

---

## Testing

All bugs can be verified using the test files:
- `Tests/TimeWarp.Nuru.Repl.Tests/TabCompletion/repl-23-tab-options.cs`
- `Tests/TimeWarp.Nuru.Repl.Tests/TabCompletion/repl-25-tab-state-management.cs`
- `Tests/TimeWarp.Nuru.Repl.Tests/TabCompletion/repl-27-tab-help-option.cs`

After fixes, re-run tests to verify:
```bash
cd Tests/TimeWarp.Nuru.Repl.Tests/TabCompletion
./repl-23-tab-options.cs
./repl-25-tab-state-management.cs
./repl-27-tab-help-option.cs
```

Expected result after fixes: All tests should pass.

---

## Notes

These bugs were discovered through systematic testing covering:
- 134 test scenarios
- All completion contexts (commands, subcommands, enums, options)
- Edge cases (case sensitivity, state management, etc.)

The test suite provides:
- Exact reproduction steps for each bug
- Automated verification after fixes
- Regression prevention for future changes
