# Round 2 — general
**Date:** 2026-11-27
**Scope reviewed:** fix delta 1fa4a72e (`fix: let a grouped raw route answer its own help`) against round 1 findings M1–M3. The files are route-help-emitter.cs, endpoint-builder.cs, routing-43-raw-catchall.cs, routing.md, changelog.md, and task.md.

## Summary

The fix pass closes all three round 1 findings. `EmitGroupHelpChecks` now skips a group whose routes include a raw catch-all with literal prefix (group words plus pattern literals, from `GetLiteralPrefix`) equal to the group words, compared case-insensitively. So `raw602-tail --help` and `-h` reach the handler, and a raw route with more literals (`git commit {**args}`) leaves the `git` summary in place. Per-route and shared-prefix help already exempted raw routes, so a sibling `raw602-tail show --help` still prints its own block. routing-43 now covers the grouped endpoint via `.Map<T>()` (full token set, `--help`, `-h`, `--version`, sibling help), a fluent `.WithAlias("t")` raw route, and both configuration-override behaviours. The recorded result is routing-43 at 18 passed, plus build and audit passing. One new nit: the fix adds public API that the changelog does not name.

## Issues

### Issue 1 — Severity: nit
- File: source/timewarp-nuru/builders/endpoint-builder.cs:111; changelog.md:11
- Description: `EndpointBuilder<TBuilder>.WithAlias(string)` is new public API. It has never existed on master (`git log -S"WithAlias" master -- source/timewarp-nuru` is empty). Before this change, the generator's `.WithAlias` dispatch could not be reached from user code. Adding the stub turns on fluent route aliases. The changelog entry and user docs mention only the raw catch-all, not the fluent alias.
- Suggestion: Add a changelog line such as "Fluent `.WithAlias(...)` on a mapped route (the generator already supported it)".
- Status: wontfix. The package is beta (4.0.0-beta.18), the change is additive and covered by a runtime test, and task.md Results already records it. Not worth a fix round on its own. Fold the changelog line into the next change that touches fluent aliases.

## Re-verification of round 1

- M1 — fixed. route-help-emitter.cs:361-363 skips the group summary. routing-43 `Should_pass_help_through_a_grouped_endpoint_raw_route` pins `--help` (with no `commands:` header), `-h`, `--version`, and the sibling `show --help`.
- M2 — fixed. routing-43 adds `Should_pass_every_token_through_a_grouped_endpoint_raw_route` (`.Map<T>()`, the full `Tokens` set on the `IsRaw` property) and `Should_pass_every_token_through_a_raw_route_alias`.
- M3 — fixed. routing.md Raw catch-all rules and Unknown Options now state both facts. routing-43 pins both: `Should_apply_a_configuration_override_and_keep_it_on_a_raw_route` and `Should_miss_a_raw_route_when_the_override_precedes_the_literal`.

No targeted command was run. Checks used only read-only `git log`/`git show` and file reads.
