# Cleanup Debug Diagnostics

## Description

Remove the remaining temporary debug output and restore warnings-as-errors that were relaxed during the
#364 / #382 investigations.

## Update 2026-09-28 (triage 477)

Partly done: the `NURU_DEBUG*` diagnostics in `app-extractor.cs` and `NURU_DEBUG_CONV1` in
`dsl-interpreter.cs` were removed by commit f59826bd (454-028). `source/Directory.Build.props` already has
`TreatWarningsAsErrors` true. What remains is below.

## Remaining work

- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`: remove the DEBUG comments
  emitted into generated code (CustomConverters.Length and converter-lookup tracing, around lines 70-72 and
  846-850 on 2026-09-28).
- Check `nuru-generator.cs` for any leftover debug code.
- Re-enable `TreatWarningsAsErrors` (currently `false`) in:
  - `tests/Directory.Build.props`
  - `tests/timewarp-nuru-tests/Directory.Build.props`
  - `samples/Directory.Build.props`
- Fix the warnings this surfaces (do not suppress wholesale). If a sample or test has a warning that is
  intentional for the scenario it demonstrates, suppress that one diagnostic locally with a comment.

## Checklist

- [ ] DEBUG comments removed from route-matcher-emitter.cs generated code
- [ ] nuru-generator.cs checked
- [ ] TreatWarningsAsErrors re-enabled in tests/, tests/timewarp-nuru-tests/, samples/
- [ ] Surfaced warnings fixed
- [ ] Release build, CI test gate, and samples check pass

## Notes

- Implementer: **commit and push your changes before reporting done.**
- Run the build and test gate in the foreground.
