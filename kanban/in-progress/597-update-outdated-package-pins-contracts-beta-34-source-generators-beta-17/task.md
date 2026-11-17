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
