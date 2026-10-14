# Round 1 — general
**Date:** 2026-10-14
**Scope reviewed:** branch vs `master`, product files only (see review-framework.md)

## Summary

The bump is correct where it matters for the release: `<Version>` reads `3.0.0`, no other product pin of
`3.0.0-beta.80` exists (the remaining hits are the historical props comment, test fixtures, dated docs,
and stale `bin/`/`obj/` output), and the stable install wording in the migration guide and `tw-nuru`
skill is right. The changelog promotion matches the checklist (empty `[Unreleased]` above
`[3.0.0] - 2026-10-14`, entries untouched, no link-reference list to update). One problem: the
rewritten "What did not move" paragraph in the migration guide states something the changelog
contradicts, and it ships in the stable user guide.

## Issues

### Issue 1 — Severity: bug
- File: documentation/user/guides/migrating-to-3.0.md:177
- Description: The rewritten paragraph says "Changes after beta.19 through stable `3.0.0` are under
  `## [3.0.0]`" and "Per-beta notes for beta.20 through beta.77 were not kept". Both claims are false
  against `changelog.md`: it has per-beta sections `## [3.0.0-beta.20]` (line 426) through
  `## [3.0.0-beta.79]` (line 28), backfilled by d7c53758 on 2026-10-05, the day after the guide was
  written (85f7d89a, 2026-10-04). `## [3.0.0]` holds only the changes since beta.79 (the beta.80 changes
  plus the post-beta.80 work). The old wording was already stale; this diff rewrote the paragraph and
  kept the wrong claim, now pointing 3.0 users at `[3.0.0]` for beta.20+ history that is not there.
- Suggestion: Replace the paragraph with what the changelog holds, e.g. "`changelog.md` has a section
  for each published 3.0 beta through `3.0.0-beta.79`. `## [3.0.0]` lists the changes since beta.79,
  including those that shipped in beta.80. Later work is under `## [Unreleased]`." Drop the "were not
  kept" sentence.
- Status: open

### Issue 2 — Severity: nit
- File: changelog.md:12
- Description: The `[3.0.0]` lead says "see the Packaging entry", but the section has two
  `**Packaging**` entries (lines 21 and 22); the Debug/Release one is the first. The lead also does not
  say what range `[3.0.0]` covers, while `[3.0.0-beta.79]` opens with "Changes since `3.0.0-beta.76`".
- Suggestion: Say "see the first Packaging entry under Fixed" (or name task 503), and open with
  "Changes since `3.0.0-beta.79`; beta.80 has no separate section." Optional.
- Status: open
