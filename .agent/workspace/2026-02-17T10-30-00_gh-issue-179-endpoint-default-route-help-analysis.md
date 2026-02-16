# GitHub Issue #179 Analysis: `[NuruRoute("")]` Endpoint Routes Intercept --help

**Date:** 2026-02-17
**Issue:** [#179](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/179)
**Status:** Open
**Priority:** High (User Experience Bug)

---

## Executive Summary

When using `[NuruRoute("")]` (empty/default route) with the Endpoint DSL, running `myapp --help` routes to the handler instead of showing built-in help. The Bug #403 fix for built-in flag skipping is only emitted for fluent `Map("")` routes, not for endpoint-class routes. This is a code path inconsistency in the source generator where the skip guard logic is applied differently based on how the route was defined.

---

## Scope

This analysis covers:
1. Root cause of the inconsistency between fluent and endpoint routes
2. The generated code path for built-in flag skipping
3. Relevant source files and emission logic
4. Missing test coverage
5. Recommended fix approach

---

## Methodology

1. Fetched GitHub issue details
2. Read the source generator emitter files:
   - `route-matcher-emitter.cs` - Contains the skip guard logic
   - `interceptor-emitter.cs` - Orchestrates route emission
   - `route-help-emitter.cs` - Per-route help generation
   - `endpoint-extractor.cs` - Endpoint route extraction
3. Examined existing tests in `help-02-default-route-help.cs`
4. Analyzed the `BuiltInFlags` constants and pattern matching

---

## Findings

### 1. Root Cause: Conditional Skip Guard Logic

The built-in flag skip guard is emitted in `EmitComplexMatch()` at lines 202-213 in `route-matcher-emitter.cs`:

```csharp
// Bug #403: Routes with no literals and no required params (minPositionalArgs == 0) generate
// "routeArgs.Length >= 0" which is always true, intercepting built-in flags like --help.
// Skip built-in flags for these "match everything" routes, UNLESS the route explicitly
// maps a built-in flag (e.g., Map("--version") should be allowed to override).
bool hasNoLiterals = !route.PositionalMatchSegments.Any();
bool isBuiltInFlagRoute = route.OriginalPattern is "--help" or "-h" or "--version" or "--capabilities";
if (minPositionalArgs == 0 && hasNoLiterals && !isBuiltInFlagRoute)
{
  sb.AppendLine("      // Skip built-in flags for routes with no literals (default/options-only)");
  sb.AppendLine($"      if (routeArgs is {BuiltInFlags.PatternMatchExpression})");
  sb.AppendLine($"        goto route_skip_{routeIndex};");
}
```

**The issue:** This skip logic only executes for **complex routes** (routes with options, catch-all, or optional positional params). For **simple routes** (literals and required parameters only), the `EmitSimpleMatch()` method is used instead, which does NOT contain this skip guard.

### 2. Route Classification Logic

In `RouteMatcherEmitter.Emit()` at lines 55-76:

```csharp
// Determine the matching strategy based on route complexity
if (route.HasOptions || route.HasCatchAll || route.HasOptionalPositionalParams)
{
  EmitComplexMatch(sb, route, routeIndex, ...);
  // ...
}
else
{
  EmitSimpleMatch(sb, route, routeIndex, ...);
  // ...
}
```

### 3. Why Fluent Routes Work But Endpoint Routes Don't

**Fluent `Map("")` routes:**
- An empty pattern `""` creates a route with no segments
- When combined with options (e.g., `Map("").WithOption("verbose")`), the route has `HasOptions = true`
- This triggers `EmitComplexMatch()` which includes the skip guard

**Endpoint `[NuruRoute("")]` routes:**
- An empty pattern `""` is valid (per `ValidateRoutePattern` in `endpoint-extractor.cs` line 155-157)
- Options are defined via `[Option]` properties
- However, if the endpoint only has options and no parameters, the route might be classified differently

**Key insight:** Looking at `endpoint-extractor.cs` lines 63-68:
```csharp
// For validated patterns, we know they are either empty or single literal
// So we can safely use the pattern as-is (no need to parse for parameters/options)
ImmutableArray<SegmentDefinition> patternSegments = string.IsNullOrEmpty(pattern)
  ? []
  : [new LiteralDefinition(0, pattern)];

// Merge segments (pattern segments first, then property segments that aren't duplicates)
ImmutableArray<SegmentDefinition> mergedSegments = MergeSegments(patternSegments, segments);
```

The segments from properties (via `[Option]` attributes) are merged and should result in `HasOptions = true`. If the endpoint has options, it should go through `EmitComplexMatch()`.

### 4. The Actual Bug Location

After further analysis, the issue is that for an endpoint with:
- `[NuruRoute("")]` (empty pattern)
- `[Option]` properties

The route WILL have `HasOptions = true` and WILL go through `EmitComplexMatch()`. However, the skip guard condition checks:

```csharp
bool hasNoLiterals = !route.PositionalMatchSegments.Any();
```

For an endpoint with `[NuruRoute("")]`:
- `GroupPrefix` is likely null (unless in a group)
- Pattern segments: `[]` (empty pattern creates no literals)
- `PositionalMatchSegments` returns only group prefix literals + pattern literals + end-of-options

If the pattern is empty and there's no group prefix, `PositionalMatchSegments` will be empty, so `hasNoLiterals = true`.

**The skip guard SHOULD be emitted.** So why isn't it working?

### 5. Further Investigation Needed

Let me check if the endpoint route is being classified correctly:

Looking at `RouteDefinition.PositionalMatchSegments` (lines 151-180 in `route-definition.cs`):
- Iterates group prefix (if any)
- Iterates segments, adding `LiteralDefinition` and `EndOfOptionsSeparatorDefinition`
- **Does NOT add `OptionDefinition` segments**

This is correct behavior - options aren't positional.

**Hypothesis:** The skip guard IS being emitted, but there may be another issue:
1. The route matching order
2. Simple vs complex classification
3. The endpoint may have a parameter that isn't being handled correctly

### 6. Test Gap Analysis

The existing tests in `help-02-default-route-help.cs` only test **fluent API** routes:

```csharp
.Map("").WithHandler(() => "default-handler-executed").WithDescription("Default route").AsQuery().Done()
```

**No tests exist for:**
```csharp
[NuruRoute("")]
internal sealed class DefaultQuery : IQuery<Unit>
{
  [Option("verbose", "v")]
  public bool Verbose { get; set; }
  // ...
}
```

### 7. Actual Bug Confirmation

After analyzing the code flow more carefully, I believe the issue is:

**For endpoint routes with empty pattern AND options:**
1. `patternSegments = []` (empty pattern)
2. Property segments are added via `ExtractSegmentsFromProperties()`
3. `mergedSegments` contains only `[OptionDefinition]` segments
4. `route.HasOptions = true` -> Uses `EmitComplexMatch()`
5. Skip guard should be emitted

**However**, looking at the skip guard condition more carefully:

```csharp
int minPositionalArgs = route.PositionalMatchSegments.Count() + route.Parameters.Count(p => !p.IsOptional && !p.IsCatchAll);

if (minPositionalArgs == 0 && hasNoLiterals && !isBuiltInFlagRoute)
```

If the endpoint has **no parameters** (only options), then:
- `route.Parameters` is empty
- `minPositionalArgs = 0`
- `hasNoLiterals = true` (empty pattern, no group prefix)
- Skip guard IS emitted

**So the skip guard SHOULD be emitted correctly!**

### 8. The Real Issue - Simple vs Complex Classification

Wait - let me re-check `EmitSimpleMatch()`. If an endpoint has:
- Empty pattern `""`
- Options but NO parameters

Then:
- `route.HasOptions = true` -> `EmitComplexMatch()` is used
- Skip guard is emitted

But what if the endpoint has:
- Empty pattern `""`  
- A **parameter** with `[Parameter]` attribute

Then:
- `route.HasOptions = false`
- `route.HasCatchAll = false` (unless catch-all param)
- `route.HasOptionalPositionalParams = false` (unless optional param)
- Uses `EmitSimpleMatch()` which has NO skip guard!

**This is the bug!** When an endpoint with `[NuruRoute("")]` has a parameter, it goes through `EmitSimpleMatch()` which lacks the skip guard.

---

## Recommended Fix

### Option A: Add Skip Guard to EmitSimpleMatch()

Add the same skip logic to `EmitSimpleMatch()` for routes that match everything:

```csharp
private static void EmitSimpleMatch(...)
{
  string pattern = BuildListPattern(route, routeIndex);

  // BUG #179: Add skip guard for routes that match everything
  bool isEmptyPattern = string.IsNullOrEmpty(route.OriginalPattern) && 
                        string.IsNullOrEmpty(route.GroupPrefix);
  if (isEmptyPattern)
  {
    sb.AppendLine($"    if (routeArgs is {BuiltInFlags.PatternMatchExpression})");
    sb.AppendLine($"      goto route_skip_{routeIndex};");
  }

  sb.AppendLine($"    if (routeArgs is {pattern})");
  sb.AppendLine("    {");
  // ...
}
```

**Problem:** This approach adds a goto label at the end of `EmitSimpleMatch()`, but `EmitSimpleMatch()` uses `if` blocks, not `goto` labels. The pattern is different.

### Option B: Always Use Complex Match for Empty/Default Routes

Force routes with empty pattern to use `EmitComplexMatch()`:

```csharp
// In RouteMatcherEmitter.Emit()
bool isEmptyDefaultRoute = string.IsNullOrEmpty(route.OriginalPattern) && 
                           string.IsNullOrEmpty(route.GroupPrefix);
if (route.HasOptions || route.HasCatchAll || route.HasOptionalPositionalParams || isEmptyDefaultRoute)
{
  EmitComplexMatch(sb, route, routeIndex, ...);
}
else
{
  EmitSimpleMatch(sb, route, routeIndex, ...);
}
```

**Pros:**
- Minimal code change
- Uses existing skip guard logic
- Consistent behavior for all "match everything" routes

**Cons:**
- Slightly more complex matching code for simple default routes

### Option C: Add Special Handling in EmitSimpleMatch Pattern

Modify `BuildListPattern()` or `EmitSimpleMatch()` to handle the edge case:

```csharp
private static void EmitSimpleMatch(...)
{
  string pattern = BuildListPattern(route, routeIndex);

  // BUG #179 & #403: Empty pattern shouldn't intercept built-in flags
  bool shouldSkipBuiltInFlags = 
    string.IsNullOrEmpty(route.OriginalPattern) && 
    string.IsNullOrEmpty(route.GroupPrefix) &&
    !route.Parameters.Any(p => !p.IsOptional);

  if (shouldSkipBuiltInFlags)
  {
    // For empty pattern with only required params, check built-in flags first
    sb.AppendLine("    // Skip built-in flags for empty pattern routes");
    sb.AppendLine($"    if (routeArgs is {BuiltInFlags.PatternMatchExpression})");
    sb.AppendLine("    {");
    // Fall through to no-match handling - built-in flags are emitted after user routes
    sb.AppendLine("    }");
    sb.AppendLine("    else");
  }

  sb.AppendLine($"    if (routeArgs is {pattern})");
  // ... rest of the method
}
```

This changes the structure but handles the case.

### Recommended Approach: Option B

Option B is the cleanest fix with minimal risk:

1. Modify the condition in `RouteMatcherEmitter.Emit()` to classify empty/default routes as complex
2. This ensures the existing skip guard in `EmitComplexMatch()` is used
3. Existing tests will pass (fluent routes already use complex matching when they have options)
4. Add new tests for endpoint routes with empty pattern

---

## Implementation Checklist

1. **Code Change:**
   - [ ] Modify `RouteMatcherEmitter.Emit()` to detect empty default routes
   - [ ] Add `isEmptyDefaultRoute` check to the routing strategy condition

2. **Test Coverage:**
   - [ ] Add test: `[NuruRoute("")]` endpoint with options should show help on `--help`
   - [ ] Add test: `[NuruRoute("")]` endpoint with parameter should show help on `--help`
   - [ ] Add test: `[NuruRoute("")]` endpoint with catch-all should show help on `--help`
   - [ ] Add test: `[NuruRoute("")]` endpoint executes with empty args `[]`
   - [ ] Add test: `[NuruRoute("")]` endpoint executes with positional args `["value"]`

3. **New Test File Location:**
   - Create `tests/timewarp-nuru-tests/help/help-03-endpoint-default-route-help.cs`

---

## References

- **GitHub Issue:** [#179](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/179)
- **Related Bug:** #403 (fixed for fluent routes only)
- **Source Files:**
  - `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs` (lines 55-76, 202-213)
  - `source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs` (lines 63-68, 153-157)
  - `source/timewarp-nuru-analyzers/generators/models/route-definition.cs` (lines 151-180)
- **Test File:** `tests/timewarp-nuru-tests/help/help-02-default-route-help.cs`