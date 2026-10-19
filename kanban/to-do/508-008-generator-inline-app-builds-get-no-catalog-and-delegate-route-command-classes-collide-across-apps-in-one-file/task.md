# Generator: inline app builds get no catalog and delegate-route command classes collide across apps in one file

## Description

Two generator defects seen while writing tests for 508-003 and 508-005. Both bite only when a compilation holds more than one `NuruApp.CreateBuilder()…Build()` chain, which is exactly the shape of every test file and of the CI multi-mode assembly.

### A. Inline app builds get no capabilities catalog (508-005)

Two `CreateBuilder()…Build()` chains written inline inside test methods, one inside a `foreach` body and one whose app was only used with `ForwardAsync`, produced an app with no generated catalog. `app.Capabilities` threw "No generated capabilities catalog is attached to this NuruApp" (`source/timewarp-nuru/nuru-app.cs:72`). The identical chains returned from a helper method worked. The 508-005 tests use helpers as a workaround (`tests/timewarp-nuru-daemon-tests/`). Likely the `Build()` interceptor that attaches the catalog (508-002) is not emitted, or is emitted for a different interception site, when the chain sits inside certain statement shapes.

### B. Delegate-route command classes collide across apps (508-003)

When an `INuruBehavior` applies to a delegate (fluent `Map().WithHandler(...)`) route, the generator emits a command class named `__Route_{routeIndex}_Command` (`command-class-emitter.cs:90`). The name uses the global route index but the class is used with the per-app index, so two apps in one file collide when one of them has behaviors on delegate routes. The 508-003 behavior test uses a `[NuruRoute]` endpoint to avoid it.

## Requirements

- A: every `Build()` call in the compilation gets its catalog regardless of the enclosing statement (loop body, lambda, expression-bodied member, local function, `using` declaration). Add a generator test that builds three apps inline in one method, one in a `foreach`, and asserts each has a catalog with its own routes.
- B: command class names are unique per app and per route; two apps in one file with behaviors on delegate routes compile and dispatch correctly. Add a test with two such apps in one file.
- Record the root cause of A in Results; if it is an interception-site key problem, check whether `Map<T>()` and `AddMcpRoute()` / `AddServeRoute()` interceptors share it.

## Checklist

- [ ] Repro A as a failing test; find the emission gap
- [ ] Fix A
- [ ] Repro B as a failing test (two apps, behaviors on delegate routes, one file)
- [ ] Fix B (app-scoped naming)
- [ ] Remove the helper-method workaround in the daemon tests and the endpoint workaround in the 508-003 behavior test, or note why they stay
- [ ] Full CI after `ganda runfile cache --clear`

## Notes

- A filed from 508-005 task Results (2026-10-19); B filed from 508-003 task Results (2026-10-18). Both marked "worth its own task" by the implementers.
- Related: 508 (parent), 508-002 (catalog attach at `Build()`), 508-003, 508-005.

## Session

- Created: 3913282 (2026-10-19)
