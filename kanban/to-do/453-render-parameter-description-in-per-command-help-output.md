# Render Parameter Description In Per Command Help Output

## Description

When using the Endpoint DSL with `[NuruRoute]` + `[Parameter(Description = "...")]`, the
description is correctly extracted into `ParameterDefinition.Description`, but it is **never
included** in the generated per-route `--help` Parameters table.

Only the Fluent DSL pipe syntax (`{param|Description here}`) produces visible parameter
descriptions in help output — and even then, only via the options path. The per-route Parameters
table hardcodes Name/Required/Type columns and ignores `Description`.

Tracks GitHub issue: https://github.com/TimeWarpEngineering/timewarp-nuru/issues/215

## Reproduction

```csharp
[Parameter(Description = "Source repository (owner/repo, https://..., or git@...:.git)")]
public required string Source { get; set; }
```

```
ganda repo fork --help

Parameters:
┌────────┬──────────┬────────┐
│ Name   │ Required │ Type   │
├────────┼──────────┼────────┤
│ source │ Yes      │ string │
└────────┴──────────┴────────┘
```

No description is shown for `source`, despite a high-quality `[Parameter(Description)]` being present.

## Root Cause

In `source/timewarp-nuru-analyzers/generators/emitters/route-help-emitter.cs`, the
`EmitRouteHelpContent` method hardcodes the parameters table:

```csharp
.AddColumn("Name")
.AddColumn("Required")
.AddColumn("Type")
...
.AddRow(name, required, type)  // param.Description is ignored
```

(Lines ~119-141 in the current emitter.)

`ParameterDefinition` (`segment-definition.cs:49`) includes `Description`, populated by:
- `endpoint-extractor.cs:762-764` (for `[Parameter(Description = ...)]`)
- `pattern-string-extractor.cs:133` (for fluent `{name|desc}` syntax)

Options already emit a Description column (lines ~150-178 in the same file). Parameter
descriptions are also captured for shell completion (`completion-data-extractor.cs:101`), so the
data is present — it's only the help emitter that drops it.

## Impact

- Endpoint DSL users (the recommended pattern for non-trivial apps) get incomplete `--help`
  for any command with positional parameters.
- Descriptions written on `[Parameter]` attributes are invisible in the most common help path.
- Inconsistent behavior between Fluent DSL and Endpoint DSL.

## Suggested Fix

Update `RouteHelpEmitter.EmitRouteHelpContent` to add a "Description" column to the Parameters
table when any parameter has a description, mirroring the existing options handling. Gracefully
omit the column (or show empty cells) when no descriptions are present.

## Checklist

- [x] Add conditional "Description" column to the Parameters table in `route-help-emitter.cs`
- [x] Mirror the options-table logic (only show column when at least one param has a description)
- [x] Verify Endpoint DSL `[Parameter(Description)]` renders in per-command `--help`
- [x] Verify Fluent DSL `{param|desc}` renders consistently
- [x] Add test covering parameter descriptions in per-route help output
- [x] Run `ganda runfile cache --clear` then help tests; all help tests pass
- [ ] Close GitHub issue #215 referencing the fix (do at merge to master)

## Implementation

- `source/timewarp-nuru-analyzers/generators/emitters/route-help-emitter.cs`:
  `EmitRouteHelpContent` now computes `anyDescriptions` over the route parameters and adds a
  `.AddColumn("Description")` plus a 4-column `.AddRow(...)` when any parameter has a description.
  When none do, the original 3-column (Name/Required/Type) table is emitted unchanged. Mirrors the
  existing Options-table handling.
- `tests/timewarp-nuru-tests/help/help-01-per-route-help.cs`: added
  `Should_show_parameter_descriptions_in_help` (Endpoint DSL `ForkEndpoint` with
  `[Parameter(Description = "Source repository identifier")]`, asserts the Description column +
  text render) and `Should_not_show_description_column_when_no_parameter_descriptions` (asserts the
  column is omitted when no parameter has a description). Used `.Map<TEndpoint>()` per the
  CI multi-mode cross-contamination guidance.

## Verification

- Full CI multi-mode suite passes under the project's SDK (.NET 10):
  **Total 1129, Passed 1122, Skipped 7, Failed 0** via
  `dotnet run tests/ci-tests/run-ci-tests.cs` (SDK pinned to 10.0.301).
- All help tests also verified individually: help-01 10/10 (+1 skipped), help-02 default 6/6,
  help-02 table 9/9, help-03 endpoint 4/4, help-04 group 6/6, help-05 1/1, help-06 param-only 7/7.
- Environment note (not related to this change): Nuru is a .NET 10 project (CI pins
  `dotnet-version: '10.0.x'`; no `global.json`). This dev machine also has .NET 11 preview
  (11.0.100-preview.5) installed, so with no `global.json` the default `dotnet` resolves to the
  preview, which mis-enforces code-style analyzers and fails to build the **existing** analyzer
  source (reproduces on a clean `git stash` baseline). Build/test with .NET 10. On the 10.0.301
  patch the file-based CI runner also needs
  `--property:ExperimentalFileBasedProgramEnableTransitiveDirectives=true`; CI's newer 10.0.x
  patch and the `dev.cs` workflow have it enabled by default.

## Notes

- Help rendering is done at compile time by source generator emitters.
- Affected version: TimeWarp.Nuru 3.0.0-beta.70 (and likely earlier).
- Observed quirk: the Fluent DSL `{name|desc}` description parser strips `(`, `)`, and `/` from
  the description text (e.g. `Source repository (owner/repo)` renders as
  `Source repository owner repo`). The Endpoint DSL `[Parameter(Description)]` path preserves the
  text verbatim. The stripping is a separate fluent-parser issue, out of scope for #215 — consider
  a follow-up task if desired.
- Related: #434 (review/clean up help-model.cs — notes "No support exists for ... parameter
  descriptions"), #436 (add Examples support to help output).
