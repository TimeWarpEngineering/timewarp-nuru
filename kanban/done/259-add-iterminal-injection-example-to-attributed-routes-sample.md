# Add ITerminal injection example to attributed-routes sample

## Description

The `samples/attributed-routes/` sample currently uses `static System.Console` directly in all Mediator handlers. This doesn't demonstrate how to inject services into handlers, which is a common need. Add an example showing `ITerminal` injection via constructor to demonstrate the dependency injection pattern with attributed routes.

## Checklist

- [x] Update `samples/attributed-routes/messages/queries/greet-query.cs`:
  - [x] Replace `using static System.Console;` with `using TimeWarp.Terminal;`
  - [x] Add `private readonly ITerminal Terminal;` field to Handler
  - [x] Add constructor `public Handler(ITerminal terminal)` with assignment
  - [x] Change `WriteLine(...)` to `Terminal.WriteLine(...)`
  - [x] Update doc comment to mention ITerminal injection
- [x] Run the sample to verify it still works: `dotnet run --project samples/attributed-routes -- greet World`
- [x] Ensure coding standards are followed (2-space indent, PascalCase fields, explicit types)
- [x] **Bonus:** Auto-register `ITerminal` in framework when using DI path (`nuru-core-app-builder.cs`)

## Notes

### Why ITerminal?

- **Testability** - Can mock `ITerminal` to capture and verify output in tests
- **Consistency** - Matches pattern used in `samples/testing/` examples
- **Rich output** - `ITerminal` supports colored output via Spectre.Console
- **Already registered** - Nuru automatically registers `ITerminal` in DI container

### Changes Made

1. **Framework fix** (`source/timewarp-nuru-core/nuru-core-app-builder.cs`):
   - Changed conditional `ITerminal` registration to always register (using configured terminal or `TimeWarpTerminal.Default`)
   - This ensures `ITerminal` is always available for injection in DI path

2. **Sample update** (`samples/attributed-routes/messages/queries/greet-query.cs`):
   - Demonstrates constructor injection of `ITerminal` in a Mediator handler

### Coding Standards Reference

- 2-space indentation (no tabs)
- PascalCase for class-scope fields (`Terminal` not `_terminal`)
- Explicit types (no `var`)
- File-scoped namespaces
