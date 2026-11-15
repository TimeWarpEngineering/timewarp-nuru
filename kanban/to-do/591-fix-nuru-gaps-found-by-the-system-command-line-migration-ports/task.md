# Fix Nuru gaps found by the System.CommandLine migration ports (task 496)

## Description

Task 496 wrote `documentation/user/guides/migrating-from-system-commandline.md` and the
`tw-nuru-migrate-system-commandline` skill, and validated them by porting five System.CommandLine tools
(SlnUp, litmus, dotnet-run-script, dotnet-httpie, grate) privately. The ports and probes against
TimeWarp.Nuru 4.0.0-beta.14 found these product defects. The guide documents each one as a known
difference with a workaround; when one is fixed, update the "Behavior differences to check" section of the
guide and the skill.

Split into child tasks during grooming if they should ship separately.

## Defects

1. **`--name=value` / `--name:value` are swallowed as configuration overrides.** The generated
   `IsConfigArg` filter (`source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs`,
   `EmitPrepare`) removes every `--x=y`, `--x:y`, `/x=y`, `/x:y` token before route matching. A user typing
   `--format=json` for a declared `--format` option gets the default value and exit 0, no error. The filter
   also runs **after `--`**, so `tool run build -- --verbosity=quiet /p:Configuration=Release` forwards
   nothing. Repro: a `[NuruRoute("run")]` with an `IsCatchAll` parameter; `run build -- --foo=bar /p:X=Y /usr/bin`
   binds `[/usr/bin]`. Expected: tokens after `--` are never filtered; a declared option's `=value` form
   binds (or at least errors).
2. **`IOptions<T>` in an endpoint `Handler` constructor is not registered.** It throws
   `No service for type 'Microsoft.Extensions.Options.IOptions`1[T]'` at run time. The repo's own sample
   fails: `dotnet run samples/endpoints/07-configuration/advanced/advanced.cs -- endpoints`. Delegate handler
   parameters (Fluent DSL) work. `services.Configure<T>(...)` reports NURU052 and does nothing.
3. **Root `--help` hides the default route's parameters and options.** A single-command tool
   (`[NuruRoute("")]` with `[Parameter]`/`[Option]`) shows only an empty-name row under `Commands:`; there is
   no way to see its options from `--help`.
4. **Extra positional arguments are silently ignored** on a route with a `[Parameter]` (`a b` binds `a`, exit
   0), but are `Unknown command` on a route with none. An option with a missing value (`-o` at the end) binds
   the default with exit 0.
5. **Ctrl+C does not cancel the handler's `CancellationToken`** in a CLI run (`RunAsync` passes `default`).
   System.CommandLine cancels the token and waits `ProcessTerminationTimeout`.
6. **A `bool` option's initializer is ignored.** `[Option("create")] public bool Create { get; set; } = true;`
   binds `false` when the flag is absent, so a default-true flag is impossible. `--create false` binds `true`
   and passes `false` on as a positional argument.
7. **Several values per token leak into positionals.** With `IsRepeated`, `--env A B` binds `A` and gives `B`
   to an optional `[Parameter]` (or fails with `Unknown command`).
8. **Generated code breaks on some member and type names.** An endpoint property named `Type` emits
   `@type_0` and fails with CS0818/CS0103. Two enum option types with the same simple name in different
   namespaces give CS0104 in generated code.
9. **Some parse errors go to stdout** (for example `Error: Invalid value 'bogus' for option '--databasetype'`).
   Errors belong on stderr.
10. **Unhandled handler exceptions abort the process** (SIGABRT, exit 134, raw stack trace). Consider a default
   handler that prints the message and returns 1.
11. **`[Parameter(Order = n)]` is not read, and `NURU_A002` is never reported.** `ExtractParameterFromAttribute`
   reads `Description` and `IsCatchAll` only; position is property declaration order. The
   `MultipleParametersRequireOrder` descriptor exists but nothing reports it, and the `ParameterAttribute`
   XML docs say `Order` is required. Honor `Order` (and `Name`), or remove them and the descriptor. See the
   open question in `source/timewarp-nuru/attributes/parameter-attribute.cs`. (Review M1 on task 496.)
12. **`[Option(IsRepeated = true)]` is not read.** Repeatability comes from the property type
   (`IsRepeatedOptionType`); `IsRepeated = true` on a scalar does nothing, silently. Read it (or warn), or
   remove it. See the open question in `source/timewarp-nuru/attributes/option-attribute.cs`. (Review M4.)
13. **Endpoint `FileInfo`/`DirectoryInfo`/`IPAddress` properties do not bind.** `GetTypeConstraintFromClrType`
   maps `Version` but not these, although fluent `{x:FileInfo}` constraints convert them through
   `TypeConversionMap`. (Review M5.)

## Checklist

- [ ] Decide which items are bugs vs documented design; split children
- [ ] Fix and test each accepted item
- [ ] Update `documentation/user/guides/migrating-from-system-commandline.md` and
      `skills/tw-nuru-migrate-system-commandline/SKILL.md` for each fix

## Notes

- Found by task 496 (System.CommandLine migration guide), 2026-11-15.

## Session

- Created: implementer-claude from task 496 (ganda task work, headless), 2026-11-15
