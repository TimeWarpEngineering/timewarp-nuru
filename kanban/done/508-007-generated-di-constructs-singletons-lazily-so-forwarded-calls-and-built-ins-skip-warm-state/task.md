# Generated DI constructs singletons lazily so forwarded calls and built-ins skip warm state

## Description

The source-generated DI (`service-resolver-emitter.cs`, `service-scope-expressions.cs`, `interceptor-emitter.cs`) constructs every singleton when services initialize, on every CLI call. That includes calls that never run a handler: `--version`, `--help`, `--capabilities`, and, since 508-005, every route the CLI forwards to a running daemon. An app that loads warm state in a singleton constructor (Roslynk's loaded Roslyn workspaces are the driving case) pays the full load in the CLI and the daemon gains nothing.

508-005 shipped the workaround: the served-app sample wraps its state in `Lazy<T>` and `documentation/user/features/served-apps.md` tells users to keep expensive work out of singleton constructors. That is advice a user can miss. The generator should make it unnecessary.

## Requirements

- A singleton is constructed on first resolution, not at service initialization. Resolution order and lifetime semantics are otherwise unchanged (one instance per app, disposed with the app).
- A forwarded call (508-005 `RouteForwarder` path), a built-in route, and a parse error construct no user singletons. A locally handled route constructs only the singletons its handler and behaviors reach.
- Behaviors that depend on a singleton still see one instance shared with the handler.
- AOT: no reflection or `Activator`; the emitted code stays trim-safe and `dotnet publish -p:PublishAot=true` on the served-app sample stays warning-free.
- Remove the `Lazy<T>` workaround from `samples/endpoints/16-daemon/served-app.cs` and soften the served-apps doc line to a note about what the generator guarantees.

## Goal (implementer restatement)

Source-generated DI must stop building user services up front. Today `EnsureServicesInitialized` builds every singleton (and process-scoped service) before routing, and `BeginSingleRunServices` / `BeginReplCommandServices` / `EnsureSessionServices` build every command- and session-scoped service, so `--version`, `--help`, `--capabilities`, a parse error and a forwarded daemon call all pay for warm state they never use.

Acceptance:

- Each cached service (singleton, process `Scoped`, session, command) is built by a generated `__Resolve…()` accessor the first time something reads it. Lifetimes stay the same: one singleton per app (reset when a new app instance initializes), one session or command instance per scope, the same disposal tracking.
- Singleton and process-scoped accessors take `__nuruInitLock` (double-checked), so concurrent daemon calls still build one instance.
- `--version`, `--capabilities`, a parse error and a forwarded call construct no user singleton (counter test in the daemon test project). A locally handled route whose handler and behavior both inject the singleton constructs it once, and both see the same instance.
- Plain `new` expressions only: no reflection, no `Activator`, trim/AOT-safe.
- The served-app sample drops `Lazy<T>`; `served-apps.md` says what the generator guarantees and gives re-measured timings.
- Full CI is green after `ganda runfile cache --clear`; `ganda repo audit` is clean.

## Checklist

- [x] Read the current emission in `service-resolver-emitter.cs` / `service-scope-expressions.cs` and record where eager construction happens
- [x] Emit lazy construction for singletons, trim-safe
- [x] Test: a singleton whose constructor records a counter is not constructed on `--version`, `--capabilities`, a parse error, or a forwarded call (daemon test project)
- [x] Test: a locally handled route constructs it exactly once across handler and behavior
- [x] Sample and docs updated; sample timing in `served-apps.md` re-measured
- [x] Full CI after `ganda runfile cache --clear`

## Results

Source-generated DI no longer builds user services when services initialize. Each cached service field now has a generated `__Resolve<field>()` accessor that builds the instance the first time something reads it. Handlers, behaviors, constructor arguments and the generated mediator's factories all read through these accessors.

Where eager construction happened before this change (all in `interceptor-emitter.cs`):

- `EnsureServicesInitializedLocked` built every singleton, and every non-REPL `Scoped` service, on the first call for an app.
- `EnsureNonReplScoped` built non-REPL `Scoped` services after a REPL first call.
- `EnsureSessionServices`, `BeginReplCommandServices` and `BeginSingleRunServices` built every session-, command- and late-`Scoped` service at the start of each call, before routing.

Each of those reached singletons through constructor arguments. `service-scope-expressions.cs` `Read` returned the bare field.

What changed:

- `service-scope-expressions.cs`: `Read` returns `__Resolve…()` calls (`ResolverName`). The `Scoped` REPL ternary picks between two accessors.
- `interceptor-emitter.cs`:
  - Singleton and process-`Scoped` accessors do a double-checked `__nuruInitLock` (`??=` inside the lock), so concurrent served calls build one instance. A `Scoped` service that injects a session or command service is still built once per single run: it is tracked with the command and cleared in `EndSingleRunServicesAsync`.
  - Session and command accessors are `field ??= Construct(...)` on the per-call `__NuruCall`, with the same `__TrackSession` / `__TrackCommand` disposal. Dependencies are tracked first, so they are still disposed after their dependents.
  - `EnsureServicesInitialized(app, configuration)` sets only the framework fields. A new app instance resets the process fields to null, which keeps one instance per app.
  - Removed `EnsureNonReplScoped`, `EnsureSessionServices`, `BeginReplCommandServices`, `BeginSingleRunServices`, `__nonReplScopedReady` and `SessionServicesReady`.
- The emitted code is plain `new` plus `??=`, with no reflection or `Activator`. `dotnet publish samples/endpoints/16-daemon/served-app.cs -c Release -r linux-x64 -p:PublishAot=true` printed no warnings. In the native binary, `--version` took 0.02 s and `--no-daemon count hello` took 1.02 s.
- Sample: `WarmIndex` now sleeps for one second in its constructor, and the `Lazy<T>` is gone. `served-apps.md` replaces the "keep expensive work out of singleton constructors" advice with what the generator guarantees, and has re-measured timings: `count hello` took 1.16 s in-process, 0.19 s forwarded, and `--version` took 0.05 s (JIT build, 2026-10-20). The changelog has an entry under Changed.
- Tests:
  - New `tests/timewarp-nuru-daemon-tests/daemon-05-lazy-singletons.cs` has 5 tests. 4 of them fail on the old emitter. The parse-error case already passed there and stays as a guard.
  - `generator-35` now asserts the resolver call.
- Full CI after `ganda runfile cache --clear`: multi-mode 2049 passed, 6 skipped. Standalone: 31 files, 0 failed. Exit 0.
- `ganda repo audit`: after the first commit, `runfile-executable` flagged the new test runfile as missing its executable bit. `ganda repo audit --fix` set it, and the audit passes.

Observed, not changed here: a method-group handler (`.WithHandler(Cls.Method)`) on an app with an `AddBehavior` does not compile in the JSON-bound (`TryBind` / `--json-args`) dispatch, because `Command = __command` is undefined there (CS0103). This task did not touch behavior-context emission. The test uses a lambda instead. It is worth its own task.

### Review round 1 fix (M1)

M1: a singleton that injected a Scoped service over a command service kept the first run's instance, and the run disposed it. The fix:

- `service-scope-expressions.cs`: new `IsPerRun` covers session and command services, and a `Scoped` service that `DependsOnInvocationScope`. `Read` now takes the service list.
  - In `ProcessInit` mode (a singleton or a non-late `Scoped` constructor), a per-run dependency is constructed inline and untracked. The singleton gets its own copy and never reads the run's slot.
  - A late `Scoped` service always reads its `__cmd_` slot on the call, in single runs and in the REPL.
- `interceptor-emitter.cs`: late `Scoped` services no longer get a static process field or a locked process resolver. They are built by the command-slot resolver, tracked with the command, and nulled in `EndSingleRunServicesAsync` (`__cmd_` field). Concurrent served calls each get their own instance, where before they shared one static field.
- `dependency-graph-builder.cs`: `DetectLifetimeMismatches` reports NURU056 for Singleton → `Scoped` when that `Scoped` service reaches a session or command service (`IsCaptiveScopedDependency`).
- Tests:
  - New `generator-57-singleton-over-per-run-scoped.cs` (CI multi-mode) makes two calls. Each handler gets a new, live token and a holder that carries it. The first token is disposed after its run. The singleton is shared, holds its own holder, and never sees a disposed token. On the previous emitter it fails at `first.Root.Holder.ShouldNotBeSameAs(first.Holder)`.
  - New `generator-58-nuru056-per-run-scoped.cs` (Roslyn-hosted, standalone in CI) checks NURU056 for the command and session cases, and checks that a plain `Scoped` service gives no warning. Removing the pragma from generator-57 fails its build with that NURU056.
- `documentation/user/features/configuration.md` and the changelog entry describe the per-command late `Scoped` lifetime and the NURU056 case.
- Full CI after `ganda runfile cache --clear` exits 0. Standalone: 32 files, 0 failed.

### Review disposition

- Rounds: 2. Roster: general. Effort: 2.
- Round 2 re-reviewed `b3c94eeb..53d490aa`. M1 stays fixed. No new findings.
- Counts: bug 0 open, 1 fixed, 0 wontfix. Suggestion 0. Nit 0.
- Disposition: clean (`review/disposition.md`).
- Paths: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-daemon-tests/daemon-05-lazy-singletons.cs
dotnet build samples/endpoints/16-daemon/served-app.cs
SD=$(mktemp -d); chmod 700 $SD; export NURU_DAEMON_SOCKET=$SD/sa.sock
DLL=$(ls -d ~/.local/share/dotnet/runfile/served-app-*/bin/debug/served-app.dll | head -1)
time dotnet $DLL --version
time dotnet $DLL --no-daemon count hello
dotnet $DLL --daemon count hello
time dotnet $DLL count hello
dotnet $DLL serve --stop
dotnet run tests/timewarp-nuru-tests/generator/generator-57-singleton-over-per-run-scoped.cs
dotnet run tests/timewarp-nuru-tests/generator/generator-58-nuru056-per-run-scoped.cs
```

Expect:

- The test file passes 5 of 5.
- generator-57 passes 1 of 1, and generator-58 passes 3 of 3.
- `--version` returns in well under 0.2 s: the 1 s `WarmIndex` load never runs.
- `--no-daemon count hello` prints `5` in about 1.1 s: it loads in-process.
- With the server running, `count hello` prints `5` in about 0.2 s: the CLI does not construct `WarmIndex`.
- The full suite `dotnet run tests/ci-tests/run-ci-tests.cs` exits 0.

## Review

- Round 1: Blocked — open M1
- Round 1 fix pass (implement-fix-2): M1 fixed. See `### Review round 1 fix (M1)` under Results.
- Round 2: Done
## Notes

- Filed from 508-005's implementer notes (2026-10-19): "Making singletons lazy in the generator would fix this for all apps; it deserves its own task."
- Measured in the sample before this task: a state-loading call took 1.07 s in-process and 0.08 s forwarded, but only because the sample used `Lazy<T>`.
- Related: 508 (parent), 508-005 (daemon), Roslynk daemon consumer.

## Session

- Created: 3910525 (2026-10-19)
- 2026-10-20 implementer (claude, ganda task work): read the emitters, then emitted `__Resolve` accessors for every cached lifetime and removed eager init. Added the daemon-05 tests, checked that they fail on the old emitter, and updated the sample, docs and changelog. Ran the AOT publish, full CI and audit (all green).
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-20T14:56:38Z
- Review oracle: grok session `01a159d0-7e82-7272-80f0-af06c514b1c3` (2026-10-20), effort 2, roster general, round 1. M1 is open. No disposition yet. See `review/round-1/merged.md`.
- Review oracle: review by reviewer-grok (grok, model grok-4.7), session not reported, max-turns 120 — 2026-10-20T15:13:43Z
- 2026-10-20 implement-fix-2 (claude, ganda task work): fixed M1. Late Scoped services moved to the call's command slot. Process constructors build their own copy of a per-run service. NURU056 now covers Singleton → late Scoped. Added generator-57 and generator-58, updated docs and changelog. Full CI is green.
- Review round 2: grok session `01a159eb-7dd2-79d2-b578-8e4331034990` (2026-10-20), effort 2, roster general. M1 fixed, no new findings, disposition clean.

## Learnings

Misread between `## Requirements` and `## Goal (implementer restatement)`. The task constructs a singleton on first resolution and keeps other lifetime semantics unchanged. The restatement defers every cached lifetime to first read, which is what stops a forwarded call from building a singleton through an unused session or command service. The code follows that model. Round 1 M1 was the captive case it created: a singleton constructor read a per-run Scoped service the call then disposed. Round 2 confirmed the fix stays on that model. The per-run Scoped service lives in the call's command slot, and a process constructor builds an untracked copy, instead of restoring eager init. The restatement's acceptance list names `--version` and `--capabilities` and leaves out `--help`. The original built-in list includes `--help`, and `daemon-05` plus the changelog cover it.
