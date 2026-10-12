# Fix migrating-to-3.0 guide capabilities description to match the flat endpoints output

## Description

**3.0.0 launch gate #3 (docs).** `documentation/user/guides/migrating-to-3.0.md` has a section
"`--capabilities` JSON is hierarchical" (around lines 41-45) saying grouped commands appear only inside a
`groups` array and ungrouped ones in `commands`. The shipped output on beta.79 is a flat `endpoints` list,
each with a `groupPath` array, plus the top-level `invocation` object (479). An agent author following the
guide would look for keys that do not exist. Line 4 also still says "The package under review is `3.0.0-beta.79`".

## Requirements

- Rewrite the section from the real output: `endpoints[]` with `pattern`, `groupPath`, `aliases`, `kind`,
  `parameters[]`, `options[]`; `invocation` with `jsonArgs`, `stdin`, `filePrefix`, `merge`, `unknownKeys`.
  Take the example from `app --capabilities` on the launch demo (`kanban/done/487-*/demo/`).
- Say what changed versus 2.x (the hierarchical shape, if 2.x had one) so the section still reads as migration.
- Replace the "package under review" sentence with release-neutral wording.
- Cross-check `documentation/user/features/agent-invocation.md` says the same thing; fix whichever disagrees.

## Checklist

- [x] Section rewritten against actual `--capabilities` output
- [x] `agent-invocation.md` consistent
- [x] Line 4 wording release-neutral
- [ ] PR merged

## Notes

- Found by task 487 (2026-10-09) while verifying the blog's capabilities claims.
- Implementation review: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`. Outcome clean. One round, general, effort 1. No open findings.

## Results

- `documentation/user/guides/migrating-to-3.0.md`: replaced "`--capabilities` JSON is hierarchical" with
  "`--capabilities` JSON is a flat `endpoints` list". History (from git tags): 2.x had no `--capabilities`;
  beta.42–beta.55 emitted nested `groups` + top-level `commands` (commit 1a45f3df); beta.56+ emits flat
  `endpoints[]` with `groupPath` (task 441); beta.79 adds `invocation` (479). The section gives an endpoint
  key table, the real JSON from the 487 launch demo app, and `--group-filter`/`-g` usage.
- Line 4 "package under review is `3.0.0-beta.79`" removed; "not on `NuruAppBuilder` in beta.78" → "in 3.0".
- `documentation/user/features/agent-invocation.md`: did not contradict; added one sentence describing the
  flat `endpoints` shape and `groupPath` so both docs say the same thing.
- The JSON example was captured 2026-10-12 by running `kanban/to-do/487-*/demo/app.cs.txt` against
  nuget `TimeWarp.Nuru 3.0.0-beta.79` (with the `InterceptorsNamespaces` workaround line).

### How to validate

Smoke:

```bash
d=$(mktemp -d) && cp kanban/to-do/487-*/demo/app.cs.txt "$d"/app.cs && cd "$d"
sed -i 's/beta.80/beta.79/' app.cs
sed -i '2a #:property InterceptorsNamespaces=$(InterceptorsNamespaces);TimeWarp.Nuru.Generated' app.cs
dotnet run app.cs -- --capabilities
```

Expect: top-level keys `name`, `version`, `endpoints`, `invocation`, and no `groups` or `commands`. Each
endpoint has `pattern`, `groupPath: []`, `aliases`, `kind` (`query` for greet, `command` for deploy),
`parameters`, and `options`. This matches the JSON block in the "`--capabilities` JSON is a flat `endpoints`
list" section of `documentation/user/guides/migrating-to-3.0.md`. `grep -n "under review\|beta.78" documentation/user/guides/migrating-to-3.0.md`
returns nothing.

### Review

- Rounds: 1. Roster: general. Effort: 1 (185-line diff).
- Counts: bug 0; suggestion 0; nit 0; open 0.
- Disposition: clean. No wontfix. No escalation.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
- Re-checked: the guide JSON block parses equal to `app --capabilities` from `kanban/to-do/487-*/demo/app.cs.txt` on `TimeWarp.Nuru` 3.0.0-beta.80, run outside the repo. Top-level keys are `name`, `version`, `endpoints`, `invocation`.

## Session

- Created: claude 2412bd45 (2026-10-09)
- Implemented: claude (ganda task work, 2026-10-12)
- Review: grok 01a12ef7-fb61-7840-9643-175e64876bf4 (2026-10-12)
- Review oracle: review by implementer-grok (grok, model grok-4.7), session not reported, max-turns 80 — 2026-10-12T07:24:40Z
