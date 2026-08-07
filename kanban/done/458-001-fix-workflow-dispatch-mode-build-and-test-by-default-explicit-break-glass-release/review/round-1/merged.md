# Round 1 — merged findings (reviewer: general-purpose/sonnet, agent ad31042f2620c12d0)

Diff: commit `dfbae796`. YAML condition matrix, injection surface, multi-mode
contamination, standalone compilability, RCS1037: all verified clean by reviewer.

| # | Sev | Finding | Status | Disposition |
|---|-----|---------|--------|-------------|
| 1 | MED | `ci-mode.cs` ships via the DevCli package wildcard glob; downstream repos with their own local `internal enum CiMode` in copied `workflow-command.cs` hit CS0101 on package bump with no migration note | fix | Accept the hard break as the intended forcing function (blind package bumps must not silently keep the old dispatch→Release behavior; CS0101 is loud and immediate). Mitigate: document the new service + migration note ("delete your local CiMode enum / update workflow-command.cs") in the DevCli readme. Org rollout (458 program) updates each repo deliberately. |
| 2 | LOW | Unknown non-empty explicit `--mode` (e.g. typo `relase`) silently falls through to Pr — quietly downgrades an intended break-glass release to a no-op build | fix | Fail loud: throw ArgumentException listing valid values. Matches the commit's own guard philosophy; the quiet-failure class is what 458 exists to remove. Update bogus-mode test to assert throw; add whitespace-only case. |
| 3 | INFO | DevCli readme services table not updated for CiMode/CiModeDetector | fix | Folded into #1's readme update. |
| 4 | INFO | Whitespace-only explicit mode untested | fix | Covered by #2's new test (now throws). |
| 5 | INFO | No actionlint/static check for workflow YAML conditions — standing repo gap, not introduced by this commit | wontfix | Out of scope for 458-001; candidate for a future hygiene check (ganda audit). Recorded here; decider: orchestrator per review posture (no scope creep). |

Open after dispositions: findings 1–4 → fix (one batch); 5 → wontfix.
