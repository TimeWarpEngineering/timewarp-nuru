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

## Checklist

- [x] Decide per item: fix it, or keep the behavior and say why in Results
- [x] Fix with tests for each accepted item
- [x] Update "Behavior differences to check" in `documentation/user/guides/migrating-from-system-commandline.md`
      and `skills/tw-nuru-migrate-system-commandline/SKILL.md` for each fix
- [ ] `ganda runfile cache --clear`, then CI tests green

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

## Results

All five items are fixed.

1. A route with no catch-all rejects tokens past its last positional (required plus at most the optional ones). Failure is a route skip: `Unknown command`, exit 1. A catch-all still takes the rest.
2. A present value option with no value token skips the route only when the value is required (`!ParameterIsOptional`). The initializer applies only when the option is absent. `--opt=` counts as a value. A nullable value (`string?`, fluent `{mode?}`) still binds null and exits 0. There is no dedicated missing-value sentence, because option parsing runs before the literal check.
3. A non-nullable `bool` whose initializer is `true` is a value option and keeps the attribute's long and short forms. Leaving it off stays true. `--create false` and `--create true` bind that bool. A bare `--create` with no token skips the route, because the value is required. A normal flag means presence is true and does not take the next token. A following `0`, `false`, `yes`, or other boolean spelling stays a positional. `--flag=true` stays an unknown option.
4. A repeated option (collection type, or fluent `*`) takes every following non-option token after one space-separated flag, until the next declared option or an option-like token. `--env A B` binds both. `--env A --env B` still works. `--env=A` stays one value. A positional after those values is swallowed. A bare repeated flag whose element is required skips the route.
5. `[Parameter]` reads `Order` and `Name`. Two or more parameters that omit `Order` report `NURU_A002` (error) and the route is kept. Parameters sort by `Order`, then declaration index. A catch-all stays last. Duplicate explicit orders keep declaration order as the tie-break. `Name` is trimmed and lowercased when it is an identifier (letter or underscore, then letters, digits, and underscores). A name such as `source-file` reports `NURU_A005` and the route uses the property name lowercased, so the matcher and the constructor both compile. In-repo multi-parameter endpoints set `Order` to their declaration index so current binding order is unchanged. A single parameter may omit `Order`. The user and generator guides set `Order` on every parameter in a multi-parameter example. The migration guide's `GetCommand` and `RunCommand`, the getting-started add command, the readme's second add command, and the endpoint guide's required/optional pair set `Order` too. A catch-all counts toward `NURU_A002`.
6. `[Option(IsRepeated = true)]` is read. On a collection it is already repeated. On a scalar it reports `NURU_A004` (error) and the option stays a single value. `IsRepeated = false` does not turn a collection into one value. The dead `LongName` / `ShortName` / `IsRequired` branches on `OptionAttribute` stay an open question.

### How to validate

Smoke:

```bash
./bin/dev build
ganda repo audit
```

Expect:

- `./bin/dev build` succeeds with 0 warnings and 0 errors.
- No test file was added or edited, and no production type changed, so no `dotnet run --file` run. The edit is documentation: `GetCommand` and `RunCommand` set `Order = 0` and `Order = 1`, and so do the getting-started add command, the readme's second add command, and the endpoint guide's required/optional pair.
- `ganda repo audit`: 45 passed, 0 failed, 1 skipped (`self-hosted-runner`).

Gates (implement-fix-3):

- `./bin/dev build` — succeeded (0 warnings, 0 errors)
- no test file — documentation only; no production type changed
- `ganda repo audit` — passed (45 passed, 0 failed, 1 skipped)

Prior pass (implement-fix-2), not rerun:

- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-43-binding-strictness.cs` — 20 passed
- `dotnet run --file tests/timewarp-nuru-tests/generator/generator-76-binding-attribute-diagnostics.cs` — 5 passed

### Review

- Round 1 (effort 3, roster general): 3 bug, 1 suggestion, 1 nit. The fix pass implemented M1–M5. Statuses stay open for review round 2.
- Round 2 (effort 2, roster general, scope `14104e13..HEAD`): M1–M5 fixed. M6 (bug) implemented in implement-fix-3:
  `GetCommand` and `RunCommand` set `Order`, and the same omission in getting-started, the readme, and the endpoint
  optionality pair does too.
- Round 3 (effort 1, roster general, scope `6b29609c..HEAD`): M6 verified fixed. No new findings.
- Final counts: bug 4 fixed, suggestion 1 fixed, nit 1 fixed; 0 open, 0 wontfix.
- Disposition: **clean** (3 rounds). No wontfix, no escalation.
- Framework: `review/review-framework.md`. Last merged: `review/round-3/merged.md`. Disposition: `review/disposition.md`.

## Learnings

- Misread (review round 1): original item 2 asks that a default-true bool not ignore its initializer and that
  `--create false` bind. The restatement widened that to "a normal flag also consumes a following boolean
  token". That widening is outside the task and is the source of review M1 (a boolean-looking positional after
  any flag is swallowed). why: review round 1 M1, 2026-11-19

## Review

- Round 1: Blocked — open M1, M2, M3, M4, M5
- Fix pass implemented M1–M5.
- Round 2: Blocked — open M6
- Fix pass implemented M6.
- Round 3: Done
## Notes

Found by task 496 (System.CommandLine migration guide) against TimeWarp.Nuru 4.0.0-beta.14. Parent: 591.

## Restart 2026-11-19: what is left

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
