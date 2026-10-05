# Findings — TimeWarp.Nuru 3.0 review

Baseline `5a06e900` (origin-home `master` at review start). Area evidence:

- `review/runtime-core.md` — R-1 … R-9
- `review/analyzers.md` — A-1 … A-7
- `review/parsing.md` — P-1
- `review/supporting.md` — S-1 … S-10, T-1, D-1

Re-verified in source on 2026-10-05. The first implementer log was not copied blindly. This node did not run `tw-implementation-review`. The host review node reviews the diff.

## Decision

Ship `3.0.0` after the fix-now children merge. Do not cut another beta only to hold this review.

`NuruAppBuilder.Services` and `AddReplOptions` stay through 3.0 (R-8). `TimeWarp.Nuru.Mcp` does not ship in that package set (S-4, 482-012).

## Disposition

| Id | Disposition | Child |
|----|-------------|-------|
| R-1 | fix-now | 482-001 |
| R-2 | post-3.0 for the stdout change. The guide is D-1 | 482-013 (guide only) |
| R-3 | fix-now | 482-002 |
| R-4 | fix-now | 482-003 |
| R-5 | fix-now | 482-005 |
| R-6 | fix-now | 482-004 |
| R-7 | fixed on this task (public XML). DevCli readme remains D-1 | 482-013 (readme only) |
| R-8 | post-3.0. Shims survive 3.0 | — |
| R-9 | fix-now | 482-014 |
| A-1 | fix-now | 482-008 |
| A-2 | fix-now | 482-006 |
| A-3 | fix-now | 482-005 |
| A-4 | fix-now | 482-007 |
| A-5 | post-3.0 | — |
| A-6 | post-3.0. Wording only. No reserved child | — |
| A-7 | post-3.0. Tests and help links | — |
| P-1 | fix-now | 482-013 |
| S-1 | fix-now | 482-009 |
| S-2 | fix-now | 482-010 |
| S-3 | fix-now | 482-011 |
| S-4 | fix-now (exclude the package) | 482-012 |
| S-5 / T-1 | post-3.0. Overlaps 219. CI is green | — |
| S-6 | fixed on this task | — |
| S-7 | fix-now | 482-013 |
| S-8 | fix-now | 482-013 |
| S-9 | fix-now | 482-013 |
| S-10 | fix-now | 482-013 |
| D-1 | fix-now. Living docs, parser reference, error-handling guide, `[NuruRoute]` remark, DevCli readme | 482-013 |
| Changelog beta.20–beta.77 | fix-now | 482-015 |

## Wont-fix for 3.0

- Four CS0436 warnings on AOT publish of the delegates test app. Zero IL2026/IL3050. Test-app `Mediator` beside Nuru's internal `Mediator`. Not a consumer trim leak.
- No snupkg. Embedded PDB + SourceLink.
- Analyzer shipped/unshipped split stays Unshipped (decision 2026-10-04). Help links (A-7) are the same class of metadata.
- Historical `documentation/posts/` are not rewritten to impersonate the 3.0 guide.

## Counts

- fix-now: R-1, R-3, R-4, R-5, R-6, R-9, A-1, A-2, A-3, A-4, P-1, S-1, S-2, S-3, S-4, S-7, S-8, S-9, S-10, D-1, changelog. Fifteen children. Several doc ids share 482-013. A-3 and R-5 share 482-005.
- fixed on this task: R-7 (public XML in `source/timewarp-nuru`), S-6 (skill).
- post-3.0: R-2 (stream), R-8, A-5, A-6, A-7, T-1.
- wont-fix: AOT CS0436, snupkg, analyzer Unshipped split.
