# Spike Aspire 13.5 WithTerminal for the Nuru REPL in the aspire-otel sample

## Description

`samples/aspire-otel/apphost.cs` pins `#:sdk Aspire.AppHost.Sdk@13.1.0` and runs the client as
`AddCSharpApp("nuruclient", "./nuru-client.cs").WithArgs("status")` with the comment
"REPL requires interactive console which Aspire doesn't provide". Aspire 13.5 added the
experimental `WithTerminal()` (`ASPIRETERMINAL001`): the resource runs under a real
pseudo-terminal (PTY) with attached stdin, a live Terminal view in the dashboard, and
`aspire terminal attach` behind `features.terminalCommandsEnabled`. That is the interactive
console the comment says Aspire lacked. This spike proves or disproves that the Nuru REPL runs
under it.

Origin: timewarp-architecture task 209 (Aspire 13.5 update / WithTerminal vs Nuru verdict).
Docs: https://aspire.dev/app-host/with-terminal/ and
https://aspire.dev/whats-new/aspire-13-5/.

## Requirements

- Bump the sample to the **13.5 train as a whole** (`#:sdk Aspire.AppHost.Sdk@13.5.x` plus every
  `Aspire.Hosting.*` package the sample restores). Do not mix 13.4 and 13.5 packages
  (`MissingMethodException` / `TypeLoadException` known issue).
- Add `.WithTerminal()` to the `nuruclient` resource and pass `--interactive` (or enable
  `ReplOptions.AutoStartWhenEmpty`) instead of `WithArgs("status")`. Suppress
  `ASPIRETERMINAL001` (and the existing `ASPIRECSHARPAPPS001`) with a pragma or `NoWarn`.
- Prove in the dashboard Terminal view and via `aspire terminal attach` (after
  `aspire config set features.terminalCommandsEnabled true`) that the REPL prompt appears,
  accepts keystrokes, and that arrow-key history, tab completion, and ANSI colouring survive
  the PTY.
- Record the outcome in Notes. If it works, replace the stale "Aspire doesn't provide" comment
  and keep the sample on `WithTerminal()`. If it does not, record the exact failure (PTY not
  attached, `AddCSharpApp` not accepting `WithTerminal`, `Console.ReadKey` behaviour) and revert
  the sample to `WithArgs("status")` on 13.5.
- Do not conflate with backlog task 083 (Blazor WASM terminal REPL over SignalR): that is a
  browser terminal, this is Aspire's own PTY attach. 083 stays as is.

## Checklist

- [x] Sample on one 13.5 train (SDK + all `Aspire.Hosting.*`); no 13.1/13.4 leftovers
- [x] `WithTerminal()` + `--interactive` wired; experimental diagnostics suppressed
- [x] Dashboard Terminal view shows the REPL prompt and accepts input
- [x] `aspire terminal attach` works with `features.terminalCommandsEnabled`
- [x] Verdict (works / does not work, with evidence) recorded in Notes
- [x] Stale "Aspire doesn't provide" comment updated or failure documented
- [x] Implementation review (effort 1, general): 3 rounds, disposition clean

## Session

- Created: ganda session 691380 (2026-09-02), reserved from timewarp-architecture task 209
  (Aspire 13.5 update / WithTerminal vs Nuru investigation)
- Implementer: grok task-work implement oracle (2026-09-02)
- Review: grok task-work review oracle (2026-09-02), effort 1 general, 3 rounds

## Notes

### Verdict: works (keep `WithTerminal()`), with a DSR leak caveat

`WithTerminal()` **is** callable on `AddCSharpApp`. The C# API is
`IResourceBuilder<T>.WithTerminal<T>(...) where T : IResource` in Aspire.Hosting 13.5.3
(docs only show `AddExecutable` / `AddContainer`; the constraint is any `IResource`).

Restored AppHost graph is a single 13.5.3 train:

- `Aspire.AppHost.Sdk` 13.5.3
- `Aspire.Hosting` 13.5.3
- `Aspire.Hosting.AppHost` 13.5.3
- `Aspire.Hosting.Orchestration.linux-x64` 13.5.3
- `Aspire.Dashboard.Sdk.linux-x64` 13.5.3

No 13.1 / 13.4 leftovers. Sample has no extra `#:package Aspire.Hosting.*` pins; the SDK
pulls the matching train.

**PTY attach works.** After `aspire start --isolated`:

- `nuruclient` Running / Healthy
- hidden `nuruclient-terminalhost-0` Running / Healthy
- `aspire terminal ps`: replica 0 `alive`, 120×30, `terminal.enabled=true`
- Dashboard Console page defaults to **Terminal** and shows the welcome banner plus green
  `otel>` prompt (ANSI survives). Evidence:
  `dashboard-terminal-repl-prompt.png`
- `aspire terminal attach nuruclient --replica 0` attaches as PRIMARY (Ctrl+B D to detach).
  Arrow-up recalled `greet Alice` from history in the attach stream. Per-keystroke syntax
  highlighting (yellow/green) was visible in the Hex1b cell updates.

**`--interactive` must be after `--`.** `AddCSharpApp` runs:

```text
dotnet run --file nuru-client.cs --no-cache --configuration Debug --no-launch-profile -- --interactive
```

Without the `--` separator, Aspire appended `--interactive` onto `dotnet run`, which is a
SDK flag ("Allows the command to stop and wait…"). The process then died with
`reading from PTY failed: read /dev/ptmx: input/output error` (exit 1) after ~5s. Sample
uses `.WithArgs("--", "--interactive")`.

**Caveat (do not revert the sample for this):** Nuru's `ReplConsoleReader.RedrawLine` /
`UpdateCursorPosition` call `Terminal.GetCursorPosition()`. On Unix that is `ESC[6n`.
Aspire's Hex1b/xterm PTY answers with `ESC[<row>;<col>R`. Those DSR bytes can arrive on
stdin after `GetCursorPosition` has returned and leak into `ReadKey` as typed text, so the
REPL echoes `[11;10R` and runs it as a command (`Unknown command. Use --help for usage.`).
Evidence: `dashboard-terminal-dsr-leak.png`. Prompt, colour, attach, and history still work;
command submit is noisier than a local kernel TTY. Follow-up is Nuru/TimeWarp.Terminal
cursor-query vs PTY, not Aspire `WithTerminal` availability.

Debugger still does not auto-attach to a `WithTerminal()` resource (documented DCP limit).
Task 083 (Blazor WASM terminal) is unrelated and unchanged.

## Results

- Bumped `samples/aspire-otel/apphost.cs` to `#:sdk Aspire.AppHost.Sdk@13.5.3` and wired
  `AddCSharpApp("nuruclient", "./nuru-client.cs").WithArgs("--", "--interactive").WithTerminal()`.
- Suppressed `ASPIRECSHARPAPPS001` and `ASPIRETERMINAL001` (pragma + `NoWarn`).
- Replaced the stale "REPL requires interactive console which Aspire doesn't provide" comment.
- Updated `nuru-client.cs`, `readme.md`, and `overview.md` for Terminal attach and the `--`
  separator.
- Proved dashboard Terminal view + `aspire terminal attach` against Aspire CLI 13.5.3.
- Review follow-up: corrected sample docs (stale `_aspire-host-otel` paths, standalone REPL
  invocation, shebang/launch-profile claims, `terminalCommandsEnabled` vs dashboard, cwd-relative
  standalone `dotnet run`).

Files changed:

- `samples/aspire-otel/apphost.cs`
- `samples/aspire-otel/nuru-client.cs`
- `samples/aspire-otel/readme.md`
- `samples/aspire-otel/overview.md`

Key decisions:

- Keep `WithTerminal()` (verdict: works). Do not revert to `WithArgs("status")`.
- Pass `--` before `--interactive` so `dotnet run` does not swallow the flag.
- Record the `GetCursorPosition` / DSR leak as a Nuru-on-Hex1b caveat, not a sample revert.
- Dashboard Terminal needs no feature flag; `features.terminalCommandsEnabled` is CLI attach only.

### Review disposition

- **Outcome:** clean
- **Effort / roster:** 1, general only
- **Rounds:** 3 (`review/round-1/` … `review/round-3/`)
- **Final counts:** 4 bug fixed, 4 suggestion fixed, 0 nit, 0 open, 0 wontfix
- **Paths:** `review/review-framework.md`, `review/round-3/merged.md`, `review/disposition.md`
- No sibling apply-review task; fixes stayed on 467.

Test outcomes:

- `dotnet build samples/aspire-otel/apphost.cs` — succeeded (WithTerminal on AddCSharpApp).
- `dotnet build samples/aspire-otel/nuru-client.cs` — succeeded.
- Isolated AppHost: `nuruclient` healthy, terminal replica `alive`.
- Dashboard Terminal: welcome + green `otel>` prompt.
- `aspire terminal attach`: PRIMARY attach, ANSI, history recall observed.

### How to validate

**Depends on:** Aspire CLI 13.5.x (`aspire --version`), .NET 10 SDK, worktree isolation.

**Smoke**

```bash
cd samples/aspire-otel
aspire start --isolated --non-interactive --format Json --apphost ./apphost.cs
aspire wait nuruclient
```

Then either:

1. Open the printed dashboard URL → Console → resource `nuruclient` (page
   `/consolelogs/resource/nuruclient`). Expect the **terminal** pane (not only console
   logs), welcome banner, and green `otel>` prompt. No `terminalCommandsEnabled` flag
   required for this path.
2. CLI attach only:

```bash
aspire config set features.terminalCommandsEnabled true --global
aspire terminal ps
aspire terminal attach nuruclient --replica 0
```

Expect PRIMARY attach, same prompt, Ctrl+B D to detach.

**Expect**

- `aspire terminal ps` lists `nuruclient` replica 0 as `alive` (size ~120×30 until a viewer
  resizes it).
- DCP command line includes `-- --interactive` (not `dotnet run … --interactive` without
  the separator).
- Dashboard Terminal shows `Aspire Host + OpenTelemetry + Nuru REPL Demo` and `otel>`.
- Typing may echo `[row;colR` / `Unknown command` if DSR leaks; that is the documented
  caveat, not a start failure.

**Automated gate**

```bash
dotnet build source/timewarp-nuru-build/timewarp-nuru-build.csproj -nologo
dotnet build samples/aspire-otel/apphost.cs -nologo
# expect: apphost.cs -> …/apphost.dll  (ASPIRE010 warning is OK)
```

**Not in scope:** task 083 (browser/SignalR terminal); debugger auto-attach on WithTerminal
resources; making `GetCursorPosition` DSR-safe in Nuru.
