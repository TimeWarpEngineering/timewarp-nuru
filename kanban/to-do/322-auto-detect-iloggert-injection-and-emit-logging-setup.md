# Auto-detect ILogger<T> injection and emit logging setup

## Summary

Generator should auto-detect when handlers or behaviors inject `ILogger<T>` and automatically emit logging infrastructure - same pattern we use for `IConfiguration`/`IOptions<T>`.

**Current behavior:** `ILogger<T>` resolves to `NullLogger` (silent discard)
**Desired behavior:** `ILogger<T>` resolves to real logger with console output

## Parent

#272 V2 Generator Phase 6: Testing (has unchecked item for `ILogger<T>` service injection)

## Existing Pattern to Follow

We already do this for configuration:
1. Scan handlers/behaviors for `IOptions<T>` injection
2. If found, emit `IConfiguration` setup code
3. Bind options from configuration

Same approach for logging:
1. Scan handlers/behaviors for `ILogger<T>` injection  
2. If found, emit `ILoggerFactory` setup code
3. Create loggers from that factory

## Checklist

### Detection
- [ ] Add `RequiresLogging` flag to `AppModel` (like existing `RequiresConfiguration`)
- [ ] Scan handler parameters for `ILogger<T>` types
- [ ] Scan behavior constructor dependencies for `ILogger<T>` types
- [ ] Set flag if any `ILogger<T>` usage detected

### Emission
- [ ] In `InterceptorEmitter`, emit logging factory setup when `RequiresLogging` is true
- [ ] Emit: `using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());`
- [ ] Update `behavior-emitter.cs` line 309-311 to use real factory instead of `NullLoggerFactory`
- [ ] Update `handler-invoker-emitter.cs` if handlers can inject `ILogger<T>`

### Package References
- [ ] Ensure `Microsoft.Extensions.Logging` package is referenced
- [ ] Ensure `Microsoft.Extensions.Logging.Console` package is referenced (or make configurable)

### Testing
- [ ] Create `generator-15-ilogger-injection.cs` test file
- [ ] Test: behavior with `ILogger<T>` constructor injection
- [ ] Test: handler with `ILogger<T>` parameter injection
- [ ] Test: multiple behaviors each with their own `ILogger<T>`
- [ ] Test: no logging setup emitted when no `ILogger<T>` usage

### Sample Update
- [ ] Verify `_unified-middleware` sample works with visible logging output
- [ ] Move sample to numbered folder (e.g., `10-unified-middleware/`)

## Technical Notes

### Current Code (behavior-emitter.cs:309-311)
```csharp
// ILogger<T> - create a null logger for now (TODO: proper logger resolution)
if (serviceTypeName.Contains("ILogger", StringComparison.Ordinal))
  return $"NullLoggerFactory.Instance.CreateLogger<{ExtractLoggerTypeArg(serviceTypeName)}>()";
```

### Expected Generated Code
```csharp
// Logging setup (auto-detected from ILogger<T> usage)
using var loggerFactory = LoggerFactory.Create(builder => 
{
  builder.AddConsole();
  builder.SetMinimumLevel(LogLevel.Information);
});

// Behavior instantiation
private static readonly Lazy<LoggingBehavior> __behavior_LoggingBehavior = 
  new(() => new LoggingBehavior(
    loggerFactory.CreateLogger<LoggingBehavior>()));
```

### Key Files
- `source/timewarp-nuru-analyzers/generators/emitters/behavior-emitter.cs` - Has the TODO
- `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` - Main emission
- `source/timewarp-nuru-analyzers/generators/models/app-model.cs` - Add `RequiresLogging` flag
- `samples/_unified-middleware/unified-middleware.cs` - Blocked sample

## Blocked

This task blocks:
- `_unified-middleware` sample migration (#312)

## Notes

### Design Decision: Console by Default
Start with `AddConsole()` as the default. Future enhancements could:
- Support `.UseLogging(config => ...)` DSL for customization
- Support log level configuration via `appsettings.json`
- Support other providers (Serilog, etc.)

### Lifetime Consideration
The `loggerFactory` needs to be scoped appropriately:
- Created once at app startup
- Disposed when app exits
- Behaviors are singletons, so factory must outlive them
