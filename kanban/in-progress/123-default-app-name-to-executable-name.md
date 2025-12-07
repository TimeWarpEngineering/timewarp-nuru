# Default App Name to Executable Name

## Description

When the consumer doesn't set the app name, help output shows `nuru-app` as the default. It should default to the actual executable name using `AppNameDetector.GetEffectiveAppName()`.

## Requirements

- Help output shows actual executable name when app name not explicitly set
- Consistent behavior across all places that need app name fallback

## Checklist

### Implementation
- [ ] Update `help-provider.cs` line 45 to use `AppNameDetector.GetEffectiveAppName()` instead of hardcoded `"nuru-app"`
- [ ] Add try/catch since `AppNameDetector` can throw `InvalidOperationException`
- [ ] Consider updating `nuru-telemetry-options.cs` to use `AppNameDetector` for consistency

### Verification
- [ ] Run sample app without setting app name, verify help shows executable name
- [ ] Verify published executable shows correct name in help

## Notes

### Current Behavior

```csharp
// help-provider.cs line 45
sb.AppendLine("  " + (appName ?? "nuru-app") + " [command] [options]");
```

### Expected Behavior

```csharp
sb.AppendLine("  " + (appName ?? GetDefaultAppName()) + " [command] [options]");

private static string GetDefaultAppName()
{
  try { return AppNameDetector.GetEffectiveAppName(); }
  catch (InvalidOperationException) { return "nuru-app"; }
}
```

### AppNameDetector Already Exists

`AppNameDetector.GetEffectiveAppName()` in `source/timewarp-nuru-core/extensions/app-name-detector.cs` already implements robust detection:
1. `Environment.ProcessPath` (works for published executables)
2. `Process.GetCurrentProcess().ProcessName` (fallback)
3. `Assembly.GetEntryAssembly()?.GetName().Name` (final fallback)
4. Throws `InvalidOperationException` if all fail

### Files to Modify

| File | Change |
|------|--------|
| `source/timewarp-nuru-core/help/help-provider.cs` | Use `AppNameDetector` instead of `"nuru-app"` |
| `source/timewarp-nuru-telemetry/nuru-telemetry-options.cs` | Optional: Use `AppNameDetector` for consistency |
