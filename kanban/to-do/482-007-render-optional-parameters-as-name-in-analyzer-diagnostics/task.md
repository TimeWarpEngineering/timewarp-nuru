# Render optional parameters as {name?} in analyzer diagnostics

## Parent

482 — Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

Finding A-4 from task 482. Fix it before the `3.0.0` tag. The parent review record is the source of the disposition. Do not reopen the whole review on this id.

Optional parameters are rewritten to square brackets, so diagnostic locations miss.

`ParameterDefinition.PatternSyntax` renders an optional parameter as `[{Name}]`, not `{name?}`. `EffectivePattern` is built from that. Overlap, duplicate, unreachable, and REPL-default lookups key on `EffectivePattern` and fall through to `Location.None` when the key is the source literal (`deploy {env} {tag?}`). The diagnostic text then shows a pattern the parser would reject.

Evidence: `source/timewarp-nuru-analyzers/generators/models/segment-definition.cs` (`PatternSyntax`), `validation/overlap-validator.cs`. Parent record: `review/analyzers.md` A-4.

## Checklist

- [ ] `PatternSyntax` for an optional parameter is `{name?}` (and `{name:type?}` when there is a constraint), not `[name]`
- [ ] Duplicate and overlap diagnostics on an optional route squiggle the `Map(...)` string
- [ ] The message text is a pattern `PatternParser` accepts
- [ ] Add a test with two routes that overlap on `{tag?}`

## How to validate

Smoke:

```bash
dotnet test tests/timewarp-nuru-tests/timewarp-nuru-tests.csproj --filter FullyQualifiedName~Overlap
```

Expect: The overlap test's diagnostic location is the source route string, and the message contains `{name?}` rather than `[name]`.

## Session

- Created: 533403 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
