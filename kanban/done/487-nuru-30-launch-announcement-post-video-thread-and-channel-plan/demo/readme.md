# Launch demo app

`app.cs.txt` is the app (saved as `.txt` so the repo audit does not treat it as a runfile; copy it out as `app.cs`) used in the video, the blog, and thread post 4. It must be run **outside this
repo**: the repo's Central Package Management rejects `#:package ...@version` (NU1008).

```bash
d=$(mktemp -d) && cp app.cs.txt "$d"/app.cs && cd "$d"
dotnet run app.cs -- greet World                                         # Hello, World!
dotnet run app.cs -- deploy staging                                      # Deploying latest to staging
dotnet run app.cs -- deploy --json-args '{"env":"staging","tag":"3.0.0"}' # Deploying 3.0.0 to staging
dotnet run app.cs -- --capabilities                                      # endpoints[] + invocation{}
dotnet run app.cs -- deploy --json-args '{"env":"x","bogus":1}'; echo $?  # Error: Unknown key 'bogus' ... / 1
```

Verified 2026-10-09 against `TimeWarp.Nuru 3.0.0-beta.79` from nuget.org. On release day change the
version pin to `3.0.0` and re-run every line.

Gotchas found while building it:

- `return await app.RunAsync(args);` — a bare `await` discards the exit code, so errors exit 0.
  Agents rely on the exit code; keep the `return`.
- An optional option is `--tag? {tag?}`. Writing `--tag {tag?}` makes the option required at match
  time (`deploy staging` then prints "Unknown command").
- `--json-args` carries values only. The route literal (`deploy`) stays on argv; JSON keys are the
  `name` strings from `parameters[]`/`options[]` in `--capabilities` (no `--`, no command name).
