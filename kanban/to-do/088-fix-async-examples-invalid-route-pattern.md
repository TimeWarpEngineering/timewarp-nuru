# Fix Async Examples Invalid Route Pattern

## Description

The `samples/async-examples/program.cs` sample contains an invalid route pattern that causes a runtime exception. The deploy command has multiple consecutive optional parameters which creates ambiguity.

## Error

```
TimeWarp.Nuru.Parsing.PatternException: Invalid route pattern 'deploy {service} {environment?} {version?}': 
Semantic Error at position 32: Multiple consecutive optional parameters create ambiguity: environment, version
```

## Location

`samples/async-examples/program.cs` line 111:
```csharp
builder.Map("deploy {service} {environment?} {version?}", 
    async (string service, string? environment, string? version) =>
```

## Checklist

- [ ] Fix the route pattern to avoid consecutive optional parameters
- [ ] Consider using options instead: `deploy {service} --env {environment?} --version {version?}`
- [ ] Verify sample runs without errors
- [ ] Update help text to match new pattern

## Notes

- Discovered during kebab-case migration validation
- This is a pre-existing bug, not caused by the migration
