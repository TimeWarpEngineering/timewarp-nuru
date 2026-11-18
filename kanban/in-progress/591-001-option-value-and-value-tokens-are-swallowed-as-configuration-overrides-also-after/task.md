# Option =value and :value tokens are swallowed as configuration overrides, also after --

## Description

The generated `IsConfigArg` filter (`source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs`,
`EmitPrepare`) removes every `--x=y`, `--x:y`, `/x=y`, `/x:y` token before route matching.

- A user typing `--format=json` for a declared `--format` option gets the default value and exit 0, with no error.
- The filter also runs after `--`, so `tool run build -- --verbosity=quiet /p:Configuration=Release` forwards nothing.

Repro: a `[NuruRoute("run")]` with an `IsCatchAll` parameter. `run build -- --foo=bar /p:X=Y /usr/bin` binds
`[/usr/bin]`.

Expected: tokens after `--` are never filtered. A declared option's `=value` form binds, or at least errors.

## Checklist

- [ ] Decide per item: fix it, or keep the behavior and say why in Results
- [ ] Fix with tests for each accepted item
- [ ] Update "Behavior differences to check" in `documentation/user/guides/migrating-from-system-commandline.md`
      and `skills/tw-nuru-migrate-system-commandline/SKILL.md` for each fix
- [ ] `ganda runfile cache --clear`, then CI tests green

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-17), split from 591 during groom

## Notes

Found by task 496 (System.CommandLine migration guide) against TimeWarp.Nuru 4.0.0-beta.14. Parent: 591.
