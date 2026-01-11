# Migrate _logging samples to Nuru DSL API

## Description

Migrate `samples/_logging/` samples to use `NuruApp.CreateBuilder()` and Nuru interfaces.
Once complete, rename folder to `NN-logging/`.

## Blocked By

- #322 - Auto-detect ILogger<T> injection and emit logging setup

## Samples

- `_logging/console-logging.cs` - Uses IQuery/IQueryHandler with ILogger<T> constructor injection
- `_logging/serilog-logging.cs` - Similar pattern with Serilog

## Checklist

- [ ] Wait for #322 to be completed (ILogger<T> injection support)
- [ ] Update `console-logging.cs` to work with generator
- [ ] Update `serilog-logging.cs` to work with generator
- [ ] Remove any Mediator references
- [ ] Verify samples run correctly
- [ ] Rename folder to numbered convention (e.g., `12-logging/`)

## Notes

The samples already use Nuru interfaces (IQuery, IQueryHandler), but the generator doesn't
resolve ILogger<T> from DI for attributed route handlers. Task #322 needs to be completed first.
