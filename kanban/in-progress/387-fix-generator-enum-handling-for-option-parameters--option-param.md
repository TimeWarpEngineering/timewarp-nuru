# Fix generator enum handling for option parameters (--option {param})

## Summary

The source generator fails to emit type conversion code for enum parameters when they are option values (e.g., `--mode {mode}` where `mode` is an enum type). This causes compile errors in generated code.

Task #372 fixed enum handling for positional parameters but did not cover option parameters.

## Problem

```csharp
.Map("deploy {env} --mode {mode}")
  .WithHandler((string env, DeploymentMode mode) => 0)
```

Generated code:
```csharp
int __handler_0(string env, global::DeploymentMode mode) => 0;
int result = __handler_0(env, mode);  // ERROR: 'mode' is never declared
```

The generator emits the handler signature correctly but doesn't generate code to:
1. Extract the option value from args
2. Convert the string to the enum type

## Related Files

- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs` - needs to handle enum option params
- `tests/timewarp-nuru-tests/repl/repl-16-enum-completion.cs` - has skipped tests referencing this task
- `tests/timewarp-nuru-tests/completion/dynamic/completion-21-integration-enabledynamic.cs` - `Should_auto_register_enum_sources` test commented out, restore after fix

## Checklist

- [ ] Investigate how option parameters are extracted in route-matcher-emitter.cs
- [ ] Add enum type conversion for option parameters (similar to #372 fix for positional params)
- [ ] Unskip tests in repl-16-enum-completion.cs
- [ ] Restore `Should_auto_register_enum_sources` test in completion-21-integration-enabledynamic.cs
- [ ] Add explicit test for `--option {enumParam}` pattern
- [ ] Run full CI to verify no regressions

## Notes

Error discovered while working on #340 (shell completion migration). The completion tests use `--mode {mode}` with a `DeploymentMode` enum.

Previous fix (#372) only handled positional enum parameters, not option parameters.
