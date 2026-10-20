# Round 1 — general
**Date:** 2026-10-20
**Scope reviewed:** branch `task/508-011-verbatim-renderer-output-and-a-problem-renderer-fo` vs `master`

## Summary

`UseVerbatimRendererOutput()` is a generator-read builder call, same shape as `UseDirectInvoke()`. Renderer text is written with `Write` or `Error.Write`. Without the call, success text still uses `WriteLine` and the default problem lines still use `WriteErrorLine`. An `IResponseRenderer<SharedProblemDetails>` is resolved like a success renderer (runtime DI, then a source-generated registration, then the one visible parameterless renderer) and its text goes to stderr. The `Status` exit code (1–255, otherwise 1) is unchanged.

The implementer restatement matches the original task. The task allowed `IProblemRenderer` or `IResponseRenderer<SharedProblemDetails>`; the change uses the existing contracts interface and does not add a Nuru type to neutral contracts. Wiring the flag through the route matcher and the `--json-args` / `RenderAsync` emitters is what makes the verbatim mode cover those paths.

Checked the emitter against the previous `EmitCommandResult`, the three call sites that pass `verbatimRendererOutput`, and `FindRenderer` for the problem type. `TestTerminal.Error` is the same `StringWriter` as `ErrorOutput`, so `Error.Write` is captured and adds no newline. Re-ran `operation-renderer-output` (10/10), `generator-56-problem-renderer` (3/3), and `operation-endpoint-dispatch` (7/7, including the default problem lines and a success renderer). No product defects.

## Issues

<!-- None. -->
