# Fix Multiple Apps in Same Block Losing Intercept Sites

## Description

When a single code block contains multiple `NuruCoreApp` instances (each with their own `RunAsync` call), only the **first** app's intercept site is captured. Subsequent apps' intercept sites are lost.

### Root Cause

In `AppExtractor.Extract` (line 84):
```csharp
return models[0] with { UserUsings = userUsings };
```

When `DslInterpreter.Interpret` returns multiple `AppModel`s (one per app in the block), `Extract` only returns `models[0]`, discarding the rest.

### Reproduction

```csharp
{
  NuruCoreApp app1 = NuruApp.CreateBuilder([])
    .Map("cmd1").WithHandler(() => "one").AsQuery().Done()
    .Build();
  await app1.RunAsync(["cmd1"]);  // Intercept site captured

  NuruCoreApp app2 = NuruApp.CreateBuilder([])
    .Map("cmd2").WithHandler(() => "two").AsQuery().Done()
    .Build();
  await app2.RunAsync(["cmd2"]);  // Intercept site LOST!
}
```

Running `app2.RunAsync` throws "RunAsync was not intercepted" at runtime.

### Affected Files
- `source/timewarp-nuru-analyzers/generators/extractors/app-extractor.cs` - Line 84 returns only `models[0]`
- `samples/_testing/test-colored-output.cs` - Test 5 has this pattern

## Checklist

- [ ] Modify `AppExtractor.Extract` to return all models or find correct model
- [ ] Option A: Return model containing the specific RunAsync's intercept site
- [ ] Option B: Return all models and update `CombineModels` to handle multiple
- [ ] Add test case for multiple apps in same block
- [ ] Fix `test-colored-output.cs` Test 5 to work properly
- [ ] Verify all RunAsync calls are intercepted

## Notes

- Discovered while investigating intercept site deduplication
- Related to task #318 (architectural refactor would also fix this)
- Current workaround: Put each app in its own scope block `{ }`
- The deduplication fix (DistinctBy) doesn't cause this - it was pre-existing
