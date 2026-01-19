# Migrate _logging samples to Nuru DSL API

## Description

Migrate `samples/_logging/` samples to use `NuruApp.CreateBuilder()` and Nuru interfaces.
Once complete, rename folder to `NN-logging/`.

## Blocked By

- #322 - Auto-detect ILogger<T> injection and emit logging setup (COMPLETED)

## Samples

- `12-logging/console-logging.cs` - Uses behaviors with ILogger<T> constructor injection
- `12-logging/serilog-logging.cs` - Similar pattern with Serilog

## Checklist

- [x] Wait for #322 to be completed (ILogger<T> injection support)
- [x] Update `console-logging.cs` to work with generator
- [x] Update `serilog-logging.cs` to work with generator
- [x] Remove any Mediator references
- [x] Verify samples run correctly
- [x] Rename folder to numbered convention (`12-logging/`)

## Results

Completed as part of #322 implementation. The samples now demonstrate:

1. **ConfigureServices pattern**: `ConfigureServices(s => s.AddLogging(b => b.AddConsole()))`
2. **Behavior-based logging**: `LoggingBehavior` with `ILogger<T>` injection
3. **Proper interface**: Using `INuruBehavior` with `ValueTask HandleAsync`

The samples use delegate routes with behaviors rather than attributed routes with IQueryHandler,
since behavior DI was implemented in #322. Attributed route handler DI is a separate feature.

### Verified Working

```bash
./samples/12-logging/console-logging.cs test
# Output:
# Test command executed
# info: LoggingBehavior[0]
#       Starting: test
# info: LoggingBehavior[0]
#       Completed: test (1ms)
```
