# Round 1 — general
**Date:** 2026-11-27
**Scope reviewed:** branch vs `master` merge base. Source: parser (`parser.segments.cs`, `semantic-validator.cs`, `parameter-matcher.cs`, `compiled-route.cs`), generator (`json-args-emitter.cs`, `route-matcher-emitter.cs`, `route-matcher-emitter.alias-match.cs`, `route-help-emitter.cs`, `help-emitter.cs`, `help-pattern-helper.cs`, `capabilities-emitter.cs`, `endpoint-extractor.properties.cs`, `pattern-string-extractor.cs`, models), `parameter-attribute.cs`, tests (parser-20, routing-43, generator-81), docs, skill, changelog. Call sites read: `interceptor-emitter.method-body.cs` (order of the interactive, shared-prefix help, and group help checks against user routes), `RouteHelpEmitter.EmitGroupHelpChecks`, `BuildHelpArgsPattern`.

## Summary

The change adds `{**name}`, a catch-all that reads a second argv, `__rawRouteArgs`. That argv has `--json-args` peeled and still holds the configuration overrides. The route skips the built-in-flag skip, the unknown-option checks, and per-route and shared-prefix help. `{*args}` is untouched and routing-37 is unedited. NURU_S011 covers both the fluent and the endpoint form. The design is sound and the swap/restore of `routeArgs` inside a block is scoped correctly. One requirement gap remains: group-level help still runs before user routes and answers `<group> --help` for a raw route whose literal prefix equals its group prefix. That is the shape timewarp-ganda task 731 (the first consumer) describes for `ganda tail`. Test coverage runs only fluent routes; the endpoint and alias forms are checked as generated text or not at all.

The implementer restatement matches the task. No misread.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/route-help-emitter.cs:330 (`EmitGroupHelpChecks`), called before user routes at source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.method-body.cs:118
- Description: The task requires `--help` and `--version` to pass through to a raw route. The diff exempts raw routes from per-route help (`EmitRouteHelp` returns) and from shared-prefix help, but not from group-level help. `EmitGroupHelpChecks` groups every route with a `GroupPrefix` and emits `if (routeArgs is ["<group words>", "--help" or "-h"]) { …group help…; return 0; }` before the route loop. An endpoint in `[NuruRouteGroup("tail")]` with `[NuruRoute("")]` and `[Parameter(IsCatchAll = true, IsRaw = true)]` becomes the route `tail {**args}`. `myapp tail --help` and `myapp tail -h` then print Nuru's group help and exit 0. The handler never runs. timewarp-ganda task 731 plans one endpoint shared by the `tail` edition (default route, root group prefix stripped by `DiscoverEndpoints(typeof(...))`) and the full `ganda` edition (`ganda tail {raw args}`). The full-edition form keeps the group prefix and hits this. Found by reading; no command run.
- Suggestion: In `EmitGroupHelpChecks`, skip the group help check when a raw route in the app has a literal prefix (`GetLiteralPrefix`) equal to the group's words, the same way `EmitSharedPrefixHelpChecks` now skips raw routes. Add a routing test: a grouped endpoint with an empty `[NuruRoute("")]` raw catch-all, mapped with `.Map<T>()`, run `["tail", "--help"]`, and expect the handler output `--help`.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/timewarp-nuru-tests/generator/generator-81-raw-catchall-diagnostic.cs:75; tests/timewarp-nuru-tests/routing/routing-43-raw-catchall.cs
- Description: Every runtime test in routing-43 uses the fluent `Map("… {**args}")`. The endpoint form (`[Parameter(IsCatchAll = true, IsRaw = true)]`) is checked only as text in the generated source (`ShouldContain("routeArgs = __rawRouteArgs;")`); it is never compiled into an app and run, so nothing shows the tokens reach the command's `string[]` property. The endpoint form is the one the first consumer uses. The raw branch added to `EmitAliasMatch` (route-matcher-emitter.alias-match.cs:45-174, its own swap/restore block) has no test at all, so a compile or behavior break in that generated code would surface only in a consumer.
- Suggestion: Add to routing-43 one endpoint test (`.Map<RawTailEndpoint>()`, not `DiscoverEndpoints()`, per the CI multi-mode rule) that passes the same `Tokens` set and asserts the property receives them unchanged. Add one fluent `.WithAlias(...)` raw route test that runs through the alias. The Issue 1 test can share the endpoint type.
- Status: open

### Issue 3 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs:193-197; documentation/user/features/routing.md (configuration-override line "A raw catch-all (`{**args}`) keeps them.")
- Description: `__rawRouteArgs` keeps every configuration override in place, while `__configArgList` still receives them. Two effects follow that the docs do not state. (a) `tail --lines=40` reaches the handler and also sets the configuration key `lines`. (b) An override typed before the raw route's leading literal (`myapp --Logging:LogLevel:Default=Debug tail -n 4`) sits at index 0 of `__rawRouteArgs`, so the literal check for `tail` fails and the raw route does not match. Before this route existed, that override was stripped and the literal matched.
- Suggestion: Say both in routing.md's raw catch-all rules (overrides still reach configuration; put them after the command's literals). Or, for (b), have the literal-prefixed raw route match its leading literals against `routeArgs` and take the tail from `__rawRouteArgs` after the literals' position. Documenting it is enough for this task.
- Status: open

## Recorded gates

`## Results` / `### How to validate` record `./bin/dev build` (0 warnings), parser-20 (8), routing-43 (13), generator-81 (6), routing-37 unedited (18), the fluent sample, and `ganda repo audit` (pass, 1 warning). That covers the change. The CI multi-mode runner (`run-ci-tests.cs`) is not recorded; the new files use `.Map(...)` or isolated generator sources, so cross-contamination is not expected and CI runs it. AOT is argued (same generated `ExecuteRouteAsync`) rather than published; no new reflection or dynamic code is added, so that argument holds.
