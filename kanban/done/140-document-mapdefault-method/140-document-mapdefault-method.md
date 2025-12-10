# Document MapDefault Method

## Description

Document the `MapDefault` method for registering a handler for the empty route (default command when no arguments provided).

## Requirements

- Add to features/routing.md
- Show example use cases
- Explain behavior when args are empty

## Checklist

- [x] Add MapDefault section to features/routing.md
- [x] Show basic usage example
- [x] Document common use case: show help when no args
- [x] Explain difference from catch-all `{*args}`

## Notes

Source: `source/timewarp-nuru-core/nuru-core-app-builder.routes.cs`

```csharp
public virtual NuruCoreAppBuilder MapDefault(Delegate handler, string? description = null)
{
  return MapInternal(string.Empty, handler, description);
}
```

Example:
```csharp
.MapDefault(() => Console.WriteLine("Usage: myapp <command>"))
```
