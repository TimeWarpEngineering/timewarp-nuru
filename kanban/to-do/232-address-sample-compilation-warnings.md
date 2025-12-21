# Address sample compilation warnings

## Description

Many samples produce compilation warnings that should be investigated and resolved. These may indicate configuration issues or areas needing attention.

## Checklist

- [ ] Investigate `MSG0005: MediatorGenerator found message without any registered handler: Default_Generated_Query` - appears in ~25 samples
- [ ] Investigate `CS0436` type conflict warnings in aot-example (Default_Generated_Query defined in both generated code and library)
- [ ] Investigate `CS8785: Generator 'NuruRouteAnalyzer' failed to generate source` - FileNotFoundException for Microsoft.Extensions.Logging.Abstractions

## Notes

### MSG0005 warning
This appears in many samples using the mediator pattern. May indicate:
- A missing handler registration
- An expected warning that should be suppressed
- A generator configuration issue

### CS0436 type conflict
In aot-example, `Default_Generated_Query` is defined in both:
- Generated code: `GeneratedDelegateCommands.g.cs`
- Imported library: `TimeWarp.Nuru`

This suggests the type shouldn't be generated when it already exists in the library.

### CS8785 generator failure
The `NuruRouteAnalyzer` generator fails with:
```
Could not load file or assembly 'Microsoft.Extensions.Logging.Abstractions, Version=10.0.0.0'
```
This may be a version mismatch issue with .NET 10 preview.
