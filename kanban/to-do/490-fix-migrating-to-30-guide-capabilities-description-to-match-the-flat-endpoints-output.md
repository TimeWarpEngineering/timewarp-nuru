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

- [ ] Section rewritten against actual `--capabilities` output
- [ ] `agent-invocation.md` consistent
- [ ] Line 4 wording release-neutral
- [ ] PR merged

## Notes

- Found by task 487 (2026-10-09) while verifying the blog's capabilities claims.

## Session

- Created: claude 2412bd45 (2026-10-09)
