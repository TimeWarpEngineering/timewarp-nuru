# TimeWarp.Nuru 3.0: CLIs your agents can call

Your AI agent already lives in a terminal. It runs `git`, `dotnet`, `gh`, and whatever scripts you
leave lying around. What it can't do well is guess how *your* CLI works: which arguments are
positional, which flags take values, and how to quote a JSON blob through three layers of shell.

TimeWarp.Nuru 3.0 fixes that from the framework side. Every Nuru app now describes itself to an
agent and accepts its arguments as JSON. You don't need an MCP server or a second schema, and there's
nothing extra to install: you reference one package and write your routes.

## A CLI in one file

On .NET 10 a single `.cs` file is a program, and with Nuru it is a full CLI:

```csharp
#!/usr/bin/env -S dotnet --
#:package TimeWarp.Nuru@3.0.0
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .Map("greet {name}").WithHandler((string name) => $"Hello, {name}!").AsQuery().Done()
  .Map("deploy {env} --tag? {tag?}")
    .WithHandler((string env, string? tag) => $"Deploying {tag ?? "latest"} to {env}")
    .AsCommand().Done()
  .EnableCompletion()
  .AddRepl()
  .Build();

return await app.RunAsync(args);
```

```bash
$ dotnet run app.cs -- greet World
Hello, World!
$ dotnet run app.cs -- deploy staging
Deploying latest to staging
```

That file has typed parameters, an optional option, generated `--help`, shell completion for bash,
zsh, fish, and PowerShell, and a REPL behind `-i`. A source generator does the routing work at
compile time and emits the matcher, so nothing parses route patterns or reflects over handlers at
runtime.

## The agent contract

Ask the app what it can do:

```bash
$ ./app --capabilities
```

```json
{
  "name": "app",
  "endpoints": [
    {
      "pattern": "deploy {env} --tag? {tag?}",
      "kind": "command",
      "parameters": [{ "name": "env", "type": "string", "required": true, "isCatchAll": false }],
      "options": [{ "name": "tag", "type": "string", "required": false, "isFlag": false, "isRepeated": false }]
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

(Trimmed. The real output lists every route, with `kind` set to `query`, `command`, or
`idempotentCommand`, and includes descriptions, defaults, and allowed values when you declare them.)

`endpoints` tells the agent what the commands are, and `invocation` tells it how to call them.
`kind` tells it which commands are safe to retry. The route literal stays on argv, and the values
go in one JSON object keyed by the names the agent just read:

```bash
$ ./app deploy --json-args '{"env":"staging","tag":"3.0.0"}'
Deploying 3.0.0 to staging
```

`--json-args` takes inline JSON, `@path` for a file, or `-` for stdin, which is the form you want
when the payload is a multi-line patch. It binds to the same handler on the same code path a human
uses. Argv values win over JSON for the same name. An unknown key is an error with a non-zero exit
code, not something the app silently ignores, so an agent that guesses wrong finds out
immediately:

```bash
$ ./app deploy --json-args '{"env":"x","bogus":1}'
Error: Unknown key 'bogus' for 'deploy {env} --tag? {tag?}'. Known names: env, tag.
```

### Finding the right CLI in the first place

Once you have a dozen tools, the problem moves up a level: *which* CLI has a deploy command? The
`TimeWarp.Nuru.Search` tool (`dotnet tool install -g TimeWarp.Nuru.Search`) installs a `nuru`
command that indexes the capabilities of the Nuru CLIs you register and searches them with SQLite
full-text search:

```bash
nuru index rebuild --cli ~/bin/app
nuru search deploy
```

### Teaching the agent the framework

To have an agent *build* a Nuru app rather than call one, the repo ships a `tw-nuru` skill
(`skills/tw-nuru/SKILL.md`). Load it into Claude Code, Grok, or any agent that reads skills, and it
has the route syntax, builder API, and testing patterns in context. In 3.0 the skill replaces the
MCP server. The skill is more reliable, needs no network round trip, and has nothing to keep
running.

## Built for humans too

- **REPL** with `Default`, `Emacs`, `Vi`, and `VSCode` key-binding profiles, plus a `key-bindings`
  command that lists what each key does.
- **Shell completion** generated from your routes: `./app --install-completion zsh`.
- **Native AOT**: `dotnet publish app.cs -c Release -r linux-x64` turns the single-file app above,
  REPL and completion included, into a 5.9 MB native binary that runs a command in about 3 ms. That
  matters more than it sounds, because dynamic tab completion runs your binary on every Tab press.
- **Endpoints as classes** when an app outgrows lambdas: `[NuruRoute]` classes with
  `TimeWarp.Mediator` handlers and pipeline behaviors, discovered at compile time.

## What changed from 2.x

3.0 is a breaking release. The headline changes:

- Command and query contracts (`ICommand<T>`, `IQuery<T>`, their handlers, and `Unit`) moved to
  `TimeWarp.Mediator`. Add `using TimeWarp.Mediator;`, and note that handlers now return `Task<T>`
  rather than `ValueTask<T>`.
- `[NuruRoute]` endpoint classes must be `public`.
- `MapDefault` is gone (use `Map("")`), and so is `AddReplSupport()` (use `AddRepl()`).
- `TimeWarp.Nuru.Mcp` no longer ships. Use the skill instead.
- The package targets `net10.0` only.

The [migration guide](https://github.com/TimeWarpEngineering/timewarp-nuru/blob/master/documentation/user/guides/migrating-to-3.0.md)
walks through each change with before and after code.

## How it was reviewed

Before tagging 3.0 I had a team of agents, Grok and Claude, review the whole codebase against the
release. They filed 28 findings. The 21 that had to be fixed before release went through 15 PRs, and
the rest are scheduled after 3.0 or declined, with reasons. Some of what they caught: a `Build()`
call that silently dropped the route it closed, shell completion that never handed file and
directory candidates to the shell, and a typed catch-all parser that produced code that didn't
compile for `uint` or `TimeSpan`. The full record is in the repo under
`kanban/done/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/`.

## Try it

```bash
dotnet new console -n mycli && cd mycli && dotnet add package TimeWarp.Nuru
```

or copy the single-file app above and `dotnet run app.cs`.

- Repo: [github.com/TimeWarpEngineering/timewarp-nuru](https://github.com/TimeWarpEngineering/timewarp-nuru)
- Package: [nuget.org/packages/TimeWarp.Nuru](https://www.nuget.org/packages/TimeWarp.Nuru)

If Nuru is useful to you, a star on the repo helps more people find it.
