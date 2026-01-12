# Bug: Typed repeated options not converted from string array

## Description

When the source generator processes route patterns with typed repeated options (e.g., `{id:int}*`), the generated code passes `string[]` directly to handlers expecting typed arrays like `int[]`, `double[]`, `bool[]`, etc., causing CS1503 type conversion errors.

## Reproduction

**Files affected:** Multiple routing tests in `tests/timewarp-nuru-core-tests/routing/`

**Example patterns:**
```csharp
.Map("process --id {id:int}*").WithHandler((int[] id) => ...)
.Map("calc --values {v:double}*").WithHandler((double[] v) => ...)
.Map("docker run -i -t --env {e}* -- {*cmd}").WithHandler((bool i, bool t, string[] e, string[] cmd) => ...)
```

## Resolution

**RESOLVED** - All typed repeated options now work correctly.

The fix was implemented through the two-pass argument processing refactor:

1. **`EmitRepeatedValueOptionParsingWithIndexTracking`** - Collects all values for repeated options into a `List<string>`
2. **`EmitRepeatedOptionTypeConversion`** - Converts `string[]` to typed arrays (`int[]`, `double[]`, etc.)
3. **Task #334 fix** - The `EndOfOptionsSeparatorDefinition` work ensured proper handling of `--` in patterns like `docker run -i -t --env {e}* -- {*cmd}`

## Test Status

```
CI Tests: ALL PASSED
- Total: 466
- Passed: 464
- Skipped: 2 (unrelated known issues)
- Failed: 0
```

**RepeatedOptions tests: 6/6 passing**
- `Should_match_basic_repeated_option_docker_run_env`
- `Should_match_empty_repeated_option_docker_run`
- `Should_match_typed_repeated_option_process_id_1_2_3`
- `Should_match_repeated_option_with_alias`
- `Should_match_mixed_repeated_single_deploy`
- `Should_match_single_value_repeated_option`

## Key Code Locations

- `route-matcher-emitter.cs` - `EmitRepeatedValueOptionParsingWithIndexTracking()`, `EmitRepeatedOptionTypeConversion()`
- `handler-invoker-emitter.cs` - Builds argument list for handler calls

## Related

- Task #334: Fix EndOfOptionsSeparator handling - **DONE** (enabled docker-style commands)
- Task #351: Merge NuruAnalyzer into NuruGenerator - **DONE**
- Task #352: Add DiscoverEndpoints() and Map<T>() - **DONE**
