# Round 1 — general
**Date:** 2026-09-29
**Scope reviewed:** branch `task/365-cleanup-debug-diagnostics` vs `origin/master` — debug-comment removal, warnings-as-errors restore, and syntax-tree diagnostic locations.

## Summary

Generated route matchers no longer emit `CustomConverters.Length` or converter-lookup comments. A search of `source/` finds no `DEBUG:` strings. `nuru-generator.cs` only changes the validation combine: `CompilationProvider` is the outermost `Combine`, and the `Select` indexes match `CreateGeneratorModelWithValidation` (extraction results, endpoints, route locations, endpoint diagnostics, assembly metadata, `hasGeneratedMediator`, compilation). Emit still keys off the equatable `GeneratorModel` via `NuruGeneratorModel`. `LocationInfo.ToLocation` binds `Location.Create(tree, TextSpan)` when `tree.FilePath` equals the captured path, which is the same path `GetLineSpan()` stores, so a `#pragma` in that file can suppress the diagnostic. NURU050–NURU056 service locations all go through that helper. `TreatWarningsAsErrors` is true in `tests/Directory.Build.props`, `tests/timewarp-nuru-tests/Directory.Build.props`, and `samples/Directory.Build.props`. Benchmarks and `tests/test-apps` keep their existing false setting; test-apps does not import the tests props and was not in the remaining-work list. `CS0436` is on the existing test `NoWarn` list with a comment about the generated mediator colliding through `InternalsVisibleTo`. Intentional `NURU056` and `RCS1174` cases are local pragmas. Re-ran `generator-21` (1 passed), `generator-37` (2 passed, including both cache asserts), and `generator-26` (10 passed, including the lifetime-mismatch pragma). No bugs, suggestions, or nits.

## Issues

<!-- none -->
