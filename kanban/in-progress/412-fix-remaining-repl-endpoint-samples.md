# Fix all remaining endpoint samples (4 REPL samples)

## Description

Fix the 4 remaining endpoint samples that fail to compile. These samples use an incorrect pattern where `DiscoverEndpoints().Build()` is called twice (once for CLI, once for REPL), causing source generator error CS9153: "The indicated call is intercepted multiple times."

**Current status:** 25/29 endpoint samples compile successfully
**Remaining:** 4 REPL samples need fixing

**Affected samples:**
- `samples/endpoints/09-repl/endpoint-repl-basic.cs`
- `samples/endpoints/09-repl/endpoint-repl-options.cs`
- `samples/endpoints/09-repl/endpoint-repl-custom-keys.cs`
- `samples/endpoints/09-repl/endpoint-repl-dual-mode.cs`

## Root Cause

The samples call `builder.DiscoverEndpoints().Build()` twice:
```csharp
// ❌ WRONG - builds app twice
NuruApp app1 = builder.DiscoverEndpoints().Build();
await app1.RunReplAsync();  // First intercept of RunAsync()

NuruApp app2 = builder.DiscoverEndpoints().Build();
return await app2.RunAsync(args);  // Second intercept of RunAsync() - CS9153!
```

This causes the source generator to emit two `RunAsync_Intercepted` methods, violating the single-intercept rule.

## Correct Pattern

The correct pattern (from fluent samples) is:
```csharp
// ✅ CORRECT - build once, RunAsync handles both CLI and REPL
NuruApp app = builder
  .DiscoverEndpoints()
  .AddRepl(options => { /* configure REPL */ })
  .Build();

return await app.RunAsync(args);  // Single intercept
```

## Required Changes

### 1. Remove duplicate Build() calls

Change from:
```csharp
NuruApp cliApp = builder.DiscoverEndpoints().Build();
return await cliApp.RunAsync(args);
```

To:
```csharp
NuruApp app = builder
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
```

### 2. Use AddRepl() for REPL configuration

The old pattern used `builder.AddRepl()` without configuration, then manually called `RunReplAsync()`. The correct pattern is:

```csharp
// Old (wrong):
builder.AddRepl();
await app.RunReplAsync();

// New (correct):
builder.AddRepl(options =>
{
  options.AutoStartWhenEmpty = true;  // or other configuration
});
// RunAsync() handles --interactive flag automatically
```

### 3. Handle --interactive flag via AutoStartWhenEmpty

The `--interactive` flag is handled automatically by the framework when REPL is enabled. The samples check for this flag manually but it's not needed.

### 4. Remove RunReplAsync() calls

The `RunReplAsync()` method should not be called directly when using `DiscoverEndpoints()`. The REPL mode is triggered by:
- `--interactive` or `-i` command line flag
- `AutoStartWhenEmpty = true` option (REPL starts when no args provided)

## Sample-by-Sample Fixes

### endpoint-repl-basic.cs

**Current (broken):**
```csharp
if (args.Contains("--interactive") || args.Contains("-i"))
{
  builder.AddRepl();
  NuruApp app = builder.DiscoverEndpoints().Build();
  await app.RunReplAsync();
  return 0;
}

NuruApp cliApp = builder.DiscoverEndpoints().Build();
return await cliApp.RunAsync(args);
```

**Fix:**
```csharp
NuruApp app = builder
  .DiscoverEndpoints()
  .AddRepl()  // Uses defaults
  .Build();

return await app.RunAsync(args);
```

### endpoint-repl-options.cs

**Issue:** Uses `ReplOptions.HistorySize`, `EnableAutoCompletion`, `EnableSyntaxHighlighting`, `MultiLineInput` - need to map to current API

**Current ReplOptions properties:**
- `MaxHistorySize` (not `HistorySize`)
- `AutoStartWhenEmpty` (not in old API)
- `ContinueOnError`
- `ShowExitCode`
- `EnableColors`
- `PromptColor`
- `ShowTiming`
- `EnableArrowHistory`
- `KeyBindingProfileName`
- `PersistHistory`
- `HistoryFilePath`

**Note:** `EnableAutoCompletion`, `EnableSyntaxHighlighting`, `MultiLineInput` may not exist in current API - need to check.

### endpoint-repl-dual-mode.cs

**Issue:** Similar to basic - calls `RunReplAsync()` directly

**Fix:** Use single Build() + AddRepl() pattern

### endpoint-repl-custom-keys.cs

**Issue:** Uses `ReplOptions.KeyBindingProfile` and custom key bindings

**Current approach:** Uses `[Parameter(IsOptional)]` on `CustomKeyBindingProfile` property

**Fix:** Remove `IsOptional` attribute (use nullable type), update to current API

## Verification

After fix:
```bash
dotnet build samples/endpoints/09-repl/endpoint-repl-basic.cs  # Should compile
dotnet build samples/endpoints/09-repl/endpoint-repl-options.cs  # Should compile
dotnet build samples/endpoints/09-repl/endpoint-repl-custom-keys.cs  # Should compile
dotnet build samples/endpoints/09-repl/endpoint-repl-dual-mode.cs  # Should compile

dev verify-samples --category endpoints  # Should show 29/29 pass
```

## Files to Modify

1. `samples/endpoints/09-repl/endpoint-repl-basic.cs`
2. `samples/endpoints/09-repl/endpoint-repl-options.cs`
3. `samples/endpoints/09-repl/endpoint-repl-custom-keys.cs`
4. `samples/endpoints/09-repl/endpoint-repl-dual-mode.cs`

## Notes

This is a continuation of Task #410, which fixed the API migration issues. The REPL samples were incorrectly marked as "blocked by REPL API changes" when they actually needed a simple pattern fix.

The source generator intercepts `RunAsync()` once per app instance. Calling `DiscoverEndpoints().Build()` twice creates two app instances, causing two intercepts.

## Checklist

- [ ] Fix `endpoint-repl-basic.cs` - single Build(), AddRepl() pattern
- [ ] Fix `endpoint-repl-options.cs` - update ReplOptions to current API
- [ ] Fix `endpoint-repl-dual-mode.cs` - single Build(), AddRepl() pattern
- [ ] Fix `endpoint-repl-custom-keys.cs` - remove IsOptional, use nullable types
- [ ] Verify all 4 REPL samples compile
- [ ] Run `dev verify-samples --category endpoints` - should show 29/29 pass
