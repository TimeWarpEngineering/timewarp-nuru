# Auto-detect ILogger<T> injection and emit logging setup

## Summary

Generator should auto-detect when handlers or behaviors inject `ILogger<T>` and honor explicit logging configuration. If no configuration provided, emit a diagnostic warning.

**Current behavior:** `ILogger<T>` silently resolves to `NullLogger`
**Desired behavior:** 
- If `ConfigureServices` has `AddLogging(...)` → use configured factory
- If `ILogger<T>` injected but no logging configured → emit warning diagnostic + use `NullLogger`

## Parent

#272 V2 Generator Phase 6: Testing (has unchecked item for `ILogger<T>` service injection)

## Blocks

- #337 - Migrate _logging samples to Nuru DSL API

## Design Decisions

### Use Standard Microsoft DI Pattern

Users configure logging the same way they would with Microsoft DI:

```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
    .ConfigureServices(services => services.AddLogging(logging =>
    {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    }))
    .Build();
```

The generator detects `AddLogging(...)` and emits equivalent `LoggerFactory.Create(...)` code.

### Add Microsoft.Extensions.Logging Package

Currently Nuru only references `Microsoft.Extensions.Logging.Abstractions` (interfaces only).
We will add `Microsoft.Extensions.Logging` as a dependency to enable `LoggerFactory.Create()`.

- If user doesn't use logging, AOT should optimize it out
- Can revisit after benchmarks if there's performance impact

### Treat ILogger<T> Like Any Other Service

No special-casing. `ILogger<T>` follows the same service resolution pattern:
- `AddSingleton<IFoo, Foo>()` → emit `Lazy<Foo>` field
- `AddLogging(...)` → emit `ILoggerFactory` field, resolve `ILogger<T>` from it

### Proper Disposal

`LoggerFactory` implements `IDisposable`. Emit disposal in `finally` block:

```csharp
finally
{
    (__loggerFactory as global::System.IDisposable)?.Dispose();
}
```

### Multiple Apps Get Separate Factories

If a file has multiple `NuruApp.CreateBuilder()` calls with different logging configs, each app gets its own factory field with unique name.

---

## Implementation Plan

### Phase 1: Add Package Reference

**File:** `source/timewarp-nuru/timewarp-nuru.csproj`

Add `Microsoft.Extensions.Logging` package alongside existing `Abstractions`.

### Phase 2: Create LoggingConfiguration Model

**File:** `source/timewarp-nuru-analyzers/generators/models/logging-configuration.cs` (new)

```csharp
public sealed record LoggingConfiguration(
    string ConfigurationLambdaBody  // The lambda body text to emit verbatim
);
```

### Phase 3: Extend AppModel

**File:** `source/timewarp-nuru-analyzers/generators/models/app-model.cs`

Add:
- `LoggingConfiguration? LoggingConfiguration` field
- `bool HasLogging => LoggingConfiguration is not null;` computed property
- Update `Empty()` factory to include `LoggingConfiguration: null`

### Phase 4: Detect AddLogging in ServiceExtractor

**File:** `source/timewarp-nuru-analyzers/generators/extractors/service-extractor.cs`

Create new method `ExtractLoggingConfiguration()` that:
1. Walks invocations inside `ConfigureServices` lambda
2. Finds `AddLogging(...)` call
3. Extracts the lambda argument's body as text
4. Returns `LoggingConfiguration` or null

### Phase 5: Update IrAppBuilder

**File:** `source/timewarp-nuru-analyzers/generators/ir-builders/ir-app-builder.cs`

Add:
```csharp
public void SetLoggingConfiguration(LoggingConfiguration config);
```

Store in builder, include in `Build()` output.

### Phase 6: Wire through DslInterpreter

**File:** `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs`

When dispatching `ConfigureServices`:
1. Call `ServiceExtractor.ExtractLoggingConfiguration()`
2. If found, call `appBuilder.SetLoggingConfiguration(config)`

### Phase 7: Emit LoggerFactory Field + Disposal

**File:** `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs`

When `AppModel.HasLogging`, emit:

```csharp
private static readonly global::Microsoft.Extensions.Logging.ILoggerFactory __loggerFactory = 
    global::Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
    {
        // User's lambda body verbatim
    });
```

In `RunAsync` interceptor, emit disposal:

```csharp
finally
{
    (__loggerFactory as global::System.IDisposable)?.Dispose();
}
```

Each app gets its own factory field (unique name per app if multiple apps).

### Phase 8: Resolve ILogger<T> Consistently

**Files:** 
- `source/timewarp-nuru-analyzers/generators/emitters/behavior-emitter.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/service-resolver-emitter.cs`

When resolving `ILogger<T>`:
- If `AppModel.HasLogging` → `__loggerFactory.CreateLogger<T>()`
- Else → `NullLoggerFactory.Instance.CreateLogger<T>()`

**Remove the special-case hack in `behavior-emitter.cs:338-340`.**

### Phase 9: Add Diagnostic NURU_H006

**File:** `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.handler.cs`

```csharp
public static readonly DiagnosticDescriptor LoggerInjectedWithoutConfiguration = new(
    "NURU_H006",
    "ILogger<T> injected without logging configuration",
    "ILogger<{0}> is injected but no logging is configured. Add .ConfigureServices(s => s.AddLogging(...)) to enable logging output.",
    "Usage",
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true);
```

Emit during extraction when `ILogger<T>` dependency found but no `AddLogging` detected.

### Phase 10: Tests

**File:** `tests/timewarp-nuru-analyzers-tests/auto/generator-15-ilogger-injection.cs` (new)

Test cases:
1. `AddLogging(b => b.AddConsole())` + handler with `ILogger<T>` → emits real factory
2. Handler with `ILogger<T>` but no `AddLogging()` → emits NullLogger + NURU_H006 warning
3. No `ILogger<T>` usage → no logging code emitted, no warning
4. Behavior with `ILogger<T>` + `AddLogging()` → works
5. Multiple apps with different logging configs → each gets own factory

### Phase 11: Update Samples

Once working:
- Update `samples/_logging/console-logging.cs` to use `ConfigureServices(s => s.AddLogging(...))`
- Update `samples/_logging/serilog-logging.cs` similarly  
- Rename folder to `samples/12-logging/`
- Mark #337 as complete

---

## Checklist

### Phase 1: Package
- [ ] Add `Microsoft.Extensions.Logging` to `timewarp-nuru.csproj`

### Phase 2-3: Models
- [ ] Create `logging-configuration.cs` with `LoggingConfiguration` record
- [ ] Add `LoggingConfiguration?` to `AppModel`
- [ ] Add `HasLogging` computed property
- [ ] Update `AppModel.Empty()` factory

### Phase 4: Detection
- [ ] Add `ExtractLoggingConfiguration()` to `ServiceExtractor`
- [ ] Extract lambda body text from `AddLogging(...)` call

### Phase 5-6: IR Builder + Interpreter
- [ ] Add `SetLoggingConfiguration()` to `IrAppBuilder`
- [ ] Wire through `DslInterpreter.DispatchConfigureServices()`

### Phase 7: Emission
- [ ] Emit `__loggerFactory` field in `interceptor-emitter.cs`
- [ ] Emit disposal in `finally` block
- [ ] Handle unique names for multiple apps

### Phase 8: Resolution
- [ ] Update `behavior-emitter.cs` to use factory when available
- [ ] Update `service-resolver-emitter.cs` similarly
- [ ] Remove special-case hack at line 338-340

### Phase 9: Diagnostic
- [ ] Create `NURU_H006` diagnostic descriptor
- [ ] Emit warning when `ILogger<T>` used without `AddLogging()`

### Phase 10: Tests
- [ ] Create `generator-15-ilogger-injection.cs`
- [ ] Test: configured logging → real factory
- [ ] Test: unconfigured → warning + NullLogger
- [ ] Test: no ILogger usage → no emission
- [ ] Test: behaviors with ILogger
- [ ] Test: multiple apps

### Phase 11: Samples
- [ ] Update `_logging/console-logging.cs`
- [ ] Update `_logging/serilog-logging.cs`
- [ ] Rename to `12-logging/`
- [ ] Mark #337 complete

---

## Key Files

| File | Purpose |
|------|---------|
| `timewarp-nuru.csproj` | Add package reference |
| `models/logging-configuration.cs` | New model |
| `models/app-model.cs` | Add LoggingConfiguration field |
| `extractors/service-extractor.cs` | Detect AddLogging |
| `ir-builders/ir-app-builder.cs` | Store logging config |
| `interpreter/dsl-interpreter.cs` | Wire detection to builder |
| `emitters/interceptor-emitter.cs` | Emit factory + disposal |
| `emitters/behavior-emitter.cs` | Resolve ILogger from factory |
| `emitters/service-resolver-emitter.cs` | Resolve ILogger from factory |
| `diagnostics/diagnostic-descriptors.handler.cs` | NURU_H006 |

---

## Expected Generated Code

### With AddLogging Configured

```csharp
private static readonly global::Microsoft.Extensions.Logging.ILoggerFactory __loggerFactory = 
    global::Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(global::Microsoft.Extensions.Logging.LogLevel.Information);
    });

private static readonly global::System.Lazy<LoggingBehavior> __behavior_LoggingBehavior = 
    new(() => new LoggingBehavior(
        __loggerFactory.CreateLogger<LoggingBehavior>()));

// In RunAsync interceptor:
try
{
    // ... routing logic ...
}
finally
{
    (__loggerFactory as global::System.IDisposable)?.Dispose();
}
```

### Without AddLogging (Warning Emitted)

```csharp
// No __loggerFactory field emitted

private static readonly global::System.Lazy<LoggingBehavior> __behavior_LoggingBehavior = 
    new(() => new LoggingBehavior(
        global::Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance.CreateLogger<LoggingBehavior>()));

// Diagnostic: warning NURU_H006: ILogger<LoggingBehavior> is injected but no logging is configured.
```
