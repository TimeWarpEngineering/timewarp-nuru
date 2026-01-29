# Investigate route matching issue: auth status incorrectly matches workfile search

## Description

User reported error when running `ccc1 auth status`:
```
Error: Invalid value '(missing)' for option '--facility'. Expected: int
```

The route `auth status` has NO parameters or options, but it's somehow matching the `workfile search` route which does have a `--facility` option.

## Checklist

- [ ] Reproduce the issue with `ccc1 auth status`
- [ ] Analyze generated route matching code to find the bug
- [ ] Determine why `workfile search` (line 654: `if (routeArgs.Length >= 2)`) matches before `auth status` (line 2156: exact pattern match)
- [ ] Review route ordering logic in source generator
- [ ] Fix route matching to prioritize exact pattern matches over length-based matches
- [ ] Add test case for this scenario
- [ ] Verify all other routes don't have similar issues

## Notes

### Investigation findings (2026-01-27):

**Evidence:**
- User's error shows: `Error: Invalid value '(missing)' for option '--facility'. Expected: int`
- This error comes from line 700 in NuruGenerated.g.cs (workfile search route)
- The `auth status` route (lines 2145-2174) uses exact pattern matching: `if (routeArgs is ["auth", "status"])`
- The `workfile search` route (lines 654-936) uses length check first: `if (routeArgs.Length >= 2)`

**Current behavior:**
- Both `dotnet run -- auth status` and installed `ccc1 auth status` work correctly (tested 2026-01-27)
- Unable to reproduce the error

**Possible causes:**
1. Stale generated code (resolved by `dotnet clean && dotnet build`)
2. Specific argument combination not tested
3. Race condition or intermittent issue
4. Config override args (--key=value) interfering with route matching

**Route matching flow:**
```csharp
// Line 654: workfile search checks length first
if (routeArgs.Length >= 2) {
  // Process ALL options (-f, --facility, -s, --status, etc.)
  // Line 912-913: Then checks if positional args match "workfile" and "search"
  if (__positionalArgs_42[0] != "workfile") goto route_skip_42;
  if (__positionalArgs_42[1] != "search") goto route_skip_42;
  // Execute workfile search handler
}
route_skip_42:;

// Line 2156: auth status - should match here
if (routeArgs is ["auth", "status"]) {
  // Execute auth status handler
}
```

**Key question:** Why would option parsing (lines 660-903) find a `--facility` value when `routeArgs = ["auth", "status"]`?

The option parsing loops through `routeArgs[__i]` checking for flags like `--facility`, `-f`, etc. With args `["auth", "status"]`:
- Index 0: "auth" - doesn't start with "--facility", "-f=", or equal "--facility"/"-f"
- Index 1: "status" - **WAIT** - does "status" somehow trigger the `-s` alias for `--status`?

**AHA! Found the bug:**

Line 722-733 checks for `--status` or `-s`:
```csharp
if (routeArgs[__i] == "--status" || routeArgs[__i] == "-s")
{
  __status_flagFound_42 = true;
  __consumed_42.Add(__i);
  // Check if next arg exists, is before end-of-options, and is NOT a defined option
  if (__i + 1 < __endOfOptions_42 && !__optionForms_42.Contains(routeArgs[__i + 1]))
  {
    status = routeArgs[__i + 1];
    __consumed_42.Add(__i + 1);
  }
  break;
}
```

But "status" != "-s", so this shouldn't match either.

**Need to:**
1. Add debug logging to generated code to see what's actually matching
2. Test with verbose output to see route matching logic
3. Consider if there's a case sensitivity issue
4. Check if config override detection (lines 218-234) is incorrectly marking "auth" or "status" as consumed

**Potential fix:**
Routes with options should be checked AFTER routes with exact pattern matches. The generator should order routes by specificity:
1. Exact pattern matches (highest priority)
2. Pattern matches with required parameters
3. Pattern matches with optional parameters
4. Catch-all patterns (lowest priority)

### Root cause hypothesis:

The issue is likely in the route ordering. The generator places `workfile search` (length-based check) before `auth status` (exact pattern check), so when processing options, even though none match, something goes wrong in the positional arg reconstruction.

Need to trace through lines 903-913 to see how `__positionalArgs_42` is built after option parsing.
