# Video script — 60 seconds, terminal only

Format: asciinema recording → agg/svg-term → mp4 (1080p, 16:9, dark theme, 20pt mono). No voice;
on-screen captions in the terminal title bar. Native upload to X; same file to YouTube Shorts (crop
to 9:16 if needed) and the blog.

| t | On screen | Caption |
|---|-----------|---------|
| 0–5 s | Empty `app.cs`. Cursor blinks. | *A CLI in one file.* |
| 5–18 s | Type `demo/app.cs.txt` (prewritten, replayed at typing speed): `#:package TimeWarp.Nuru@3.0.0`, `Map("greet {name}")…AsQuery().Done()`, `Map("deploy {env} --tag? {tag?}")…AsCommand().Done()`, `.EnableCompletion().AddRepl().Build()`, `return await app.RunAsync(args);` | *Routes, not boilerplate.* |
| 18–24 s | `dotnet run app.cs -- greet World` → `Hello, World!` | *Runs.* |
| 24–30 s | `./app dep<TAB>` → completes `deploy`; `./app deploy staging --t<TAB>` → `--tag` (after `./app --install-completion bash`) | *Shell completion generated for you.* |
| 30–36 s | `./app -i`, run `greet Ada` and `deploy staging`, Ctrl-R history, `exit` | *Built-in REPL.* |
| 36–50 s | Split pane. Left: agent prompt "deploy staging with tag 3.0.0 using ./app". Right: the agent runs `./app --capabilities`, then `./app deploy --json-args '{"env":"staging","tag":"3.0.0"}'` → `Deploying 3.0.0 to staging` | *Your agent reads the contract and calls it. No MCP server.* |
| 50–56 s | `dotnet publish app.cs -c Release -r linux-x64 -p:PublishAot=true` (sped up) → `ls -lh` → `time ./app greet x` | *AOT. 5.9 MB. 3 ms.* (the 2026-10-09 take; re-caption to what a release-day take shows) |
| 56–60 s | End card: logo, `github.com/TimeWarpEngineering/timewarp-nuru`, "⭐ if useful" | |

Production notes
- Prewrite every command in a script so the take is clean; use `asciinema rec --idle-time-limit 1`.
- The agent pane can be a real Claude Code / Grok session or a scripted replay; a real one is more
  credible if the output is short.
- AOT numbers: the shipped mp4 is the 2026-10-09 beta.79 take (5.9 MB, median 2.9 ms). Re-measured
  2026-10-14 on published beta.80, linux-x64: `ls -lh` prints `6.0M` (6,187,808 bytes, 5.90 MiB),
  a quiet `time` prints `0.003s`, median 2.52 ms over 20 runs. The VHS frame in `shot-6.png` reads
  `0.006s` because the recorder is loaded; the thread post matches that frame (`~6 ms`). The blog
  and the r/dotnet draft keep the quiet-shell figure (`~3 ms`).
  `documentation/user/reference/performance.md` lists 3.3 MB and 4.8 MB for Direct and Mediator AOT
  on .NET 9, with startup under 1 ms. It does not say those binaries omit REPL or completion.
  Caption what you measure, not the docs.
- `--json-args` carries values only. The route literal (`deploy`) stays on argv, and the JSON has no
  `command` key. An earlier draft of this script had it wrong.
- Record outside the repo: CPM rejects `#:package …@version` inside it (see `demo/readme.md`). The
  `#:property InterceptorsNamespaces=…` line that beta.79 needed is no longer required as of
  beta.80 (task 488).
- The REPL flag is `-i` / `--interactive`, not `--repl`.
