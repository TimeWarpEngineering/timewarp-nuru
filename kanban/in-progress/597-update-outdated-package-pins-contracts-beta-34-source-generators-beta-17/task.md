# Update outdated package pins: Contracts beta.34, SourceGenerators beta.17

## Description

`ganda nuget outdated` on master (2026-11-18) reports two pins behind nuget.org. Bump both in
`Directory.Packages.props`.

| Package | From | To | Use |
|---|---|---|---|
| TimeWarp.Contracts | 2.0.0-beta.33 | 2.0.0-beta.34 | library dependency, ships in the Nuru package |
| TimeWarp.SourceGenerators | 1.0.0-beta.16 | 1.0.0-beta.17 | dev-only |

## Requirements

- Bump both pins. Fix any build error or new analyzer diagnostic the bumps bring; do not suppress.
- No version bump of Nuru itself and no release from this task.

## Checklist

- [ ] Both pins bumped in `Directory.Packages.props`
- [ ] `ganda nuget outdated` reports 0 outdated
- [ ] `ganda runfile cache --clear`, then `./bin/dev build` and `./bin/dev test` green

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-18)
