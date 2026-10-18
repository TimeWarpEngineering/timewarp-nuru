# Round 1 — general
**Date:** 2026-10-19
**Scope reviewed:** branch `task/508-003-expose-generated-json-args-binding-and-handler-inv` vs `master`

## Summary

`NuruApp.TryBind` names a route by its catalog pattern and binds a JSON object through the same generated per-route method `--json-args` calls. `InvokeAsync` runs configuration, services, the command scope, behaviors, and the handler, and returns the boxed result. `RenderAsync` writes that result with the CLI renderer, problem-details, or output strategy and returns the CLI exit code. The generated code stays reflection-free, `Build()`-only apps get the entry points, and an empty-argv invoke does not auto-start the REPL.

Checked the emitter, `NuruApp`, the new invocation types, docs, and the generated bind/invoke for the parity app against the task and the implementer restatement. Re-ran `routing-34-json-bind-invoke.cs` (10 passed) and `routing-33-json-args.cs` (19 passed). No defect found.
