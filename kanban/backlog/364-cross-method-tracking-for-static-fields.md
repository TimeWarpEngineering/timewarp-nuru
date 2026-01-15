# Cross-Method Tracking for Static Fields

## Problem

The generator cannot trace entry point calls (RunAsync/RunReplAsync) back to their Build() call when the app is stored in a static field and accessed from a different method.

### Fails:
```csharp
private static NuruCoreApp? App;

public static async Task Setup()
{
  App = NuruApp.CreateBuilder([])
    .Map("hello").WithHandler(() => "Hello!").AsQuery().Done()
    .Build();  // Build() is here
}

public static async Task SomeTest()
{
  await App!.RunReplAsync();  // Can't trace back to Build()
}
```

### Works:
```csharp
public static async Task Test()
{
  NuruCoreApp app = NuruApp.CreateBuilder([])
    .Map("hello").WithHandler(() => "Hello!").AsQuery().Done()
    .Build();
  await app.RunReplAsync();  // Same method - can trace
}
```

## Why It Fails

The `DslInterpreter.ResolveIdentifier()` method uses the semantic model to trace variables within a single method/block. When `App.RunReplAsync()` is evaluated:

1. It looks up the symbol for `App`
2. Finds it's a static field
3. Cannot find the initializer in the current scope
4. Returns null - no app to intercept

## Impact

~128 REPL tests use the Setup() pattern and fail because their entry points aren't intercepted.

## Potential Solutions

### Option A: Static Field Assignment Tracking
1. Find all assignments to static fields of type `NuruCoreApp`
2. For each assignment, extract the builder chain
3. Create a mapping: field symbol → AppModel
4. When evaluating entry point on static field, look up the mapping

### Option B: Two-Pass Extraction
1. First pass: Find all Build() calls, extract AppModels, track variable symbols
2. Second pass: Find all entry point calls, match to AppModels by symbol

### Option C: Whole-Compilation Analysis
Analyze the entire compilation to build a complete picture of:
- Which Build() calls exist
- Which variables/fields they're assigned to
- Which entry point calls reference those variables/fields

## Complexity

This is a significant architectural change. The current incremental generator model processes each syntax node independently. Cross-method tracking requires:
- Additional passes over the syntax tree
- Symbol-based tracking across method boundaries
- Careful caching to maintain incremental build performance

## Acceptance Criteria

- [ ] Tests using Setup() pattern pass
- [ ] Static field assignment is tracked
- [ ] Instance field assignment is tracked
- [ ] Property assignment is tracked
- [ ] Incremental build performance is maintained
