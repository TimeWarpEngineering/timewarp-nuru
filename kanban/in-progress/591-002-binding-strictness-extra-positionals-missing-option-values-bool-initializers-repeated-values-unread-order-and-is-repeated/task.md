# Binding strictness: extra positionals, missing option values, bool initializers, repeated values, unread Order and IsRepeated

## Description

Five binding defects where input is silently dropped or an attribute property is silently ignored. They share
the endpoint binding and attribute extraction code, so they are one task.

1. **Extra positional arguments are silently ignored** on a route with a `[Parameter]` (`a b` binds `a`, exit 0),
   but are `Unknown command` on a route with none. An option with a missing value (`-o` at the end) binds the
   default with exit 0.
2. **A `bool` option's initializer is ignored.** `[Option("create")] public bool Create { get; set; } = true;`
   binds `false` when the flag is absent, so a default-true flag is impossible. `--create false` binds `true` and
   passes `false` on as a positional argument.
3. **Several values per token leak into positionals.** With `IsRepeated`, `--env A B` binds `A` and gives `B` to
   an optional `[Parameter]` (or fails with `Unknown command`).
4. **`[Parameter(Order = n)]` is not read, and `NURU_A002` is never reported.** `ExtractParameterFromAttribute`
   reads `Description` and `IsCatchAll` only; position is property declaration order. The
   `MultipleParametersRequireOrder` descriptor exists but nothing reports it, and the `ParameterAttribute` XML
   docs say `Order` is required. Honor `Order` (and `Name`), or remove them and the descriptor. See the open
   question in `source/timewarp-nuru/attributes/parameter-attribute.cs`. (Review M1 on task 496.)
5. **`[Option(IsRepeated = true)]` is not read.** Repeatability comes from the property type
   (`IsRepeatedOptionType`); `IsRepeated = true` on a scalar does nothing, silently. Read it (or warn), or remove
   it. See the open question in `source/timewarp-nuru/attributes/option-attribute.cs`. (Review M4.)

## Goal (implementer restatement)

Binding must not drop input or ignore an attribute the docs already promise. The five original items stay fixed. Review round 1 narrowed two of them and closed three gaps.

1. A route with no catch-all rejects tokens past its last positional. A value option that is present but has no value does not keep a default and does not run the handler. Both fail as Unknown command, exit 1. `--opt=` counts as a value. The default applies only when the option is absent.

2. A non-nullable bool whose initializer is true is a value option. Leaving it off stays true. `--create false` and `--create true` bind that bool. A bare `--create` has no value, so it is Unknown command, exit 1. A normal flag means presence is true and does not take the next token, even when that token is spelled like a boolean (`0`, `false`, `yes`). That token stays a positional. `--flag=true` stays an unknown option.

3. A repeated option takes every following non-option token after one space-separated flag, until the next declared option. `--env A B` binds both. A positional after those values is swallowed, and that is documented.

4. `[Parameter]` reads `Order` and `Name`. Two or more parameters that omit `Order` report `NURU_A002`. Parameters sort by `Order`, then declaration order. A catch-all stays last. `Name` must be an identifier (letter or underscore, then letters, digits, and underscores). A name such as `source-file` reports `NURU_A005` and the route uses the property name, so the matcher and the constructor still compile. Docs that show several parameters set `Order` and say it is required.

5. `[Option(IsRepeated = true)]` on a scalar reports `NURU_A004`. On a collection the option is already repeated. `IsRepeated = false` does not turn a collection into one value.

Acceptance: the routing tests show a boolean-looking positional after a flag still binds, and a bare default-true bool exits 1; the generator test reports `NURU_A005` for a hyphenated `Name` and the generated source does not declare that hyphenated local; the endpoint guides set `Order`; the migration guide and the migrate skill describe the flag and the bare `--create` rejection; the unused property-syntax extractor is gone; build, the touched test files, and one repo audit pass.

This fix pass closes review M6. A reader who copies an example with two `[Parameter]` properties must get a build that passes `NURU_A002`. A catch-all counts as one of those properties. `GetCommand` and `RunCommand` in the System.CommandLine migration guide set `Order = 0` on the positional and `Order = 1` on the catch-all. The getting-started add command, the readme's second add command, and the endpoint guide's required/optional pair are the same omission, so they set `Order` too. The binder does not change.

Acceptance for this pass: those examples set `Order`. `./bin/dev build` and one `ganda repo audit` pass. No test file changes, because the edit is documentation.

This pass makes the branch mergeable. The binding behaviour above stays. Two things were blocking the pull request.

1. The Roslyn-hosted binding diagnostic runfile must not join the CI multi assembly. `Lexer` and `Token` already exist in both `TimeWarp.Nuru` and `TimeWarp.Nuru.Analyzers`, so compiling that runfile into the multi assembly is CS0433. `generator-76` is the converter-constraint test. `generator-77` and `generator-78` are already on `feature/591-system-commandline-gaps` (endpoint `IOptions<T>`, and keyword and shared type names), so this runfile is `generator-79-binding-attribute-diagnostics.cs`. It is listed in `CiTestExcludes` and in `tests/ci-standalone/run-standalone-tests.cs`, the same way the other Roslyn-hosted generator tests are. Its namespace uses 79.

2. Merge `origin/feature/591-system-commandline-gaps` and keep both sides. This task still rejects extra positionals, rejects a present value option that has no value, treats a default-true bool as a value option, lets a repeated option take the following values, reads `Order` and `Name`, and reports `NURU_A004` and `NURU_A005`. The feature branch still binds `FileInfo`, `DirectoryInfo`, and `IPAddress` endpoint properties, cancels the handler token on Ctrl+C, writes a handler exception to stderr and returns 1, injects `IOptions<T>` into an endpoint handler, and emits legal names for keyword members and shared type names. `GetTypeConstraintFromClrType` maps those three CLR types. The migration guide and the migrate skill say both.

Acceptance for this pass: the renamed runfile is excluded from the multi assembly and listed for the standalone phase. The merge keeps both behaviours. `./bin/dev build`, `dotnet run --file` on the renamed runfile, `dotnet run tests/ci-tests/run-ci-tests.cs` (multi phase and standalone phase), and one `ganda repo audit` pass.

This fix pass closes review M7 and M8. A repeated option still takes every following non-option value, and an optional positional still reserves none, so `--env A B` with `{name?}` binds both values. A required positional that is still unbound takes back trailing values from the rightmost greedy run. That run keeps one value when its element is required. When a required positional or a catch-all follows, a typed repeated option also stops at the first token that does not convert, so `-p 80 nginx echo hi` binds the image and the command. `routing-09` uses `--tag A run.sh` again. The binding-strictness runfile is `routing-45`, because `routing-43` is the cli-failures runfile. The migration guide and the migrate skill say that only an optional positional is swallowed.

Acceptance for this pass: `--tag A run.sh` binds the script. `--env A B` with an optional name binds both values. `docker build --build-arg K1=v1 --build-arg K2=v2 .` binds the path. The fluent docker-run shape binds `nginx` as the image. An endpoint collection leaves the required parameter. `./bin/dev build`, `dotnet run --file` on each edited routing runfile, and one `ganda repo audit` pass.

This fix pass closes review M9 and M10. A string repeated option still takes every following value when the route has no catch-all, and a required positional still takes back what it needs. When a catch-all follows and the route has no `--` before that catch-all, a string element cannot be told apart from the command tokens, so one flag takes one value. `fluent-docker-run --env A=1 nginx echo hi` binds `e=[A=1]`, `image=nginx`, `cmd=[echo,hi]`. `docker --env A echo hi` on `docker --env? {e}* {*cmd}` binds `e=[A]`, `cmd=[echo,hi]`. Repeat the flag for more values. A `--` before the catch-all still takes every value before the separator. A typed element still takes every token that converts. The migration guide and the migrate skill say this. The runfile cache is cleared, and both CI phases run.

Acceptance for this pass: those two routing-45 cases pass, and the `--` route still binds both env values. `./bin/dev build`, `dotnet run --file` on routing-45, `ganda runfile cache --clear`, `env -u NURU_CI_PHASE dotnet run tests/ci-tests/run-ci-tests.cs` with both phase counts recorded, and one `ganda repo audit` pass.

This fix pass closes review M11 and M12. The binding behaviour stays. routing-45 gets back the blank lines that were removed to slip under the 500-line cap. The greedy-run, give-back, catch-all, and endpoint-collection cases move to `routing-46-repeated-option-binding.cs`, with their own test class and the same `#if !JARIBU_MULTI` entry point. Those builder chains wrap across lines. The one-value rule is a string element, or an element with no built-in conversion (an enum, a custom converter). User docs say string or enum. An element with a built-in conversion still takes every token that converts.

Acceptance for this pass: both runfiles stay under 500 lines, routing-45 has its blank lines back, and routing-46's chains are wrapped. The emitter summary, the migration guide's option-syntax row and checklist item 3, and the migrate skill say that. `./bin/dev build`, `dotnet run --file` on routing-45 and on routing-46, and one `ganda repo audit` pass.

## Checklist

- [x] Decide per item: fix it, or keep the behavior and say why in Results
- [x] Fix with tests for each accepted item
- [x] Update "Behavior differences to check" in `documentation/user/guides/migrating-from-system-commandline.md`
      and `skills/tw-nuru-migrate-system-commandline/SKILL.md` for each fix
- [x] `ganda runfile cache --clear`, then CI tests green

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-17), split from 591 during groom
- Implementer: all five items fixed. Cache cleared. Touched runfiles passed. Full CI is the pull request, so the last checklist box stays open.
- Boy-scout: option attribute reading moved to `endpoint-extractor.option-attributes.cs` so the properties file stays under the 500-line cap. The repeated-option test uses `IReadOnlyList<string>` so CA1819 needs no suppression. `ganda repo audit --fix` wrote the gitignored `.vscode/settings.json` window title.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1f399-cd39-7b01-801f-35c11cd59b70, max-turns 500, Done — 2026-11-19T12:23:27Z
- Review round 1 (2026-11-19): review oracle, Claude Opus 5.5, effort 3, roster general. Scope `cdbd12eb..HEAD`.
  5 findings open (M1–M3 bug, M4 suggestion, M5 nit); fix pass requested. Files under `review/round-1/`.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session d6032c14-55c2-4816-8407-2b95f06f556f, max-turns 200, artifacts general.md, merged.md — 2026-11-19T12:27:50Z
- Fix pass (implement-fix-2, 2026-11-19): M1 a plain flag sets true and leaves the next token. M2 `NURU_A005` plus the property-name fallback. M3 the endpoint guides set `Order`. M4 a bare `--create` stays Unknown command and the migration guide and skill say so. M5 deleted `ExtractSegmentFromProperty`. The review example `seek --verbose {pos:int?}` is a value option whose parameter is `pos`, so the collision tests use `seek {pos:int?} --verbose` and `repeat {count:int} --quiet` with the boolean token still after the flag.
- Review round 2 (2026-11-19): review oracle, Claude Opus 5.5, effort 2, roster general. Scope `14104e13..HEAD`.
  M1–M5 fixed. New M6 (bug) open: two migration-guide examples omit `Order`. Fix pass requested. Files under `review/round-2/`.
- Fix pass (implement-fix-3, 2026-11-19): M6 sets `Order` on `GetCommand` and `RunCommand`. The same omission was in the getting-started add command, the readme's second add command, and the endpoint guide's required/optional pair. Those set `Order` too. No binder change.
- Review round 3 (2026-11-19): review oracle, Claude Opus 5.5, effort 1, roster general. Scope `6b29609c..HEAD`.
  M6 fixed, no new findings. Disposition clean. Files under `review/round-3/` and `review/disposition.md`.
- Restart (2026-11-19): merged `origin/feature/591-system-commandline-gaps` and kept both sides. The binding diagnostic runfile is `generator-79` because 76, 77, and 78 were already taken. `GetTypeConstraintFromClrType` maps `FileInfo`, `DirectoryInfo`, and `IPAddress`. The complex-integration case uses `--tag=A` so the following positional is not swallowed.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1f4ac-8cab-7bb0-bba5-1566e920e113, max-turns 500, Done — 2026-11-19T17:09:49Z
- Review round 4 (2026-11-20): review oracle, Claude Opus 5.5, effort 3, roster general. Scope `8fceec27..61bb293f`
  (restart merge `06f35a7a`). The merge resolution and the generator-79 exclusion are correct. New M7 (bug):
  a greedy repeated option swallows a required positional, which breaks `{args}* {path}` routes; the routing-09
  input edit hid it. New M8 (nit): duplicate routing-43 number. Fix pass requested. Files under `review/round-4/`.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 196231a7-47bc-48b0-8d5a-14a9659b75ad, max-turns 200, artifacts general.md, merged.md, disposition.md — 2026-11-19T17:12:45Z
- Fix pass (implement-fix-2, 2026-11-20): M7 gives trailing values of the rightmost greedy run back to required positionals that are still unbound, and a typed element stops at a token that does not convert when a required positional or a catch-all follows. An optional positional still reserves nothing. routing-09 uses `--tag A run.sh` again. M8 renames the binding-strictness runfile to `routing-45`.
- Review round 5 (2026-11-20): review oracle, Claude Opus 5.5, effort 2, roster general. Scope `61bb293f..59102fad`.
  M7 and M8 fixed. New M9 (bug): a string repeated option starves `{image} {*cmd}`, so the required positional
  binds the wrong token and the run exits 0. New M10 (suggestion): no cache clear and no CI-suite run recorded
  after the generator change. Fix pass requested. Files under `review/round-5/`.
- Fix pass (implement-fix-3, 2026-11-20): M9 takes one value per flag for a string repeated option when a catch-all follows and the route has no `--` before that catch-all. A typed element still takes every converting token. A `--` before the catch-all still takes every value before the separator. M10 clears the runfile cache and records both CI phases. routing-45 was at the 500-line cap, so blank lines inside the tests and between command members came out to fit the new cases.
- Review round 6 (2026-11-20): review oracle, Claude Opus 5.5, effort 2, roster general. Scope `59102fad..1d29674c`.
  M9 and M10 fixed. New M11 (bug): routing-45 was squeezed under the 500-line cap instead of being split. New M12
  (nit): the one-value wording names only string elements. Fix pass requested. Files under `review/round-6/`.
- Fix pass (implement-fix-4, 2026-11-20): M11 restores the blank lines in routing-45 and moves the greedy-run, give-back, catch-all, and endpoint-collection cases to routing-46, with the builder chains wrapped. M12 says the one-value rule is a string element or an element with no built-in conversion. User docs say string or enum.
- Review round 7 (2026-11-20): review oracle, Claude Opus 5.5, effort 2, roster general. Scope `1d29674c..HEAD`.
  M11 and M12 fixed. No new findings. Disposition clean. Files under `review/round-7/`.

## Results

All five items are fixed.

1. A route with no catch-all rejects tokens past its last positional (required plus at most the optional ones). Failure is a route skip: `Unknown command`, exit 1. A catch-all still takes the rest.
2. A present value option with no value token skips the route only when the value is required (`!ParameterIsOptional`). The initializer applies only when the option is absent. `--opt=` counts as a value. A nullable value (`string?`, fluent `{mode?}`) still binds null and exits 0. There is no dedicated missing-value sentence, because option parsing runs before the literal check.
3. A non-nullable `bool` whose initializer is `true` is a value option and keeps the attribute's long and short forms. Leaving it off stays true. `--create false` and `--create true` bind that bool. A bare `--create` with no token skips the route, because the value is required. A normal flag means presence is true and does not take the next token. A following `0`, `false`, `yes`, or other boolean spelling stays a positional. `--flag=true` stays an unknown option.
4. A repeated option (collection type, or fluent `*`) takes every following non-option token after one space-separated flag, until the next declared option or an option-like token. `--env A B` binds both. `--env A --env B` still works. `--env=A` stays one value. An optional positional after those values is swallowed. A required positional that is still unbound takes back trailing values from the rightmost greedy run, and that run keeps one value when its element is required. When a required positional or a catch-all follows, an element with a built-in conversion stops at the first token that does not convert. When a catch-all follows and the route has no `--` before that catch-all, a string element, or an element with no built-in conversion, takes one value per flag, so `fluent-docker-run --env A=1 nginx echo hi` binds `e=[A=1]`, `image=nginx`, `cmd=[echo,hi]`. An element with a built-in conversion still takes every token that converts. Repeat the flag for more values. A `--` before the catch-all still takes every value before the separator. A bare repeated flag whose element is required skips the route.
5. `[Parameter]` reads `Order` and `Name`. Two or more parameters that omit `Order` report `NURU_A002` (error) and the route is kept. Parameters sort by `Order`, then declaration index. A catch-all stays last. Duplicate explicit orders keep declaration order as the tie-break. `Name` is trimmed and lowercased when it is an identifier (letter or underscore, then letters, digits, and underscores). A name such as `source-file` reports `NURU_A005` and the route uses the property name lowercased, so the matcher and the constructor both compile. In-repo multi-parameter endpoints set `Order` to their declaration index so current binding order is unchanged. A single parameter may omit `Order`. The user and generator guides set `Order` on every parameter in a multi-parameter example. The migration guide's `GetCommand` and `RunCommand`, the getting-started add command, the readme's second add command, and the endpoint guide's required/optional pair set `Order` too. A catch-all counts toward `NURU_A002`.
6. `[Option(IsRepeated = true)]` is read. On a collection it is already repeated. On a scalar it reports `NURU_A004` (error) and the option stays a single value. `IsRepeated = false` does not turn a collection into one value. The dead `LongName` / `ShortName` / `IsRequired` branches on `OptionAttribute` stay an open question.

The branch now contains `feature/591-system-commandline-gaps` through 591-007. `FileInfo`, `DirectoryInfo`, and `IPAddress` still map to `fileinfo`, `directoryinfo`, and `ipaddress` in `GetTypeConstraintFromClrType` (the method lives in `endpoint-extractor.option-attributes.cs`). The migration guide and the migrate skill keep both the binding rules and the feature-branch rules (stderr errors, `RunAsync` returns 1, Ctrl+C cancels the handler token, `IOptions<T>` in a handler constructor, keyword and shared type names). They also say that a string or enum repeated option, or any other element with no built-in conversion, takes one value per flag when a catch-all follows and the route has no `--` before it. The binding diagnostic runfile is `generator-79-binding-attribute-diagnostics.cs` and is excluded from the CI multi assembly. `routing-09-complex-integration.cs` passes `--tag A run.sh`, and `run.sh` stays the script. The binding-strictness runfile is `routing-45-binding-strictness.cs` (420 lines). The greedy-run, give-back, catch-all, and endpoint-collection cases are `routing-46-repeated-option-binding.cs` (237 lines).

### How to validate

Smoke:

```bash
./bin/dev build
dotnet run --file tests/timewarp-nuru-tests/routing/routing-45-binding-strictness.cs
dotnet run --file tests/timewarp-nuru-tests/routing/routing-46-repeated-option-binding.cs
ganda repo audit
```

Expect:

- `./bin/dev build` succeeds with 0 warnings and 0 errors.
- routing-45 passes 20 tests. Blank lines are back between the builder, the run, and the assertions, and between command members. The file is under 500 lines.
- routing-46 passes 11 tests. `--env A=1 nginx echo hi` binds `e=[A=1]`, `image=nginx`, `cmd=[echo,hi]`. `docker --env A echo hi` on `docker --env? {e}* {*cmd}` binds `e=[A]`, `cmd=[echo,hi]`. `--env A B -- echo hi` on a route with `--` before the catch-all binds both env values. `-p 80 443 nginx echo hi` still binds both ports. `--env A B` with `{name?}` binds both values, and the endpoint collection leaves the required parameter. Builder chains wrap across lines. The file is under 500 lines.
- `ganda repo audit` passes.

Gates (implement-fix-4, 2026-11-20):

- `./bin/dev build` — succeeded (0 warnings, 0 errors)
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-45-binding-strictness.cs` — 20 passed. Standalone runfile (`#if !JARIBU_MULTI`)
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-46-repeated-option-binding.cs` — 11 passed. Standalone runfile (`#if !JARIBU_MULTI`)
- `ganda repo audit` — passed (45 passed, 0 failed, 1 skipped)

Prior gates (implement-fix-3, 2026-11-20), not rerun:

- `ganda runfile cache --clear` — cleared 100 entries
- `./bin/dev build` — succeeded (0 warnings, 0 errors)
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-45-binding-strictness.cs` — 31 passed. Standalone runfile (`#if !JARIBU_MULTI`)
- `env -u NURU_CI_PHASE dotnet run tests/ci-tests/run-ci-tests.cs` — multi 2332 passed, 7 skipped, 0 failed (total 2339); standalone 50 files, 0 failed
- `ganda repo audit` — passed (45 passed, 0 failed, 1 skipped)

Prior gates (implement-fix-2, 2026-11-20), not rerun:

- `./bin/dev build` — succeeded (0 warnings, 0 errors)
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-45-binding-strictness.cs` — 26 passed. Standalone runfile (`#if !JARIBU_MULTI`)
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-09-complex-integration.cs` — 4 passed. Standalone runfile (`#if !JARIBU_MULTI`)
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-06-repeated-options.cs` — 6 passed. Standalone runfile (`#if !JARIBU_MULTI`). Covers `RepeatedOptionsTests`; the file was not edited
- `ganda repo audit` — passed (45 passed, 0 failed, 1 skipped)

Prior gates (restart, 2026-11-19), not rerun:

- `ganda runfile cache --clear` — cleared 191 entries
- `./bin/dev build` — succeeded (0 warnings, 0 errors)
- `dotnet run --file tests/timewarp-nuru-tests/generator/generator-79-binding-attribute-diagnostics.cs` — 5 passed
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-44-endpoint-file-directory-ip.cs` — 9 passed
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-09-complex-integration.cs` — 4 passed (that pass used `--tag=A`; this pass restored `--tag A run.sh`)
- `env -u NURU_CI_PHASE dotnet run --file tests/ci-tests/run-ci-tests.cs` — multi 2321 passed, 7 skipped, 0 failed; standalone 50 files, 0 failed
- `ganda repo audit` — passed (45 passed, 0 failed, 1 skipped)

Prior pass (implement-fix-3), not rerun:

- `./bin/dev build` — succeeded (0 warnings, 0 errors)
- no test file — documentation only; no production type changed
- `ganda repo audit` — passed (45 passed, 0 failed, 1 skipped)

Prior pass (implement-fix-2), not rerun:

- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-43-binding-strictness.cs` — 20 passed (file is now routing-45)
- `dotnet run --file tests/timewarp-nuru-tests/generator/generator-76-binding-attribute-diagnostics.cs` — 5 passed (file is now generator-79)

### Review

- Round 1 (effort 3, roster general): 3 bug, 1 suggestion, 1 nit. The fix pass implemented M1–M5. Statuses stay open for review round 2.
- Round 2 (effort 2, roster general, scope `14104e13..HEAD`): M1–M5 fixed. M6 (bug) implemented in implement-fix-3:
  `GetCommand` and `RunCommand` set `Order`, and the same omission in getting-started, the readme, and the endpoint
  optionality pair does too.
- Round 3 (effort 1, roster general, scope `6b29609c..HEAD`): M6 verified fixed. No new findings.
- Rounds 1–3 closed clean on 2026-11-19 (bug 4 fixed, suggestion 1 fixed, nit 1 fixed).
- Round 4 (effort 3, roster general, scope `8fceec27..61bb293f`, the restart merge): M1–M6 still fixed. New M7
  (bug) and M8 (nit) implemented in this fix pass. A required positional takes back trailing greedy values.
  The binding-strictness runfile is `routing-45`. Statuses stay open for the next review.
- Round 5 (effort 2, roster general, scope `61bb293f..59102fad`, the M7/M8 fix pass): M7 and M8 fixed. New M9
  (bug) and M10 (suggestion) implemented in this fix pass. A string repeated option takes one value per flag
  when a catch-all follows and the route has no `--` before it. The cache clear and both CI phases are in
  the gate record. Statuses stay open for the next review.
- Round 6 (effort 2, roster general, scope `59102fad..1d29674c`, the M9/M10 fix pass): M9 and M10 fixed. New M11
  (bug): routing-45 was squeezed under the 500-line cap (blank lines removed, builder chains on one long line)
  instead of being split. New M12 (nit): the one-value rule also covers an enum or custom-converter element, and
  the text says only "string". Fix pass requested.
- This fix pass implements M11 and M12. routing-45 has its blank lines back. routing-46 holds the greedy-run,
  give-back, catch-all, and endpoint-collection cases, with wrapped builder chains. The one-value wording names
  a string element or an element with no built-in conversion. Statuses stay open for the next review.
- Round 7 (the host's review round 4 of this walk; effort 2, roster general, scope `1d29674c..HEAD`, the M11/M12
  fix pass): M11 and M12 fixed. No new findings.
  - All 11 moved cases have the same routes, argv, and expected output.
  - routing-45 is 420 lines and routing-46 is 237.
  - The emitter diff touches only comments.
- Final counts: bug 7 fixed, suggestion 2 fixed, nit 3 fixed (M1–M12), 0 open, 0 wontfix.
- Disposition: **clean** (7 rounds).
- Framework: `review/review-framework.md`. Last merged: `review/round-7/merged.md`. Disposition: `review/disposition.md`.

## Learnings

- Misread (review round 1): original item 2 asks that a default-true bool not ignore its initializer and that
  `--create false` bind. The restatement widened that to "a normal flag also consumes a following boolean
  token". That widening is outside the task and is the source of review M1 (a boolean-looking positional after
  any flag is swallowed). why: review round 1 M1, 2026-11-19
- Misread (review round 4): original item 3 treats a value that lands in an *optional* `[Parameter]` as the
  leak. Restatement item 3 widened the fix to "a positional after those values is swallowed", and that
  includes a *required* positional. So `{args}* {path}` routes stop matching (routing-09, the docker
  intercept design docs, the fluent-docker-run sample). An existing test that never ran on this branch
  (CI failed at compile) hid the break until the restart. why: review round 4 M7, 2026-11-20
- The M7 give-back returns only the required-positional deficit. A string repeated option accepts every
  token, so a following catch-all with no `--` before it was left empty and the last token became the
  required positional. One value per flag is the rule for that shape. A typed element still stops at the
  first token that does not convert, and a `--` before the catch-all still keeps the greedy run.
  why: review round 5 M9, 2026-11-20

## Review

- Round 1: Blocked — open M1, M2, M3, M4, M5
- Fix pass implemented M1–M5.
- Round 2: Blocked — open M6
- Fix pass implemented M6.
- Round 3: Done
- Round 4 (restart merge): Blocked — open M7, M8
- Round 4: Blocked — open M7, M8
- Fix pass implemented M7 and M8.
- Round 5: Blocked — open M9, M10
- Fix pass implemented M9 and M10.
- Round 6: Blocked — open M11, M12
- Fix pass implemented M11 and M12.
- Round 7: Done
## Notes

Found by task 496 (System.CommandLine migration guide) against TimeWarp.Nuru 4.0.0-beta.14. Parent: 591.

## Restart 2026-11-19: what is left

Done in this pass. The notes below are the brief that was open.

The implementation and three review rounds are on this branch (PR #428, base `feature/591-system-commandline-gaps`).
Do not re-implement. Two things keep the PR from merging:

1. **CI red, `test-multi` (run 44529607941).** CS0433: `Lexer` and `Token` exist in both `TimeWarp.Nuru.Analyzers`
   and `TimeWarp.Nuru`. Cause: the new `tests/timewarp-nuru-tests/generator/generator-76-binding-attribute-diagnostics.cs`
   references the analyzers project and is compiled into the CI multi assembly. Add it to `CiTestExcludes` in
   `tests/ci-tests/Directory.Build.props` and to the standalone list in `tests/ci-standalone/run-standalone-tests.cs`,
   the same way `generator-76-converter-constraint-alias.cs` is listed. That file already uses number 76; renumber
   the new one to 77.
2. **Conflicts with the base branch.** 591-004 to 591-007 merged onto the feature branch after this child parked.
   `git merge origin/feature/591-system-commandline-gaps` conflicts in
   `documentation/user/guides/migrating-from-system-commandline.md`, `skills/tw-nuru-migrate-system-commandline/SKILL.md`
   and `source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.properties.cs`. Merge the base in and
   keep both sides' behaviour.

Then run the CI tests (`dotnet run tests/ci-tests/run-ci-tests.cs`) and the standalone phase.
