# Update NuGet packages to latest (TimeWarp.Mediator 14.0.0-beta.5 dogfood)

## Description

TimeWarp.Mediator `14.0.0-beta.5` shipped 2026-10-13 (https://github.com/TimeWarpEngineering/timewarp-mediator/releases/tag/v14.0.0-beta.5).
It carries the 22 repository-review changes (mediator kanban 013, PR #95): net10.0-only runtime and Contracts,
smaller runtime public API, generator handler binding and type-switch fixes, incremental generator pipeline,
TWM005-TWM011 diagnostics replacing generated-code compile errors, trimming/AOT annotations on the reflection
path. Nuru consumes `TimeWarp.Mediator.Contracts` and `TimeWarp.Mediator.Generators` at `14.0.0-beta.4`
(`Directory.Packages.props:33-34`, task 475). This is the dogfood pass before Mediator `14.0.0` stable, which
is the dependency Nuru 3.0.0 waits on (task 489, option 1).

Also take every other pin in `Directory.Packages.props` to latest per the repo audit policy. At filing time
`ganda nuget outdated` listed 4 packages: the two Mediator pins (→ 14.0.0-beta.5), Microsoft.CodeAnalysis.CSharp
5.6.0 → 5.9.0, Microsoft.Build.Utilities.Core 18.9.6 → 18.10.1.

Dogfood results so far from the other consumers: timewarp-architecture 342 built and tested on beta.5 with zero
TWM diagnostics and no runtime problems; its verdict is "ready for 14.0.0 stable". timewarp-state 119 is the
same bump for State. Nuru is the heaviest generator consumer (Mcp, Search, samples, AOT), so its verdict matters most.

Filed by Claude (timewarp-mediator cockpit session) at Steven's request.

## Requirements

- Both TimeWarp.Mediator pins at `14.0.0-beta.5`; other pins at latest unless a bump breaks the build and the
  reason is recorded here. Microsoft.CodeAnalysis.CSharp: check whether 5.6.0 is a deliberate floor for Nuru's own
  generators/analyzers before bumping; a hold with the reason recorded is acceptable.
- Release build clean with zero new TWM005-TWM011 diagnostics, or each triaged: fixed here, or filed on
  timewarp-mediator with a repro when the generator is wrong.
- CS0436 count in `dotnet build tests/ci-tests/run-ci-tests.cs -c Release` recorded before and after (task 475
  recorded 4, caused by InternalsVisibleTo to `run-ci-tests`; expected unchanged).
- Full CI test gate green, samples build and run, AOT sample publishes. Any failure caused by the Mediator bump
  is filed on timewarp-mediator before this task closes.
- Changelog entry under Unreleased.
- `ganda repo audit` clean.

## Checklist

- [x] `ganda nuget outdated` re-run at start
- [x] Bump `Directory.Packages.props` (Mediator x2, CodeAnalysis.CSharp, Build.Utilities.Core, or holds recorded)
- [x] Release build: no CS errors; zero TWM diagnostics or each triaged
- [x] CS0436 before/after recorded
- [x] Build, tests, samples, AOT publish pass
- [x] Breakages from the Mediator bump filed on timewarp-mediator (none)
- [x] Changelog updated
- [x] Results: before/after versions table, TWM findings, verdict for Mediator 14.0.0 stable readiness
- [ ] `ganda repo audit` clean; PR merged (audit on this walk; PR is the host open-pr node)

## Session

- Created: 1480458 (2026-10-13)
- Implementation: grok `01a134e8-b551-7b50-99f2-bb4ea6c6e5e3` (2026-10-13)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-13T11:29:38Z
- Review oracle: claude (claude-opus-5-5), effort 1, roster general, 1 round, disposition clean — 2026-10-13
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 80 — 2026-10-13T11:30:48Z

## Results

`ganda nuget outdated` at start listed the same 4 packages as the filing. Both Mediator pins are now `14.0.0-beta.5`. The other two stay on their existing floors.

| Package | From | Latest | Decision |
|---------|------|--------|----------|
| TimeWarp.Mediator.Contracts | 14.0.0-beta.4 | 14.0.0-beta.5 | updated |
| TimeWarp.Mediator.Generators | 14.0.0-beta.4 | 14.0.0-beta.5 | updated |
| Microsoft.CodeAnalysis.CSharp | 5.6.0 | 5.9.0 | **held at 5.6.0** |
| Microsoft.Build.Utilities.Core | 18.9.6 | 18.10.1 | **held at 18.9.6** |

- **Roslyn floor**: `TimeWarp.Nuru.Analyzers` compiles against `Microsoft.CodeAnalysis.CSharp` and does not pack it. SDK 10.0.3xx ships Roslyn 5.6.0; 5.9.0 ships only in SDK 10.0.400+. Raising the pin would stop the analyzer loading on 10.0.3xx consumers. The comment in `Directory.Packages.props` already records this. Confirmed still the right hold.
- **MSBuild floor**: 18.10.1 drops `lib/net10.0` (net11.0 only) and stops copying `Microsoft.Build.Framework.dll` into the build-task output that TimeWarp.Nuru packs. 18.9.6 remains the latest stable with `net10.0`. Comment already records this.

**TWM005–TWM011:** none.

- `dotnet build timewarp-nuru.slnx -c Release`: 0 warnings, 0 errors. No TWM diagnostics.
- `./bin/dev verify-samples`: 64/64. No TWM diagnostics.
- `dotnet publish` of `tests/test-apps/timewarp-nuru-testapp-delegates` (`-c Release -r linux-x64 -p:PublishAot=true`): exit 0. No TWM diagnostics. No IL2026 or IL3050. Four CS0436 warnings, all `Mediator` imported from `TimeWarp.Nuru` (InternalsVisibleTo). The native binary runs (`status` prints `✓ System is running`).
- CI runner build: no TWM diagnostics.

Nothing from this bump was filed on timewarp-mediator.

**CS0436** for `dotnet build tests/ci-tests/run-ci-tests.cs -c Release`:

The runner's `NoWarn` includes CS0436 (`tests/Directory.Build.props` and `tests/ci-tests/Directory.Build.props`), so the stock command prints **0** before and **0** after. Task 475 recorded the warnings that suppression hides. With CS0436 removed from `NoWarn` and listed in `WarningsNotAsErrors` (not committed):

- Before (14.0.0-beta.4): **4**
- After (14.0.0-beta.5): **4**

All four name `Mediator` imported from `TimeWarp.Nuru.Mcp`. Same cause as task 475: the multi-mode assembly needs MCP and search internals, and that friend access re-exposes the internal generated `Mediator`. Unchanged.

**Tests:** `ganda runfile cache --clear` then `dotnet run tests/ci-tests/run-ci-tests.cs` exited 0. Multi-mode grand total 1883, passed 1877, skipped 6, failed 0. Standalone phase passed (generator-17 through generator-54, including the two check-version files).

**Samples run:** `endpoint-hello-world` with no args prints `Hello World`; `greet Ada` prints `Hello, Ada!`. `calculator add 2 3` prints `5`.

**Verdict:** ready for Mediator 14.0.0 stable. Nuru's generator hosts (library, MCP, search, samples, CI runner, Native AOT test app) build and run on 14.0.0-beta.5 with zero TWM005–TWM011 diagnostics and no runtime failures in the CI gate.

The packed `TimeWarp.Nuru` nuspec depends on `TimeWarp.Mediator.Contracts` and `TimeWarp.Mediator.Generators` at `14.0.0-beta.5`.

`ganda repo audit`: 30 passed, 0 failed. "Repository passes all audit checks."

### Review disposition

- Rounds: 1. Effort 1, roster: general.
- Final counts: bug 0, suggestion 0, nit 0. Open 0, fixed 0, wontfix 0.
- Disposition: **clean**. No findings were raised and there were no escalations.
- Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

### How to validate

Smoke:

```bash
ganda nuget outdated
dotnet build timewarp-nuru.slnx -c Release
./bin/dev verify-samples
dotnet run tests/ci-tests/run-ci-tests.cs
dotnet publish tests/test-apps/timewarp-nuru-testapp-delegates/timewarp-nuru-testapp-delegates.csproj -c Release -r linux-x64 -p:PublishAot=true
dotnet run samples/endpoints/01-hello-world/endpoint-hello-world.cs --
dotnet run samples/endpoints/02-calculator/calculator.cs -- add 2 3
unzip -p artifacts/packages/TimeWarp.Nuru.3.0.0-beta.80.nupkg TimeWarp.Nuru.nuspec | grep Mediator
ganda repo audit
```

Expect:

- `ganda nuget outdated` lists only `Microsoft.CodeAnalysis.CSharp` (5.6.0 → 5.9.0) and `Microsoft.Build.Utilities.Core` (18.9.6 → 18.10.1).
- The Release solution build succeeds with 0 warnings and 0 errors, and prints no TWM005–TWM011.
- `verify-samples` prints `64/64 samples built successfully`.
- The CI runner exits 0. Multi-mode total 1883, passed 1877, skipped 6, failed 0.
- AOT publish exits 0, prints no IL2026 or IL3050, and prints 4 CS0436 warnings for `Mediator` imported from `TimeWarp.Nuru`.
- Hello world prints `Hello World`. Calculator prints `5`.
- The nuspec lists both Mediator packages at `14.0.0-beta.5`.
- Audit prints that the repository passes all audit checks.

## Notes

- Related: task 489 (Mediator prerelease dependency decision for 3.0.0). Mediator 14.0.0 stable will follow
  once this and timewarp-state 119 report clean; 489 then bumps to stable and 3.0.0 can ship without NU5104.
- Mediator release notes: documentation/release-notes/release-notes-v14.0.0.md in timewarp-mediator.
- Mediator diagnostics reference: documentation/diagnostics.md in timewarp-mediator (TWM005-TWM011).
