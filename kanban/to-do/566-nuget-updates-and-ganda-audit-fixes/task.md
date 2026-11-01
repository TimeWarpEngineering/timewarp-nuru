# NuGet updates and ganda audit fixes

## Description

Bring Nuru's package versions up to date and clear the current `ganda repo audit` failures.

Audit on master `058e60bf` (2026-11-01): 33 passed, 1 failed:

- `required-gitignore-entries` (Error, fixable): the root `.gitignore` is missing `bin/dev`, `bin/dev.dbg`
  and `bin/dev.exe`.

Known package updates:

- **TimeWarp.Contracts 2.0.0-beta.29 → 2.0.0-beta.30** (released 2026-11-01). It now ships a `net11.0` build,
  and it still depends on `TimeWarp.Mediator.Contracts` 14.0.0.
- Re-check every other dependency since 481 merged (2026-10-31). That covers `Directory.Packages.props`,
  `.config/dotnet-tools.json`, and the `#:package` and `#:sdk` directives. Check the org feed
  `https://nuget.pkg.github.com/TimeWarpEngineering/index.json` as well as nuget.org.

## Requirements

- Run `ganda repo audit --fix` and commit whatever it changes (policy). Fix by hand anything left that
  `--fix` does not handle, until `ganda repo audit` passes.
- Bump every package that has a newer version on its current line, including TimeWarp.Contracts beta.30.
- **Keep TimeWarp.Mediator at 14.0.0.** Two blockers recorded in 481 still stand: Contracts beta.30 still
  depends on Mediator.Contracts 14, and Mediator 15 removes `ICommandHandler`/`IQueryHandler`, which is a
  Nuru API redesign. Update the hold comment in `Directory.Packages.props` to name beta.30.
- .NET 11 GA: if `11.0.100` GA is out when this runs, move `global.json` to it with `allowPrerelease: false`,
  move the `Microsoft.Extensions.*`, `Microsoft.Data.Sqlite.Core` and `NetAnalyzers` RC pins to GA, and drop
  `dotnet-quality: preview` in `.github/actions/setup-dotnet`. If GA is not out, leave the RC3 pins and
  say so in Results.
- No API changes. A package whose update needs code changes beyond a mechanical fix is held back, with
  the reason recorded.
- Gates: `ganda runfile cache --clear`, then build, samples, test-multi and test-standalone through
  `dev workflow --stage ...`. `ganda repo audit` must pass.
- Results: a table of every version moved (old to new), anything held back with the reason, and the
  audit fixes applied.

## Checklist

- [ ] `ganda repo audit --fix` applied and committed; audit passes
- [ ] TimeWarp.Contracts → 2.0.0-beta.30; Mediator hold comment updated
- [ ] Every other outdated package bumped (CPM, tools, directives)
- [ ] .NET 11 GA moved, or RC kept and noted
- [ ] Gates green
- [ ] Results table

## Session

- Created: Nuru cockpit 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-11-01)

## Notes

- Follows 481 (#382, .NET 11 and dependency refresh).
