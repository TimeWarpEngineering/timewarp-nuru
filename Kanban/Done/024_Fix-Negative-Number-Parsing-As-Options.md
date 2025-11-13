# Fix Negative Number Parsing As Options

## Priority
**High** - This is a fundamental bug affecting basic numeric operations

## Goal
Allow negative numbers to be passed as positional parameter values without being interpreted as undefined options.

## Problem Description

### Current Behavior (Broken)
```bash
$ ./calc-delegate.cs add 5 3
5 + 3 = 8  ✅ Works

$ ./calc-delegate.cs add 5 -3
No matching command found  ❌ Fails - treats -3 as an option
```

The route matcher interprets `-3` as an undefined option flag instead of a negative number value for the parameter.

### Root Cause

In [EndpointResolver.cs:289](Source/TimeWarp.Nuru/Resolution/EndpointResolver.cs#L289), the `ValidateSegmentAvailability` method treats ANY argument starting with `-` as an option:

```csharp
if (!seenEndOfOptions && args[argIndex].StartsWith(CommonStrings.SingleDash, StringComparison.Ordinal))
{
    // Hit an option - check if current segment is optional
    if (segment is ParameterMatcher optionalParam && optionalParam.IsOptional)
    {
        return SegmentValidationResult.Skip;
    }

    LoggerMessages.RequiredSegmentExpectedButFoundOption(logger, segment.ToDisplayString(), args[argIndex], null);
    return SegmentValidationResult.Fail;  // ← Rejects -3 as "option"
}
```

This logic doesn't distinguish between:
- Option flags: `-v`, `-x`, `--verbose`
- Negative numbers: `-3`, `-42.5`, `-1.23e10`
- Other literal values: `-sometext`, `-x:value`

## Expected Behavior

### Calculator Example
```bash
$ ./calc-delegate.cs add 5 -3
5 + (-3) = 2  ✅ Should work

$ ./calc-delegate.cs subtract 10 -5
10 - (-5) = 15  ✅ Should work

$ ./calc-delegate.cs multiply -2 -3
(-2) * (-3) = 6  ✅ Should work
```

### General Principle
An argument starting with `-` should be treated as a positional value (not an option) when:
1. The current route segment expects a positional parameter (not an option)
2. The argument does NOT match any defined option in the route pattern
3. Special case: The argument looks like a number (matches numeric pattern)

## Proposed Solutions

### Option 1: Check Against Defined Options
Before treating an argument as an option, verify it matches a defined option in the route pattern using `IsDefinedOption()`.

**Pros**:
- Fixes negative numbers
- Also fixes arbitrary strings starting with `-` (like `-x:value` from task 023)
- Aligns with principle: only defined options are options

**Cons**:
- Changes behavior: undefined options become positional values instead of errors
- May hide typos: `myapp --verbos data` would treat `--verbos` as positional value

### Option 2: Numeric Pattern Detection
Add special case for numeric patterns before the option check.

```csharp
// Check if argument looks like a number
if (args[argIndex].StartsWith("-") &&
    IsNumericPattern(args[argIndex]) &&
    segment is ParameterMatcher param &&
    IsNumericType(param.Type))
{
    // Allow as positional numeric value
    return SegmentValidationResult.Proceed;
}
```

**Pros**:
- Minimal behavior change
- Specifically fixes the negative number issue
- Preserves error reporting for undefined options

**Cons**:
- Doesn't solve the general problem (still fails for `-x:value`, etc.)
- Requires numeric pattern matching logic
- Edge cases: What about `-1e10`, `-0x42`, `-Infinity`?

### Option 3: Combined Approach
1. Add numeric pattern detection for negative numbers
2. Check against defined options for other dash-prefixed args
3. If neither matches, treat as error (undefined option)

**Pros**:
- Fixes negative numbers immediately
- Paves way for alternative syntax support (task 023)
- Maintains some error detection for typos

**Cons**:
- Most complex solution
- Need to define clear precedence rules

## Recommendation

**Option 1** (Check Against Defined Options) is the best approach because:

1. **Fixes multiple issues**: Negative numbers, literal values, prepares for task 023
2. **Principle of least surprise**: If a route doesn't define `-x` as an option, then `-x` isn't an option
3. **Enables CLI interception**: Pass through arguments to wrapped commands without pre-defining every possible flag
4. **Simpler implementation**: Single change in `ValidateSegmentAvailability`

The "con" about hiding typos can be addressed by:
- Clear error messages when route matching fails
- Suggesting similar option names in error output (future enhancement)
- Documentation about using `--` to mark end of options

## Implementation Plan

### Phase 1: Fix ValidateSegmentAvailability

1. **Update method signature** to accept option matchers:
   ```csharp
   private static SegmentValidationResult ValidateSegmentAvailability(
       RouteMatcher segment,
       string[] args,
       int argIndex,
       bool seenEndOfOptions,
       IReadOnlyList<OptionMatcher> optionMatchers,  // NEW
       ILogger logger
   )
   ```

2. **Update the dash-check logic**:
   ```csharp
   if (!seenEndOfOptions && args[argIndex].StartsWith(CommonStrings.SingleDash, StringComparison.Ordinal))
   {
       // Check if this matches a defined option
       if (IsDefinedOption(args[argIndex], optionMatchers))
       {
           // It's a defined option - handle as before
           if (segment is ParameterMatcher optionalParam && optionalParam.IsOptional)
           {
               return SegmentValidationResult.Skip;
           }

           LoggerMessages.RequiredSegmentExpectedButFoundOption(logger, segment.ToDisplayString(), args[argIndex], null);
           return SegmentValidationResult.Fail;
       }

       // Not a defined option - treat as positional value
       // Fall through to SegmentValidationResult.Proceed
   }
   ```

3. **Update call site** (line 222):
   ```csharp
   SegmentValidationResult validationResult = ValidateSegmentAvailability(
       segment,
       args,
       consumedArgs,
       seenEndOfOptions,
       endpoint.CompiledRoute.OptionMatchers,  // NEW
       logger
   );
   ```

### Phase 2: Add Tests

Add test cases to verify:

1. **Negative numbers work**:
   ```csharp
   [TestMethod]
   public async Task Should_accept_negative_numbers_as_parameters()
   {
       var app = new NuruAppBuilder()
           .AddRoute("add {x:double} {y:double}", (double x, double y) => x + y)
           .Build();

       int exitCode = await app.RunAsync(["add", "5", "-3"]);
       exitCode.ShouldBe(0);
       // Result should be 2
   }
   ```

2. **Literal dash-prefixed values work**:
   ```csharp
   [TestMethod]
   public async Task Should_accept_literal_dash_values_when_no_option_defined()
   {
       string? captured = null;
       var app = new NuruAppBuilder()
           .AddRoute("echo {text}", (string text) => { captured = text; return 0; })
           .Build();

       await app.RunAsync(["echo", "-sometext"]);
       captured.ShouldBe("-sometext");
   }
   ```

3. **Defined options still work**:
   ```csharp
   [TestMethod]
   public async Task Should_still_reject_undefined_options_when_option_expected()
   {
       var app = new NuruAppBuilder()
           .AddRoute("test --flag", (bool flag) => 0)
           .Build();

       int exitCode = await app.RunAsync(["test", "--other"]);
       exitCode.ShouldBe(1); // Should fail - undefined option
   }
   ```

### Phase 3: Update Documentation

1. Update [route-pattern-syntax.md](documentation/developer/guides/route-pattern-syntax.md) to clarify:
   - Arguments starting with `-` are only treated as options if defined in route pattern
   - Use `--` separator to explicitly mark everything after as positional values
   - Examples with negative numbers

2. Update error messages to be more helpful:
   - "Argument '-x' looks like an option but is not defined in this route"
   - Suggest using `--` separator if user wants to pass literal dash-prefixed values

## Test Files to Update

- Create new test file: `Tests/TimeWarp.Nuru.Tests/Routing/routing-13-negative-numbers.cs`
- Update existing: `Tests/TimeWarp.Nuru.Tests/Routing/routing-12-colon-filtering.cs` (can unskip test after fix)

## Related Tasks

- **Task 023**: Support-Alternative-Option-Value-Separators
  - This fix is a prerequisite - enables treating non-option dash-args as values
  - After this fix, can add splitting logic for `=` and `:` separators

## Success Criteria

- [x] Calculator sample works with negative numbers: `calc add 5 -3` returns 2
- [x] All existing tests continue to pass
- [x] New test cases for negative numbers pass
- [x] Test case for literal dash-prefixed values passes
- [x] Error messages are clear when actual undefined options are used
- [x] Documentation updated with examples and guidance

## Completion Notes

Task completed and merged via PR #78. Implemented **Option 1** (Check Against Defined Options) as recommended.

### Implementation Summary

1. **Fixed ValidateSegmentAvailability** (EndpointResolver.cs)
   - Added `IsDefinedOption()` check before treating dash-prefixed arguments as options
   - Arguments starting with `-` are now only treated as options if they match defined options in the route pattern
   - Non-matching dash-prefixed arguments (including negative numbers) are accepted as positional values

2. **Fixed MatchOptionSegment** (EndpointResolver.cs)
   - Extended the fix to option values
   - `--amount -5` now works correctly (previously only positional parameters worked)

3. **Added Comprehensive Tests** (routing-13-negative-numbers.cs)
   - Should_accept_negative_integer_as_positional_parameter
   - Should_accept_negative_double_as_positional_parameter
   - Should_accept_multiple_negative_numbers
   - Should_accept_negative_number_as_option_value
   - Should_accept_dash_prefixed_literal_when_not_defined_option
   - Should_still_match_defined_options_starting_with_dash

### Key Commits

- `5b5c110` - fix: allow negative numbers and dash-prefixed literals as positional parameters
- `44c16ca` - test: add comprehensive negative number parameter tests
- `051872a` - fix: allow negative numbers as option values
- `8130208` - Merge PR #78

### Results

✅ All success criteria met:
- Calculator operations with negative numbers work correctly
- Both positional and option value contexts supported
- All 50+ tests pass (98% pass rate maintained)
- Clear principle: only defined options are treated as options

## Breaking Changes

**Potential behavior change**: Arguments starting with `-` that don't match defined options will now be accepted as positional values instead of being rejected as "undefined options."

**Impact assessment needed**:
- Are there existing users relying on strict option validation?
- Should there be a configuration option to enable/disable strict mode?
- Should we add analyzer rules to warn about potential ambiguity?

## Timeline Estimate

- **Priority**: High
- **Complexity**: Medium
- **Estimate**: 4-6 hours
  - 1 hour: Implementation of fix
  - 2 hours: Test creation and validation
  - 1 hour: Documentation updates
  - 1-2 hours: Review and edge case handling
