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

## Decision 2026-09-29: Option 1, show help for all routes sharing the prefix

`deploy --help` lists every route whose literal prefix is `deploy` (here `deploy` and `deploy {env}`), each
with its pattern, description, parameters, options, and examples, in the same format per-route help uses
today. This is what the skipped test already expects:
`tests/timewarp-nuru-tests/help/help-01-per-route-help.cs` `Should_show_help_for_multiple_routes_with_same_prefix`
(around line 232, `[Skip(... See kanban #370)]`).

## Requirements

- When `X --help` matches more than one route that starts with the literal segment(s) `X`, print help for all
  of them, most specific first. A single match keeps today's output unchanged.
- Only routes whose leading literal segments equal the typed literals count; do not pull in `deployment` for
  `deploy`, and do not list routes from other groups.
- Works for fluent `.Map(...)`, `[NuruRoute]` endpoints, and grouped routes (`WithGroupPrefix`).
- Remove the `[Skip]` and make `Should_show_help_for_multiple_routes_with_same_prefix` pass; add cases for a
  single match (unchanged), a group prefix, and a non-matching near-prefix (`deploy` vs `deployment`).
- Update the per-route help doc to describe the multi-route case.
- Out of scope: new analyzer diagnostics (Options 2 and 3). File a follow-up if still wanted.

## Related Patterns

- Optional parameters: `deploy {env?}` handles both cases in one route
- Group prefix: `.WithGroupPrefix("deploy")` with subcommands `""` and `{env}`

## Analyzer Considerations

- Should NURU warn when two routes share same literal prefix?
- Recommendation to use optional param or group?
- Related to existing NURU_R003 (unreachable route) detection

## Notes

Discovered while testing per-route help (Task #356). Test skipped pending design decision.

## Results

`deploy --help` prints every route whose leading literal segments equal `deploy`, most specific first, in the existing per-route layout (pattern, description, parameters, options, examples). A single match stays that one route. `deployment` and `deploy status` are different literal sequences, and a `git deploy` route stays on `git deploy --help`.

Shared-prefix help is emitted before the group summary. `.WithGroupPrefix("deploy")` with `""` and `{env}` therefore prints those two per-route blocks, and a longer subcommand such as `deploy status` is left out of that invocation. Group summaries for prefixes like `worktree` (where each subcommand adds another literal) are unchanged.

Fluent `.Map`, `[NuruRoute]` endpoints, and grouped routes all go through the same check. No analyzer follow-up: the decision is to list the routes, not to diagnose them toward `{env?}` or a group.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/help/help-01-per-route-help.cs
```

Expect: exit code 0, `Total: 19`, `Passed: 19`. `Should_show_help_for_multiple_routes_with_same_prefix` fails if either description is missing or `Deploy to environment` is not printed before `Simple deploy`.

`dotnet build timewarp-nuru.slnx` succeeded with 0 warnings and 0 errors. `dotnet run tests/ci-tests/run-ci-tests.cs` exited 0: multi-mode total 1770, passed 1764, skipped 6 (`PerRouteHelp` 19 passed), and every standalone phase passed.

## Notes for the implementer

- **Commit and push your changes before reporting done.**
- Run the build and test gate in the foreground.
- Task 435 (blank line before Commands) runs first in the same stacked set and touches `help-emitter.cs`; build on it.
