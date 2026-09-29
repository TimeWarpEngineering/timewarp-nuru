# Cleanup Debug Diagnostics

## Description

Remove the remaining temporary debug output and restore warnings-as-errors that were relaxed during the
#364 / #382 investigations.

## Update 2026-09-28 (triage 477)

Partly done: the `NURU_DEBUG*` diagnostics in `app-extractor.cs` and `NURU_DEBUG_CONV1` in
`dsl-interpreter.cs` were removed by commit f59826bd (454-028). `source/Directory.Build.props` already has
`TreatWarningsAsErrors` true. What remains is below.

## Remaining work

- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`: remove the DEBUG comments
  emitted into generated code (CustomConverters.Length and converter-lookup tracing, around lines 70-72 and
  846-850 on 2026-09-28).
- Check `nuru-generator.cs` for any leftover debug code.
- Re-enable `TreatWarningsAsErrors` (currently `false`) in:
  - `tests/Directory.Build.props`
  - `tests/timewarp-nuru-tests/Directory.Build.props`
  - `samples/Directory.Build.props`
- Fix the warnings this surfaces (do not suppress wholesale). If a sample or test has a warning that is
  intentional for the scenario it demonstrates, suppress that one diagnostic locally with a comment.

## Checklist

- [x] DEBUG comments removed from route-matcher-emitter.cs generated code
- [x] nuru-generator.cs checked
- [x] TreatWarningsAsErrors re-enabled in tests/, tests/timewarp-nuru-tests/, samples/
- [x] Surfaced warnings fixed
- [x] Release build, CI test gate, and samples check pass

## Notes

- Implementer: **commit and push your changes before reporting done.**
- Run the build and test gate in the foreground.

## Results

Generated route matchers no longer emit converter-lookup comments. `nuru-generator.cs` had no leftover debug output; the only edit there passes the compilation into validation so diagnostic locations keep their syntax tree. Path-only locations have no `SourceTree`, so `#pragma warning disable NURU056` did not apply once warnings were errors.

`TreatWarningsAsErrors` is true again in `tests/Directory.Build.props`, `tests/timewarp-nuru-tests/Directory.Build.props`, and `samples/Directory.Build.props`. The repo-root default stays false for benchmarks.

Warnings that the switch surfaced:

- Removed temporary `Console.WriteLine` / `WriteLine` dumps from routing, repl, and generator tests. The CI runner still writes progress to the process console, with a local `RS0030` suppression.
- Dropped the duplicate `#:package Microsoft.Extensions.Logging.Console` on `generator-15-runtime-di.cs` (`NU1504`). The test props already reference that package.
- `NURU056` on the singleton-depends-on-transient registrations the tests exist to resolve is suppressed locally. `RCS1174` is suppressed in `generator-47` because the `async`/`await` bodies are the emission scenario.
- Collection-initializer warnings (`IDE0300`, `IDE0305`) are written as collection expressions.
- `CS0436` is in the test `NoWarn` list. Test assemblies generate `TimeWarp.Mediator.Generated.Mediator` while `TimeWarp.Nuru` (and, in the multi-mode assembly, `TimeWarp.Nuru.Mcp`) already contains that internal type via `InternalsVisibleTo`. The compiler uses the locally generated type.

### How to validate

Smoke:

```bash
dotnet tools/dev-cli/dev.cs -- build
dotnet tools/dev-cli/dev.cs -- verify-samples
ganda runfile cache --clear
dotnet tools/dev-cli/dev.cs -- test
rg -n 'DEBUG:' source/timewarp-nuru-analyzers || echo "no DEBUG comments"
ganda repo audit
```

Expect:

- `dev build` prints `Build succeeded.` for each shipping project.
- `verify-samples` prints `64/64 samples built successfully`.
- The CI runner exits 0. Multi-mode total 1770, passed 1764, skipped 6. Standalone files then pass, and the runner prints `Tests completed successfully!`.
- `rg` prints `no DEBUG comments`.
- Audit prints `Repository passes all audit checks.`

