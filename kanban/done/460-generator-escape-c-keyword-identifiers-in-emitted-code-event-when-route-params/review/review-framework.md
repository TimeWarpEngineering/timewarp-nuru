# Review framework — task 460

**Date:** 2026-08-10
**Host task:** kanban/in-progress/460-generator-escape-c-keyword-identifiers-in-emitted-code-event-when-route-params/
**Diff scope:** commit `dc3f79c9` (`fix: escape C# keyword identifiers…`) vs plan commit `83105343` on branch `dev`
**Plan / brief:** Harden EscapeIfKeyword with SyntaxFacts; escape untyped positional captures and method-handler args; restore sample `{event}`/`{when}`; generator-39 tests.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Orchestrator grok (2026-08-10)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Files in scope

- `source/timewarp-nuru-analyzers/generators/emitters/csharp-identifier-utils.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs`
- `source/timewarp-nuru-analyzers/generators/extractors/handler-extractor.cs`
- `samples/fluent/08-type-converters/fluent-type-converters-builtin.cs`
- `tests/timewarp-nuru-tests/generator/generator-39-keyword-param-identifiers.cs`
