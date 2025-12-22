# Add ITerminal injection example to attributed-routes sample

## Description

The `samples/attributed-routes/` sample currently uses `static System.Console` directly in all Mediator handlers. This doesn't demonstrate how to inject services into handlers, which is a common need. Add an example showing `ITerminal` injection via constructor to demonstrate the dependency injection pattern with attributed routes.

## Checklist

- [ ] Update `samples/attributed-routes/messages/queries/greet-query.cs`:
  - [ ] Replace `using static System.Console;` with `using TimeWarp.Terminal;`
  - [ ] Add `private readonly ITerminal Terminal;` field to Handler
  - [ ] Add constructor `public Handler(ITerminal terminal)` with assignment
  - [ ] Change `WriteLine(...)` to `Terminal.WriteLine(...)`
  - [ ] Update doc comment to mention ITerminal injection
- [ ] Run the sample to verify it still works: `dotnet run --project samples/attributed-routes -- greet World`
- [ ] Ensure coding standards are followed (2-space indent, PascalCase fields, explicit types)

## Notes

### Why ITerminal?

- **Testability** - Can mock `ITerminal` to capture and verify output in tests
- **Consistency** - Matches pattern used in `samples/testing/` examples
- **Rich output** - `ITerminal` supports colored output via Spectre.Console
- **Already registered** - Nuru automatically registers `ITerminal` in DI container

### Target File Pattern

```csharp
namespace AttributedRoutes.Messages;

using TimeWarp.Nuru;
using TimeWarp.Terminal;
using Mediator;

/// <summary>
/// Simple greeting query with a required parameter.
/// This is a Query (Q) - read-only, safe to retry.
/// Demonstrates ITerminal injection for testable output.
/// </summary>
[NuruRoute("greet", Description = "Greet someone by name")]
public sealed class GreetQuery : IQuery<Unit>
{
  [Parameter(Description = "Name of the person to greet")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler : IQueryHandler<GreetQuery, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public ValueTask<Unit> Handle(GreetQuery query, CancellationToken ct)
    {
      Terminal.WriteLine($"Hello, {query.Name}!");
      return default;
    }
  }
}
```

### Coding Standards Reference

- 2-space indentation (no tabs)
- PascalCase for class-scope fields (`Terminal` not `_terminal`)
- Explicit types (no `var`)
- File-scoped namespaces
