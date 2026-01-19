# Migrate samples to DSL API

## Results

**COMPLETE** - Superseded by #312, which is now done.

All numbered samples (01-12, 99) migrated to `NuruApp.CreateBuilder()`.
Remaining underscore samples tracked in #338, #339, #340.

## Description

Update all samples to use the current DSL API. Many samples use deprecated APIs like `CreateSlimBuilder()` or methods the source generator doesn't recognize (`ConfigureServices`, `AddMediator`, `AddAutoHelp`).

## Completed Samples

All numbered samples (01-12) are now working with `NuruApp.CreateBuilder()`:
- 01-hello-world (3 samples)
- 02-calculator (3 samples)
- 03-attributed-routes (csproj)
- 04-syntax-examples
- 05-aot-example (csproj)
- 06-async-examples
- 07-pipeline-middleware (6 samples)
- 08-testing (4 samples + runfile harness)
- 09-configuration (4 samples)
- 10-type-converters (2 samples)
- 11-unified-middleware
- 12-logging (2 samples)
- 99-timewarp-nuru-sample

## Remaining Work (separate tasks)

- #338 - `_repl-demo/` (REPL library migration)
- #339 - `_aspire-*` (Telemetry library migration)
- #340 - `_completion-*` (Completion library migration)

See #312 for complete implementation details.
