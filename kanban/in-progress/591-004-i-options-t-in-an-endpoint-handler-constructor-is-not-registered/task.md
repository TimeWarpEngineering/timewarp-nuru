# IOptions<T> in an endpoint Handler constructor is not registered

## Goal (implementer restatement)

An endpoint `Handler` constructor that takes `IOptions<T>` should receive the same configuration binding a Fluent delegate parameter already gets: the section named by `[ConfigurationKey]`, or the type name with a trailing `Options` removed, including command-line overrides, and `IValidateOptions<T>` when one exists. The repo sample `samples/endpoints/07-configuration/advanced/advanced.cs` must run `endpoints` without `No service for type 'IOptions<T>'`.

`services.Configure<T>(...)` under source-generated DI stays a `NURU052` warning and does not run. That call is the Microsoft options pipeline (named options, post-configure, change tokens). Source-generated DI does not execute it. `.UseMicrosoftDependencyInjection()` still runs it, and `NURU052` stays silent there. `IOptionsSnapshot<T>` and `IOptionsMonitor<T>` stay unbound. A constructor that asks for them gets `NURU050`, the same rule delegate parameters already follow.

Done when constructor injection is covered by tests, the migration guide and `tw-nuru-migrate-system-commandline` describe the new constructor behavior and the remaining `Configure` limit, and the build, those tests, and one `ganda repo audit` pass.

## Description

`IOptions<T>` in an endpoint `Handler` constructor throws
`No service for type 'Microsoft.Extensions.Options.IOptions`1[T]'` at run time.

- The repo's own sample fails: `dotnet run samples/endpoints/07-configuration/advanced/advanced.cs -- endpoints`.
- Delegate handler parameters (Fluent DSL) work.
- `services.Configure<T>(...)` reports NURU052 and does nothing.

## Checklist

- [x] Decide per item: fix it, or keep the behavior and say why in Results
- [x] Fix with tests for each accepted item
- [x] Update "Behavior differences to check" in `documentation/user/guides/migrating-from-system-commandline.md`
      and `skills/tw-nuru-migrate-system-commandline/SKILL.md` for each fix
- [x] `ganda runfile cache --clear`, then the changed test files green (full CI suite runs on the PR)

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-17), split from 591 during groom
- 2026-11-19 implementer (grok, ganda task work, session 1730565): endpoint Handler constructors bind IOptions<T> the same way delegate parameters do. Configure stays NURU052.

## Notes

Found by task 496 (System.CommandLine migration guide) against TimeWarp.Nuru 4.0.0-beta.14. Parent: 591.

## Results

Fixed the constructor injection. An endpoint `Handler` constructor that takes `IOptions<T>` now gets the same binding a Fluent delegate parameter already got: the section from `[ConfigurationKey]`, or the type name with a trailing `Options` removed, including command-line overrides and an `IValidateOptions<T>` found in the compilation. The source-gen mediator container registers that `IOptions<T>` before `AddGeneratedMediator`, and the handler factory still calls `GetRequiredService`. `UseDirectInvoke()` binds the same section from the route's configuration local. `samples/endpoints/07-configuration/advanced/advanced.cs -- endpoints` prints `Configured Endpoints:` and exits 0.

Kept: `services.Configure<T>(...)` under source-generated DI still reports NURU052 and does not run. That call is the Microsoft options pipeline (named options, post-configure, data annotations, change tokens). Source-generated DI does not execute it. `.UseMicrosoftDependencyInjection()` still runs it, and NURU052 stays silent there. The handler factory on that path still uses `GetRequiredService`, so a closed convention registration is not inserted ahead of `Configure`. `IOptionsSnapshot<T>` and `IOptionsMonitor<T>` stay unbound. A constructor that asks for them gets NURU050, the same rule delegate parameters already follow.

The validation sample's `[Required]` / data annotations still do not run by themselves. Auto-validation is `IValidateOptions<T>` only. `AddOptions().ValidateDataAnnotations().ValidateOnStart()` stays a runtime-DI feature.

Out of scope: a behavior constructor, and a registered service constructor, that take `IOptions<T>` still do not get this binding.

`documentation/user/guides/migrating-from-system-commandline.md` item 6, `skills/tw-nuru-migrate-system-commandline/SKILL.md` step 6, `documentation/user/features/configuration.md`, and the changelog describe the new constructor behavior and the remaining `Configure` limit.

`ganda repo audit --fix` wrote the gitignored local `.vscode/settings.json` window title so `vscode-window-icon` passes. Nothing from that fix is committed.

### Gates

- `./bin/dev build`: pass (0 warnings, 0 errors). The first run failed IDE0028 on `new(StringComparer.Ordinal)`; replaced with `[with(StringComparer.Ordinal)]`; rerun pass.
- `ganda runfile cache --clear`: cleared 464 entries, before the test runs.
- `dotnet run --file tests/timewarp-nuru-tests/generator/generator-77-endpoint-handler-ioptions.cs`: 5/5 pass. Standalone runfile (`#if !JARIBU_MULTI`).
- `dotnet run --file tests/timewarp-nuru-tests/generator/generator-69-endpoint-constructor-logger-warning.cs`: 7/7 pass. Standalone runfile, and `tests/ci-tests/Directory.Build.props` already excludes it from the CI multi assembly (Roslyn-hosted, CS0433).
- Smoke: `dotnet run --file samples/endpoints/07-configuration/advanced/advanced.cs -- endpoints` printed `Configured Endpoints:` and exited 0.
- `ganda repo audit`: first run exit 0, repository passes, 1 advisory (`vscode-window-icon`). `ganda repo audit --fix --checks vscode-window-icon` fixed the gitignored file and reran: 45 passed, 0 failed, 1 skipped, "Repository passes all audit checks."

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run --file tests/timewarp-nuru-tests/generator/generator-77-endpoint-handler-ioptions.cs
dotnet run --file samples/endpoints/07-configuration/advanced/advanced.cs -- endpoints
```

Expect: generator-77 reports 5 passed. `e591-show-db --E591Database:Host=myhost --E591Database:Port=3306` prints `Host: myhost` and `Port: 3306`. `e591-show-db` with no overrides prints `Host: localhost` and `Port: 5432`. `e591-show-api --E591Api:Endpoint=https://test.com` prints that endpoint. `e591-validated --E591Checked:Port=0` throws `OptionsValidationException` whose message contains `Port must be between`. `e591-direct-db --E591Database:Host=direct-host` under `UseDirectInvoke()` prints `Host: direct-host`. The sample command prints `Configured Endpoints:` and exits 0.
