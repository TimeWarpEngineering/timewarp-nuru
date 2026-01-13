# BUG: End-of-options with positional parameter before separator fails

## Description

Route patterns with a positional parameter before the `--` end-of-options separator fail to match.

## Reproduction

```csharp
// This pattern fails to match
.Map("execute {script} -- {*args}")
  .WithHandler((string script, string[] args) => ...)

// Input: ["execute", "run.sh", "--", "--verbose", "file.txt"]
// Expected: script="run.sh", args=["--verbose", "file.txt"]
// Actual: Route fails to match (exit code 1)
```

## Working Patterns

These patterns work correctly:
```csharp
// No positional before separator - WORKS
.Map("run -- {*args}")

// Flag before separator - WORKS
.Map("docker run --detach -- {*cmd}")
```

## Failing Test

`tests/timewarp-nuru-core-tests/routing/routing-08-end-of-options.cs`
- Test: `Should_match_parameter_before_end_of_options_execute_run_sh_verbose_file_txt`

## Checklist

- [ ] Debug route matching for `{param} -- {*args}` pattern
- [ ] Fix parser/matcher to handle positional before `--`
- [ ] Verify test passes

## Notes

Discovered while reviewing routing-08 for CI inclusion. The `UseDebugLogging()` call was removed since it's in a separate project not referenced by tests.
