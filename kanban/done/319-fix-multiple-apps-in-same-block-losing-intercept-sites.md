# Fix Multiple Apps in Same Block - Route Isolation and Intercept Sites

## Description

When a single code block contains multiple `NuruCoreApp` instances, two problems occur:

1. **Lost intercept sites**: Only the first app's intercept site is captured; subsequent apps throw "RunAsync was not intercepted" at runtime.

2. **Shared routes**: All routes from all apps are merged together, so any app can match any route - even routes defined in a different app instance.

### Root Cause Analysis

The architecture already correctly tracks per-app data:

1. **`DslInterpreter.Interpret`** returns `IReadOnlyList<AppModel>` with one model per app
2. Each `IrAppBuilder` correctly tracks its own routes and intercept sites separately
3. `FinalizeModel()` packages each app's routes and intercept sites into separate `AppModel` instances

**The problems are:**

1. **`AppExtractor.Extract`** (line 84) returns only `models[0]`, discarding all other models:
   ```csharp
   return models[0] with { UserUsings = userUsings };
   ```

2. **`CombineModels`** in `nuru-generator.cs` merges ALL routes from ALL models into ONE combined model:
   ```csharp
   // Collect all routes
   allRoutes.AddRange(model.Routes);
   ```

3. **`InterceptorEmitter`** generates ONE interceptor method with ALL routes, so any `RunAsync` call can match any route.

## Solution Implemented

### Per-App Interceptor Methods

Generate a **separate interceptor method for each app instance**, each with its own routes:

```csharp
// Generated code - one method per app
[InterceptsLocation(1, "data_for_app1_runasync")]
public static async Task<int> RunAsync_Intercepted_0(this NuruCoreApp app, string[] args)
{
  // Only app1's routes here
  if (routeArgs is ["deploy", var env]) { ... }  // required
  return 1; // no match
}

[InterceptsLocation(1, "data_for_app2_runasync")]
public static async Task<int> RunAsync_Intercepted_1(this NuruCoreApp app, string[] args)
{
  // Only app2's routes here
  if (routeArgs is ["deploy"]) { ... }  // optional
  if (routeArgs is ["deploy", var env]) { ... }  // optional with value
  return 1; // no match
}
```

### Changes Made

1. **`AppExtractor.Extract`** - Now finds the correct model for each `RunAsync` by matching intercept sites

2. **`NuruGenerator`** - Created new `GeneratorModel` that preserves per-app route isolation instead of merging routes

3. **`InterceptorEmitter`** - Generates per-app interceptor methods, each with only that app's routes

4. **New model `GeneratorModel`** - Holds multiple `AppModel`s with shared metadata for helper method generation

## Checklist

- [x] Modify `AppExtractor.Extract` to return correct model for each RunAsync
- [x] Create `GeneratorModel` to preserve per-app route isolation
- [x] Modify `InterceptorEmitter` to generate per-app interceptor methods
- [x] Handle method name conflicts (suffix with index)
- [x] Handle shared helper methods appropriately
- [x] Test route isolation (routes don't leak between apps)
- [x] Verify `routing-03-optional-parameters.cs` tests pass (was 5/9 fail, now 9/9 pass)
- [ ] Fix pre-existing catch-all parameter bug (see task #332)
- [ ] Verify full solution build succeeds

## Results

**Before fix:** `routing-03-optional-parameters.cs` had 5/9 tests failing because routes leaked between test methods.

**After fix:** All 9 tests pass! Each app instance now has isolated routes.

## Notes

- Discovered a pre-existing catch-all parameter bug during testing - tracked as task #332
- The catch-all bug blocks full solution build but is unrelated to #319
- Related to task #318 (architectural refactor) - this fix simplifies #318
