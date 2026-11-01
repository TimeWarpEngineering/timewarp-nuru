# NuGet updates, ganda audit fixes, and adopt TimeWarp.Mediator 15

## Description

Bring Nuru's package versions up to date and clear the current `ganda repo audit` failures.

Audit on master `058e60bf` (2026-11-01): 33 passed, 1 failed:

- `required-gitignore-entries` (Error, fixable): the root `.gitignore` is missing `bin/dev`, `bin/dev.dbg`
  and `bin/dev.exe`.

Known package updates:

- **TimeWarp.Contracts 2.0.0-beta.29 → 2.0.0-beta.31.** beta.31 depends on `TimeWarp.Mediator`
  15.0.0-beta.4, not on `Mediator.Contracts` 14, in both its net10 and net11 groups. That clears 481's
  blocker 1, and it means Contracts beta.31 and Mediator 15 move together.
- **TimeWarp.Mediator 14.0.0 → 15.0.0-beta.4** (Steve 2026-11-01: "2, fold it all into 566 and launch").
- Re-check every other dependency since 481 merged (2026-10-31). That covers `Directory.Packages.props`,
  `.config/dotnet-tools.json`, and the `#:package` and `#:sdk` directives. Check the org feed
  `https://nuget.pkg.github.com/TimeWarpEngineering/index.json` as well as nuget.org.

## Requirements

- Run `ganda repo audit --fix` and commit whatever it changes (policy). Fix by hand anything left that
  `--fix` does not handle, until `ganda repo audit` passes.
- Bump every package that has a newer version on its current line, including TimeWarp.Contracts beta.30.
- **Adopt TimeWarp.Mediator 15.0.0-beta.4 with TimeWarp.Contracts 2.0.0-beta.31.**
  - Replace `TimeWarp.Mediator.Contracts` 14.0.0 with `TimeWarp.Mediator` 15.0.0-beta.4, bump
    `TimeWarp.Mediator.Generators` to 15.0.0-beta.4, and remove the hold comment.
  - **Use Mediator's own migration docs as the source.** Don't rediscover the change. The docs are in
    `~/worktrees/github.com/TimeWarpEngineering/timewarp-mediator/master`:
    - `documentation/migration.md`
    - `documentation/release-notes/15.0.0.md`
    - the decision record `kanban/done/021-002-contracts-150-valuetask-contracts-messagehandlerdelegate-delete-compat-only-types/task.md`
    - Mediator's own inventory of Nuru usage, `kanban/done/021-deep-code-review-150-breaking-redesign-for-architectural-purity-and-performance/research/timewarp-nuru-usage.md`, which has file and line refs into Nuru.
  - The expected mapping, which you should confirm against those docs:
    - `Handler : ICommandHandler<X, T>`, `IQueryHandler<X, T>` and `IIdempotentCommandHandler<X, T>` become
      `IRequestHandler<X, T>`.
    - `Task` / `Task<T>` become `ValueTask` / `ValueTask<T>`.
    - `Unit.Task` becomes `Unit.ValueTask`.
    - `RequestHandlerDelegate` becomes `MessageHandlerDelegate`.
  - Handlers stay nested. The message markers `ICommand<T>`, `IQuery<T>`, `IIdempotentCommand<T>` and
    `IIdempotent` stay, and they are what classifies endpoints (`endpoint-extractor.cs`). So catalog, MCP
    and capabilities output must not change.
  - Update the generator's handler detection, which matches on names in `add-configuration-locator.cs`,
    to the 15 shape. Prefer the SemanticModel. Consider `GeneratedMediatorAttribute` for mediator
    detection instead of the `build_property.TimeWarpMediator*` reads, if 15 provides it.
  - Cover everything: the runtime, the generator and emitters, the analyzers, diagnostics, DevCli content,
    `tools/dev-cli`, Search, samples, user and developer docs, the Nuru skill (`skills/nuru/`), and tests.
  - This is a deliberate breaking change for Nuru consumers. Write the changelog entry and a migration
    section. Results must list exactly what an org repo's own endpoints must change, so the cockpit can file
    a task per consumer.
  - If something in 15 makes a Nuru feature impossible, stop and record it as a blocker. Do not stub it out.
- .NET 11 GA: if `11.0.100` GA is out when this runs, move `global.json` to it with `allowPrerelease: false`,
  move the `Microsoft.Extensions.*`, `Microsoft.Data.Sqlite.Core` and `NetAnalyzers` RC pins to GA, and drop
  `dotnet-quality: preview` in `.github/actions/setup-dotnet`. If GA is not out, leave the RC3 pins and
  say so in Results.
- Apart from the Mediator 15 move, no API changes. Any other package whose update needs more than a
  mechanical fix is held back, with the reason recorded.
- Gates: `ganda runfile cache --clear`, then build, samples, test-multi and test-standalone through
  `dev workflow --stage ...`. `ganda repo audit` must pass.
- Results: a table of every version moved (old to new), anything held back with the reason, and the
  audit fixes applied.

## Checklist

- [ ] `ganda repo audit --fix` applied and committed; audit passes
- [ ] TimeWarp.Contracts → 2.0.0-beta.31
- [ ] Mediator 14 to 15 mapping confirmed from Mediator's migration docs, recorded in Results
- [ ] Nuru on TimeWarp.Mediator 15.0.0-beta.4 everywhere; handlers nested; catalog/MCP/capabilities output unchanged
- [ ] Changelog, migration section, consumer change list
- [ ] Every other outdated package bumped (CPM, tools, directives)
- [ ] .NET 11 GA moved, or RC kept and noted
- [ ] Gates green
- [ ] Results table

## Session

- Created: Nuru cockpit 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-11-01)

## Notes

- Follows 481 (#382, .NET 11 and dependency refresh). 481 Results hold the first Mediator 15 investigation.
- Generator/emitter work: implementer-claude.
