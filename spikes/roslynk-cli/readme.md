# Roslynk CLI spike on Nuru

Hand-written pitch for a Roslynk CLI on TimeWarp.Nuru. Two routes call Roslynk in-process:

- `diagnostics` calls `GetDiagnosticsTool.GetDiagnostics`
- `refs` calls `FindReferencesTool.FindReferences`

Route and option descriptions are the tools' `[Description]` text. `--capabilities` and `--json-args` are Nuru's agent contract. This is a spike in the Nuru repo so the route shape is visible. The CLI belongs with Roslynk.

## Build

`Directory.Build.props` looks for `Morris.Roslynk.csproj` at `TimeWarpEngineering/Roslynk` or `mrpmorris/Roslynk` beside this repo. Override with `-p:RoslynkProject=/path/to/Morris.Roslynk.csproj`.

File-based apps default `PublishAot`, which turns reflection JSON and dynamic code off. Roslynk's solution load needs both. `Directory.Build.targets` turns that off for this host.

```bash
dotnet run -c Release --file spikes/roslynk-cli/roslynk-nuru.cs -- --help
dotnet run -c Release --file spikes/roslynk-cli/roslynk-nuru.cs -- diagnostics --help
dotnet run -c Release --file spikes/roslynk-cli/roslynk-nuru.cs -- --capabilities
```

## Calls

```bash
dotnet run -c Release --file spikes/roslynk-cli/roslynk-nuru.cs -- \
  diagnostics ./timewarp-nuru.slnx --json-args '{"includeErrors":true,"includeAnalyzers":false}'

dotnet run -c Release --file spikes/roslynk-cli/roslynk-nuru.cs -- \
  refs ./timewarp-nuru.slnx TimeWarp.Nuru.NuruApp --json-args '{"maxResults":5}'
```

`--json-args -` reads stdin. `--json-args @path` reads a file. An inline `{...}` object is the third form. Keys are the `name` values from `--capabilities`. Positional names are lowercased (`solutionid`, `symbolname`). Option names keep the long form passed to `[Option]`.

`timing/warm-pair.cs` is the same host with two top-level `RunAsync` calls. It prints `warmPair.firstMs` and `warmPair.secondMs` on stderr. The first call loads the solution and compiles. The second call uses that loaded solution. Repeating the same `diagnostics` arguments hits Roslynk's diagnostics memo, whose key is `includeAnalyzers` alone. A second `refs` call still searches. That search is the warm cost that is not a cache hit. The entry is not a route on the pitch CLI.

`binding/` is a separate probe for nested objects, maps, arrays, and enums. `schema/` lists every `[McpServerTool]` parameter and whether that CLR shape fits `--json-args`.
