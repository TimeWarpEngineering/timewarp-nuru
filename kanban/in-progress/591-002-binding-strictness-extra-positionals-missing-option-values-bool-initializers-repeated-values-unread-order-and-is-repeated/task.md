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

Binding must not drop input or ignore an attribute the docs already promise. All five items are fixed, not kept.

1. A route with no catch-all rejects tokens past its last positional (required plus optional). A value option that is present but has no value (space form with nothing after it) does not keep a default and does not run the handler. Both fail as Unknown command, exit 1, the same way a route with no parameters already fails an extra token. `--opt=` counts as a value. The default applies only when the option is absent. No new missing-value sentence: option parsing runs before the literal check, so a dedicated message would fire on the wrong command.

2. A non-nullable bool option whose initializer is true is a value option, same rule task 508 already uses for derived operations, and it keeps the attribute's long and short forms. Leaving it off stays true. `--create false` and `--create true` bind that bool. A normal flag still means "present is true", and it also consumes a following boolean token (`true`/`false`/`yes`/`no`/`1`/`0`/`on`/`off`/`enabled`/`disabled`) that is not itself an option. `--flag=true` stays an unknown option.

3. A repeated option (collection type, or fluent `*`) takes every following non-option token after one space-separated flag, until the next declared option. `--env A B` binds both. `--env A --env B` still works. `--env=A` stays one value. A positional after those values is swallowed; that break is documented.

4. `[Parameter]` reads `Order` and `Name`. Two or more parameters on one command that omit `Order` report `NURU_A002`. Parameters sort by `Order`, then declaration order. A catch-all stays last. `Name` is the route segment name and the generated local, so the matcher and the command constructor still agree.

5. `[Option(IsRepeated = true)]` is read. On a collection it is already repeated. On a scalar it is an error (`NURU_A004`). `IsRepeated = false` on a collection does not turn the collection into a single value.

Acceptance: each item has a test that fails on the old behavior; in-repo multi-parameter endpoints set `Order` so `NURU_A002` compiles; the migration guide and the migrate skill describe the new behavior; build, the touched test files, and one repo audit pass.

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

## Results

All five items are fixed.

1. A route with no catch-all rejects tokens past its last positional (required plus at most the optional ones). Failure is a route skip: `Unknown command`, exit 1. A catch-all still takes the rest.
2. A present value option with no value token skips the route only when the value is required (`!ParameterIsOptional`). The initializer applies only when the option is absent. `--opt=` counts as a value. A nullable value (`string?`, fluent `{mode?}`) still binds null and exits 0. There is no dedicated missing-value sentence, because option parsing runs before the literal check.
3. A non-nullable `bool` whose initializer is `true` is a value option and keeps the attribute's long and short forms. Leaving it off stays true. `--create false` and `--create true` bind that bool. A bare `--create` with no token skips the route, because the value is required. A normal flag still means presence is true, and it consumes a following `BooleanConverter` spelling that is not itself an option. `--flag=true` stays an unknown option.
4. A repeated option (collection type, or fluent `*`) takes every following non-option token after one space-separated flag, until the next declared option or an option-like token. `--env A B` binds both. `--env A --env B` still works. `--env=A` stays one value. A positional after those values is swallowed. A bare repeated flag whose element is required skips the route.
5. `[Parameter]` reads `Order` and `Name`. Two or more parameters that omit `Order` report `NURU_A002` (error) and the route is kept. Parameters sort by `Order`, then declaration index. A catch-all stays last. Duplicate explicit orders keep declaration order as the tie-break. `Name` is trimmed, lowercased, and used as the route segment and the generated local. In-repo multi-parameter endpoints set `Order` to their declaration index so current binding order is unchanged. A single parameter may omit `Order`.
6. `[Option(IsRepeated = true)]` is read. On a collection it is already repeated. On a scalar it reports `NURU_A004` (error) and the option stays a single value. `IsRepeated = false` does not turn a collection into one value. The dead `LongName` / `ShortName` / `IsRequired` branches on `OptionAttribute` stay an open question.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
./bin/dev build
dotnet run --file tests/timewarp-nuru-tests/routing/routing-43-binding-strictness.cs
dotnet run --file tests/timewarp-nuru-tests/generator/generator-76-binding-attribute-diagnostics.cs
dotnet run --file tests/timewarp-nuru-tests/help/help-11-root-default-route-parameters.cs
ganda repo audit --fix
```

Expect:

- `./bin/dev build` succeeds.
- routing-43: 17 passed. Extra token after an optional parameter exits 1. A true bool initializer stays true when omitted, and `--create false` binds false. `--env A B` binds both. Parameters bind by `Order`. Help shows the `Name` override.
- generator-76: 4 passed. Two parameters that omit `Order` report `NURU_A002` twice. `IsRepeated` on a `string` reports `NURU_A004` once. One parameter, and `IsRepeated` on a collection, report neither.
- help-11: 6 passed.
- `ganda repo audit --fix`: 45 passed, 0 failed, 1 skipped (`self-hosted-runner`). The first audit failed on a new CA1819 suppression; the property is `IReadOnlyList<string>` instead. `--fix` also set the gitignored window title.

Gates:

- `ganda runfile cache --clear` — cleared 176 entries
- `./bin/dev build` — succeeded
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-43-binding-strictness.cs` — 17 passed
- `dotnet run --file tests/timewarp-nuru-tests/generator/generator-76-binding-attribute-diagnostics.cs` — 4 passed
- `dotnet run --file tests/timewarp-nuru-tests/help/help-11-root-default-route-parameters.cs` — 6 passed
- `ganda repo audit --fix` — passed (45 passed, 0 failed, 1 skipped)
- `ganda repo audit` — passed again after the new runfiles were marked executable (the first commit's attest failed `runfile-executable`)

## Notes

Found by task 496 (System.CommandLine migration guide) against TimeWarp.Nuru 4.0.0-beta.14. Parent: 591.
