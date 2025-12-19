# Remove Map(pattern, handler) and MapDefault Overloads

## Description

Remove the old `Map(string pattern, Delegate handler)` and `MapDefault(Delegate handler)` API in favor of the new fluent pattern:

```csharp
// Old (remove)
app.Map("deploy {env}", (string env) => Deploy(env));
app.MapDefault(() => ShowHelp());

// New (keep)
app.Map("deploy {env}").WithHandler((string env) => Deploy(env)).AsCommand().Done();
app.Map("").WithHandler(() => ShowHelp()).AsQuery().Done();
```

**Breaking change for 3.0** - Compiler errors will guide migration.

## Parent

151-implement-delegate-generation-phase-2

## Checklist

- [ ] Remove `Map(string pattern, Delegate handler)` overloads from `NuruCoreAppBuilder`
- [ ] Remove `MapDefault(Delegate handler)` overloads from `NuruCoreAppBuilder`
- [ ] Verify `Map(string pattern)` returns `EndpointBuilder<TBuilder>` (should already exist)
- [ ] Verify `EndpointBuilder.WithHandler(Delegate)` exists and works
- [ ] Build to confirm removal - expect compile errors in samples/tests
- [ ] Do NOT fix samples/tests yet (that's Task 198)

## Notes

This is a clean API removal. The compile errors from samples/tests are expected - they'll be fixed in Task 198.

Files to modify:
- `source/timewarp-nuru-core/nuru-core-app-builder.routes.cs`
