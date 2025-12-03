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

- [x] Fix the route pattern to avoid consecutive optional parameters
- [x] Consider using options instead: `deploy {service} --env? {environment} --version? {version}`
- [x] Verify sample runs without errors
- [x] Update help text to match new pattern

## Solution

Changed the route pattern from:
```csharp
builder.Map("deploy {service} {environment?} {version?}", ...)
```

To:
```csharp
builder.Map("deploy {service} --env? {environment} --version? {version}", ...)
```

This uses optional options (`--env?`, `--version?`) instead of consecutive optional positional parameters. The options can now be provided in any combination:
- `deploy myservice` - uses defaults for both
- `deploy myservice --env staging` - specifies environment only
- `deploy myservice --version 2.0.0` - specifies version only  
- `deploy myservice --env staging --version 2.0.0` - specifies both

Also fixed pre-existing issue where `NuruAppBuilder` was being instantiated directly instead of using `NuruApp.CreateSlimBuilder(args)`.

## Notes

- Discovered during kebab-case migration validation
- This is a pre-existing bug, not caused by the migration
