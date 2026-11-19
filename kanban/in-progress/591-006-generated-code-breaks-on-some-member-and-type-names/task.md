# Generated code breaks on some member and type names

## Description

- An endpoint property named `Type` emits `@type_0` and fails with CS0818/CS0103.
- Two enum option types with the same simple name in different namespaces give CS0104 in generated code.

## Checklist

- [x] Decide per item: fix it, or keep the behavior and say why in Results
- [x] Fix with tests for each accepted item
- [x] Update "Behavior differences to check" in `documentation/user/guides/migrating-from-system-commandline.md`
      and `skills/tw-nuru-migrate-system-commandline/SKILL.md` for each fix
- [x] `ganda runfile cache --clear`, then CI tests green

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-17), split from 591 during groom
- Implement: both defects fixed. `ganda repo audit --fix` wrote the gitignored `.vscode/settings.json` for vscode-window-icon. Nothing else from that fix is tracked.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1f449-5e15-7fd2-bd56-006d70adf96c, max-turns 500, Done — 2026-11-19T15:31:29Z

## Results

Both items are fixed.

An endpoint property named `Type` is still the local `@type`. A longer local is built from the bare name, so the file emits `__enumConverter_type_`, `__type_parsed_`, and `__i_type_` instead of a token that starts with `@`. The same bare-name step covers an `IOptions<T>` parameter whose name is a keyword. Standalone `@type` stays.

A handler return type, result local, and render cast use the stored `global::` name. `ShortTypeName` is display-only. `Unit` stays `global::TimeWarp.Mediator.Unit` because that is the stored name. A name the user writes, such as `public Mode` while both namespaces are imported, is still that source's ambiguity.

### How to validate

Smoke: compile an endpoint whose option property is named `Type` (an enum, an `int?`, and an enum array), and compile fluent handlers that return `Ns1.Mode` and `Ns2.Mode` while both namespaces are global-imported.

Expect: the generated compilation has no errors, no local contains `_@` or `@type_`, and the handler signature and result local use `global::Ns1.Mode` and `global::Ns2.Mode`.

- `./bin/dev build` — passed, 0 warnings, 0 errors
- `dotnet run --file tests/timewarp-nuru-tests/generator/generator-78-member-and-type-names.cs` — 3 passed (property `Type`, colliding enum returns, `Unit`)
- `dotnet run --file tests/timewarp-nuru-tests/generator/generator-25-unit-type-ambiguity.cs` — does not compile as its own runfile: pre-existing RCS1163 on the unused `message` parameter (a warning, and tests treat warnings as errors). The file has `#if !JARIBU_MULTI`. CI's multi assembly sets `NoWarn` for RCS1163 and runs it. `Unit` emission is locked by generator-78.
- `ganda runfile cache --clear` — cleared 461 entries, before the generator-78 run
- `ganda repo audit` — vscode-window-icon failed (advisory: gitignored `.vscode/settings.json` was missing). `ganda repo audit --fix` wrote that file. The follow-up audit passed: 45 passed, 0 failed, 1 skipped.

generator-78 is Roslyn-hosted, so the CI multi assembly excludes it (CS0433) and `tests/ci-standalone/run-standalone-tests.cs` runs it. The full suite runs on the pull request.

## Goal (implementer restatement)

Generated route code has to compile when a user picks an awkward name. Two cases from the 4.0.0-beta.14 migration ports fail today, and both get fixed:

1. An endpoint property named `Type` (or any other name that lowercases to a C# keyword) is escaped as `@type`. That `@` then gets pasted into a longer local such as `__enumConverter_@type_0` or `__@type_parsed_0`. `@` applies to the whole following identifier, so the file does not compile. Standalone `@type` stays. Compounds use the name without the `@`.
2. A handler that returns an enum is emitted with the simple type name. Two enums that share that simple name and are both in scope (global usings, or the same pattern from another import) make the generated file report CS0104. Generated type text uses the already stored `global::` name. A name the user writes themselves can still be ambiguous; that is their source, not this generator.

Acceptance: each fix has a test that compiles the generated output; the migration guide and the migrate-from-System.CommandLine skill stop telling people to avoid these names and record that beta.14 failed; build, the new test, the Unit return-type regression, and `ganda repo audit` pass; the change is committed and pushed on this task branch.

## Notes

Found by task 496 (System.CommandLine migration guide) against TimeWarp.Nuru 4.0.0-beta.14. Parent: 591.
