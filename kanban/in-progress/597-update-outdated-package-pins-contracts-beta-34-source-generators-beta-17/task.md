# Update outdated package pins: Contracts beta.34, SourceGenerators beta.17

## Description

`ganda nuget outdated` on master (2026-11-18) reports two pins behind nuget.org. Bump both in
`Directory.Packages.props`.

| Package | From | To | Use |
|---|---|---|---|
| TimeWarp.Contracts | 2.0.0-beta.33 | 2.0.0-beta.34 | library dependency, ships in the Nuru package |
| TimeWarp.SourceGenerators | 1.0.0-beta.16 | 1.0.0-beta.17 | dev-only |

## Goal (implementer restatement)

Bring the two stale pins in `Directory.Packages.props` up to the latest on nuget.org:
TimeWarp.Contracts 2.0.0-beta.33 → beta.34 and TimeWarp.SourceGenerators 1.0.0-beta.16 → beta.17.

Acceptance:
- Both `PackageVersion` entries carry the new versions.
- `ganda nuget outdated` reports 0 outdated.
- After `ganda runfile cache --clear`, `./bin/dev build` and `./bin/dev test` pass; any new
  diagnostic from the bumps is fixed in code, not suppressed.
- Nuru's own version is unchanged; no release.

## Requirements

- Bump both pins. Fix any build error or new analyzer diagnostic the bumps bring; do not suppress.
- No version bump of Nuru itself and no release from this task.

## Checklist

- [x] Both pins bumped in `Directory.Packages.props`
- [x] `ganda nuget outdated` reports 0 outdated
- [x] `ganda runfile cache --clear`, then `./bin/dev build` and `./bin/dev test` green

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-18)
- Implement (2026-11-18): bumped both pins; no code changes or new diagnostics were needed.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session f302caf4-ac96-4a87-991e-9e7bcf00fadb, max-turns 500, Done — 2026-11-17T18:13:54Z
- Review oracle: reviewed by reviewer-grok (grok, vendor xai, model grok-4.7), session 01a1eab7-4b0b-75e1-bf1f-67ff1bf1c92b, max-turns 80, effort 1 (general), Done — 2026-11-18
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1eab7-4b0b-75e1-bf1f-67ff1bf1c92b, max-turns 80, artifacts general.md, merged.md, disposition.md — 2026-11-17T18:19:04Z

## Results

`Directory.Packages.props`: TimeWarp.Contracts 2.0.0-beta.33 → 2.0.0-beta.34,
TimeWarp.SourceGenerators 1.0.0-beta.16 → 1.0.0-beta.17. The bumps brought no build errors or
new analyzer diagnostics, so no source changes and no suppressions. Nuru version unchanged.

### Gates

- `ganda runfile cache --clear` → cleared 269 entries.
- `./bin/dev build` → Build succeeded, 0 warnings, 0 errors.
- `./bin/dev test` → exit 0; all suites green (multi-mode + standalone, 0 failed).
- `ganda repo audit` → Repository passes; 1 advisory warning (`vscode-window-icon`:
  `.vscode/settings.json` missing). Non-blocking and pre-existing; not touched.
- `ganda nuget outdated` → All packages are up to date.

### How to validate

Smoke:
```bash
grep -E 'TimeWarp\.(Contracts|SourceGenerators)"' Directory.Packages.props
ganda nuget outdated
ganda runfile cache --clear && ./bin/dev build && ./bin/dev test
```

Expect: Contracts shows `2.0.0-beta.34` and SourceGenerators shows `1.0.0-beta.17`;
`ganda nuget outdated` prints "All packages are up to date!"; build reports 0 warnings and 0
errors; tests end with "Tests completed successfully!".

### Review

Round 1 (general, effort 1) raised no findings. The implementer restatement matches the task. Final counts: bug 0, suggestion 0, nit 0; open 0. Disposition: clean. No wontfix and no escalation. Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Review

- Round 1: Done
