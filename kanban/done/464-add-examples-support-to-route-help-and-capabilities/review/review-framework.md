# Review framework — task 464

**Date:** 2026-08-13
**Host task:** kanban/in-progress/464-add-examples-support-to-route-help-and-capabilities/
**Diff scope:** commit a1ae675d (implementation) vs ce1c6720, on dev
**Plan / brief:** Route Examples support — `[NuruRouteExample]` attribute + fluent `.WithExample()`,
threaded through RouteDefinition (EquatableArray) into route-help-emitter (Examples section after
Options) and capabilities-emitter (`examples` array, omitted when empty). Full plan in `task.md`
Notes.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** orchestrator 0f730c83-90e5-4a4c-8bb2-3020fdd469d6; implementer subagent a62335c989015cb74

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
- Special attention areas: generated-code string escaping, incremental-generator cache safety
  (EquatableArray semantics), capabilities JSON stability for unannotated routes, DslInterpreter
  argument extraction (named vs positional args)
