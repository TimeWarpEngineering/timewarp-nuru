# X thread draft — seven posts

Post 1 is also the Nostr blip (≤ 280 chars). Screenshots: one per feature, dark theme, code only.

1. **Hook + video (native upload)**
   > TimeWarp.Nuru 3.0 is out. Your .NET CLI is now a tool your AI agent can call. No MCP server. One package. 60 seconds, start to agent-callable: 🧵
   (≤ 280 chars; put the GitHub link in post 7, not here — links in post 1 cut reach)

2. **Agent contract** — screenshot: `--capabilities` JSON with the `invocation` block
   > Every Nuru app answers `--capabilities` with a machine-readable map of its routes, including how to invoke them. Agents read it. Humans don't have to write it.

3. **JSON args** — screenshot: `--json-args '{"command":"deploy","env":"staging"}'`
   > Agents don't do shell quoting well. `--json-args` takes `-`, `@file`, or inline JSON and binds it to the same handler. Same code path as the human CLI.

4. **Single file** — screenshot: the 12-line runfile
   > `dotnet run app.cs`. Routes, typed parameters, options, catch-all. The source generator emits the matcher at compile time: no reflection, no runtime parsing.

5. **REPL + completion** — screenshot: REPL session with `key-bindings`
   > Built-in REPL with Default, Emacs, Vi, and VSCode key-binding profiles. Shell completion for bash and zsh generated from your routes.

6. **AOT** — screenshot: `ls -lh` + `time`
   > Native AOT: ~4 MB binary, ~8 ms startup. The generator does the work so the runtime doesn't.

7. **Ask**
   > 3.0 moved contracts to TimeWarp.Mediator, dropped the MCP server in favor of an in-context skill, and shipped 15 fixes from an agent-run code review. Migration guide + repo: github.com/TimeWarpEngineering/timewarp-nuru — a ⭐ helps more than you'd think.

## @Grok replies (post within the hour, as replies to post 1)

- "@Grok compare this to System.CommandLine and Spectre.Console.Cli for building CLIs that AI agents call."
- "@Grok make a hero image: a lantern lighting a terminal, dark, minimal, Swahili 'nuru' means light."
- Optional: "@Grok a 5-second synth sting for this video intro" (Suno) — only if the result isn't embarrassing.

Grok's answers are public, long, and earn their own engagement; they also keep the thread native.

## Second-shot post (day 7) — process story

> I had a Grok + Claude agent team code-review Nuru 3.0 before release. 21 findings, 15 PRs, all merged in a day. The misses were the interesting part: 🧵
Then 4–5 posts from `review/findings.md`: the `Build()` route drop, completion never delegating to the filesystem, the typed catch-all parser, the frozen MCP tests breaking on a doc rename. End with the link to the review record in the repo.
