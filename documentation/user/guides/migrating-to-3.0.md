# Migrating to TimeWarp.Nuru 3.0

Guide for applications moving from the 2.x line, or from a 3.0 beta, to the
stable 3.0 release.

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
come from `TimeWarp.Mediator` (flowed in by the Nuru package). Published 3.0
betas through 3.0.0-beta.80 reference `TimeWarp.Mediator` 14.0.0-beta.4. Later
Nuru builds, including stable 3.0.0, reference stable `TimeWarp.Mediator` 14.0.0,
so you don't need a prerelease opt-in for Mediator. If you also pin
`TimeWarp.Mediator.*` yourself (for example in `Directory.Packages.props`),
use 14.0.0 with those Nuru versions.

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

## TimeWarp.Amuru is stable 2.0.0

`TimeWarp.Nuru` depends on `TimeWarp.Amuru` for process execution (REPL clipboard
and the generated `--capabilities` search call). This is a new dependency for 2.x
users: Nuru 2.0.0 had no Amuru reference, so upgrading adds the `TimeWarp.Amuru`
package to your app. Stable 3.0.0 depends on stable `TimeWarp.Amuru` 2.0.0, so
you don't need a prerelease opt-in for Amuru (3.0.0-beta.80 was the one beta
that depended on the 2.0.0-beta.2 prerelease). If you also pin `TimeWarp.Amuru`
yourself (for example in `Directory.Packages.props`), use 2.0.0 with Nuru 3.0.0.
`TimeWarp.Amuru.Tools` 2.0.0 is the matching pin for this repo's dev CLI; it
does not flow from the Nuru package.

## `--capabilities` JSON is a flat `endpoints` list

2.x had no `--capabilities` route. Betas 3.0.0-beta.42 through beta.55 emitted a
hierarchical document: grouped commands sat inside a nested `groups` array and
ungrouped commands in a top-level `commands` array. From beta.56 on, and in
3.0, those keys are gone. An agent that walks `groups` or `commands` finds
nothing.

3.0 emits one flat `endpoints` array. Each entry carries its group as a
`groupPath` array of prefix segments. An ungrouped route has `groupPath: []`.
A route under `WithGroupPrefix("admin")` then `WithGroupPrefix("config")` has
`groupPath: ["admin", "config"]`, and its `pattern` is the full pattern
(`admin config get {key}`). The top-level `invocation` object names the
`--json-args` transport. See [Agent invocation](../features/agent-invocation.md).

Each endpoint has:

| Key | Meaning |
|-----|---------|
| `pattern` | Full route pattern, group prefix included |
| `groupPath` | Group prefix segments, outermost first; `[]` when ungrouped |
| `aliases` | Alternate patterns |
| `description` | Route description; omitted when not set |
| `kind` | `query`, `command`, `idempotentCommand`, or `unspecified` |
| `parameters[]` | Positional parameters: `name`, `type`, `required`, `isCatchAll` |
| `options[]` | Options: `name`, `type`, `required`, `isFlag`, `isRepeated` |

Output of `app --capabilities` for this app:

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .Map("greet {name}").WithHandler((string name) => $"Hello, {name}!").AsQuery().Done()
  .Map("deploy {env} --tag? {tag?}")
    .WithHandler((string env, string? tag) => $"Deploying {tag ?? "latest"} to {env}")
    .AsCommand().Done()
  .Build();
```

```json
{
  "name": "app",
  "version": "1.0.0",
  "endpoints": [
    {
      "pattern": "greet {name}",
      "groupPath": [],
      "aliases": [],
      "kind": "query",
      "parameters": [
        { "name": "name", "type": "string", "required": true, "isCatchAll": false }
      ],
      "options": []
    },
    {
      "pattern": "deploy {env} --tag? {tag?}",
      "groupPath": [],
      "aliases": [],
      "kind": "command",
      "parameters": [
        { "name": "env", "type": "string", "required": true, "isCatchAll": false }
      ],
      "options": [
        { "name": "tag", "type": "string", "required": false, "isFlag": false, "isRepeated": false }
      ]
    }
  ],
  "invocation": {
    "jsonArgs": "--json-args",
    "stdin": "-",
    "filePrefix": "@",
    "merge": "argvOverridesJson",
    "unknownKeys": "error"
  }
}
```

To select one group, filter `endpoints` on a `groupPath` prefix, or run
`app --capabilities --group-filter admin` (short form `-g`; nested groups use
dots, `admin.config`). The filtered document adds `"filter": { "group": "admin" }`.

## Members removed during the 3.0 betas

These APIs are not on `NuruAppBuilder` in 3.0. Replace them before you
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

## TimeWarp.Nuru.Mcp is not part of 3.0

The `TimeWarp.Nuru.Mcp` tool package does not ship with 3.0. The MCP server is
frozen, and its syntax and handler-generation answers do not match the 3.0
surface. Do not install a `3.0.0` `TimeWarp.Nuru.Mcp`; none is published.
Use the Nuru skill (`skills/tw-nuru/SKILL.md`) and the `samples/` files
instead. The source stays in `source/timewarp-nuru-mcp` and still builds; pass
`-p:NuruMcpPack=true` to `dotnet pack` for a local package.

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
