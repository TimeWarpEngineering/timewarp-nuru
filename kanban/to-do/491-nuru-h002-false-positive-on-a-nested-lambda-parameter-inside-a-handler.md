# NURU_H002 false positive on a nested lambda parameter inside a handler

## Description

**3.0.0 launch gate #4 (optional for the tag, but a reader will hit it).** `NURU_H002` ("Closure detected in
lambda handler", `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.handler.cs:25`,
emitted from `validation/handler-validator.cs`) fires on a nested lambda's own parameter:

```csharp
.Map("sum {*ids:int}").WithHandler((int[] ids) => ids.Sum(i => (long)i))
```

`i` is declared by the inner lambda, not captured from an enclosing scope, so this is not a closure.
The warning becomes an error under `TreatWarningsAsErrors`, which every TimeWarp repo sets.

## Requirements

- The validator distinguishes symbols declared inside the handler lambda (including nested lambda parameters
  and locals) from captured outer symbols. Use the semantic model (`DataFlowAnalysis.Captured` on the
  handler lambda, or symbol containment), not syntax text.
- Tests: a nested-lambda-parameter case that must not report; a real capture case that still reports.

## Checklist

- [ ] Fix in `handler-validator.cs` using semantic containment / data flow
- [ ] Generator/analyzer tests for both cases
- [ ] `changelog.md` Unreleased: Fixed entry
- [ ] PR merged

## Notes

- Found by task 487 (2026-10-09). The blog has no catch-all example, so it does not block copy; it blocks a
  reader who copies the pattern.

## Session

- Created: claude 2412bd45 (2026-10-09)
