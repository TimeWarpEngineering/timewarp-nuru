# Auto-detect ILogger<T> injection and emit logging setup

## Summary

Generator should auto-detect when handlers or behaviors inject `ILogger<T>` and honor explicit logging configuration. If no configuration provided, emit a diagnostic warning.

**Current behavior:** `ILogger<T>` silently resolves to `NullLogger`
**Desired behavior:** 
- If `ConfigureServices` has `AddLogging(...)` → use configured factory
- If `ILogger<T>` injected but no logging configured → emit warning diagnostic + use `NullLogger`

## Parent

#272 V2 Generator Phase 6: Testing (has unchecked item for `ILogger<T>` service injection)

## Design Approach

**Respect explicit configuration, warn on missing configuration:**

1. Scan handlers/behaviors for `ILogger<T>` injection
2. Check if `ConfigureServices` contains `AddLogging(...)` call
3. If logging configured → emit factory setup from user's configuration
4. If `ILogger<T>` used but NOT configured → emit diagnostic warning + fallback to `NullLogger`

This matches how we handle `IOptions<T>` - we don't assume what configuration source, we just detect usage and bind from whatever is configured.

## Checklist

### Detection
- [ ] Add `RequiresLogging` flag to `AppModel` (like existing `RequiresConfiguration`)
- [ ] Add `HasLoggingConfiguration` flag to `AppModel`
- [ ] Scan handler parameters for `ILogger<T>` types
- [ ] Scan behavior constructor dependencies for `ILogger<T>` types
- [ ] Detect `AddLogging(...)` calls in `ConfigureServices`

### Diagnostic
- [ ] Create `NURU_H006` analyzer diagnostic: "ILogger<T> injected but no logging configured"
- [ ] Emit warning when `RequiresLogging && !HasLoggingConfiguration`
- [ ] Suggest: "Add .ConfigureServices(s => s.AddLogging(...)) to configure logging providers"

### Emission (when logging IS configured)
- [ ] Parse `AddLogging(...)` lambda to extract configuration
- [ ] Emit `LoggerFactory.Create(...)` with user's configuration
- [ ] Update `behavior-emitter.cs` to use real factory
- [ ] Update `handler-invoker-emitter.cs` if handlers inject `ILogger<T>`

### Emission (when logging NOT configured)
- [ ] Keep current `NullLoggerFactory` behavior
- [ ] Diagnostic warning provides guidance

### Testing
- [ ] Create `generator-15-ilogger-injection.cs` test file
- [ ] Test: `ILogger<T>` with `AddLogging()` configured → real logger
- [ ] Test: `ILogger<T>` without `AddLogging()` → warning + NullLogger
- [ ] Test: No `ILogger<T>` usage → no warning, no logging setup
- [ ] Test: Multiple behaviors each with their own `ILogger<T>`

### Sample Update
- [ ] Update `_unified-middleware` to use `ConfigureServices(s => s.AddLogging(...))`
- [ ] Verify visible logging output
- [ ] Move sample to numbered folder (e.g., `10-unified-middleware/`)

## Technical Notes

### Current Code (behavior-emitter.cs:309-311)
```csharp
// ILogger<T> - create a null logger for now (TODO: proper logger resolution)
if (serviceTypeName.Contains("ILogger", StringComparison.Ordinal))
  return $"NullLoggerFactory.Instance.CreateLogger<{ExtractLoggerTypeArg(serviceTypeName)}>()";
```

### Expected Generated Code (with configuration)
```csharp
// Logging setup (from ConfigureServices)
using var loggerFactory = LoggerFactory.Create(builder => 
{
  builder.AddConsole();  // User's configuration
});

// Behavior instantiation
private static readonly Lazy<LoggingBehavior> __behavior_LoggingBehavior = 
  new(() => new LoggingBehavior(
    loggerFactory.CreateLogger<LoggingBehavior>()));
```

### Expected Diagnostic (without configuration)
```
warning NURU_H006: ILogger<LoggingBehavior> is injected but no logging is configured. 
Add .ConfigureServices(s => s.AddLogging(b => b.AddConsole())) to enable logging output.
```

### Key Files
- `source/timewarp-nuru-analyzers/generators/emitters/behavior-emitter.cs` - Has the TODO
- `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` - Main emission
- `source/timewarp-nuru-analyzers/generators/models/app-model.cs` - Add flags
- `source/timewarp-nuru-analyzers/generators/extractors/service-extractor.cs` - Detect AddLogging
- `samples/_unified-middleware/unified-middleware.cs` - Blocked sample

## Blocked

This task blocks:
- `_unified-middleware` sample migration (#312)

## Notes

### Why Not Default to Console?
We don't know what logging provider the user wants:
- Console
- Debug
- File (Serilog, NLog)
- OpenTelemetry
- Custom

Better to require explicit configuration and warn if missing, rather than assume.

### Lifetime Consideration
The `loggerFactory` needs to be scoped appropriately:
- Created once at app startup
- Disposed when app exits
- Behaviors are singletons, so factory must outlive them
