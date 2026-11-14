# Add Design regions to public-API source files

## Description

Add `#region Design` to the public-API source files so intent can be checked against the source of truth. Task 580 (PR #388, 2026-11-13) backfilled `#region Purpose` in every hand-written file (423 of 475 non-generated `.cs` files under `source/`; the rest are `*.g.cs`), so Purpose is no longer this task's work. Design is at 95 of 475. Public API files such as `nuru-logging-extensions.cs`, `nuru-app-builder-extensions.updates.cs`, `nuru-mcp-server-builder-extensions.cs` and `check-updates-types.cs` have none, so a reviewer (or the 523 consumer check) cannot tell whether a failing behavior is a code defect or intended. Several recent defects (logging NURU_S999, check-updates CS0122, WithNuruTools on an ASP.NET chain) were hard to triage for exactly this reason.

Use the `tw-agent-context-regions` skill for the format. Start with every file that declares public builder or extension APIs; then widen. Where intent is unclear, record an `Open Questions` region rather than guessing.

## Goal (implementer restatement)

Give the public-API source files under `source/` a `#region Design` that records the decisions
behind the API (the why, constraints, rejected alternatives), so a reviewer can tell a code defect
from intended behavior without guessing. This is a comment-only change.

Acceptance criteria:

- Every non-generated file that declares a public builder or extension API has a Design region
  (Phase 1), placed after the existing Purpose region and before the namespace.
- The remaining public-surface files get a Design region where a genuine decision exists (Phase 2).
  Trivial files (plain enums, marker attributes, DTO records) are not padded with invented
  rationale; they are listed in Notes instead.
- Where intent cannot be determined from the code, git history, or kanban, the file gets an
  `Open Questions` region instead of a guess, and Notes lists each one for the human.
- Purpose stubs are refined only where they are a one-line restatement of the type name.
- Internals (analyzers/generators, internal types) stay out of scope; continuing there is a human
  decision.
- `./bin/dev build`, `./bin/dev test` and `ganda repo audit` pass. Results records the before and
  after Design counts.

## Checklist

- [x] Read the `tw-agent-context-regions` skill
- [x] Inventory: list non-generated `.cs` files under `source/` lacking Design (record counts in Notes)
- [x] Phase 1: every file declaring public builder/extension APIs gets Design (Purpose already present from 580; refine it where it is a one-line stub)
- [x] Record unresolved intent as `Open Questions` regions (do not guess); list them in Notes for the human
- [x] Phase 2: remaining public-surface files (done); continuing to internals is left for the human to decide (see Notes)
- [x] Build passes (warnings are errors; no trailing whitespace, RCS1037)
- [x] Record the new counts in Results

## Review

- Round 1: Blocked — open M1, M2
- Fix pass 2 (2026-11-14): M1 and M2 fixed (see Results, Fix pass 2)

## Notes

- Files from the 523 findings to include early: `nuru-logging-extensions.cs`, `nuru-app-builder-extensions.updates.cs`, `nuru-mcp-server-builder-extensions.cs`, `check-updates-types.cs`.
- Comment-only change; no behavior change.
- 2026-11-14 groom: rescoped from Purpose+Design to Design only after 580 landed Purpose everywhere.

### Inventory (2026-11-14, implementer)

- Non-generated `.cs` files under `source/` (excluding `obj/`, `bin/`, `*.g.cs`): 428 (the 475 in
  the description counted differently; Purpose is on all of them).
- Before: 103 with Design, 325 without. Of the 325, 120 declare public types outside
  `timewarp-nuru-analyzers` (the Phase 1 + 2 scope); the rest are analyzer/generator internals.
- 523 files: `nuru-logging-extensions.cs` no longer exists (task 527 removed UseLogging and the
  UseConsoleLogging family). `nuru-app-builder-extensions.updates.cs` and `check-updates-types.cs`
  already had Design. `nuru-mcp-server-builder-extensions.cs` got Design; its ASP.NET
  `IMcpServerBuilder` scope is settled by task 531, so no Open Question was needed.

### Public-surface files left without Design (deliberately)

Trivial, so no Design was invented: `daemon/nuru-daemon-server.session.cs` (main file has it),
`mcphost/nuru-mcp-tool-catalog.schema-writers.cs`, `parsing/syntax/segment-syntax.cs`,
`parsing/syntax/syntax-node.cs`, `search/endpoints/index-group.cs`, `search/endpoints/search-group.cs`,
`completion/completion/completion-candidate.cs`, `completion-context.cs`, `completion-type.cs`,
`repl/display/syntax-colors.cs`, `repl/key-bindings/key-binding-result.cs`, `key-binding-row.cs`,
`type-conversion/itype-converter-registry.cs`, and the ReplConsoleReader partials `basic-editing`,
`cursor-movement`, `editing`, `history`, `word-operations` (the type-level Design is in
`repl-console-reader.cs`). `endpoints/execution-strategy.cs` has an Open Questions region only.

Blocked by the file-length rule: `repl/key-bindings/key-binding-config-loader.cs` has 497 lines,
and TWA0038 fails a file over 500, so even an empty region does not fit. It needs a responsibility
extracted (code change, not this task) before it can carry Design.

### Open Questions for the human (all `Q1 (2026-11-14, claude)` unless noted)

Find them with `rg "#region Open Questions" source`. Paths are under `source/`.

1. `timewarp-nuru/builders/group-endpoint-builder.cs`: no `Implements<TFilter>` on grouped fluent
   routes. Intentional (use endpoint classes) or a gap?
2. `timewarp-nuru/builders/nested-compiled-route-builder.cs` and
   `timewarp-nuru/builders/nuru-app-builder/nuru-app-builder.routes.cs`: the
   `Map(Func<NestedCompiledRouteBuilder…>)` overload is documented as compile-time extracted, but
   `DispatchMap` requires a string pattern and the interpreter has no WithLiteral/WithParameter/
   WithOption case. Supported, or dead API?
3. `timewarp-nuru/completion/nuru-app-builder-extensions.cs`: `EnableCompletion`'s `appName` is
   discarded by the generator. Honor it, or remove/obsolete it?
4. `timewarp-nuru/io/nuru-test-context.cs`: `TryExecuteTestRunner` and `Reset` have no callers.
   Is the test-runner hook dead after the interceptor-based RunAsync?
5. `timewarp-nuru-build/GenerateNuruJsonContextTask.Extraction.cs`: a bare catch in
   `ExtractFromDelegateRoutes` contradicts the "no silent fallback" rule (470 M24). Narrow it?
6. `timewarp-nuru-parsing/parsing/runtime/matchers/route-matcher.cs`: `RouteMatcher.TryMatch` has
   almost no callers. Intended public API, or leftover from pre-generator matching?
7. `timewarp-nuru-search/services/search-index.cs`: `ClearIndexAsync` deletes two tables without
   a transaction while sibling methods use one. Intended?
8. `timewarp-nuru/SuppressNuruInvokerGenerationAttribute.cs`: nothing reads this attribute. Delete?
9. `timewarp-nuru/attributes/option-attribute.cs`: Q1 `IsRepeated` is never read (repeatability
   comes from the property type); Q2 the extractor reads `LongName`/`ShortName`/`IsRequired`
   named arguments this attribute does not define. Dead branches or intended settings?
10. `timewarp-nuru/attributes/parameter-attribute.cs`: `Order` and `Name` are not read by the
    generator although the XML docs say they matter. Honor them or remove them?
11. `timewarp-nuru/endpoints/execution-strategy.cs`: public enum with no references. Remove?
12. `timewarp-nuru/type-conversion/type-converter-registry.cs`: only tests use
    `TypeConverterRegistry`/`ITypeConverterRegistry`. Public API, or internalize/remove?
13. `timewarp-nuru/completion/completion/sources/enum-completion-source.cs`: tags candidates
    `CompletionType.Parameter` while generated enum candidates use `CompletionType.Enum`. Intended?

Side observations not recorded in source: `iroute-type-converter.cs` XML doc has
`<see cref="Alias"/>` but the property is `ConstraintAlias`; `CompletionType.Custom` is unused.

### Phase 2 decision for the human

Internals left without Design: 137 files in `timewarp-nuru-analyzers` plus 87 internal or
trivial files elsewhere. Whether to continue there is a human call (separate task if yes).

## Session

- 2026-11-14 implementer (claude, ganda task work): merged origin/master; inventory; Design added in
  five parallel batches (sonnet helpers), each diff checked to be comment-only; spot-checked claims.
- 2026-11-14 implement-fix-2 (claude, ganda task work): fixed round-1 M1 (`nuru-telemetry-options.cs`
  Design now matches `TelemetryOptionsExtractor`) and M2 (`message-type.cs` Design records the
  Unspecified split across CompiledRoute, help, the capabilities catalog, and MCP annotations).

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-11-14T00:32:04Z
- Review oracle: general reviewer (cursor, grok 4.7), session not reported, review budget 200 (cursor uncapped), round 1 open — 2026-11-14
- Review oracle: review by reviewer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 200), artifacts general.md, merged.md — 2026-11-14T00:37:01Z

## Results

Comment-only change: 101 files under `source/` gain `#region Design` and/or `#region Open Questions`;
no code, XML docs, or usings changed (`git diff -U0 -- source | grep '^-[^-]'` is empty).

| Count (non-generated `.cs` under `source/`) | Before | After |
|---|---|---|
| Files | 428 | 428 |
| With `#region Design` | 103 | 203 |
| With `#region Open Questions` | 0 | 14 |
| Public-surface (non-analyzer) files without Design | 120 | 19 (listed in Notes) |

### Gates

- `./bin/dev build`: first run failed with TWA0038 (`key-binding-config-loader.cs` went to 507
  lines). That file was reverted and is listed in Notes. Rerun: passed ("Build completed successfully!").
- `./bin/dev test`: passed. Multi-mode 2213 total, 2207 passed, 6 skipped, 0 failed; standalone 47
  files, 0 failed.
- `ganda repo audit`: passed (exit 0, "Repository passes"). One advisory warning,
  `vscode-window-icon` (`.vscode/settings.json` missing). That file is gitignored (`.gitignore:404`),
  so it is local machine setup with nothing to commit; left as is.

### Fix pass 2 (round-1 findings)

- M1 `source/timewarp-nuru/telemetry/nuru-telemetry-options.cs`: Design lists every member
  `TelemetryOptionsExtractor` reads (ServiceName, ServiceVersion, EnableTracing/Metrics/Logging,
  OtlpEndpoint, ActivitySources.Add / Meters.Add) and the NURU_T001 fallback for each, taken from
  `AssignmentFallback` and the Add path. The unexplained "trust boundary" sentence is gone.
- M2 `source/timewarp-nuru-parsing/message-type.cs`: Design no longer says "everywhere". It records
  the split: CompiledRoute defaults to Command, help shows ( ), the capabilities catalog keeps
  `EndpointKind.Unspecified`, and `NuruMcpToolCatalog.CreateAnnotations` sets no readOnly/idempotent
  hints, which MCP clients read as the spec defaults (false), the same as Command. No Open Question:
  the behavior agents see is the same.
- Still comment-only. Gates after the fix:
  - `./bin/dev build`: passed ("Build completed successfully!").
  - `./bin/dev test`: passed (standalone: 0 failed; "Tests completed successfully!"). It ran twice
    by mistake (once on its own, once chained in front of the audit).
  - `ganda repo audit`: passed ("Repository passes"), with the same advisory `vscode-window-icon`
    warning (gitignored `.vscode/settings.json`).

### How to validate

Smoke:

```bash
rg -l "#region Design" source --glob '*.cs' --glob '!*.g.cs' --glob '!**/obj/**' | wc -l
rg -l "#region Open Questions" source --glob '*.cs'
git diff origin/master -U0 -- source | grep -E '^[+-][^+-]' | grep -vE '^[+-]\s*(//|#region|#endregion)'
./bin/dev build
```

Fix pass 2: `git diff e95f5752 -- source` touches only the Design regions of
`nuru-telemetry-options.cs` and `message-type.cs`; compare them with `AssignmentFallback` in
`telemetry-options-extractor.cs` and `CreateAnnotations` in `nuru-mcp-tool-catalog.cs`.

Expect: 203 files with Design; the 14 Open Questions files listed in Notes; the third command
prints nothing (only comment and region lines changed); the build succeeds.

### Review

Round 1, roster general, effort 3, turn cap 200. The restatement matches the groomed task (Design only). Disposition is not written: M1 and M2 are open bugs. Counts: bug 2/0/0, suggestion 0/0/0, nit 0/0/0 (open/fixed/wontfix). Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.
