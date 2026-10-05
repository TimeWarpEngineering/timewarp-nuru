# Migrating to TimeWarp.Nuru 3.0

Guide for applications moving from the 2.x line, or from a 3.0 beta, to the
stable 3.0 release. The package under review is `3.0.0-beta.78`.

`TimeWarp.Nuru` targets `net10.0`. Install the prerelease until `3.0.0` is
published:

```bash
dotnet add package TimeWarp.Nuru --prerelease
```

```csharp
#:package TimeWarp.Nuru
```

## Mediator contracts live in TimeWarp.Mediator

TimeWarp.Nuru no longer defines `IMessage`, `IQuery<T>`, `ICommand<T>`,
`IIdempotentCommand<T>`, `IIdempotent`, `IQueryHandler<,>`,
`ICommandHandler<,>`, `IIdempotentCommandHandler<,>`, or `Unit`. Those types
come from `TimeWarp.Mediator` 14.0.0-beta (flowed in by the Nuru package).

1. Add `using TimeWarp.Mediator;` next to `using TimeWarp.Nuru;`, or a global
   `<Using Include="TimeWarp.Mediator" />`.
2. Handler methods return `Task<T>`, not `ValueTask<T>`.
3. In a non-async `Task<Unit>` handler, return `Unit.Task`. Replace
   `new ValueTask<T>(value)` with `Task.FromResult(value)`.
4. Do not write `using static TimeWarp.Mediator.Unit;`. `Unit` has a static
   `Task` property, so a static using hides `System.Threading.Tasks.Task`.
   Use `Unit.Value` and `Unit.Task`.
5. `[NuruRoute]` endpoint classes must be `public` for now. The Mediator
   generator emits public `Send` overloads, and an `internal` request fails
   with CS0051
   ([timewarp-mediator#64](https://github.com/TimeWarpEngineering/timewarp-mediator/issues/64)).

Endpoint kind classification uses explicit precedence: Query, then
IdempotentCommand (`IIdempotentCommand<T>`, or `ICommand<T>` plus
`IIdempotent`), then Command.

## `--capabilities` JSON is hierarchical

Grouped commands appear only inside the `groups` array. They are not repeated
in the top-level `commands` array. Ungrouped commands stay in `commands`.
Agents that walked a flat command list need to walk `groups` as well.

## Members removed during the 3.0 betas

These APIs are not on `NuruAppBuilder` in beta.78. Replace them before you
compile against 3.0:

| Removed | Use instead |
|---------|-------------|
| `MapDefault(...)` | `Map("")` |
| `AddReplSupport()` | `AddRepl()` or `AddRepl(Action<ReplOptions>)` |
| `AddRoute(...)` as shown in older samples | `Map(...).WithHandler(...).Done()` |

## Obsolete members that stay for 3.0

These compile with an obsolete warning and remain in 3.0 so existing calls
keep a migration message. They are scheduled to leave after 3.0, not in the
3.0.0 cut.

- `NuruAppBuilder.Services` always throws `InvalidOperationException`.
  Register services with `ConfigureServices(Action<IServiceCollection>)`.
  For a runtime Microsoft.Extensions.DependencyInjection container, call
  `UseMicrosoftDependencyInjection()` before `ConfigureServices`.
- `AddReplOptions(...)` still works. New code calls `AddRepl()`.

## What did not move

Direct delegate routes (`Map(...).WithHandler(...).Done()`), route-pattern
syntax (literals, `{param}`, `{param?}`, `{param:int}`, `{*args}`, `--option`,
`-o`, `--option,-o`, repeated options), `[NuruRoute]` / `[NuruRouteGroup]` /
`[GroupOption]`, and `AddRepl` are the 3.0 surface. See
`skills/tw-nuru/SKILL.md` for current examples.

Changelog entries before `3.0.0-beta.19` are listed in `changelog.md`.
Changes after beta.19 are under `## [Unreleased]` there. Per-beta notes for
beta.20 through beta.77 were not kept; do not treat `## [Unreleased]` as a
complete beta-by-beta history.
