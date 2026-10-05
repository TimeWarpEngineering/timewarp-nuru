# Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

TimeWarp.Nuru is currently published as `3.0.0-beta.78`. Before cutting the official
`3.0.0` release and leaving beta, perform a complete, area-by-area code review of the
shipped packages and their supporting surface (tests, docs, samples, release plumbing).

The goal is a durable set of findings with a disposition for each (fix now / fix post-3.0 /
won't-fix), so the 3.0 release is a deliberate decision rather than "the beta that
happened to be last". Fixes that fall out of the review are tracked as child tasks of
this one; this task holds the review record itself.

Scope (shipped packages first, supporting surface second):

| Area | Location | Approx. size |
|------|----------|--------------|
| Runtime core | `source/timewarp-nuru` | 116 files / 15.5k lines |
| Analyzers + source generator | `source/timewarp-nuru-analyzers` | 118 files / 28k lines |
| Parsing | `source/timewarp-nuru-parsing` | 36 files / 3.3k lines |
| Search | `source/timewarp-nuru-search` | 12 files / 1.1k lines |
| Build / dev CLI | `source/timewarp-nuru-build`, `source/timewarp-nuru-devcli` | 24 files / 3.7k lines |
| MCP server | `source/timewarp-nuru-mcp` | frozen 2026-07-14; review only for release blockers |

Review artifacts live under `review/` in this task folder (see `tw-implementation-review`):
one findings file per area, then a merged `findings.md` with dispositions.

## Checklist

### Setup

- [x] Confirm review baseline commit (origin-home master at review start) and record it in Notes
- [x] Run `ganda runfile cache --clear` then full CI (`dotnet run tests/ci-tests/run-ci-tests.cs`); record pass/fail counts as the baseline
- [x] Build AOT sample (`dotnet publish -c Release -r linux-x64 -p:PublishAot=true`) and record trim/AOT warnings as baseline

### Runtime core (`source/timewarp-nuru`)

- [x] `nuru-app.cs`, `nuru-app-builder.cs`, `builders/` — public builder API surface, naming, `Done()`/`Build()` lifecycle, obsolete members to remove before 3.0
- [x] `repl/` (9.1k lines, largest area) — key bindings, multiline buffer, selection, yank, history; look for dead code and duplicated editing logic
- [x] `completion/` — shell completion generation and runtime completion
- [x] `dependency-injection/` — service registration, constructor resolution, scope lifetime
- [x] `type-conversion/` — converters, nullable/optional handling, error messages
- [x] `help/`, `options/`, `configuration/`, `logging/`, `telemetry/`, `serialization/`, `capabilities/`, `io/`, `check-updates/`
- [x] `attributes/`, `abstractions/`, `endpoints/`, `extensions/`, `services/` — small files, check each for public-API leakage
- [x] Public API audit: every `public` type/member is intentional, documented (XML docs), and AOT/trim safe
- [x] Error handling consistent with `documentation/developer/reference/error-handling.md`

### Analyzers and source generator (`source/timewarp-nuru-analyzers`)

- [x] `generators/nuru-generator.cs` pipeline — incrementality, cacheability of models, no `SemanticModel` captured in outputs
- [x] `generators/locators/`, `extractors/`, `ir-builders/`, `interpreter/`, `emitters/` — SemanticModel over syntax-string type resolution (per `.agent/local/nuru-specific.md`)
- [x] `diagnostics/` — every descriptor has an id, category, help link, and a test; messages are user-facing quality
- [x] `validation/` — route pattern validation matches runtime parser behaviour
- [x] Generated code compiles warning-free under `TreatWarningsAsErrors` in consumer projects

### Parsing (`source/timewarp-nuru-parsing`)

- [x] Lexer / parser / semantic passes against `documentation/developer/reference/parser-classes-syntax-vs-semantics.md`
- [x] Route pattern syntax coverage: literals, params, optional, options, catch-all, group options — matches docs and `skills/nuru/SKILL.md`
- [x] `common-strings.cs` and `message-type.cs` — message text quality

### Search, build, dev CLI, MCP

- [x] `source/timewarp-nuru-search` — review as shipped package
- [x] `source/timewarp-nuru-build`, `source/timewarp-nuru-devcli` — review for correctness only (not shipped to consumers)
- [x] `source/timewarp-nuru-mcp` — confirm frozen state; no release blockers; note whether to ship or exclude from 3.0

### Tests, samples, docs

- [x] Test coverage gaps per area (`tests/timewarp-nuru-tests/*`) — untested diagnostic ids are A-7; unfiltered `DiscoverEndpoints()` is T-1
- [x] All tests use `.Map<TEndpoint>()` not `.DiscoverEndpoints()` (CI multi-mode cross-contamination)
- [x] `samples/` all build and run against the current API
- [x] `documentation/user` and `documentation/developer` accuracy pass (removed-API inventory in `review/supporting.md` D-1; `docs-accuracy-validator` was not spawned)
- [x] `skills/nuru/SKILL.md` matches 3.0 API

### Release readiness

- [x] Breaking changes since 2.x enumerated; migration guide exists or is written
- [x] `[Obsolete]` members: remove or confirm they survive into 3.0
- [x] Package metadata (`source/Directory.Build.props`): description, tags, readme, license, repo URL, symbols
- [x] Changelog / release notes draft for 3.0.0
- [x] Release pipeline per `tw-release` convention verified against this repo (OIDC publish, attestation)

### Wrap-up

- [x] Write `review/analyzers.md` (A-1 … A-7), `review/parsing.md` (P-1), `review/supporting.md` (S-1 … S-10, T-1, D-1) from the verified findings
- [x] Merge per-area findings into `review/findings.md` with disposition for each
- [x] Create child tasks for every fix-now finding; link them under `## Children` here
  - [x] Ids 482-001 … 482-015 reserved and claimed with titles (see `## Children`)
  - [x] Each child body written: Description (finding, evidence, file paths), Checklist, validation command
  - [x] Each child committed in its worktree and published to the origin-home inbox (`ganda kanban publish 482-NNN`)
- [x] Decision recorded in Notes: ship 3.0.0 after children merge, or further beta

## Resume (2026-10-04)

The first implementer run (grok, Herdr pane) hit **max turns** after context compaction, 35 minutes in,
and exited 1. State on disk when it stopped:

- Written: `review/runtime-core.md` (R-1 … R-9), `documentation/user/guides/migrating-to-3.0.md`,
  XML-doc fixes in `source/timewarp-nuru` (R-7), `skills/tw-nuru/SKILL.md`, changelog pointer. All committed
  on this task branch by the cockpit after the failure.
- **Not written:** `review/analyzers.md`, `review/parsing.md`, `review/supporting.md`, `review/findings.md`.
  The A-*, P-*, S-*, T-1 and D-1 findings exist only as prose in `oracle-implement-20261003t175050879z.log`
  in this folder. Re-verify each against the code before writing it down; do not copy the log blindly.
- **Child tasks 482-001 … 482-015 exist as empty template bodies** in their own worktrees
  (`../task-482-NNN-*`), uncommitted and claimed by dead sessions. Fill each body from the matching finding,
  commit inside that child worktree, and `ganda kanban publish 482-NNN` so it lands in the inbox.
  Do not create new ids for findings that already have a child.
- The checklist above was reset to reflect what is actually on disk. Check items only when the artifact exists.
- Do not re-run the full CI or AOT baseline; both are recorded in Notes. Do not re-review the runtime core.

## Notes

- Package version at task creation: `3.0.0-beta.78` (`source/Directory.Build.props`).
- Baseline commit: `5a06e900` (origin-home `master` at review start, "publish kanban 482").
- CI baseline (`ganda runfile cache --clear`, then `dotnet run tests/ci-tests/run-ci-tests.cs`): exit 0.
  3742 passed, 0 failed, 12 skipped (219 multi-mode suite summaries: 3648 passed + 12 skipped;
  25 standalone suite summaries: 94 passed). Cleared 234 runfile cache entries.
- AOT baseline: `dotnet publish tests/test-apps/timewarp-nuru-testapp-delegates/timewarp-nuru-testapp-delegates.csproj -c Release -r linux-x64 -p:PublishAot=true`.
  Exit 0. Zero IL2026/IL3050. Four CS0436 warnings (`Mediator` in the test app vs internal `Mediator` in TimeWarp.Nuru via `InternalsVisibleTo`). Not a consumer trim leak. **wont-fix** for 3.0.
- `AnalyzerReleases.Shipped.md` / `AnalyzerReleases.Unshipped.md`: diagnostics stay in Unshipped
  by decision (2026-10-04). The shipped/unshipped split is a Microsoft-internal convention and is
  not part of the 3.0 release criteria.
- Obsolete public API: `NuruAppBuilder.Services` and `AddReplOptions` **survive into 3.0** as shims. Remove post-3.0 (R-8).
- MCP: **exclude** `TimeWarp.Nuru.Mcp` from the 3.0 ship (482-012). Freeze `79706ebf` (2026-07-14) plus path-traversal fix `34ee7300` (2026-09-23). Syntax and handler tools still describe a pre-3.0 API. Do not rewrite MCP on this id.
- Symbols: embedded PDB + SourceLink. No snupkg. **wont-fix**.
- Changelog: last dated section is `3.0.0-beta.19`. beta.20–beta.77 are not logged (482-015). This task did not invent per-beta entries. Unreleased now points at the migration guide.
- Seven test files still call unfiltered `DiscoverEndpoints()` inside the multi-mode assembly (T-1). CI is green. **post-3.0**, overlaps 219. Not migrated here.
- Samples are runfiles. Static scan only (no per-sample `dotnet run`). No `MapDefault`, `AddReplOptions`, `TimeWarp.Nuru.Unit`, or `ValueTask` handlers. Two stale comments are post-3.0.
- Skill path in this repo is `skills/tw-nuru/SKILL.md`.
- Public XML examples that called `AddReplSupport()` or `CreateBuilder(args)` were corrected on this task. `error-handling.md` still disagrees with the generator; that guide is 482-013. Binding-error stdout stays for 3.0 (R-2, post-3.0 behavior change).
- Related open tasks: 219 (large test file refactor) and 481 (.NET 11 upgrade). Neither blocks this review. 481 is a separate decision from the 3.0 tag.
- Review record is `review/` on this task (area files + `findings.md`). This node did not run `tw-implementation-review`; the host review node reviews the diff.
- **Decision:** ship `3.0.0` after the fix-now children merge. Do not cut another beta only to hold this review.
- 2026-10-05: 482-001 … 482-015 bodies filled from the re-verified findings and published to origin-home (`90decb4` through `43df760`). Claims released. Inbox paths are `kanban/to-do/482-00N-*/task.md` on `master`.
- Docs accuracy pass was a direct comparison of living docs to the 3.0 API (see D-1). `docs-accuracy-validator` was not spawned. 482-013 owns the rewrite.

## Children

- **482-001** — `EndpointBuilder.Build()` registers the open route (R-1)
- **482-002** — shell completion parameter and type lookup (R-3)
- **482-003** — file and directory completion (R-4)
- **482-004** — `RunReplAsync` stub message (R-6)
- **482-005** — try-parse for `DateTimeOffset`, `Version`, and typed catch-all / repeated options (A-3, R-5)
- **482-006** — diagnostics for invalid route patterns; no fallback literal (A-2)
- **482-007** — optional parameters render as `{name?}` (A-4)
- **482-008** — stop caching Roslyn `Location` on `ExtensionMethodCall` (A-1)
- **482-009** — `nuru search` must not append JSON after the listing (S-1)
- **482-010** — `--group` matches dotted group paths (S-2)
- **482-011** — index rebuild remembers the executable path (S-3)
- **482-012** — exclude `TimeWarp.Nuru.Mcp` from the 3.0 package set (S-4)
- **482-013** — refresh guides that still show removed 2.x APIs (D-1, P-1, error-handling guide)
- **482-014** — remove or internalize `NuruAppHolder` and `ResponseDisplay` (R-9)
- **482-015** — backfill changelog from `3.0.0-beta.20` through `beta.77`

## Results

The 3.0 review record is on this task. Area files are `review/runtime-core.md`, `review/analyzers.md`, `review/parsing.md`, and `review/supporting.md`. Dispositions are in `review/findings.md`.

Fix-now work is children 482-001 through 482-015. Each child kitchen now has the finding, the file paths, a checklist, and a validation command. This task also landed the 2.x → 3.0 migration guide, corrected public XML that called `AddReplSupport()` and `CreateBuilder(args)`, and aligned `skills/tw-nuru/SKILL.md` with `Task<T>` handlers.

Ship `3.0.0` after those children merge. `NuruAppBuilder.Services` and `AddReplOptions` stay through 3.0. `TimeWarp.Nuru.Mcp` does not ship in that package set. Binding-error stdout (R-2), obsolete shims (R-8), attribute simple-name matching (A-5), diagnostic wording (A-6), descriptor tests and help links (A-7), and the seven unfiltered `DiscoverEndpoints()` files (T-1) are post-3.0.

CI on baseline `5a06e900` exited 0: 3742 passed, 0 failed, 12 skipped. AOT publish of the delegates test app exited 0 with four CS0436 warnings and no IL2026 or IL3050. This node did not re-run that baseline. No product source changed in this pass.

### How to validate

**Smoke**

```bash
test -f kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md
test -f kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/analyzers.md
test -f kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/parsing.md
test -f kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/supporting.md
grep -n 'fix-now' kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md
```

**Expect:** all four review files exist. `findings.md` has fix-now rows for R-1, A-2, S-1, and S-4, and names children 482-001 through 482-015. `analyzers.md` cites `ExtensionMethodCall` and `MapParseErrorToDiagnostic`. `supporting.md` cites `SearchQuery` and `TimeWarp.Nuru.Mcp`.

**Automated gate:** none for this pass. The review record is markdown. Baseline CI and AOT publish were already recorded in Notes and were not re-run.

**Not in scope:** implementing the fifteen fixes, rewriting MCP, or moving binding errors to stderr.

## Session

- Created: 463927 (2026-10-04)
- Implementer: grok 01a102e3-e4f9-72a2-a86d-9e8206c8a56e (2026-10-04) — max turns, exit 1
- Cockpit triage + relaunch: claude 2412bd45 (2026-10-04)
- Implementer: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05) — area findings, dispositions, child bodies
