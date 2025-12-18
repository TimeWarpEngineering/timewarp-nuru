# Migrate remaining lexer tests to Jaribu multi-mode (14 files)

## Description

Migrate the remaining 14 lexer test files to support Jaribu multi-mode pattern. The infrastructure (InternalsVisibleTo, static using for LexerTestHelper) is already in place from task 166.

**Each file must be migrated and verified individually before moving to the next.**

## Checklist

For each file:
1. Modify test file (add `#if !JARIBU_MULTI`, `[ModuleInitializer]`, remove `[ClearRunfileCache]`)
2. Add file to `ci-tests/Directory.Build.props`
3. Verify standalone mode works
4. Verify multi-mode works

**Files to migrate (in order):**
- [ ] lexer-02-valid-options.cs - migrate, verify standalone, verify multi-mode
- [ ] lexer-03-invalid-double-dashes.cs - migrate, verify standalone, verify multi-mode
- [ ] lexer-04-invalid-trailing-dashes.cs - migrate, verify standalone, verify multi-mode
- [ ] lexer-05-multi-char-short-options.cs - migrate, verify standalone, verify multi-mode
- [ ] lexer-06-end-of-options.cs - migrate, verify standalone, verify multi-mode
- [ ] lexer-07-invalid-angle-brackets.cs - migrate, verify standalone, verify multi-mode
- [ ] lexer-08-whitespace-handling.cs - migrate, verify standalone, verify multi-mode
- [ ] lexer-09-complex-patterns.cs - migrate, verify standalone, verify multi-mode
- [ ] lexer-10-edge-cases.cs - migrate, verify standalone, verify multi-mode
- [ ] lexer-11-error-reporting.cs - migrate, verify standalone, verify multi-mode
- [ ] lexer-12-description-tokenization.cs - migrate, verify standalone, verify multi-mode
- [ ] lexer-13-parameter-context.cs - migrate, verify standalone, verify multi-mode
- [ ] lexer-14-token-position.cs - migrate, verify standalone, verify multi-mode
- [ ] lexer-15-advanced-features.cs - migrate, verify standalone, verify multi-mode

## Implementation Details

### Per-file changes

**Before:**
```csharp
#!/usr/bin/dotnet --

return await RunTests<TestClassName>();

[TestTag("Lexer")]
[ClearRunfileCache]
public class TestClassName
{
```

**After:**
```csharp
#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Lexer")]
public class TestClassName
{
    [ModuleInitializer]
    internal static void Register() => RegisterTests<TestClassName>();
```

### Adding to Directory.Build.props

Add each file to `tests/ci-tests/Directory.Build.props`:
```xml
<Compile Include="../timewarp-nuru-tests/lexer/lexer-XX-name.cs" />
```

### Verification commands

**Standalone:**
```bash
dotnet tests/timewarp-nuru-tests/lexer/lexer-XX-name.cs
```

**Multi-mode:**
```bash
dotnet tests/ci-tests/run-ci-tests.cs
```

## Parent Task

This is part of [task 164](../in-progress/164-migrate-tests-to-jaribu-multi-mode-for-faster-execution.md).

## Notes

- Infrastructure already in place from task 166 (InternalsVisibleTo, static using)
- lexer-test-helper.cs is a helper file, no migration needed
- Each file must pass both standalone and multi-mode verification before proceeding
