# REPL Completion Missing Enum Values and --help Option

## Description

The generated `GetCompletions()` method in `GeneratedReplRouteProvider` is incomplete. It only generates completions for:
1. Command prefixes (e.g., "deploy", "git status")
2. Options (only when input starts with `-` or `--`)

Missing completions:
1. **Enum parameter values** - After typing "deploy ", should show Dev, Staging, Prod
2. **Built-in --help option** - Should always be offered as a completion
3. **Parameter type hints** - Could show `<name>`, `<count:int>` etc.

## Failing Tests (10 failures in repl-17-sample-validation.cs)

Enum completions:
- `Should_show_enum_values_in_completions_after_deploy_space`
- `Should_show_help_option_in_completions_after_deploy_space`
- `Should_filter_enum_completions_with_partial_p`
- `Should_filter_enum_completions_with_partial_s`
- `Should_filter_enum_completions_with_partial_d`

Option/command completions:
- `Should_show_available_completions_on_first_tab`
- `Should_cycle_to_first_completion_on_second_tab`
- `Should_show_build_options_on_tab`
- `Should_show_completions_and_autocomplete_unique_match`
- `Should_show_option_after_git_commit_space`

## Root Cause

In `repl-emitter.cs`, the `EmitGetCompletionsMethod()` function:
- Emits command prefix completions ✓
- Emits option completions (only when starting with `-`) ✓
- Does NOT emit enum value completions ✗
- Does NOT emit --help as a completion ✗
- Does NOT emit completions based on current parameter position ✗

## Required Changes

### 1. Add --help completion

Always offer `--help` as a completion when appropriate:
```csharp
// In GetCompletions
if (currentInput.StartsWith("-", StringComparison.Ordinal) || hasTrailingSpace)
{
  if ("--help".StartsWith(currentInput, StringComparison.OrdinalIgnoreCase))
    yield return new CompletionCandidate("--help", "Show help", CompletionType.Option);
}
```

### 2. Add enum value completions

The generator already extracts `ParameterInfo` with `TypeConstraint`. For enum types, it needs to:
1. Detect that the current position expects an enum parameter
2. Emit the enum values as completions

```csharp
// If current position is an enum parameter
foreach (var enumValue in GetEnumValues(parameterType))
{
  if (enumValue.StartsWith(currentInput, StringComparison.OrdinalIgnoreCase))
    yield return new CompletionCandidate(enumValue, null, CompletionType.Argument);
}
```

### 3. Position-aware completions

Need to track which command prefix is matched and what parameter position we're at:
- "deploy " → position 0 → enum Environment
- "deploy dev " → position 1 → optional string tag

## Architecture Consideration

The ReplEmitter already extracts `ParameterInfo` including `TypeConstraint` and `CommandPrefix`. The challenge is:
1. Determining the current command context from parsed args
2. Matching to the right route's parameters
3. Emitting context-aware completions

## Priority

Medium - Core REPL functionality works, but tab completion is less helpful
