# X thread: seven posts (text final 2026-10-09)

Post 1 is `documentation/posts/blips/2026-xx-xx-nuru-3.0-release.md` (144 chars). Nostr gets its own
blip with the link (`...-nostr.md`, 220 chars) because it is not threaded. Every post is ≤ 280 chars.

Screenshots: one per feature, dark theme, code only, 1600×900. Capture them from
`demo/app.cs.txt`, run outside the repo (see `demo/readme.md`). File names `shot-2.png` … `shot-6.png` in
this folder. Attach them in post order.

1. **Hook + video (native upload)** (144 chars)
   > TimeWarp.Nuru 3.0 is out. Your .NET CLI is now a tool your AI agent can call. No MCP server. One package. 60 seconds, start to agent-callable: 🧵

   No link here. Links in post 1 cut reach, so the link goes in post 7.

2. **Agent contract**. `shot-2.png`: `./app --capabilities` showing one endpoint plus the `invocation` block (244 chars)
   > Every Nuru app answers --capabilities with a machine-readable map of its commands: typed parameters, options, and whether each one is a query or a command. Plus an invocation block that says how to call them. Agents read it. You don't write it.

3. **JSON args**. `shot-3.png`: `./app deploy --json-args '{"env":"staging","tag":"3.0.0"}'` and its output, then the `bogus` key error (245 chars)
   > Agents are bad at shell quoting. --json-args takes inline JSON, @file, or - for stdin and binds it to the same handler a human calls. The command stays on argv. Only the values go in the JSON. Unknown keys fail loudly, so a wrong guess shows up.

4. **Single file**. `shot-4.png`: `demo/app.cs.txt` (beta.79 needs the `#:property` line. Drop it from the shot if the package fix lands, see the gates in task.md) (205 chars)
   > dotnet run app.cs. One file: routes, typed parameters, optional options, help, completion, REPL. A source generator emits the matcher at compile time, so there's no reflection and no runtime route parsing.

5. **REPL + completion**. `shot-5.png`: `./app -i` session, then `key-bindings --profile Emacs` (191 chars)
   > A built-in REPL (-i) with Default, Emacs, Vi, and VSCode key-binding profiles. Shell completion for bash, zsh, fish, and PowerShell, generated from your routes: ./app --install-completion zsh

6. **AOT**. `shot-6.png`: `ls -lh` of the published binary and `time ./app greet x`. Measured 2026-10-09 on beta.79, linux-x64 (WSL2): 5.9 MB, median 2.9 ms over 20 runs. Re-measure on the day and keep the text equal to the screenshot
   > Native AOT: that one-file app, REPL and completion included, publishes to a 5.9 MB binary and runs a command in ~3 ms. The generator does the work so the runtime doesn't.

7. **Ask** (265 chars, URL counts as 23 on X)
   > 3.0 moves contracts to TimeWarp.Mediator, swaps the MCP server for an in-context agent skill, and fixed 22 issues an agent-run code review found before release. Migration guide and repo: github.com/TimeWarpEngineering/timewarp-nuru. A ⭐ helps more than you'd think.

## @Grok replies (post within the hour, as replies to post 1)

- "@Grok compare this to System.CommandLine and Spectre.Console.Cli for building CLIs that AI agents call."
- "@Grok make a hero image: a lantern lighting a terminal, dark, minimal. 'Nuru' is Swahili for light."
- Optional: "@Grok a 5-second synth sting for this video intro" (Suno). Only use it if the result isn't embarrassing.

Grok's answers are public and long, and they earn their own engagement. They also keep the thread
native.

## Second-shot post (day 7): process story

> I had a Grok + Claude agent team code-review Nuru 3.0 before release. 28 findings, 22 fixed before the tag. The misses were the interesting part: 🧵

Then four posts from `kanban/done/482-…/review/`:

- R-1, `Build()` dropping the open route.
- R-4, completion never handing `File`/`Directory` to the shell.
- R-5, the typed catch-all emitting uncompilable code for `uint`/`TimeSpan`.
- S-4, the MCP tools answering with a pre-3.0 API, which is why the package was dropped.

End with the link to the review record in the repo.
