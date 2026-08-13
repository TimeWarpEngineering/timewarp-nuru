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

- [ ] Design attribute surface (`NuruRouteExampleAttribute` with AllowMultiple vs array property); confirm with analyzer conventions
- [ ] Extend `RouteDefinition` model + `EndpointExtractor` extraction
- [ ] Render `Examples:` section in `route-help-emitter.cs` after Options
- [ ] Emit `examples` array in `capabilities-emitter.cs` (omitted when empty)
- [ ] Fluent DSL support or documented exclusion
- [ ] Tests: help output snapshots + capabilities JSON shape for 0/1/N examples
- [ ] Verify no-regression: existing sample CLIs produce unchanged help/capabilities
- [ ] Docs: README + skill doc updated

## Notes

- Prior exploration (2026-08-13): help is emitted by
  `source/timewarp-nuru-analyzers/generators/emitters/help-emitter.cs` (app help) and
  `route-help-emitter.cs` (per-route: Pattern → Description → Parameters → Options);
  `capabilities-emitter.cs` sits beside them and reads the same model. `NuruRouteAttribute`,
  `ParameterAttribute`, `OptionAttribute` each carry only a single-string `Description`.
- Downstream consumer task: timewarp-ganda 211 (annotate `skills add`); ganda 210
  (well-known discovery) will add a fifth example form later.

## Session

- Created: 0f730c83-90e5-4a4c-8bb2-3020fdd469d6 (2026-08-13)
