# Option =value and :value tokens are swallowed as configuration overrides, also after --

## Description

The generated `IsConfigArg` filter (`source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs`,
`EmitPrepare`) removes every `--x=y`, `--x:y`, `/x=y`, `/x:y` token before route matching.

- A user typing `--format=json` for a declared `--format` option gets the default value and exit 0, with no error.
- The filter also runs after `--`, so `tool run build -- --verbosity=quiet /p:Configuration=Release` forwards nothing.

Repro: a `[NuruRoute("run")]` with an `IsCatchAll` parameter. `run build -- --foo=bar /p:X=Y /usr/bin` binds
`[/usr/bin]`.

Expected: tokens after `--` are never filtered. A declared option's `=value` form binds, or at least errors.

## Goal (implementer restatement)

Stop the generated configuration-override filter (`IsConfigArg`) from eating tokens it should not.

Acceptance criteria:

- Nothing after the first `--` is filtered. `run build -- --foo=bar /p:X=Y /usr/bin` binds all three
  tokens to the catch-all, and those tokens do not become configuration overrides either.
- A declared long option given as `--name=value` binds its value. `--name:value` binds the same way.
- A declared flag given as `--flag=value` is an error (unknown option, exit 1), not a silent default.
- Undeclared `--Section:Key=value` and `/Key=value` tokens before `--` stay configuration overrides.
- Tests cover each item, the skipped `--env=staging` enum test runs, and the System.CommandLine
  migration guide and skill drop the fixed behavior difference.

## Checklist

- [x] Decide per item: fix it, or keep the behavior and say why in Results
- [x] Fix with tests for each accepted item
- [x] Update "Behavior differences to check" in `documentation/user/guides/migrating-from-system-commandline.md`
      and `skills/tw-nuru-migrate-system-commandline/SKILL.md` for each fix
- [x] `ganda runfile cache --clear`, then the changed test files green (full CI suite runs on the PR)

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-17), split from 591 during groom
- 2026-11-19 implementer (claude-opus-5-5, ganda task work): fixed both items, tests, docs.

## Notes

Found by task 496 (System.CommandLine migration guide) against TimeWarp.Nuru 4.0.0-beta.14. Parent: 591.

## Results

Both items fixed.

- **Tokens after `--`.** The generated filter (`json-args-emitter.cs`, `EmitPrepare`) records the first `--`
  and passes every later token to routing unchanged. `ConfigurationEmitter` passes only the args before `--`
  to `AddCommandLine`, so forwarded `--x=y` tokens are not configuration either. `--json-args` is still
  peeled after `--`: routing-34 pins that a route with a literal `--` in its pattern takes `--json-args` after it.
- **Declared options.** The generator emits `__IsDeclaredLongOption`, which lists every long option of the
  app's routes and endpoints. Before `--`, a `--name=value` or `--name:value` token whose name is in that
  list stays a route token. `:` is rewritten to `=` so the existing matcher binds it. A declared flag given
  `--flag=true` now fails with `Unknown option '--flag=true'`, exit 1. Undeclared `--Section:Key=v` and
  `/Key=v` tokens are still configuration overrides. Matching is ordinal, like the matcher.
- `EmitPrepare` now takes the app's route list. `interceptor-emitter.method-body.cs` builds
  `allRoutesOrdered` before calling it.
- Tests: new `routing-42-option-equals-and-end-of-options.cs` (8 tests). The skipped
  `Should_bind_enum_option_with_equals_syntax` in routing-24 now runs and passes. Updated the stale comment in
  routing-41.
- Docs: the System.CommandLine migration guide and the `tw-nuru-migrate-system-commandline` skill no longer
  list the `--name=value` difference. The `--` passthrough table, the catch-all rules, the argv-rewrite
  workaround (now only for undeclared legacy aliases) and the checklist were updated to match.

### Gates

- `./bin/dev build`: pass (first run failed on IDE0028 in the new emitter code; fixed with `[with(StringComparer.Ordinal)]`, rerun pass)
- `ganda runfile cache --clear`: done before the test runs
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-42-option-equals-and-end-of-options.cs`: 8/8 pass
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-24-enum-option-parameters.cs`: 10/10 pass
- `tests/timewarp-nuru-tests/routing/routing-41-argv-fuzz-property.cs`: comment-only edit; not a standalone runfile (no `#if !JARIBU_MULTI` guard), CI runs it
- Extra regression runs on code paths this change touches: routing-12-colon-filtering 11/11, configuration-02-cli-overrides 7/7,
  routing-33-json-args 15/15, routing-34-json-bind-invoke 10/10 (failed 9/10 on a first version that stopped
  peeling `--json-args` after `--`; that part was reverted), routing-37-unknown-options 18/18, capabilities-02-integration 10/10
- `ganda repo audit`: pass with 1 advisory warning (`vscode-window-icon`: local `.vscode/settings.json` missing). `ganda repo audit --fix` wrote the git-ignored local file; the rerun passes all checks (45 passed, 0 failed). The commit-hook attest then failed `runfile-executable` because the new routing-42 runfile had no executable bit; fixed with `chmod +x` in a follow-up commit

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run --file tests/timewarp-nuru-tests/routing/routing-42-option-equals-and-end-of-options.cs
dotnet run --file tests/timewarp-nuru-tests/routing/routing-24-enum-option-parameters.cs
```

Expect: both files report all tests passed (8 and 10). In routing-42, `report --format=json` and
`report --format:json` print `format:json`; `build --verbose=true` exits 1 with
`Unknown option '--verbose=true'`; `run build -- --foo=bar /p:X=Y --Section:Key=v /usr/bin` prints
`run:build|--foo=bar,/p:X=Y,--Section:Key=v,/usr/bin`; `pass --Before=yes -- --After=yes` sets
configuration `Before` and leaves `After` unset.
