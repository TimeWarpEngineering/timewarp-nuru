# Decide Nuru GA exit from perpetual beta

Parent: 458 (finding F8 in `458-*/review/findings.md`). **Decision task — requires
Steven, not implementable by an agent alone.**

## Description

Nuru mainline is at `3.0.0-beta.71` — seventy-one consecutive prereleases. SemVer
ordering works, but "beta" tells consumers "unstable, hidden without
`-Prerelease`" while the framework is used as production infrastructure across the
org. Convention.md rule 9: prerelease on mainline is legitimate only as a declared
pre-GA state with written exit criteria — not by drift.

Decide one of:

1. **Ship `3.0.0` GA** — next release drops the `-beta.N` suffix; subsequent work
   uses patch/minor bumps per SemVer.
2. **Stay prerelease deliberately** — write the exit criteria (e.g. "API frozen
   after source-generator endpoint work X lands, GA at that point") into the
   releasing guide, with a review date.

## Checklist

- [ ] Decision recorded here (option, rationale, date)
- [ ] If GA: bump props to `3.0.0`, cut release per the (new) release flow
- [ ] If staying beta: exit criteria + review date written into the releasing guide (458-008)

## Notes

Not blocking the other 458 children — the mechanics are identical either way.
