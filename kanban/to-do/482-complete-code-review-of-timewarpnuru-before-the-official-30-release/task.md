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

- [ ] Confirm review baseline commit (origin-home master at review start) and record it in Notes
- [ ] Run `ganda runfile cache --clear` then full CI (`dotnet run tests/ci-tests/run-ci-tests.cs`); record pass/fail counts as the baseline
- [ ] Build AOT sample (`dotnet publish -c Release -r linux-x64 -p:PublishAot=true`) and record trim/AOT warnings as baseline

### Runtime core (`source/timewarp-nuru`)

- [ ] `nuru-app.cs`, `nuru-app-builder.cs`, `builders/` — public builder API surface, naming, `Done()`/`Build()` lifecycle, obsolete members to remove before 3.0
- [ ] `repl/` (9.1k lines, largest area) — key bindings, multiline buffer, selection, yank, history; look for dead code and duplicated editing logic
- [ ] `completion/` — shell completion generation and runtime completion
- [ ] `dependency-injection/` — service registration, constructor resolution, scope lifetime
- [ ] `type-conversion/` — converters, nullable/optional handling, error messages
- [ ] `help/`, `options/`, `configuration/`, `logging/`, `telemetry/`, `serialization/`, `capabilities/`, `io/`, `check-updates/`
- [ ] `attributes/`, `abstractions/`, `endpoints/`, `extensions/`, `services/` — small files, check each for public-API leakage
- [ ] Public API audit: every `public` type/member is intentional, documented (XML docs), and AOT/trim safe
- [ ] Error handling consistent with `documentation/developer/reference/error-handling.md`

### Analyzers and source generator (`source/timewarp-nuru-analyzers`)

- [ ] `generators/nuru-generator.cs` pipeline — incrementality, cacheability of models, no `SemanticModel` captured in outputs
- [ ] `generators/locators/`, `extractors/`, `ir-builders/`, `interpreter/`, `emitters/` — SemanticModel over syntax-string type resolution (per `.agent/local/nuru-specific.md`)
- [ ] `diagnostics/` — every descriptor has an id, category, help link, and a test; messages are user-facing quality
- [ ] `validation/` — route pattern validation matches runtime parser behaviour
- [ ] `AnalyzerReleases.Unshipped.md` (44 lines) vs `AnalyzerReleases.Shipped.md` (2 lines) — move all diagnostics shipping in 3.0 to Shipped with the 3.0 release header
- [ ] Generated code compiles warning-free under `TreatWarningsAsErrors` in consumer projects

### Parsing (`source/timewarp-nuru-parsing`)

- [ ] Lexer / parser / semantic passes against `documentation/developer/reference/parser-classes-syntax-vs-semantics.md`
- [ ] Route pattern syntax coverage: literals, params, optional, options, catch-all, group options — matches docs and `skills/nuru/SKILL.md`
- [ ] `common-strings.cs` and `message-type.cs` — message text quality

### Search, build, dev CLI, MCP

- [ ] `source/timewarp-nuru-search` — review as shipped package
- [ ] `source/timewarp-nuru-build`, `source/timewarp-nuru-devcli` — review for correctness only (not shipped to consumers)
- [ ] `source/timewarp-nuru-mcp` — confirm frozen state; no release blockers; note whether to ship or exclude from 3.0

### Tests, samples, docs

- [ ] Test coverage gaps per area (`tests/timewarp-nuru-tests/*`, 214 test files) — list untested public behaviour
- [ ] All tests use `.Map<TEndpoint>()` not `.DiscoverEndpoints()` (CI multi-mode cross-contamination)
- [ ] `samples/` all build and run against the current API
- [ ] `documentation/user` and `documentation/developer` accuracy pass (use `docs-accuracy-validator` agent)
- [ ] `skills/nuru/SKILL.md` matches 3.0 API

### Release readiness

- [ ] Breaking changes since 2.x enumerated; migration guide exists or is written
- [ ] `[Obsolete]` members: remove or confirm they survive into 3.0
- [ ] Package metadata (`source/Directory.Build.props`): description, tags, readme, license, repo URL, symbols
- [ ] Changelog / release notes draft for 3.0.0
- [ ] Release pipeline per `tw-release` convention verified against this repo (OIDC publish, attestation)

### Wrap-up

- [ ] Merge per-area findings into `review/findings.md` with disposition for each
- [ ] Create child tasks for every fix-now finding; link them under `## Children` here
- [ ] Decision recorded in Notes: ship 3.0.0 after children merge, or further beta

## Notes

- Package version at task creation: `3.0.0-beta.78` (`source/Directory.Build.props`).
- Baseline commit: _to be recorded at review start_.
- MCP server is frozen as of 2026-07-14 (kanban 454-033 closed won't-do); it is in scope only
  to decide whether it ships under the 3.0 banner.
- Related open tasks: 219 (large test file refactor) and 481 (.NET 11 upgrade). Neither blocks
  this review; 481 should be sequenced relative to the 3.0 release as a separate decision.
- Review method: `tw-implementation-review` with per-area reviewer agents, then a single
  merged disposition pass. Findings are written to `review/`, not left in chat.
- Always `ganda runfile cache --clear` before CI runs during the review; stale generator
  output produces false results.

## Session

- Created: 463927 (2026-10-04)
