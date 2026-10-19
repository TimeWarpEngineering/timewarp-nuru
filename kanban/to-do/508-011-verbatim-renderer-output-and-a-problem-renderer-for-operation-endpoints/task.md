# Verbatim renderer output and a problem renderer for operation endpoints

## Description

Filed from 508-006 (Roslynk CLI finding 3).

A renderer's text is written with `Terminal.WriteLine`, which adds a newline. A `SharedProblemDetails` is written to stderr as `"{Type} {Title}"` plus `Detail`.

Roslynk's CLI needs the success text to be byte-identical to its MCP text. It also needs the problem text to be its own `error=` outline.

Wanted:

- A verbatim mode that writes renderer output with `Write` (no added newline).
- An `IProblemRenderer` (or `IResponseRenderer<SharedProblemDetails>`) that replaces the default problem lines while keeping the `Status` exit code.

The emitter is `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs`. See the problem pair branch around `result.IsT1`, and the renderer lines `WriteLine(...Render(...))`.

## Checklist

- [ ] Design the opt-in (attribute, builder call, or renderer interface)
- [ ] Emit `Write` for verbatim output
- [ ] Problem renderer keeps the `Status` exit code
- [ ] Tests
- [ ] Docs in `documentation/user/features/neutral-contracts.md`

## Session

- Created: 4087962 (2026-10-19)

## Notes

Related Roslynk task 003 / TimeWarpEngineering/Roslynk#4.
