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

## Checklist

- [ ] Read the current emission in `service-resolver-emitter.cs` / `service-scope-expressions.cs` and record where eager construction happens
- [ ] Emit lazy construction for singletons, trim-safe
- [ ] Test: a singleton whose constructor records a counter is not constructed on `--version`, `--capabilities`, a parse error, or a forwarded call (daemon test project)
- [ ] Test: a locally handled route constructs it exactly once across handler and behavior
- [ ] Sample and docs updated; sample timing in `served-apps.md` re-measured
- [ ] Full CI after `ganda runfile cache --clear`

## Notes

- Filed from 508-005's implementer notes (2026-10-19): "Making singletons lazy in the generator would fix this for all apps; it deserves its own task."
- Measured in the sample before this task: a state-loading call took 1.07 s in-process and 0.08 s forwarded, but only because the sample used `Lazy<T>`.
- Related: 508 (parent), 508-005 (daemon), Roslynk daemon consumer.

## Session

- Created: 3910525 (2026-10-19)
