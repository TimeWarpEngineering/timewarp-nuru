# Implement Comprehensive Tab Completion Tests

## Description

Create a comprehensive test suite for REPL tab completion covering all scenarios in `repl-basic-demo.cs`. Current coverage is ~20 tests (15% of scenarios). This task will implement 135-175 automated tests across 20 categories of completion behavior, providing systematic validation of all tab completion features and catching bugs before users encounter them.

**Goal**: Build robust test coverage enabling confident REPL refactoring and ensuring all completion scenarios work correctly.

## Parent

Related to:
- Task 027_Implement-REPL-Mode-with-Tab-Completion (completed)
- Task 031_Implement-REPL-Tab-Completion (completed)
- Task 054_Extract-Tab-Completion-Logic-From-ReplConsoleReader (completed)

## Requirements

- Create 7-8 new test files covering all completion scenarios
- Implement test helper utilities for assertions and key sequences
- All tests must use TestTerminal (no manual testing required)
- Tests validate against actual routes in `repl-basic-demo.cs`
- Coverage increases from ~15% to ~90%+
- All tests timeout at 5000ms for safety
- Tests are organized by category for maintainability

## Checklist

### Phase 0: Planning & Infrastructure
- [x] Analyze current test coverage gaps (see `.agent/workspace/2025-11-25-Comprehensive-Tab-Completion-Test-Plan.md`)
- [ ] Review test plan with team
- [ ] Create test helper utilities file
  - [ ] `CompletionAssertions` extension methods
  - [ ] `KeySequenceHelpers` extension methods
  - [ ] `TestAppFactory` for shared app configuration

### Phase 1: HIGH Priority Tests (60-70 tests)

#### File 1: Basic Command Completion
- [ ] Create `Tests/TimeWarp.Nuru.Repl.Tests/TabCompletion/repl-20-tab-basic-commands.cs`
- [ ] Empty input tab (show all commands)
- [ ] Partial matching: a-z letters
- [ ] Unique matches auto-complete (st→status)
- [ ] Multiple matches show list (s→status,search)
- [ ] After complete command space tab (show --help)
- [ ] Target: ~25-30 tests

#### File 2: Subcommand Completion
- [ ] Create `Tests/TimeWarp.Nuru.Repl.Tests/TabCompletion/repl-21-tab-subcommands.cs`
- [ ] git space tab (show status, commit, log)
- [ ] git <partial> tab (filter subcommands)
- [ ] git status space tab (show --help)
- [ ] git commit space tab (show -m option)
- [ ] git log space tab (show --count option)
- [ ] Target: ~15-20 tests

#### File 3: Enum Completion
- [ ] Create `Tests/TimeWarp.Nuru.Repl.Tests/TabCompletion/repl-22-tab-enums.cs`
- [ ] deploy space tab (show Dev, Staging, Prod)
- [ ] deploy <letter> tab (filter by prefix: d→Dev, s→Staging, p→Prod)
- [ ] Case sensitivity (dev, Dev, DEV all work)
- [ ] deploy dev space tab (optional tag parameter)
- [ ] Target: ~15-20 tests

#### File 4: Option Completion
- [ ] Create `Tests/TimeWarp.Nuru.Repl.Tests/TabCompletion/repl-23-tab-options.cs`
- [ ] Single dash tab: build -<tab> (show -v)
- [ ] Double dash tab: build --<tab> (show --verbose, --help)
- [ ] Option aliases: -v vs --verbose
- [ ] Options with values: search foo --limit <tab>
- [ ] Combined options: backup data -c -o <tab>
- [ ] Short option completion: -<letter><tab>
- [ ] Target: ~25-30 tests

### Phase 2: MEDIUM Priority Tests (35-40 tests)

#### File 5: Cycling Behavior
- [ ] Create `Tests/TimeWarp.Nuru.Repl.Tests/TabCompletion/repl-24-tab-cycling.cs`
- [ ] Forward cycling: s<tab><tab> (status→search→status)
- [ ] Reverse cycling: s<tab><shift+tab> (search→status)
- [ ] Alt+= show all: s<alt+=> (display list without cycling)
- [ ] Cycling with subcommands: git <tab><tab><tab>
- [ ] Cycling with enums: deploy <tab><tab><tab><tab>
- [ ] Target: ~15-20 tests

#### File 6: State Management
- [ ] Create `Tests/TimeWarp.Nuru.Repl.Tests/TabCompletion/repl-25-tab-state-management.cs`
- [ ] Escape cancels completion (s<tab><esc> returns to "s")
- [ ] Typing resets state (s<tab>→type "t"→filters to "status")
- [ ] Backspace resets state (sta<tab>→<backspace>→"statu")
- [ ] Delete resets state
- [ ] Tab after character input (state fresh)
- [ ] Target: ~15-20 tests

### Phase 3: LOW Priority Tests (35-40 tests)

#### File 7: Edge Cases
- [ ] Create `Tests/TimeWarp.Nuru.Repl.Tests/TabCompletion/repl-26-tab-edge-cases.cs`
- [ ] Multiple spaces: status<space><space><tab>
- [ ] Invalid contexts: status extra<tab>
- [ ] Too many arguments: greet Alice Bob<tab>
- [ ] Case variations: Status<tab>, GIT<tab>
- [ ] Special characters handling
- [ ] Very long input with tab
- [ ] Empty completions (z<tab> - no matches)
- [ ] Target: ~15-20 tests

#### File 8: Help Option Availability
- [ ] Create `Tests/TimeWarp.Nuru.Repl.Tests/TabCompletion/repl-27-tab-help-option.cs`
- [ ] Help in all command lists
- [ ] Help completion: --he<tab>→--help
- [ ] Help after valid commands: status <tab>
- [ ] Help mixed with other options
- [ ] Target: ~10-15 tests

### Testing & Documentation
- [ ] Run all new tests and verify they pass
- [ ] Document any bugs found during test implementation
- [ ] Update test status report in `.agent/workspace/`
- [ ] Verify test execution time is reasonable (< 5 min total)

## Notes

### Analysis Document

Comprehensive test plan created in:
**`.agent/workspace/2025-11-25-Comprehensive-Tab-Completion-Test-Plan.md`**

The document includes:
- Analysis of 12 routes in repl-basic-demo.cs
- 20 test scenario categories
- 135-175 total test scenarios
- Test helper utilities needed
- Current test gaps identified
- Implementation priority (Phase 1-3)
- Test template examples

### Current Test Coverage

**Existing**: `Tests/TimeWarp.Nuru.Repl.Tests/repl-17-sample-validation.cs` (~20 tests)

**Covers**:
- ✅ Enum completions after "deploy "
- ✅ Enum filtering with partials
- ✅ Git subcommands
- ✅ Partial command completion
- ✅ Basic tab cycling
- ✅ Build options
- ✅ Git commit options

**Missing** (to be implemented):
- ❌ Empty input tab
- ❌ Single dash vs double dash
- ❌ Option aliases (-v vs --verbose)
- ❌ Escape behavior
- ❌ Typing during completion
- ❌ Backspace during completion
- ❌ Shift+Tab reverse cycling
- ❌ Alt+= show all
- ❌ Case sensitivity
- ❌ Multiple spaces handling
- ❌ Invalid context handling
- ❌ Help option availability
- ❌ Complex option sequences

### Test Helper Utilities Design

**Location**: Create `Tests/TimeWarp.Nuru.Repl.Tests/TabCompletion/CompletionTestHelpers.cs`

```csharp
// Assertion helpers
public static class CompletionAssertions
{
  public static void ShouldShowCompletions(this TestTerminal terminal, params string[] expected);
  public static void ShouldNotShowCompletions(this TestTerminal terminal, params string[] unexpected);
  public static void ShouldAutoComplete(this TestTerminal terminal, string expected);
  public static void ShouldShowCompletionList(this TestTerminal terminal);
}

// Key sequence helpers
public static class KeySequenceHelpers
{
  public static void TypeAndTab(this TestTerminal terminal, string text);
  public static void TabMultipleTimes(this TestTerminal terminal, int count);
  public static void CleanupAndExit(this TestTerminal terminal);
}

// App factory
public static class TestAppFactory
{
  public static NuruApp CreateReplDemoApp(TestTerminal terminal);
  // Returns app configured exactly like repl-basic-demo.cs
}
```

### Test Organization

```
Tests/TimeWarp.Nuru.Repl.Tests/
├── TabCompletion/                                    [NEW DIRECTORY]
│   ├── CompletionTestHelpers.cs                     [NEW - utilities]
│   ├── repl-20-tab-basic-commands.cs                [NEW - 25-30 tests]
│   ├── repl-21-tab-subcommands.cs                   [NEW - 15-20 tests]
│   ├── repl-22-tab-enums.cs                         [NEW - 15-20 tests]
│   ├── repl-23-tab-options.cs                       [NEW - 25-30 tests]
│   ├── repl-24-tab-cycling.cs                       [NEW - 15-20 tests]
│   ├── repl-25-tab-state-management.cs              [NEW - 15-20 tests]
│   ├── repl-26-tab-edge-cases.cs                    [NEW - 15-20 tests]
│   └── repl-27-tab-help-option.cs                   [NEW - 10-15 tests]
├── repl-17-sample-validation.cs                     [EXISTING]
└── [other test files...]
```

### Test Template Example

```csharp
[Timeout(5000)]
public static async Task Should_show_all_commands_on_empty_tab()
{
  // Arrange: Empty input, press Tab
  Terminal!.QueueKey(ConsoleKey.Tab);
  Terminal.CleanupAndExit();

  // Act
  await App!.RunReplAsync();

  // Assert: Should show all top-level commands
  Terminal.ShouldShowCompletions(
    "status", "time", "greet", "add", "deploy", 
    "echo", "git", "build", "search", "backup"
  );
  Terminal.ShouldShowCompletionList();
}

[Timeout(5000)]
public static async Task Should_auto_complete_unique_match_st_to_status()
{
  // Arrange: Type "st" then Tab
  Terminal!.TypeAndTab("st");
  Terminal.CleanupAndExit();

  // Act
  await App!.RunReplAsync();

  // Assert: Should auto-complete to "status"
  Terminal.ShouldAutoComplete("status");
}
```

### Benefits of This Work

1. **Catch regressions early** - Any REPL changes are validated automatically
2. **Document expected behavior** - Tests serve as living specification
3. **Enable confident refactoring** - Can improve completion code safely
4. **Build user confidence** - Know exactly what works and what doesn't
5. **Guide development** - Tests reveal missing features naturally
6. **Reduce manual testing** - Automate repetitive scenarios
7. **Improve code quality** - Well-tested code is more reliable

### Estimated Effort

| Phase | Tests | Effort |
|-------|-------|--------|
| Phase 0: Helpers | 1 file | 2 hours |
| Phase 1: HIGH | 60-70 tests | 8-10 hours |
| Phase 2: MEDIUM | 35-40 tests | 5-6 hours |
| Phase 3: LOW | 35-40 tests | 5-6 hours |
| **TOTAL** | **135-175 tests** | **20-24 hours** |

**Recommendation**: Implement in 3-4 focused sessions over 1-2 weeks.

### Success Criteria

- [ ] All Phase 1 tests implemented and passing (60-70 tests)
- [ ] Test helper utilities working and reusable
- [ ] Test execution time < 5 minutes total
- [ ] Code coverage > 90% for tab completion logic
- [ ] No flaky tests (all pass consistently)
- [ ] Tests catch at least 3-5 existing bugs
- [ ] Documentation updated with test organization

### Known Issues to Document

As tests are implemented, document any bugs found:
- Completion behavior inconsistencies
- State management issues
- Missing features
- Performance problems
- Unexpected behavior in edge cases

### Future Enhancements

After comprehensive tests are in place:
- Add performance benchmarks for completion
- Test with different terminal sizes
- Test with very long command histories
- Test with custom completion providers
- Test with dynamic completions

### Related Tasks

Consider after this task:
- Implement fixes for bugs revealed by tests
- Optimize completion performance if slow
- Add fuzzy matching for completions
- Improve completion ranking algorithm
- Add completion descriptions/hints

### Reference Implementation

See test plan for detailed scenarios:
- `.agent/workspace/2025-11-25-Comprehensive-Tab-Completion-Test-Plan.md`

All 20 test categories documented with examples:
1. Empty input / start of command
2. Partial command matching
3. After complete command (space + tab)
4. Subcommand completion
5. Option completion (-)
6. Option completion (--)
7. Enum parameter completion
8. Mid-word completion
9. Completion after options
10. Multiple tabs (forward cycling)
11. Shift+Tab (reverse cycling)
12. Alt+= (show all)
13. Escape (cancel)
14. Typing after completion
15. Backspace during completion
16. Complex option sequences
17. Invalid completion contexts
18. Case sensitivity
19. Help option availability
20. Edge cases

---

## Implementation Notes

### Phase Boundaries

**Phase 1 Complete When**:
- Basic command, subcommand, enum, and option tests passing
- Most common user interactions covered
- Foundation for future test expansion

**Phase 2 Complete When**:
- State management fully tested
- Cycling behavior validated
- Interactive features verified

**Phase 3 Complete When**:
- Edge cases handled
- Help system verified
- Comprehensive coverage achieved

### Testing Strategy

1. **Start with helpers** - Build solid foundation
2. **Test happy paths first** - Common scenarios
3. **Add edge cases** - Unusual inputs
4. **Document failures** - Track bugs found
5. **Iterate on fixes** - Improve completion code
6. **Maintain tests** - Keep updated as features evolve
