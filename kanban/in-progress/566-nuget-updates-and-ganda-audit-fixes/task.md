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

## Goal (implementer restatement)

Move Nuru from TimeWarp.Mediator 14 to 15 together with TimeWarp.Contracts 2.0.0-beta.31, clear the one
`ganda repo audit` failure, and bring every other dependency to its newest version on its current line.

Acceptance criteria:

- `ganda repo audit --fix` output is committed and `ganda repo audit` passes.
- `TimeWarp.Mediator.Contracts` 14.0.0 is replaced by `TimeWarp.Mediator` 15.0.0-beta.4; Generators are
  15.0.0-beta.4; TimeWarp.Contracts is 2.0.0-beta.31; the hold comment is gone.
- Every endpoint handler in source, DevCli content, Search, tools, samples, tests, docs, and the Nuru skill
  implements `IRequestHandler<X, T>`, returns `ValueTask<T>`, and uses `Unit.ValueTask`. Handlers stay
  nested; the message markers still set the kind, so catalog, MCP, and capabilities output do not change.
- The generator detects the 15 generated mediator without the removed profile property, registers it
  from its real class and namespace, and matches 15 handler interfaces (semantic model where possible).
- Changelog entry, a migration section, and an exact consumer change list in Results.
- Every other package is at its latest on its line; anything held back has a reason. .NET 11 GA is
  adopted if out, otherwise the RC3 pins stay and Results says so.
- Gates: runfile cache clear, then build, samples, test-multi, and test-standalone stages pass.

## Checklist

- [x] `ganda repo audit --fix` applied and committed; audit passes
- [x] TimeWarp.Contracts → 2.0.0-beta.31
- [x] Mediator 14 to 15 mapping confirmed from Mediator's migration docs, recorded in Results
- [x] Nuru on TimeWarp.Mediator 15.0.0-beta.4 everywhere; handlers nested; catalog/MCP/capabilities output unchanged
- [x] Changelog, migration section, consumer change list
- [x] Every other outdated package bumped (CPM, tools, directives)
- [x] .NET 11 GA moved, or RC kept and noted
- [x] Gates green
- [x] Results table

## Results

### Versions moved

| Package | Old | New | Note |
|---|---|---|---|
| TimeWarp.Mediator.Contracts | 14.0.0 | removed | replaced by `TimeWarp.Mediator` (15 reuses that id for the contracts) |
| TimeWarp.Mediator | (not referenced) | 15.0.0-beta.4 | `timewarp-nuru.csproj` and sample 17's contracts library |
| TimeWarp.Mediator.Generators | 14.0.0 | 15.0.0-beta.4 | still flows to apps (`PrivateAssets="none"`) |
| TimeWarp.Contracts | 2.0.0-beta.29 | 2.0.0-beta.31 | beta.31 depends on TimeWarp.Mediator 15.0.0-beta.4 in its net10 and net11 groups |

Nothing else moved: every other pin was already the newest version on its line (checked against nuget.org
on 2026-11-01 for every `PackageVersion`, every `.config/dotnet-tools.json` tool, and the one `#:sdk`
directive, `Aspire.AppHost.Sdk` 13.6.4; no runfile pins a `#:package` version). The org GitHub feed
lists only TimeWarp.Taratibu(.Routines), Ganda, Flexbox, and Zana, none of which Nuru consumes.

### Held back

| Package | Current | Newer | Reason |
|---|---|---|---|
| .NET 11 SDK and `Microsoft.Extensions.*`, `Microsoft.Data.Sqlite.Core`, `Microsoft.CodeAnalysis.NetAnalyzers` | 11.0.0-rc.3.26522.148 / 11.0.100-rc.3 | none | .NET 11 is not GA yet: nuget.org's newest is still rc.3. `global.json`, the RC pins, and `dotnet-quality: preview` stay. |
| OpenTelemetry (3 packages) | 1.20.1 | 1.21.0-rc.1 | prerelease of the next line |
| System.CommandLine | 2.0.14 | 3.0.0-rc.3 | new major, and NamingConventionBinder is pinned to the 2.0 beta5 line (benchmarks only) |
| Spectre.Console.Cli | 0.57.2 | 1.0.0-alpha | alpha of a new major (benchmarks only) |
| Serilog | 4.4.0 | 4.4.1-dev | dev build |
| CommandLineParser | 2.9.1 | 2.9.2-ci | CI build (benchmarks only) |

### Audit fixes

- `required-gitignore-entries` (Error): `ganda repo audit --fix` added `bin/dev`, `bin/dev.dbg`, and
  `bin/dev.exe` to the root `.gitignore` (commit 9eb4a433). `ganda repo audit` now passes all checks.

### Mediator 14 to 15 mapping (confirmed)

Sources: timewarp-mediator `documentation/migration.md` (sections Packages, Handlers/behaviors/Unit,
Removed types, Build properties, TimeWarp.Nuru), `documentation/release-notes/15.0.0.md`, decision record
021-002, and the 021 Nuru usage inventory. The task's expected mapping is correct:

- `ICommandHandler<X, T>`, `IQueryHandler<X, T>`, `IIdempotentCommandHandler<X, T>` → `IRequestHandler<X, T>`.
- `Task` / `Task<T>` on handlers, `ISender`, `IPublisher`, `IMediator` → `ValueTask` / `ValueTask<T>`.
- `Unit.Task` → `Unit.ValueTask` (`Unit.Value` unchanged).
- `RequestHandlerDelegate<TResponse>` → `MessageHandlerDelegate<TRequest, TResponse>`; `next(request, ct)`.
- Markers `ICommand<T>`, `IQuery<T>`, `IIdempotentCommand<T>`, `IIdempotent` stay.

Two additions the docs make that the task did not list, both handled:

- The registration class is `{namespace without dots}ServiceCollectionExtensions` in the internal
  generated namespace (`TimeWarp.Mediator.Generated` by default), not the 14 class
  `Microsoft.Extensions.DependencyInjection.GeneratedMediatorServiceCollectionExtensions`.
- 15 registers concrete handler types only, not `IRequestHandler<,>` interfaces, so Nuru's run-time
  `GetService<IXxxHandler<...>>` probe for a not-visible handler would always be null.

### What changed in Nuru

- **Generator detection** (`generated-mediator-detector.cs`): reads `build_property.TimeWarpMediatorGenerator`
  (set only by the Generators props) instead of `TimeWarpMediatorAssembly` and the removed
  `TimeWarpMediatorProfile`, and returns the `TimeWarpMediatorNamespace` namespace. The model carries
  `GeneratedMediatorNamespace`; `HasGeneratedMediator` is derived from it. `GeneratedMediatorAttribute`
  was considered and not used: a generator cannot see another generator's output in the same
  compilation, so `[assembly: GeneratedMediator]` is invisible to Nuru's generator.
- **Registration** (`interceptor-emitter.cs`): both the source-gen `__GetMediator()` container and the
  runtime-DI setup call `global::{ns}.{NsNoDots}ServiceCollectionExtensions.AddGeneratedMediator(services)`.
- **Missing handler** (`EmitMissingHandlerOnDispatch`, `handler-invoker-emitter.cs`): a route whose handler
  was not visible at build time exits 1 with `No handler is registered for <type>.` and emits no `Send`.
  15's graph is this compilation plus public types of `[MediatorAssembly]` references, which Nuru's
  visibility scan already covers.
- **Handler detection**: `add-configuration-locator.cs` matches `IRequestHandler` (semantic check on
  `TimeWarp.Mediator`, error types still match); `operation-endpoint-extractor.cs` scans `IRequestHandler<,>`
  / `IRequestHandler<>`; `HandlerInterfaceName` is `IRequestHandler<X, T>`. Classification in
  `endpoint-extractor.cs` is unchanged (markers).
- **NURU_H008** message and description name `IRequestHandler` and the compile-time graph rule.
- **Mechanical rewrite** (228 .cs files plus 16 docs): Search endpoints, DevCli content, `tools/dev-cli`,
  `tools/nuru-key-bindings`, samples, tests, benchmarks/operation-dispatch, `skills/tw-nuru-verify` probes,
  the MCP handler template, user and developer docs, `readme.md`, sample readmes, and `skills/tw-nuru`.
  One test returned `ISender.Send` directly (`generator-51`) and one test behavior used
  `RequestHandlerDelegate` (`operation-endpoint-dispatch.cs`); both fixed by hand.
- **New tests** in `generator-55`: default and configured namespace registration, `TimeWarpMediatorAssembly`
  alone does not mean a mediator, and a not-visible handler exits 1 with no probe and no `Send`.
- **Docs**: changelog entry (Unreleased, Changed), new guide
  `documentation/user/guides/migrating-to-timewarp-mediator-15.md` (linked from the guides overview, user
  overview, and the 3.0 guide), `neutral-contracts.md` handler and dispatch sections, endpoint-generator
  design note, and the Nuru skill.

Catalog, MCP, and capabilities output: unchanged. `dev --capabilities` from origin/master and from this
branch differ only in the `version` git suffix. The capabilities, catalog, and MCP test classes pass.

### Decisions and deviations

- **Nuru still references `TimeWarp.Mediator.Generators` and lets it flow** (`PrivateAssets="none"`).
  Mediator's migration page says a library should not, because 15 emits an (internal, empty) mediator
  into it and `TimeWarpMediatorAssembly=false` no longer stops that. Keeping the flow means consumers
  need no new package reference; the internal copy in Nuru is unused and the CS0436 suppression in
  the test props already covers the IVT collision. Moving the generator reference to consumers would
  be a second breaking change, so it is not done here.
- **`__GetMediator()` keeps its hidden `ServiceCollection`.** The migration page suggests a small
  `IServiceProvider` over Nuru's static graph instead. That is an optimization, not required for 15,
  and would be an internal redesign beyond this task.
- **TWM001–TWM004** live in `TimeWarp.Mediator` and do not flow transitively, so they do not report in
  a consumer that references only Nuru. Unchanged from 14, where the analyzers package was not referenced.
- **`skills/tw-nuru-verify` probes** now use the 15 shape. They verify a shipped release, so they match
  the next release, not 4.0.0-beta.11.
- **Not migrated:** `spikes/roslynk-cli` (already broken before this task, outside CI), and
  `.agent/workspace` reports and kanban history, which are historical.
- No Nuru feature is impossible on 15. No blocker.

### Consumer change list (one task per org repo)

An org repo with Nuru endpoints must change:

1. `TimeWarp.Contracts` to 2.0.0-beta.31 or later, if it pins it (earlier betas pull Mediator.Contracts 14).
2. `TimeWarp.Mediator.Contracts` to `TimeWarp.Mediator` 15.0.0-beta.4, if referenced directly (no `PrivateAssets`).
   Contracts libraries keep `[assembly: MediatorAssembly]`.
3. Every `ICommandHandler<`, `IQueryHandler<`, `IIdempotentCommandHandler<` → `IRequestHandler<`.
4. Every handler `Task<T> Handle(` → `ValueTask<T> Handle(` (an `async` body stays).
5. Every `Unit.Task` → `Unit.ValueTask`.
6. `Task.FromResult(x)` / `Task.CompletedTask` returned from a handler → `ValueTask.FromResult(x)` / `ValueTask.CompletedTask`.
7. Any method that returns `ISender.Send(...)` / `IPublisher.Publish(...)` directly → `ValueTask` return type;
   `Task.WhenAll` over sends takes `.AsTask()`.
8. Any `IPipelineBehavior` → `MessageHandlerDelegate<TRequest, TResponse>` and `next(request, ct)`.
9. Delete `TimeWarpMediatorProfile`; a `TimeWarpMediatorAssembly=false` no longer stops emission.
10. A handler for an `[Operation]` that this compilation cannot see now always exits 1 (no run-time DI lookup).

### Gates (local, SDK 11.0.100-rc.3)

- `ganda runfile cache --clear`, then:
- `dev workflow --stage build`: Pipeline SUCCEEDED; "contains all 18 required payload entries"; Release build verified.
- `dev workflow --stage samples`: 68/68 samples built.
- `dev workflow --stage test-multi`: 2176 total, 2170 passed, 6 skipped, 0 failed.
- `dev workflow --stage test-standalone`: Pipeline SUCCEEDED (generator-55: 16/16).
- `tests/devcli-consumer-check`: `dotnet build` 0 warnings, 0 errors.
- `benchmarks/operation-dispatch/operation-dispatch-overhead.cs` and `tools/nuru-key-bindings` build.
- `ganda repo audit`: passes all checks.

### Review

Round 1 (general, effort 3) raised M1 (suggestion): the Unreleased .NET 11 changelog bullet still stated Contracts 2.0.0-beta.29 and Mediator 14.0.0 as the current pins. Implement-fix-2 rewrote that bullet. Round 2 (general, effort 1) re-checked `bf4210f6...HEAD`. The bullet now names TimeWarp.Contracts 2.0.0-beta.31 and TimeWarp.Mediator 15.0.0-beta.4 as current and keeps the 481 hold as history. No new findings. Final counts: bug 0, suggestion 1 fixed, nit 0; open 0. Disposition: clean. No wontfix and no escalation. Paths: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run --file tools/dev-cli/dev.cs -- workflow --stage build
dotnet run --file tools/dev-cli/dev.cs -- workflow --stage samples
dotnet run --file tools/dev-cli/dev.cs -- workflow --stage test-multi
dotnet run --file tools/dev-cli/dev.cs -- workflow --stage test-standalone
dotnet run tests/timewarp-nuru-tests/generator/generator-55-missing-operation-handler.cs
unzip -p artifacts/packages/TimeWarp.Nuru.4.0.0-beta.11.nupkg TimeWarp.Nuru.nuspec | grep -i mediator
git grep -nE "ICommandHandler|IQueryHandler|IIdempotentCommandHandler|Unit\.Task\b" -- source samples tests tools skills documentation/user readme.md
grep -n "NET 11 (breaking)" changelog.md
ganda repo audit
```

Expect:

- Every stage prints "Pipeline SUCCEEDED"; samples report 68/68; test-multi reports 0 failed (2170 passed, 6 skipped).
- generator-55 reports 16 tests passed.
- The nuspec lists `TimeWarp.Mediator` 15.0.0-beta.4 and `TimeWarp.Mediator.Generators` 15.0.0-beta.4,
  and no `TimeWarp.Mediator.Contracts`.
- The `git grep` prints only historical or before/after text: `migrating-to-3.0.md` (3.0 as shipped), the new
  `migrating-to-timewarp-mediator-15.md` before/after rows, and the skill's removal note (`SKILL.md:68`).
- The changelog `.NET 11 (breaking)` bullet names TimeWarp.Contracts 2.0.0-beta.31 and TimeWarp.Mediator
  15.0.0-beta.4 as current, with beta.29 / Mediator 14.0.0 only as the 481 history (round-1 M1).
- `ganda repo audit` prints "Repository passes all audit checks."

## Session

- Created: Nuru cockpit 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-11-01)
- Implement oracle: implementer-claude (claude-opus-5-5), 2026-11-01. Audit fix (bin/dev gitignore entries), Mediator 15 + Contracts beta.31 adoption, docs, gates green.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-11-01T05:54:40Z
- Review oracle: reviewed by reviewer-grok (grok, vendor xAI, model grok-4.7), session not reported, max-turns 200 (cursor uncapped), round 1 open M1 — 2026-11-01
- Review oracle: review by reviewer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 200) — 2026-11-01T06:01:36Z
- Fix oracle (implement-fix-2): implementer-claude (claude-opus-5-5), 2026-11-01. Fixed round-1 M1 (changelog .NET 11 bullet now names Contracts 2.0.0-beta.31 and Mediator 15.0.0-beta.4; 481 hold kept as history).
- Review oracle: reviewed by reviewer-cursor (cursor, vendor xAI, model grok-4.7), session not reported, max-turns uncapped (no --max-turns flag; bound 80), round 2 clean — 2026-11-01

## Review

- Round 1: Blocked — open M1
- Round 2: Done
## Notes

- Follows 481 (#382, .NET 11 and dependency refresh). 481 Results hold the first Mediator 15 investigation.
- Generator/emitter work: implementer-claude.
