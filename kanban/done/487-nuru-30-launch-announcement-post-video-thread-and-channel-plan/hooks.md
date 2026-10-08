# Hook candidates

**Decision (2026-10-08, maintainer: "go with your choice"):** launch on **#1 Agent contract**;
run **#2 Process story** as the second shot on day 7. #3 is the video's opening beat only.
Everything else (video, thread, blog title, HN title) derives from #1.

## 1. Agent contract — recommended for the launch

> Your CLI is now a tool your AI agent can call. No MCP server. One package.

- Why: it is the one thing 3.0 has that System.CommandLine, Spectre.Console.Cli, and Cocona do not.
  `--capabilities` returns a machine-readable description of every route including the `invocation`
  contract; `--json-args` (`-`, `@path`, or inline JSON) lets an agent call a command without shell
  quoting; `nuru search` indexes commands across every Nuru CLI on the box; the `tw-nuru` skill lets
  Claude Code or Grok build an app from one prompt.
- Risk: "agent" fatigue. Mitigate by showing it, not saying it: the video ends with an agent
  actually calling the CLI.
- Blog title candidate: *TimeWarp.Nuru 3.0: CLIs your agents can call*
- Show HN title candidate: *Show HN: Nuru 3.0 – .NET CLI framework where every command is agent-callable*

## 2. Process story — recommended as the second shot, ~1 week later

> I had a Grok + Claude agent team code-review Nuru 3.0. 28 findings, 22 fixed before the tag. Here's what they caught.

- Why: true, specific, novel. Names both models (X rewards this). The AI-dev audience engages with
  process more than with frameworks. Material already exists:
  `kanban/done/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md`
  and the stacked PR #298 with children #283–#297.
- Angle: the misses are the interesting part (the `Build()` route drop, completion never delegating to
  the filesystem, the frozen MCP tests breaking on a doc rename). Honest > triumphant.

## 3. Single-file app

> `dotnet run app.cs` and it's a full CLI: routing, tab completion, REPL, AOT.

- Why: fastest "oh nice" for .NET devs; the runfile demo is 12 lines.
- Why not as headline: it's the video's opening beat, not a reason to care in 2026.

## Rejected

- "Web-style routing for the console" — the 2025 hook; now expected, not exciting.
- "3.0 is out" / version-number framing — gives no one a reason to click.
