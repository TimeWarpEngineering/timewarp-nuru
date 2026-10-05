# Round 1 — general
**Date:** 2026-10-05
**Scope reviewed:** same as framework

## Summary

The diff is documentation and kanban only: a migration guide, public XML `<example>` corrections, a skill alignment, and the review record. Spot-checked falsifiable claims against source: `NuruApp.CreateBuilder()` is parameterless only (`source/timewarp-nuru/nuru-app.cs:155`); `AddReplSupport`/`MapDefault` are gone; `Services` and `AddReplOptions` are `[Obsolete]` shims; `samples/endpoints/15-httpclient` exists; `Unit.Task` is used by samples; `MapParseErrorToDiagnostic` and `ExtensionMethodCall` evidence in `analyzers.md` matches the code. One gap: R-7 is recorded as fixed but four public XML examples still call the removed `CreateBuilder([])` overload.

## Issues

### Issue 1 — Severity: suggestion
- File: source/timewarp-nuru/builders/nuru-app-builder/nuru-app-builder.configuration.cs:50
- Description: R-7 replaced `CreateBuilder(args)` in XML examples but missed `CreateBuilder([])` at configuration.cs:50, :78, :126 and `logging/nuru-logging-extensions.cs:21`. `CreateBuilder` takes no arguments, so the copied example fails to compile. `findings.md` marks R-7 fixed.
- Suggestion: change to `NuruApp.CreateBuilder()`.
- Status: open

### Issue 2 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:392
- Description: internal comments in `dsl-interpreter.cs:392` and `app-extractor.cs:326` and the checked-in agent instructions `.agent/local/nuru-specific.md` still show `CreateBuilder([])` / `CreateBuilder(args)`. Agents follow the instructions file when writing tests.
- Suggestion: update to `CreateBuilder()`.
- Status: open
