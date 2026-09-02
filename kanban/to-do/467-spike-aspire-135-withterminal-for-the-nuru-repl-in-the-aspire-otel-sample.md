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

- [ ] Sample on one 13.5 train (SDK + all `Aspire.Hosting.*`); no 13.1/13.4 leftovers
- [ ] `WithTerminal()` + `--interactive` wired; experimental diagnostics suppressed
- [ ] Dashboard Terminal view shows the REPL prompt and accepts input
- [ ] `aspire terminal attach` works with `features.terminalCommandsEnabled`
- [ ] Verdict (works / does not work, with evidence) recorded in Notes
- [ ] Stale "Aspire doesn't provide" comment updated or failure documented

## Session

- Created: ganda session 691380 (2026-09-02), reserved from timewarp-architecture task 209
  (Aspire 13.5 update / WithTerminal vs Nuru investigation)

## Notes

Unknowns to resolve (not documented by Aspire as of 13.5.3):

- Whether `WithTerminal()` is callable on the `AddCSharpApp` builder; docs only show
  `AddExecutable` and `AddContainer`.
- Whether the PTY presents to .NET `Console` APIs the way Nuru's REPL reader expects
  (`Console.ReadKey(intercept: true)`, resize).
- Debugger does not auto-attach to a `WithTerminal()` resource (documented DCP limitation).
