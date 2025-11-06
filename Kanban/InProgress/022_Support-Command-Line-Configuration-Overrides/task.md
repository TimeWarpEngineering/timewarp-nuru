# 022: Support Command-Line Configuration Overrides

## Description

Enable ASP.NET Core-style command-line configuration overrides by filtering arguments containing colons before passing them to the route resolver. This resolves GitHub Issue #75 where users expect `--Section:Key=value` syntax to override configuration values without interfering with route matching.

**Issue Reference:** [GitHub Issue #75](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/75)

## Requirements

1. **Argument Filtering**: Args containing `:` should be excluded from route matching in `EndpointResolver`
2. **Configuration Pass-Through**: All original args (including those with colons) must still reach `AddCommandLine(args)` in the configuration system
3. **Backward Compatibility**: Existing route patterns and functionality must remain unchanged
4. **Documentation**: Users should understand the colon filtering behavior and limitations

## Problem Statement

Currently, when users try to use configuration overrides like:
```bash
./app.cs run --FooOptions:Url=https://override.example.com
```

The route resolver treats `--FooOptions:Url=...` as a potential route option and fails to match, preventing the command from executing. However, the configuration system (via `AddCommandLine(args)`) would correctly handle these overrides if they could reach it.

## Solution Approach

Filter arguments containing colons before passing to `EndpointResolver.Resolve()` in `NuruApp.RunAsync()`:

```csharp
// Separate route args from config override args
string[] routeArgs = args.Where(arg => !arg.Contains(':')).ToArray();

// Route matching uses filtered args
EndpointResolutionResult result = EndpointResolver.Resolve(routeArgs, Endpoints, TypeConverterRegistry, logger);
```

**Why this works:**
- Route patterns cannot have colons in option names (lexer doesn't allow it)
- Route options like `--force`, `--dry-run` don't contain colons
- Configuration overrides like `--Section:Key=value` always contain colons
- The full args array is still passed to `AddConfiguration(args)` separately

## Checklist

### Implementation
- [x] Create sample to demonstrate expected behavior (`command-line-overrides.cs`)
- [x] Modify `NuruApp.RunAsync()` to filter args before calling `EndpointResolver.Resolve()`
- [x] Add helper method or inline logic to separate route args from config args
- [x] Ensure original args array is preserved for configuration system

### Testing
- [x] Test that `command-line-overrides.cs` sample works with `--Section:Key=value` syntax
- [x] Verify existing route patterns with options still work correctly
- [x] Add test cases for mixed route options and config overrides
- [x] Test edge cases (arg values containing colons, etc.)

### Documentation
- [x] Update `Samples/Configuration/Overview.md` to document the new sample
- [ ] Document the colon filtering behavior in architecture docs
- [ ] Add note about limitation (can't use colons in custom route option names)
- [ ] Update GitHub Issue #75 with solution and examples

## Notes

### Key Insights

1. **Two Separate Parsing Contexts**:
   - **Build-time**: Lexer/Parser process route PATTERNS (where `:` means type constraint like `{age:int}`)
   - **Run-time**: EndpointResolver matches runtime ARGS (where `:` in args is just a character)

2. **Perfect Heuristic**: Since the lexer doesn't allow colons in identifiers, no valid route pattern can have an option like `--my:option`. Therefore, any arg with a colon is guaranteed to be a configuration override, not a route option.

3. **Configuration System Already Works**: `AddCommandLine(args)` from Microsoft.Extensions.Configuration already handles the colon syntax correctly. We just need to prevent route matching from interfering.

### Related Files

- `Source/TimeWarp.Nuru/NuruApp.cs` - Line 62: Where `EndpointResolver.Resolve()` is called
- `Source/TimeWarp.Nuru/NuruAppBuilder.cs` - Line 144: Where `AddCommandLine(args)` is called
- `Samples/Configuration/command-line-overrides.cs` - Demo sample that will work after fix
- `.agent/workspace/claude/issue-75-analysis.md` - Detailed technical analysis

### Potential Future Enhancements

- Consider making the filtering strategy configurable
- Add explicit API like `AddConfiguration(args, filterRouteArgs: true)`
- Provide diagnostic logging when args are filtered
- Support custom filtering predicates for advanced scenarios

## Implementation Notes

[To be filled in during implementation]
