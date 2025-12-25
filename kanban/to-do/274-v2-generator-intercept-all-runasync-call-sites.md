# V2 Generator: Intercept All RunAsync Call Sites

## Description

Currently the V2 source generator only intercepts the **first** `RunAsync` call site found in the compilation. This causes test failures when multiple test methods each create their own app and call `RunAsync`.

The generator should emit `[InterceptsLocation]` attributes for **all** `RunAsync` call sites, not just the first one.

## Parent

#273 V2 Generator Design Issue: Lambda Body Capture for Delegate Handlers

## Problem

In `nuru-generator.cs`, the `CombineModels` method stops at the first `AppModel`:

```csharp
foreach (AppModel? model in appModels)
{
    if (model is not null)
    {
        baseModel = model;
        break;  // <-- STOPS AT FIRST
    }
}
```

This results in only one `[InterceptsLocation]` attribute being emitted.

## Design Decision

**All apps in the compilation will share the same routes.** This is acceptable because:
- Production apps will **never** have multiple `RunAsync` calls
- This approach primarily benefits test scenarios
- Each test method's `RunAsync` call will be intercepted and routed to the same generated code

## Implementation Plan

### Step 1: Update `AppModel` to hold multiple intercept sites

**File:** `generators/models/app-model.cs`

Change from:
```csharp
InterceptSiteModel InterceptSite
```
To:
```csharp
ImmutableArray<InterceptSiteModel> InterceptSites
```

### Step 2: Update `AppModelBuilder` to handle multiple sites

**File:** `generators/extractors/builders/app-model-builder.cs`

- Change `InterceptSite` property to `InterceptSites` (collection)
- Add method to add sites: `AddInterceptSite(InterceptSiteModel site)`
- Update `Build()` to use the collection

### Step 3: Update `CombineModels` to collect all sites

**File:** `generators/nuru-generator.cs`

Collect ALL intercept sites from all AppModels instead of taking just the first.

### Step 4: Update `InterceptorEmitter` to emit multiple attributes

**File:** `generators/emitters/interceptor-emitter.cs`

Loop over all sites when emitting:
```csharp
foreach (InterceptSiteModel site in model.InterceptSites)
{
    sb.AppendLine($"  {site.GetAttributeSyntax()}");
}
```

## Checklist

- [ ] Update `AppModel` record: `InterceptSite` -> `InterceptSites` (ImmutableArray)
- [ ] Update `AppModelBuilder` to handle collection of sites
- [ ] Update `CombineModels` in `NuruGenerator` to collect all sites
- [ ] Update `InterceptorEmitter` to emit multiple `[InterceptsLocation]` attributes
- [ ] Verify all 4 minimal intercept tests pass

## Files to Modify

| File | Change |
| ---- | ------ |
| `generators/models/app-model.cs` | `InterceptSite` -> `InterceptSites` |
| `generators/extractors/builders/app-model-builder.cs` | Handle collection |
| `generators/nuru-generator.cs` | Collect all sites in `CombineModels` |
| `generators/emitters/interceptor-emitter.cs` | Loop over sites |

## Expected Result

Generated code will have multiple `[InterceptsLocation]` attributes:

```csharp
[InterceptsLocation(1, "...site1...")]
[InterceptsLocation(1, "...site2...")]
[InterceptsLocation(1, "...site3...")]
[InterceptsLocation(1, "...site4...")]
public static async Task<int> RunAsync_Intercepted(...)
{
    // All routes from all apps merged here
}
```

## Test Validation

Run `tests/timewarp-nuru-core-tests/routing/temp-minimal-intercept-test.cs`:
- Currently: 1 pass, 3 fail
- Expected: 4 pass

## Notes

This task was discovered during V2 cleanup when the first test passed but subsequent tests failed with:
```
InvalidOperationException: RunAsync was not intercepted. Ensure the source generator is enabled.
```
