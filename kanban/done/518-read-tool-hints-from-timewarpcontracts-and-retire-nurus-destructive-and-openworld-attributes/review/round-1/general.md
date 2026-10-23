# Round 1 — general
**Date:** 2026-10-23
**Scope reviewed:** branch a58b48fc vs master ba022741 (21 files, +146/−94), plus call sites of the touched extractor helpers

## Summary

The change bumps `TimeWarp.Contracts` to 2.0.0-beta.24, switches the generator's hint lookup to
`TimeWarp.Contracts.DestructiveAttribute` / `OpenWorldAttribute` by metadata name, and adds
`TitleValue`: an `[Operation]` takes its title only from `OperationAttribute.Title` (blank or whitespace
is none), and a `[NuruRoute]` class with no operation keeps `[DisplayName]`. The Nuru attribute pair is
deleted. Docs, the skill, the calculator sample, and the changelog (Changed + Removed) follow. Risk is
low. The tri-state rule is unchanged (`ConstructorArguments.Length == 0 || Value is not false`). Host
overrides are untouched. No remaining reference to `TimeWarp.Nuru.DestructiveAttribute` /
`OpenWorldAttribute` exists outside kanban and historical changelog entries.

Verified here (after `ganda runfile cache --clear`):

- `mcphost-04-schema-fidelity.cs`: Total 6, Passed 6. It covers the operation Title, `[DisplayName]`
  ignored on an operation, a whitespace Title giving no title, and the host override.
- `mcphost-01-tools.cs`: Total 16, Passed 16. A route class takes `[Destructive]` and its `[DisplayName]`
  title, and the file imports both `TimeWarp.Nuru` and `TimeWarp.Contracts` and uses bare `[Destructive]`,
  so the CS0104 case compiles.
- `calculator-contracts.cs -- --capabilities` shows `"openWorld": false` and `"title": "Total numbers"`.

Restatement check: the implementer restatement matches the task Goal. It picks the preferred options the
task recommends: route endpoints use the Contracts attributes, and an operation's title is Contracts
`Title` only. No misread.

## Issues

### Issue 1 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/models/route-definition.cs:27
- Description: The rewritten `<param name="Destructive">`, `<param name="OpenWorld">`, and
  `<param name="Title">` XML doc lines (27–30, 41–45) are indented two spaces. Every other `///` line in
  that type-level doc block starts at column 0.
- Suggestion: Remove the two leading spaces so the block is uniform.
- Status: open
