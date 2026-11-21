# NuruRouteExample proof capture (task 570)

`calc.cs` is a one-route runfile that uses the published package `TimeWarp.Nuru` 4.0.0-beta.16,
the newest version on nuget.org on 2026-11-21. The version is pinned in the repository
`Directory.Packages.props`, because the `cpm-consistency` audit check refuses an inline
`#:package …@version`. The sample does not use the repository's project references. The
`Directory.Build.*` files in this folder stop MSBuild from importing the repository build and
analyzers. The calculator-contracts sample was not used; it does not declare `[NuruRouteExample]`.

The route under test:

```csharp
[NuruRoute("add", Description = "Add two numbers")]
[NuruRouteExample("add 2 3", Description = "Adds 2 and 3, prints 5")]
public sealed class AddQuery : IQuery<double>
```

## Files

| File | Command that produced it |
|------|--------------------------|
| `capture/help-add.txt` | `./calc.cs add --help` (per-route help, shows `Examples:`) |
| `capture/help-root.txt` | `./calc.cs --help` |
| `capture/capabilities.json` | `./calc.cs --capabilities` (the route's `examples[]`) |
| `capture/json-args.txt` | every `--json-args` command, its output, exit code, and PASS/FAIL |

Each `*.ansi.txt` is the raw output with its color codes, for the terminal video. Each `*.txt`
without `.ansi` is the same output with the ANSI escape sequences removed by `sed`. Version
4.0.0-beta.16 writes color codes even when stdout is a pipe and `NO_COLOR` is set.

## Regenerate

```bash
./capture.sh
```

It builds `calc.cs`, rewrites everything under `capture/`, and prints the PASS and FAIL counts.
The 2026-11-21 run printed `8 PASS` and `0 FAIL`.

## `--json-args` results (2026-11-21, .NET SDK 11.0.100, Linux)

| Form | Command | Result |
|------|---------|--------|
| inline | `./calc.cs add --json-args '{"x":2,"y":3}'` | PASS, prints `5`, exit 0 |
| stdin `-`, pipe | `printf '{"x":2,"y":3}' \| ./calc.cs add --json-args -` | PASS, prints `5`, exit 0 |
| stdin `-`, heredoc | `./calc.cs add --json-args - <<'JSON'` … `JSON` | PASS, prints `5`, exit 0 |
| `@path` | `./calc.cs add --json-args @add-2-3.json` | PASS, prints `5`, exit 0 |
| argv overrides JSON | `./calc.cs add 10 --json-args '{"x":2,"y":3}'` | PASS, prints `13`, exit 0 |
| unknown key | `./calc.cs add --json-args '{"x":2,"y":3,"z":4}'` | PASS, exit 1, `Unknown key 'z'` |
| attached form | `./calc.cs add '--json-args={"x":2,"y":3}'` | PASS, exit 1, must be a separate token |
