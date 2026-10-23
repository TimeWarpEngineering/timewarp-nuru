# Add Purpose and Design regions to public-API source files

## Description

Add `#region Purpose` and `#region Design` to the public-API source files so intent can be checked against the source of truth. Only 64 of 360 non-generated `.cs` files under `source/` have `#region Purpose` (55 have Design). Public API files such as `nuru-logging-extensions.cs`, `nuru-app-builder-extensions.updates.cs`, `nuru-mcp-server-builder-extensions.cs` and `check-updates-types.cs` have none, so a reviewer (or the 523 consumer check) cannot tell whether a failing behavior is a code defect or intended. Several recent defects (logging NURU_S999, check-updates CS0122, WithNuruTools on an ASP.NET chain) were hard to triage for exactly this reason.

Use the `tw-agent-context-regions` skill for the format. Start with every file that declares public builder or extension APIs; then widen. Where intent is unclear, record an `Open Questions` region rather than guessing.

## Checklist

- [ ] Read the `tw-agent-context-regions` skill
- [ ] Inventory: list non-generated `.cs` files under `source/` lacking Purpose and/or Design (record counts in Notes)
- [ ] Phase 1: every file declaring public builder/extension APIs gets Purpose and Design
- [ ] Record unresolved intent as `Open Questions` regions (do not guess); list them in Notes for the human
- [ ] Phase 2: remaining public-surface files; decide with the human whether to continue to internals
- [ ] Build passes (warnings are errors; no trailing whitespace, RCS1037)
- [ ] Record the new counts in Results

## Notes

- Files from the 523 findings to include early: `nuru-logging-extensions.cs`, `nuru-app-builder-extensions.updates.cs`, `nuru-mcp-server-builder-extensions.cs`, `check-updates-types.cs`.
- Comment-only change; no behavior change.

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
