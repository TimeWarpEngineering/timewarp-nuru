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
