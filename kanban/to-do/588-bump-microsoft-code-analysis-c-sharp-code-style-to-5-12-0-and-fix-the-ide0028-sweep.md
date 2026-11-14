# Bump Microsoft.CodeAnalysis.CSharp.CodeStyle to 5.12.0 and fix the IDE0028 sweep

## Description

PR #409 (feature/generator-fixes-2026-11-14, 2026-11-15) moved `Microsoft.CodeAnalysis.CSharp` and
`Microsoft.CodeAnalysis.Analyzers` to 5.12.0 because TimeWarp.SourceGenerators 1.0.0-beta.16 requires
Roslyn 5.12. `Microsoft.CodeAnalysis.CSharp.CodeStyle` stayed at 5.9.0 on purpose: at 5.12.0 the
CodeStyle analyzers report IDE0028 (collection initialization can be simplified) as a build error
(warnings are errors in this repo) across the analyzers project. Probe on that branch:

- 90 IDE0028 errors in 19 files, all under `source/timewarp-nuru-analyzers/`
  (validation/service-validator.cs, generators/extractors/service-registration-methods.cs,
  generators/emitters/completion-data-extractor.cs, generators/dependency-graph-builder.cs,
  generators/extractors/extension-method-lowerer.cs, and others)

This task finishes the pin currency: bump CodeStyle and make the code conform.

## Checklist

- [ ] `Directory.Packages.props`: `Microsoft.CodeAnalysis.CSharp.CodeStyle` 5.9.0 → 5.12.0
- [ ] Fix every IDE0028 (collection expressions / simplified initializers); use the code fix, do not suppress
- [ ] Any other new IDE diagnostics 5.12 turns on: fix the same way, or justify a severity change in `.editorconfig` with a reason and this task id
- [ ] `ganda runfile cache --clear`, `./bin/dev build`, `./bin/dev test` green
- [ ] `ganda repo audit` passes; `ganda nuget outdated` no longer lists CodeStyle
- [ ] Results section with the before/after count

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-15)

## Notes

Mechanical sweep, no behaviour change intended. Keep the analyzer project's generated output byte-identical
(generator tests cover it). Pin comment in Directory.Packages.props already names 5.12 as the Roslyn floor.
