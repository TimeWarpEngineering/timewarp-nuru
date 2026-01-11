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

### Reproduction - Lost Intercept Sites

```csharp
{
  NuruCoreApp app1 = NuruApp.CreateBuilder([])
    .Map("cmd1").WithHandler(() => "one").AsQuery().Done()
    .Build();
  await app1.RunAsync(["cmd1"]);  // Intercept site captured

  NuruCoreApp app2 = NuruApp.CreateBuilder([])
    .Map("cmd2").WithHandler(() => "two").AsQuery().Done()
    .Build();
  await app2.RunAsync(["cmd2"]);  // Intercept site LOST - throws at runtime!
}
```

### Reproduction - Shared Routes (More Subtle)

```csharp
// Test file with multiple test methods, each with its own app
public static async Task Test_required_parameter()
{
  NuruCoreApp app = NuruApp.CreateBuilder([])
    .Map("deploy {env}").WithHandler((string env) => $"env:{env}").AsCommand().Done()
    .Build();
  await app.RunAsync(["deploy", "prod"]);  // Works
  await app.RunAsync(["deploy"]);          // Should fail - missing required param
                                           // BUT matches optional route from other test!
}

public static async Task Test_optional_parameter()
{
  NuruCoreApp app = NuruApp.CreateBuilder([])
    .Map("deploy {env?}").WithHandler((string? env) => $"env:{env}").AsCommand().Done()
    .Build();
  await app.RunAsync(["deploy"]);  // Works - optional param
}
```

In this case, `Test_required_parameter`'s second `RunAsync(["deploy"])` incorrectly matches the optional route from `Test_optional_parameter` because all routes are merged!

### Affected Files

- `source/timewarp-nuru-analyzers/generators/extractors/app-extractor.cs` - Returns only `models[0]`
- `source/timewarp-nuru-analyzers/generators/nuru-generator.cs` - `CombineModels` merges all routes
- `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` - Emits one method with all routes
- `samples/_testing/test-colored-output.cs` - Test 5 has this pattern
- `tests/timewarp-nuru-core-tests/routing/*.cs` - Many test files affected by route sharing

## Solution Design

### Option A: Per-App Interceptor Methods (Recommended)

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

Each `[InterceptsLocation]` attribute targets a specific `RunAsync` call site, and each method only contains routes for that specific app.

### Implementation Steps

1. **Modify `AppExtractor.Extract`** to return ALL models (not just `models[0]`)
   - Change return type or create new method that returns `IReadOnlyList<AppModel>`
   - Attach `UserUsings` to all models

2. **Modify `NuruGenerator` pipeline**
   - Don't combine routes in `CombineModels` - keep each `AppModel` separate
   - Pass list of `AppModel`s to emitter instead of one combined model

3. **Modify `InterceptorEmitter`**
   - Generate multiple interceptor methods (one per `AppModel`)
   - Each method gets its own `[InterceptsLocation]` attributes (for that app's `RunAsync` calls)
   - Each method only contains routes for that specific app
   - Suffix method names to avoid conflicts (e.g., `RunAsync_Intercepted_0`, `RunAsync_Intercepted_1`)

4. **Handle shared infrastructure**
   - Helper methods (PrintHelp, PrintVersion, etc.) can remain shared
   - Service fields can remain shared (or be scoped per-app if needed)
   - Command classes may need per-app scoping if routes have same patterns

## Checklist

- [ ] Modify `AppExtractor.Extract` to return all models
- [ ] Update `NuruGenerator.CombineModels` to preserve per-app route isolation
- [ ] Modify `InterceptorEmitter` to generate per-app interceptor methods
- [ ] Handle method name conflicts (suffix with index)
- [ ] Handle shared helper methods appropriately
- [ ] Add test case for multiple apps in same block (intercept site test)
- [ ] Add test case for route isolation (routes don't leak between apps)
- [ ] Fix `test-colored-output.cs` Test 5 to work properly
- [ ] Verify all `RunAsync` calls are intercepted
- [ ] Verify routes don't leak between app instances
- [ ] Update routing tests that were blocked by this issue

## Notes

- Discovered while investigating intercept site deduplication
- Related to task #318 (architectural refactor) - this fix may simplify #318
- Current workaround for intercept sites: Put each app in its own scope block `{ }`
- Current workaround for route isolation: None - must wait for fix or use unique route patterns
- The deduplication fix (DistinctBy) doesn't cause this - it was pre-existing
- Blocking multiple routing tests that need rewriting (routing-03 through routing-18, etc.)
