# Round 1 — general
**Date:** 2026-10-21
**Scope reviewed:** `4c68dda8` vs `master` — DSL interpreter app selection, per-app command class names, generator-61, generator-62, daemon-01 inline call sites, routing-34 delegate behavior test.

## Summary

The change makes one compilation that holds several `CreateBuilder()…Build()` chains attribute each `Build()` to the app that call builds, and it names delegate-route command classes with the app suffix the route matcher already uses. That matches both the original task and the implementer restatement. No misread.

Root cause of A is app selection, not an interception-site key. `ExtractFromBuildCall` used to interpret the containing block and keep `models[0]`. `InterpretBuildCall` now records `BuildCalls` and returns the app for the invocation that triggered extraction. `ProcessStatement` walks nested blocks, both `if` arms, loops, `using` / `lock` / `fixed` / `checked` / `unsafe`, labels, and `switch` sections. A `Build()` the walk does not reach (expression lambda, argument, expression-bodied member) is evaluated on its own. `Map<T>()`, `AddMcpRoute()`, and `AddServeRoute()` are not interceptors; they are interpreted onto the builder, so the same selection fix covers them. The generator still intercepts only `Build()`, `RunAsync()`, and `RunReplAsync()`.

Fix B emits `__Route{methodSuffix}_{routeIndex}_Command` per app from `RoutesInIndexOrder`. A single app keeps `__Route_0_Command`. `EmitPipelineWrapper` takes that suffix from the route matcher and from the JSON-args path. Both call sites pass it.

Checked against the repo: generator-61 is 5/5 and generator-62 is 1/1 on this worktree. Generated `generator-61` source has sixteen `Build_Intercepted_*` methods, one per `Build()` in that file, each wiring its own `GetCapabilities_N`. Generated `generator-62` source declares `__Route_0_0_Command`, `__Route_1_0_Command`, and `__Route_1_1_Command`, and each app's behavior code constructs its own class. The routing-34 behavior test now uses a delegate route and the `JbiTracedQuery` endpoint is gone. The two daemon-01 call sites that hit A are inline; the shared helpers stay, and Results says why. Full CI was not re-run in this review; the implementer reports it green after `ganda runfile cache --clear`.

Walking both `if` arms (and every `switch` section) is the static view the interceptors need. A builder that maps the same pattern on two exclusive arms can now get NURU_R002. That is disclosed in Results and is the cost of seeing every `Build()`. It is not a defect against the acceptance criteria.

## Issues

<!-- none -->
