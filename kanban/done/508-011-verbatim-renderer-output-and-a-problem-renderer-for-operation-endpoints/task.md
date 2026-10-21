# Verbatim renderer output and a problem renderer for operation endpoints

## Description

Filed from 508-006 (Roslynk CLI finding 3).

A renderer's text is written with `Terminal.WriteLine`, which adds a newline. A `SharedProblemDetails` is written to stderr as `"{Type} {Title}"` plus `Detail`.

Roslynk's CLI needs the success text to be byte-identical to its MCP text. It also needs the problem text to be its own `error=` outline.

Wanted:

- A verbatim mode that writes renderer output with `Write` (no added newline).
- An `IProblemRenderer` (or `IResponseRenderer<SharedProblemDetails>`) that replaces the default problem lines while keeping the `Status` exit code.

The emitter is `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs`. See the problem pair branch around `result.IsT1`, and the renderer lines `WriteLine(...Render(...))`.

## Goal (implementer restatement)

Roslynk wants its CLI text to match its MCP text byte for byte, and wants to write its own
error text. Today the generated code adds a newline after every renderer result, and always
writes a problem as `"{Type} {Title}"` plus `Detail`.

Design: an app-level builder call, `UseVerbatimRendererOutput()`, read by the generator the
same way as `UseDirectInvoke()`. Neutral contracts stay free of Nuru types. The problem
renderer is the existing `TimeWarp.Contracts.IResponseRenderer<SharedProblemDetails>`, found
the same way as a success renderer (registered service, runtime DI, or the one visible
parameterless renderer). No new interface.

Acceptance criteria:

- With `UseVerbatimRendererOutput()`, renderer text is written with `Write` and gets no added
  newline. This covers registered, runtime-DI, and visible renderers, on the argv, `--json-args`,
  and `RenderAsync` paths. Output-strategy text (raw, ISO, JSON) still ends with a newline.
- Without the call, output is the same as before.
- When an `IResponseRenderer<SharedProblemDetails>` is available, a problem result writes that
  renderer's text to stderr instead of the default lines. The exit code is still `Status` when it
  is from 1 to 255, and 1 otherwise. In verbatim mode that stderr text gets no added newline.
- With no problem renderer, the default problem lines are unchanged.
- Tests cover all of the above. `neutral-contracts.md` documents both features.

## Checklist

- [x] Design the opt-in (attribute, builder call, or renderer interface)
- [x] Emit `Write` for verbatim output
- [x] Problem renderer keeps the `Status` exit code
- [x] Tests
- [x] Docs in `documentation/user/features/neutral-contracts.md`

## Session

- Created: 4087962 (2026-10-19)
- 2026-10-20 implementer (claude, ganda task work): builder call + problem renderer, tests, docs.
  The pre-push attest audit failed `runfile-executable`: the new
  `operation-renderer-output.cs` was committed as 100644. `ganda repo audit --fix` set the bit.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-20T09:24:25Z
- Review oracle: grok session `01a158a0-5b2a-7e03-83fc-2c854553312b` (2026-10-20), effort 3, roster general, round 1, disposition clean
- Review oracle: review by reviewer-grok (grok, model grok-4.7), session not reported, max-turns 200 — 2026-10-20T09:34:36Z

## Results

### What changed

- `UseVerbatimRendererOutput()` on `NuruAppBuilder` is a generator-read builder call, like
  `UseDirectInvoke()`. It goes through the interpreter, `IrAppBuilder`, and
  `AppModel.VerbatimRendererOutput`, then to `RouteMatcherEmitter` (argv), `JsonArgsEmitter`
  (`--json-args`, `InvokeAsync`), and `HandlerInvokerEmitter.EmitRender` (`RenderAsync`).
  Renderer text is written with `Write`, and problem-renderer text with `Error.Write`.
  Output-strategy text and the default problem lines still end with a newline.
- Problem renderer: `IResponseRenderer<SharedProblemDetails>` from TimeWarp.Contracts (no new
  interface). It is looked up the same way as a success renderer: runtime DI, then a
  source-generated registration, then the one visible parameterless renderer
  (`HandlerDefinition.ProblemRendererTypeName`, from `DescribeResponse`). Its text goes to stderr.
  The `Status` exit code rule is unchanged.
- `EmitCommandResult` now uses one helper, `EmitRendererOrFallback`, for both the success and
  problem lookups.
- Design choice: an app-level builder call, not a per-renderer attribute. Renderers live in
  neutral contracts assemblies and should not need a Nuru type, and Roslynk wants every
  renderer in its CLI to be verbatim.

### Tests

- `tests/timewarp-nuru-tests/auto/operation-renderer-output.cs` (CI multi-mode and standalone):
  10 tests. They cover the default newline, verbatim on argv, `--json-args`, and `RenderAsync`,
  the output-strategy newline kept in verbatim mode, a registered problem renderer (status 17),
  verbatim problem text, out-of-range status giving exit 1, a runtime-DI problem renderer, and
  the default problem lines when no renderer is present.
- `tests/timewarp-nuru-tests/generator/generator-56-problem-renderer.cs` (Roslyn-hosted,
  standalone list in `run-ci-tests.cs`, excluded from multi-mode): 3 tests for a visible problem
  renderer, normal and verbatim, plus the default lines with no renderer. The generated code
  compiles.
- Full `dotnet run tests/ci-tests/run-ci-tests.cs` after `ganda runfile cache --clear`:
  4205 passed, 12 skipped, 0 failed (multi-mode plus standalone).
- `ganda repo audit`: passes.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/auto/operation-renderer-output.cs
dotnet run tests/timewarp-nuru-tests/generator/generator-56-problem-renderer.cs
dotnet run tests/timewarp-nuru-tests/auto/operation-endpoint-dispatch.cs
```

Expect: 10/10, 3/3, and 7/7 passed. In `operation-renderer-output`,
`Should_write_renderer_text_verbatim_when_opted_in` asserts that stdout is exactly
`"line one\nline two"` with no trailing newline. `Should_write_a_registered_problem_renderer_to_stderr_and_keep_the_status`
asserts exit code 17 and stderr starting with `error=Bad input status=17`.
`operation-endpoint-dispatch` still asserts the default `"{Type} {Title}"` problem lines.

### Review

Round 1, roster general, effort 3 (889 lines). The restatement matches the original task: `IResponseRenderer<SharedProblemDetails>` is one of the two interfaces the task allowed, and the builder call is the opt-in the checklist asked for.

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

Disposition: **clean**. No wontfix and no escalation.

Reviewer re-ran `operation-renderer-output` (10/10), `generator-56-problem-renderer` (3/3), and `operation-endpoint-dispatch` (7/7).

Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Review

- Round 1: Done

## Notes

Related Roslynk task 003 / TimeWarpEngineering/Roslynk#4.
