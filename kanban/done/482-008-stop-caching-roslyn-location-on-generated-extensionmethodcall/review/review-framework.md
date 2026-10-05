# Review framework — task 482-008

**Date:** 2026-10-05
**Host task:** kanban/to-do/482-008-stop-caching-roslyn-location-on-generated-extensionmethodcall/
**Diff scope:** commit `a9fd7822` on branch `task/482-008-stop-caching-roslyn-location-on-generated-extensio` (vs its parent). Product files: `service-extraction-result.cs` (`ExtensionMethodCall.Location`), `service-extractor.cs`, `service-validator.cs`, `model-validator.cs`, `generator-37-incrementality-caching.cs`.
**Plan / brief:** Finding A-1. Stop storing a Roslyn `Location` on `ExtensionMethodCall` so an opaque `ConfigureServices` extension call does not break emit-model value equality. Keep NURU052 on a source span.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok 01a10b54-b754-76b0-a925-425e6568800f (2026-10-05)

## Budget (by-diff)

- Lines changed: 181
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
