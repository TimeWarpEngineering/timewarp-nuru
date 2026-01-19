# Help Behavior for Routes with Same Prefix

## Description

Design question: When multiple routes share the same prefix, what should `--help` show?

## Example

```csharp
.Map("deploy").WithDescription("Simple deploy").Done()
.Map("deploy {env}").WithDescription("Deploy to environment").Done()
```

When user runs `deploy --help`, what should happen?

## Current Behavior

- Matches `deploy {env}` (higher specificity)
- Shows help for that route only
- "Deploy to environment" is displayed

## Options

1. **Show help for BOTH routes** - list all routes starting with "deploy"
2. **Recommend `{env?}`** - analyzer suggests using optional parameter instead
3. **Recommend groups** - analyzer suggests using `.WithGroupPrefix("deploy")`
4. **Keep current behavior** - document that more specific route wins

## Related Patterns

- Optional parameters: `deploy {env?}` handles both cases in one route
- Group prefix: `.WithGroupPrefix("deploy")` with subcommands `""` and `{env}`

## Analyzer Considerations

- Should NURU warn when two routes share same literal prefix?
- Recommendation to use optional param or group?
- Related to existing NURU_R003 (unreachable route) detection

## Notes

Discovered while testing per-route help (Task #356). Test skipped pending design decision.
