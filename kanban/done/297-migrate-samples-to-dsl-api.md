# Migrate samples to DSL API

## Description

Update all samples to use the current DSL API. Many samples use deprecated APIs like `CreateSlimBuilder()` or methods the source generator doesn't recognize (`ConfigureServices`, `AddMediator`, `AddAutoHelp`).

## Results

**Superseded by #312** which tracked the same work with more detail.

All numbered samples (01-11) are now working with `NuruApp.CreateBuilder()`:
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

Remaining underscore samples require library migrations (Repl, Completion, Telemetry) which are separate work.

See #312 for complete details.
