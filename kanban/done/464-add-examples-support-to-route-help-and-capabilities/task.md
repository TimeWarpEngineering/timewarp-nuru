# Add Examples support to route help and capabilities

## Description

Add an Examples feature to the endpoint DSL so a route can declare usage examples that render
in both generated surfaces:

- **Per-route `--help`**: an `Examples:` section after the Options table, rendered as
  full-width lines (help tables truncate cell text, which is why long Description strings
  can't carry this today).
- **`--capabilities` JSON**: an `examples` array on each endpoint entry, so agents consuming
  the capabilities dump (e.g. via a CLAUDE.md `!`cli --capabilities`` hook) see concrete
  invocations, not just one-line descriptions.

Both emitters read from the same `RouteDefinition` model, so one attribute-level feature
feeds both. This was previously deferred as Phase 3 of task 144
(kanban/archived/144-improve-help-output-formatting-and-readability.md).

Motivating consumer: `ganda skills add` supports four source forms (local path, `worktree://`,
GitHub tree URL, `owner/repo` package with `--skill`/`--all`/`--list`) that cannot be made
clear in truncated table cells — timewarp-ganda task 211 annotates it once this ships.

## Requirements

- New attribute surface on routes, e.g. `[NuruRouteExample("skills add nuru ./skills/nuru", Description = "Add from a local path")]`
  with `AllowMultiple = true` (or an `Examples` string-array property on `NuruRouteAttribute` —
  pick whichever fits the extractor/analyzer model best; separate attribute preferred for
  per-example descriptions).
- Examples flow through `RouteDefinition` (generators/models/route-definition.cs) and
  `EndpointExtractor` (generators/extractors/endpoint-extractor.cs).
- `route-help-emitter.cs`: render an `Examples:` section after the Options section
  (insertion point ~line 198), one example per line (command line, then optional dimmed
  description), no table/truncation.
- `capabilities-emitter.cs`: emit `"examples": [{"command": "...", "description": "..."}]`
  (omit the property when a route has none, keeping the JSON stable for existing consumers).
- Fluent DSL parity: a `.WithExample(...)` (or documented equivalent) so Map()-style routes
  aren't left out — or explicitly scope to Endpoint DSL only with a documented rationale.
- Routes without examples produce byte-identical help/capabilities output to today
  (no regressions for existing CLIs).
- Analyzer/generator tests cover: single example, multiple examples, no examples,
  example with and without description, capabilities JSON shape.
- Update the tw-nuru skill documentation / README route-attribute docs to mention Examples.

## Checklist

- [x] NuruRouteExampleAttribute (runtime package) with XML docs
- [x] ExampleDefinition model + RouteDefinition.Examples (EquatableArray) + Create param
- [x] EndpointExtractor: extract multiple [NuruRouteExample] attributes
- [x] Fluent DSL: WithExample no-op, DslInterpreter dispatch, IrRouteBuilder + RouteDefinitionBuilder
- [x] route-help-emitter: Examples section after Options (escaped literals, .Dim() descriptions)
- [x] capabilities: ExampleCapability DTO + nullable Examples property + serializer context + emitter
- [x] Tests: help-08-route-examples.cs + capabilities-06-examples.cs (0/1/N, fluent, escaping, omission)
- [x] No-regression: unannotated sample --help/--capabilities byte-identical; full CI test sweep green
- [x] Docs: readme, tw-nuru skill, auto-help/built-in-routes/endpoints feature docs

## Notes

- Prior exploration (2026-08-13): help is emitted by
  `source/timewarp-nuru-analyzers/generators/emitters/help-emitter.cs` (app help) and
  `route-help-emitter.cs` (per-route: Pattern → Description → Parameters → Options);
  `capabilities-emitter.cs` sits beside them and reads the same model. `NuruRouteAttribute`,
  `ParameterAttribute`, `OptionAttribute` each carry only a single-string `Description`.
- Downstream consumer task: timewarp-ganda 211 (annotate `skills add`); ganda 210
  (well-known discovery) will add a fifth example form later.

### Implementation plan (2026-08-13)

Attribute surface: new `NuruRouteExampleAttribute` (source/timewarp-nuru/attributes/nuru-route-example-attribute.cs), AttributeUsage Class + AllowMultiple, ctor (string command) + named Description — modeled on NuruRouteAliasAttribute (extraction precedent is symbol-based name matching in EndpointExtractor).

Model: new `ExampleDefinition(string Command, string? Description)` record (generators/models/example-definition.cs); RouteDefinition gains trailing `EquatableArray<ExampleDefinition> Examples = default` (EquatableArray preserves incremental-generator caching — guarded by generator-37-incrementality-caching test) + `HasExamples` helper; RouteDefinition.Create gains optional examples param.

Endpoint DSL extraction: EndpointExtractor gains ExtractNuruRouteExampleAttributes modeled on the alias extraction but accumulating across all matches (AllowMultiple, source order preserved); skips null/empty commands.

Fluent DSL: `.WithExample(command, description?)` — declarative no-op on EndpointBuilder + GroupEndpointBuilder; DslInterpreter dispatch case (modeled on DispatchWithAlias) with new ExtractStringArgumentAt helper for the optional second arg; IIrRouteBuilder/IrRouteBuilder members; RouteDefinitionBuilder accumulator.

Help: route-help-emitter.cs EmitRouteHelpContent, after the Options block (~line 197): blank line, "Examples:" header, per example a two-space-indented command line and (if present) a four-space-indented `.Dim()` description line; plain string literals via EmitterStringUtils.EscapeForStringLiteral (safe for quotes/braces — help-07 regression class). App-level help-emitter.cs and group help unchanged; examples are per-route help only.

Capabilities: capabilities-response.cs gains `ExampleCapability { Command, Description? }` and EndpointCapability gains `IReadOnlyList<ExampleCapability>? Examples` as LAST property defaulting to null (WhenWritingNull omits it — JSON byte-identical for unannotated routes); serializer context registrations added; capabilities-emitter.cs EmitEndpointCapabilityAdd conditionally emits the Examples initializer.

Tests (Jaribu runfiles, .Map<T>(), unique literals, ganda runfile cache --clear after generator changes): tests/timewarp-nuru-tests/help/help-08-route-examples.cs (attribute + fluent + no-examples negative + special-chars escaping) and tests/timewarp-nuru-tests/capabilities/capabilities-06-examples.cs (DTO shape, integration, omission when none). CI picks both up via glob; full sweep via dotnet run tests/ci-tests/run-ci-tests.cs.

Docs: readme.md attribute snippet, skills/tw-nuru/SKILL.md (attribute + .WithExample), documentation/user/features/auto-help.md + built-in-routes.md + endpoints.md.

Resolved forks: description lines use .Dim() (first color in per-route help, accepted per task intent); example text is verbatim (what the user types after the executable — document this); examples key emitted last in capabilities JSON; [NuruRouteExample] on group base classes is out of scope (silently ignored — noted as possible follow-up); no new analyzer (empty commands silently skipped).

## Session

- Created: 0f730c83-90e5-4a4c-8bb2-3020fdd469d6 (2026-08-13)
- Planning: 0f730c83-90e5-4a4c-8bb2-3020fdd469d6 (2026-08-13)
- Implementation: 0f730c83-90e5-4a4c-8bb2-3020fdd469d6 (2026-08-13) — implemented per plan;
  all checklist items done. `timewarp-nuru-analyzers`/`timewarp-nuru` build clean
  (warnings-as-errors), help-08 (7/7) and capabilities-06 (4/4) pass, full CI sweep
  (`tests/ci-tests/run-ci-tests.cs`, exit 0, 1595 total/1588 passed/7 skipped in multi-mode
  plus all standalone phases green) unaffected. No-regression verified by running
  `samples/endpoints/02-calculator` `--help`, `add --help`, and `--capabilities` before
  (git stash) and after the change: outputs byte-identical. Not committed per instructions.
- Review + disposition: 0f730c83-90e5-4a4c-8bb2-3020fdd469d6 (2026-08-13)

## Results

### What was done

- New `[NuruRouteExample("...", Description = "...")]` attribute (AllowMultiple) and fluent `.WithExample(command, description?)` on both EndpointBuilder and GroupEndpointBuilder — commit a1ae675d.
- Examples thread through `RouteDefinition` as `EquatableArray<ExampleDefinition>` (incremental-generator cache-safe) via EndpointExtractor (attribute path) and DslInterpreter/RouteDefinitionBuilder (fluent path).
- Per-route `--help` renders an `Examples:` section after Options: full-width lines, escaped literals, `.Dim()` descriptions (route-help-emitter.cs). App-level and group help unchanged.
- `--capabilities` emits `"examples": [{"command", "description"}]` per endpoint, declared last on EndpointCapability and omitted entirely when a route has none — JSON byte-identical for unannotated routes (verified via stash-diff on the calculator sample).
- Review fixes: fluent named-argument resolution (NameColon-preferred; was silently swapping `.WithExample(description: ..., command: ...)`), empty-command/-description normalization consistent across both DSL paths, and non-literal command arguments now fail the build with NURU_S999 instead of being silently dropped — commits 6c7ac49a, f8c3936b.
- Bonus latent-bug fix (f8c3936b): `AppExtractor.ExtractFromBuildCall`'s no-model fallback returned `Empty`, discarding all diagnostics collected before an aborted fluent chain — such apps compiled clean and failed at runtime ("RunAsync was not intercepted"). Now returns `Failure(diagnostics)` so NURU_S999/H005/etc. surface at build time.
- Tests: help-08-route-examples (8), capabilities-06-examples (4), generator-40-with-example-non-literal-command (Roslyn-hosted, pins S999 + H005 surviving together). Docs: readme, tw-nuru skill, auto-help/built-in-routes/endpoints feature docs.
- Follow-up task created: 465 (escape U+0085/U+2028/U+2029 in generated string literals — pre-existing systemic gap found in review).

### Review (Phase 4b)

- Rounds: 3; roster: general (effort 1); artifacts under `review/`.
- Findings: 1 bug, 1 suggestion, 3 nits. Fixed: M1 (bug), M2, M3 (nits), M5 (suggestion). Wontfix: M4 (nit) → follow-up task 465. Final: 0 open.
- Disposition: **accepted-exceptions** (`review/disposition.md`). No escalations.

### How to validate

Smoke:
```bash
cd <repo>
dotnet build timewarp-nuru.slnx                 # clean, warnings-as-errors
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/help/help-08-route-examples.cs            # 8/8
dotnet run tests/timewarp-nuru-tests/capabilities/capabilities-06-examples.cs  # 4/4
dotnet run tests/timewarp-nuru-tests/generator/generator-40-with-example-non-literal-command.cs  # 1/1
```

Manual eyeball: annotate any sample endpoint with `[NuruRouteExample("demo run --verbose", Description = "Run verbosely")]`, then `dotnet <sample>.cs demo --help` shows an `Examples:` section after Options, and `dotnet <sample>.cs --capabilities` shows an `examples` array on that endpoint (and no `examples` key on unannotated endpoints).

Expect: all commands green as annotated; unannotated routes' help/capabilities output unchanged from before this feature.

Automated gate: `dotnet run tests/ci-tests/run-ci-tests.cs` (full sweep — 1596 tests, exit 0).

Depends on: nothing. Not in scope: escaping gap for U+0085/U+2028/U+2029 (task 465); consuming this feature in ganda `skills add` (timewarp-ganda task 211, blocked on the next Nuru release containing this).
