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

- [ ] NuruRouteExampleAttribute (runtime package) with XML docs
- [ ] ExampleDefinition model + RouteDefinition.Examples (EquatableArray) + Create param
- [ ] EndpointExtractor: extract multiple [NuruRouteExample] attributes
- [ ] Fluent DSL: WithExample no-op, DslInterpreter dispatch, IrRouteBuilder + RouteDefinitionBuilder
- [ ] route-help-emitter: Examples section after Options (escaped literals, .Dim() descriptions)
- [ ] capabilities: ExampleCapability DTO + nullable Examples property + serializer context + emitter
- [ ] Tests: help-08-route-examples.cs + capabilities-06-examples.cs (0/1/N, fluent, escaping, omission)
- [ ] No-regression: unannotated sample --help/--capabilities byte-identical; full CI test sweep green
- [ ] Docs: readme, tw-nuru skill, auto-help/built-in-routes/endpoints feature docs

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
