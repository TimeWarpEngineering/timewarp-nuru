# RunReplAsync is not intercepted by source generator

## Description

`RunReplAsync()` is a public API method on `NuruCoreApp` that is supposed to be intercepted by the source generator, but it is **never intercepted** - not even when called inline.

### Works (inline):
```csharp
NuruCoreApp app = NuruApp.CreateBuilder([])
  .Map("hello")
    .WithHandler(() => "Hello!")
    .AsQuery()
    .Done()
  .AddRepl()
  .Build();

await app.RunReplAsync(); // ✅ NOW WORKS - intercepted when inline
```

### Fails (cross-method/static field):
```csharp
private static NuruCoreApp? App;

public static async Task Setup()
{
  App = NuruApp.CreateBuilder([])...Build();  // App created here
}

public static async Task SomeTest()
{
  await App!.RunReplAsync();  // ❌ FAILS - generator can't trace through static field
}
```

## Architecture Refactoring (Completed)

### Phase 1: Generalized `InterceptSitesByMethod` dictionary

Replaced separate fields with a dictionary to support N entry point methods:

```csharp
// Before
ImmutableArray<InterceptSiteModel> InterceptSites,
ImmutableArray<InterceptSiteModel> ReplInterceptSites,

// After
ImmutableDictionary<string, ImmutableArray<InterceptSiteModel>> InterceptSitesByMethod,
```

### Phase 2: Build()-Based Extraction

Changed the generator pipeline to start from `Build()` calls instead of entry points:

**Before (flawed):**
1. Find RunAsync calls → extract AppModel
2. Find RunReplAsync calls → extract AppModel
3. Deduplicate by routes (BROKEN - different apps with same routes got merged!)

**After (correct):**
1. Find Build() calls → extract AppModel with all entry points
2. Deduplicate by BuildLocation (each Build() = one unique app)

Benefits:
- No duplicate extractions when app has multiple entry points
- Correct app identity (BuildLocation) instead of routes
- Scales to N entry points (just add locator + emitter)
- Clean - no guessing which app an entry point belongs to

## Files Modified

| File | Change |
|------|--------|
| `models/app-model.cs` | Added `BuildLocation` field, `InterceptSitesByMethod` dictionary |
| `ir-builders/ir-app-builder.cs` | `AddInterceptSite(methodName, site)` |
| `ir-builders/abstractions/iir-app-builder.cs` | Updated interface |
| `interpreter/dsl-interpreter.cs` | Pass method name |
| `nuru-generator.cs` | Extract from Build() calls, deduplicate by BuildLocation |
| `emitters/interceptor-emitter.cs` | Access dictionary by method name |
| `extractors/app-extractor.cs` | New `ExtractFromBuildCall()` method |

## Test Results

**Before refactoring:** 706 passed, 204 failed
**After refactoring:** 782 passed, 128 failed

Key improvements:
- BasicMatching: 9/9 (was 8/9)
- OptionMatching: 31/31 (was 28/31)
- SessionLifecycle: 11/11 (was 1/11)
- HistoryManagement: 8/8 (was 2/8)
- BuiltinCommands: 8/8 (was 0/8)
- NuruAppIntegration: 8/8 (was 1/8)

## Remaining Issue: Cross-Method Tracking

The remaining 128 failures are tests using the `Setup()` pattern where:
1. App is created in `Setup()` and stored in a static field
2. `RunReplAsync()` is called on the static field in test methods

The generator can trace within a single method/block but cannot trace through:
- Static fields assigned in different methods
- Instance fields assigned in constructors
- Properties

This is a fundamental limitation requiring semantic analysis across method boundaries.

## Next Steps

To fix cross-method tracking, the generator needs to:
1. Find static field/property assignments across the compilation
2. Track which builder chain produced which variable
3. Match entry point calls to their originating builder

This is a significant change to the extraction architecture.

## Checklist

- [x] Generalize to `InterceptSitesByMethod` dictionary
- [x] Extract from Build() calls instead of entry points
- [x] Use BuildLocation for app identity (not routes)
- [x] Verify inline case works
- [x] Fix CI test failures from route-based deduplication bug
- [ ] Fix cross-method tracking (static fields, Setup pattern)
- [ ] Verify all REPL tests pass
