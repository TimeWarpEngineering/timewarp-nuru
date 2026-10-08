# GitHub Release notes: v3.0.0 (draft)

Paste everything below the line over the `--generate-notes` output on release day. Keep the
generated "Full Changelog" compare link at the bottom. Before pasting, re-check the
"Launch gates" in `task.md`. Every bullet here was verified against `3.0.0-beta.79` on 2026-10-09.

---

## TimeWarp.Nuru 3.0: CLIs your agents can call

Every Nuru app now describes itself to an agent and accepts its arguments as JSON. You don't need an
MCP server or a second schema. It's one package.

```bash
myapp --capabilities                                        # endpoints[] + invocation{}
myapp deploy --json-args '{"env":"staging","tag":"3.0.0"}'  # route literal on argv, values as JSON
myapp apply_patch --json-args @./patch.json                 # or - for stdin
```

📝 Blog: <BLOG URL> · 📖 [Migrating to 3.0](https://github.com/TimeWarpEngineering/timewarp-nuru/blob/master/documentation/user/guides/migrating-to-3.0.md)

### Agent contract

- **`--capabilities`** emits a flat `endpoints` list. Each endpoint carries `pattern`, `groupPath`,
  `kind` (`query` / `command` / `idempotentCommand`), `parameters`, `options`, and `examples`. A
  top-level **`invocation`** object names the transport: `jsonArgs`, `stdin: "-"`,
  `filePrefix: "@"`, `merge: "argvOverridesJson"`, and `unknownKeys: "error"`.
- **`--json-args`** binds parameter and option values from inline JSON, `@path`, or `-` (stdin) on
  the same code path as argv. Argv values override JSON. An unknown key is an error with exit code 1.
- **`--capabilities --search <q>`** and **`--group-filter <g>`** narrow the document.
- **`TimeWarp.Nuru.Search`** (`dotnet tool install -g TimeWarp.Nuru.Search`) adds `nuru index` and
  `nuru search`, which run full-text search over the capabilities of every CLI you index.
- **`tw-nuru` skill** (`skills/tw-nuru/SKILL.md`) gives an agent the route syntax, the builder API,
  and the testing patterns in context. It replaces the MCP server.

### Also new in 3.0

- REPL key-binding profiles: `Default`, `Emacs`, `Vi`, and `VSCode`. A global JSON profile can be
  set with `NURU_KEYBINDINGS`, and the `key-bindings` command lists the bindings.
- Shell completion for bash, zsh, fish, and PowerShell: `--generate-completion` and
  `--install-completion`. Completion can hand `File` and `Directory` candidates to the shell.
- Route examples appear in `--help` and in `--capabilities`.
- Endpoint classes use `TimeWarp.Mediator` contracts. The generated `IMediator`, `ISender`, and
  `IPublisher` are available with or without Microsoft DI.
- Pre-release review: Grok and Claude agents filed 28 findings. 21 were fixed before release across
  15 PRs (#283–#297, stacked in #298). The record is in
  `kanban/done/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/`.

### ⚠️ Breaking changes

- **Contracts moved to `TimeWarp.Mediator`**: `IMessage`, `IQuery<T>`, `ICommand<T>`,
  `IIdempotentCommand<T>`, `IIdempotent`, the three handler interfaces, and `Unit`. To migrate:
  - Add `using TimeWarp.Mediator;`.
  - Handlers return `Task<T>` instead of `ValueTask<T>`. Use `Unit.Task` and `Task.FromResult`.
  - Remove `using static ...Unit;`.
- **`[NuruRoute]` endpoint classes must be `public`**
  ([timewarp-mediator#64](https://github.com/TimeWarpEngineering/timewarp-mediator/issues/64)).
- **Endpoint kind precedence**: Query, then IdempotentCommand, then Command.
- **`--capabilities` output is a flat `endpoints` list** with `groupPath`. It used to be grouped.
- **Removed**:
  - `MapDefault`. Use `Map("")`.
  - `AddReplSupport()`. Use `AddRepl()`.
  - The `TimeWarp.Nuru.Mcp` package, which is no longer published. Use the skill.
- **Obsolete, still present in 3.0**: `NuruAppBuilder.Services` (it throws) and `AddReplOptions`.
- **Targets `net10.0` only.** The minimum SDK is 10.0.3xx.

See [Migrating to 3.0](https://github.com/TimeWarpEngineering/timewarp-nuru/blob/master/documentation/user/guides/migrating-to-3.0.md)
for before and after code, and [changelog.md](https://github.com/TimeWarpEngineering/timewarp-nuru/blob/master/changelog.md)
for every beta.
