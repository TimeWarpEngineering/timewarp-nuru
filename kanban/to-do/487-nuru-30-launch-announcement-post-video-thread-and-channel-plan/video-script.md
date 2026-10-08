# Video script — 60 seconds, terminal only

Format: asciinema recording → agg/svg-term → mp4 (1080p, 16:9, dark theme, 20pt mono). No voice;
on-screen captions in the terminal title bar. Native upload to X; same file to YouTube Shorts (crop
to 9:16 if needed) and the blog.

| t | On screen | Caption |
|---|-----------|---------|
| 0–5 s | Empty `app.cs`. Cursor blinks. | *A CLI in one file.* |
| 5–18 s | Type the app (prewritten, replayed at typing speed): `#!/usr/bin/dotnet --`, `#:package TimeWarp.Nuru`, `NuruApp.CreateBuilder().Map("greet {name}").WithHandler((string name) => $"Hello, {name}!").AsCommand().Done().Map("deploy {env} --tag {tag?}")...Build().RunAsync(args)` | *Routes, not boilerplate.* |
| 18–24 s | `dotnet run app.cs greet World` → `Hello, World!` | *Runs.* |
| 24–30 s | `./app dep<TAB>` → completes `deploy`; `--t<TAB>` → `--tag` | *Shell completion generated for you.* |
| 30–36 s | `./app --repl`, run two commands, Ctrl-R history, exit | *Built-in REPL.* |
| 36–50 s | Split pane. Left: an agent prompt "deploy staging with tag 3.0.0 using the app". Right: the agent runs `./app --capabilities`, then `./app --json-args '{"command":"deploy","env":"staging","tag":"3.0.0"}'`, structured output returns. | *Your agent reads the contract and calls it. No MCP server.* |
| 50–56 s | `dotnet publish -p:PublishAot=true` → `ls -lh` shows ~4 MB; `time ./app greet x` → ~8 ms | *AOT. 4 MB. 8 ms.* |
| 56–60 s | End card: logo, `github.com/TimeWarpEngineering/timewarp-nuru`, "⭐ if useful" | |

Production notes
- Prewrite every command in a script so the take is clean; use `asciinema rec --idle-time-limit 1`.
- The agent pane can be a real Claude Code / Grok session or a scripted replay; a real one is more
  credible if the output is short.
- Verify the AOT numbers on the day against `documentation/user` claims (3.3–4.8 MB, 7–10 ms).
