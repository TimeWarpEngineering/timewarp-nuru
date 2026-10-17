# Roslynk CLI spike on Nuru (pitch for Pete)

## Description

Hand-write a small Roslynk CLI on Nuru to show Pete (Peter Morris, author of Roslynk) what a Roslynk CLI built on Nuru looks like. This is a pitch: it stays Pete's project, and Steven helps. Peter said he is considering Nuru for a Roslynk CLI generated from the `[McpServerTool]` and `[Description]` attributes. This spike proves the route shape by hand before any bridge or generator exists.

Do not frame it to Pete as "Nuru's Code Mode". Contacting SimCube or upstreaming anything is Pete's call; bots don't reach out.

## Context

- timewarp-intel `evaluations/2026-10-15-opencode2-nuru.md` (Busara: GREEN). Recommended order item 1: Roslynk hand-written spike with two commands, `diagnostics` and `refs`. Size S.
- timewarp-intel `evaluations/2026-10-15-nuru-client-server.md` (Busara: daemon (A) GREEN, gated on this spike's cold-vs-warm measurement). Reference design for a warm daemon: SimCubeLtd/roslynk-cli (System.CommandLine client plus a local daemon over a Unix socket or named pipe).

## Requirements

- Two hand-written Nuru routes that call Roslynk's tool methods in-process: `diagnostics` and `refs`, with `[Description]` text carried into help and `--capabilities`.
- Agent contract works end to end: `--capabilities` lists both endpoints; `--json-args` (`-`, `@path`, inline) invokes each.
- Test the `--json-args` to MCP JSON Schema mapping against Roslynk's real tool inputs (nested objects, arrays, enums). Record which inputs map cleanly and which do not. This result decides the shape of task 507 (McpBridge).
- Measure the token size of the full `--capabilities` output, and extrapolate to Roslynk's full tool set (about 40 tools). This decides whether group or prefix filtering beyond the existing `CapabilitiesFilter.Group` is needed.

## Deliverable: cold-vs-warm startup timing

Explicit deliverable, because the optional daemon package (`TimeWarp.Nuru.Daemon`, working name, from the client-server eval) depends on this result:

- Cold: one command in a fresh process, including solution load.
- Warm: a second call to the same command in the same process, with the solution already loaded.
- Report median and spread over repeated runs on the same solution, with the machine and build configuration. Compare with SimCube's `roslynk-cli` daemon as a baseline where possible.
- State the gap. If it is small, the daemon is nice-to-have; if it is large, the daemon is GREEN.

## Goal (implementer restatement)

Show Pete a small hand-written Nuru CLI. It is not a generator and not a bridge. Two routes, `diagnostics` and `refs`, call Roslynk's `GetDiagnostics` and `FindReferences` in-process and carry each method's `[Description]` into help and `--capabilities`.

Both routes honor the agent contract: `--capabilities` lists them, and `--json-args` accepts stdin (`-`), a file (`@path`), and an inline object.

Write down which of Roslynk's real tool inputs `--json-args` can bind, and which it cannot. Six of the clean parameters are non-nullable `bool` values whose default is `true`. They still count as clean, and the task 507 note says a generator emits them as `bool?` and supplies `true` when the option is omitted. That record, the measured `--capabilities` token size, and the extrapolation to the full tool set decide task 507 and whether `CapabilitiesFilter.Group` is enough.

Cold versus warm is repeated runs on one solution, with the machine and the build configuration. Cold is solution load plus the first compile. The sub-millisecond diagnostics figure is a cache hit: the warm pair repeats Roslynk's `includeAnalyzers` memo key. The uncached warm cost to cite is the `refs` median, about 26 ms. Compare with SimCube's `roslynk-cli` daemon when it can be run. A large gap means the daemon is worth building for a client that starts a process per command.

This fix pass is done when the pitch states that cache distinction, the 507 paragraph names the six `bool` defaults and the `bool?` rule, `schema-report` prints `parameters.boolDefaultTrue=6`, and the option long-form sentence is separate from the positional-name sentence. Acceptance stays the checklist, a short pitch in `## Results`, and a non-empty `### How to validate` with Smoke and Expect.

## Checklist

- [x] Spike app with `diagnostics` and `refs` routes on Nuru
- [x] `--capabilities` and `--json-args` proof for both routes
- [x] `--json-args` vs MCP JSON Schema mapping findings
- [x] `--capabilities` token size, measured and extrapolated
- [x] Cold-vs-warm timing results written into this task
- [x] Short pitch summary for Pete (what it looks like, what it costs, what is next)
- [x] Round 1 M1–M3: cache-hit wording, six `bool` defaults, split naming sentences

## Review

- Round 1: M1, M2, M3 addressed on this branch.
- Round 2: M1–M3 verified fixed, no new findings. Disposition: clean (`review/disposition.md`).

## Notes

- Nuru's `RunAsync` must be intercepted by the source generator (`source/timewarp-nuru/nuru-app.cs`), so the spike uses normal compile-time routes.
- Follow-ups: task 507 (McpBridge go/no-go) and the daemon package decision both read this task's results.

## Session

- Created: 4064788 (2026-10-15)
- Boy-scout audit fix: ran `ganda repo audit --fix` (root .gitignore porcelain + routine-journal lines; untracked kanban review-round markers) to unblock publish.
- 2026-10-18: Hand-wrote the spike under `spikes/roslynk-cli`, then measured mapping, token size, and cold versus warm.
- 2026-10-18: Set the executable bit on the four spike shebangs so `runfile-executable` passes.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-17T17:33:19Z
- Review oracle round 1 (2026-10-18): general reviewer (claude-opus-5-5), effort 3, turn cap 200, session id not exposed. Re-ran the smoke tests, the json-args forms on the real solution, warm-pair, and `ganda repo audit`. Findings: 0 bug, 2 suggestion, 1 nit, all open. Fix pass requested for M1–M3 (`review/round-1/merged.md`).
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-17T17:38:02Z
- 2026-10-18: Fix pass for review round 1 (M1–M3). The pitch calls the 0.679 ms diagnostics warm time a cache hit on `includeAnalyzers` and cites the `refs` warm median (25.9 ms) as the uncached warm cost. Cold is load plus the first compile. No new changed-key timing run: the finding asks to cite that existing `refs` median. `schema-report` prints `parameters.boolDefaultTrue` and the six names. The 507 paragraph says those bools become `bool?` and the handler supplies `true`. Option long forms and positional lowercasing are separate sentences.
- Review oracle round 2 (2026-10-18): general reviewer (claude-opus-5-5), effort 1, turn cap 80, session id not exposed. Reviewed the fix delta `08d3d19f..3cd65ee7`. M1–M3 fixed, no new findings. Disposition clean (`review/disposition.md`).
- Review oracle round 2, headless re-run (2026-10-18): reviewer-claude (claude-opus-5-5), effort 1, turn cap 80, session id not exposed. The Herdr review-2 pane hung after it committed round 2. Re-verified `schema-report` (`parameters.boolDefaultTrue=6`) and `ganda repo audit` (pass). No product change since `3cd65ee7`, and no new findings. Disposition stays clean.

## Results

### Pitch for Pete

Two hand-written routes, `diagnostics` and `refs`, call `GetDiagnosticsTool.GetDiagnostics` and `FindReferencesTool.FindReferences` in-process. The method `[Description]` text is the route description in help and in `--capabilities`. Option long forms are the tool parameter names (`includeErrors`). Positional names are lowercased by Nuru (`symbolName` becomes `symbolname`). An agent lists the routes with `--capabilities` and calls them with `--json-args` (`-`, `@path`, or an inline object).

What it costs, on this machine (WSL2 Linux 6.18.40.1, AMD Ryzen 9 7950X3D, 32 logical CPUs, .NET SDK 10.0.402, Release), against this repo's already-built `timewarp-nuru.slnx`, Roslynk `788735a12c91d8a603c5de50474b96a7424a2007`:

- A fresh process that asks for compiler errors (`includeErrors` true, `includeAnalyzers` false) takes a median of 6.00 seconds (five runs, 5.60–7.14). That cold time is solution load plus the first compile.
- The same call a second time in that process takes a median of 0.679 ms (five runs, 0.596–0.936). The pair repeats `includeAnalyzers`, and that value is Roslynk's whole diagnostics cache key in `GetDiagnosticsTool`, so the second call is a memo hit. It is not what a warm call costs when the call still has work to do.
- A warm `refs` call for `TimeWarp.Nuru.NuruApp` (`maxResults` 5) has a median of 25.9 ms (three pairs, 25.2–26.3). The solution is already loaded and the search still runs. That is the uncached warm cost.
- `--capabilities` for these two routes is 6,519 characters, about 1,630 tokens at four characters per token. A Nuru-shaped catalog of the 29 reflected tools is about 15,800 tokens, and about 22,000 if the set grows to 40 at the same density.

What is next: task 507 can generate routes for the 137 parameters whose CLR types already bind. Six of those are non-nullable `bool` parameters that default to `true`; emit them as `bool?` and supply `true` in the handler when the option is omitted. Two parameters do not bind (`multi_query.operations`, `apply_patch.baseVersions`). Put tools in groups so the existing `--group-filter` can cut the catalog. A warm process is worth having. Cold is load plus the first compile, about 6 seconds. The sub-millisecond diagnostics number is a cache hit. A warm call that still does work is the `refs` median, about 26 ms. A daemon is the right shape when each command is a new process. A host that can call `RunAsync` again in the same process does not need a socket for this gap. The daemon verdict stays GREEN.

The spike stays in the Nuru repo so the route shape is reviewable. The CLI belongs with Roslynk. Contacting SimCube or upstreaming anything is Pete's call.

### Routes

Source: `spikes/roslynk-cli/`. The app is not under `samples/`, so `verify-samples` does not build it, and it is not in `timewarp-nuru.slnx`. `Directory.Build.props` project-references `Morris.Roslynk.csproj` from `TimeWarpEngineering/Roslynk` or `mrpmorris/Roslynk` and fails the build when neither exists.

Each handler full-paths the solution id, waits on `InstanceRegistry.GetOrAddAsync`, and then calls the tool. If that wait finishes faulted, the CLI prints the load exception. The tool method on its own answers `Indexing` for a faulted model.

`includeAnalyzers` is `bool?`. A non-nullable bool flag always starts false, so the tool's default of true would be lost. Omission stays null and the handler passes true.

File-based apps default `PublishAot`, which disables reflection JSON and dynamic code. Roslynk's MSBuild workspace load then faults in about half a second with "Reflection-based serialization has been disabled". `Directory.Build.targets` sets `PublishAot` and `PublishTrimmed` false and turns those two features back on.

`timing/warm-pair.cs` is a second entry with two top-level `RunAsync` calls. The source generator does not walk `if` statements, so a flag on the pitch CLI cannot hide the second call and still intercept it. Both intercepted calls share one static service provider, and with it the loaded solution. `--warm-pair` is not a route and does not appear in `--capabilities`. Repeating `diagnostics` there hits Roslynk's `includeAnalyzers` memo. Repeating `refs` still runs the search.

### `--json-args` versus Roslynk's tool inputs

Reflected `[McpServerTool]` methods on that Roslynk commit: 29 tools. Parameters: 137 clean, 2 blocked, 13 host (`CancellationToken`). There is no top-level enum parameter. The enum lives inside the blocked `multi_query` operation.

Clean, and checked at runtime on the binding probe unless noted:

| Shape | Result |
| --- | --- |
| bool | JSON `true`/`false` binds. A flag's initializer `= true` does not survive: the generated local starts false, and omission is indistinguishable from false. Six clean parameters default to `true`: `get_diagnostics.includeAnalyzers`, `get_members.includeMethods`, `get_members.includeFields`, `get_members.includeProperties`, `get_members.includeEvents`, and `get_members.includeNestedTypes`. |
| bool? | Omission is null. JSON `true` and `false` both stick. This is how `includeAnalyzers` keeps the tool default. Capabilities type is `bool?` and the option is not a flag. |
| int, int? | JSON numbers bind. `startItem = 1` survives omission. |
| string | JSON string binds. |
| string[] | A JSON array binds (`["CS0246","IDE0005"]`). Omission becomes an empty array, not null. JSON `null` clears a nullable array to null. `GetDiagnostics` treats null and empty as no id filter. |
| enum | JSON string, case-insensitive (`"GET_SYMBOL"` → `get_symbol`) when the property is nullable. A non-nullable enum option does not compile: the argv path produces a nullable enum and the assignment does not convert. Roslynk's tools do not have a top-level enum parameter. |
| argv over JSON | `--includeErrors` forces the flag true when the JSON value is false. |

Blocked at compile time. The generator treats a nested object as `string`, an array of objects as `string[]`, and a dictionary as `string[]`. Those probes live in `spikes/roslynk-cli/binding/rejected/` and are not compiled:

| Tool parameter | CLR type | Why it does not map |
| --- | --- | --- |
| `multi_query.operations` | `IReadOnlyList<MultiQueryOperation>` | Array of objects. Each object is `MultiQueryOp tool` (enum) plus `IReadOnlyDictionary<string, JsonElement>? arguments`. |
| `apply_patch.baseVersions` | `IReadOnlyList<FileVersion>` | Array of objects (`Path`, `Version`). |

Task 507 shape: generate `[Option("mcpName")]` routes for the clean parameters. Six of those 137 are non-nullable `bool` values that default to `true` (`get_diagnostics.includeAnalyzers`, `get_members.includeMethods`, `get_members.includeFields`, `get_members.includeProperties`, `get_members.includeEvents`, `get_members.includeNestedTypes`). A non-nullable flag would flip those defaults to false. Emit them as `bool?` and have the handler pass `true` when the option is omitted. `schema-report` prints `parameters.boolDefaultTrue=6` and one `boolDefaultTrue=` line per parameter. Classify still counts a non-nullable enum as clean. That shape does not compile, and this commit has no top-level enum parameter, so the miss is latent. Do not emit a direct binder for those two object arrays. A later bridge can take them as one JSON string and parse it in the handler, or leave those two tools out of the first generator. Positional `[Parameter]` names are `property.Name.ToLowerInvariant()` (`solutionId` → `solutionid`). The extractor does not read `[Parameter(Name)]`. Option long forms keep the string passed to `[Option]`, which is how `includeErrors` matches the tool.

Proof on the real solution, all exit 0: `diagnostics` and `refs` each with inline JSON, `@path`, and stdin `-`. `refs` resolved `TimeWarp.Nuru.NuruApp` (`resolvedSymbol=TimeWarp.Nuru.NuruApp`, `count=5`). `diagnostics` with analyzers off reported `errors=0`.

### Token size

Estimator: `(characters + 3) / 4`. Python `tiktoken` is not installed.

| Document | Characters | Tokens |
| --- | --- | --- |
| Live `--capabilities` for `diagnostics` and `refs` | 6,519 | 1,630 |
| Synthetic catalog of those two tools (descriptions included, Nuru's extra fields omitted) | 3,371 | 843 |
| Synthetic catalog of all 29 tools | 32,599 | 8,150 |
| Live-shaped extrapolation, 32,599 × 6,519 / 3,371 | 63,041 | 15,761 |
| Same density at 40 tools | | about 21,700 |

`--capabilities --group-filter diagnostics` returns zero endpoints. Both routes have `groupPath: []`. The filter works and does nothing until a route is grouped. `--capabilities --search` exits with "Search requires TimeWarp.Nuru.Search to be installed." That tool is not part of this host.

Decision: a new filter beyond `CapabilitiesFilter.Group` is not required if the generator assigns groups (diagnostics, symbols, edits, and so on). A flat catalog of about 16,000 tokens is large enough that one ungrouped list is a poor default. Group the routes and the existing `--group-filter` is the cut.

### Cold versus warm

Build: Release. Host: the runfile dll via `dotnet roslynk-nuru.dll` (pitch CLI) and `dotnet warm-pair.dll` (two `RunAsync` calls). Solution: `timewarp-nuru.slnx` in this worktree, with existing build outputs. Command: `diagnostics` with `includeErrors` true, `includeAnalyzers` false, `summaryOnly` true, `maxSize` 1500. Cold is a fresh process: solution load plus the first compile. Warm is the second `RunAsync` in that process. This diagnostics pair repeats `includeAnalyzers`, and that string is the only diagnostics cache key in `GetDiagnosticsTool`, so the warm column is a memo hit.

Diagnostics, five runs:

| | Cold wall (s) | In-process first call (ms) | Warm second call (ms) |
| --- | --- | --- | --- |
| Values | 6.00, 5.60, 5.61, 6.06, 7.14 | 5777, 5542, 5426, 5562, 5429 | 0.596, 0.899, 0.936, 0.672, 0.679 |
| Median | 6.00 | 5542 | 0.679 |
| Spread | 5.60–7.14 | 5426–5777 | 0.596–0.936 |

`refs` for `TimeWarp.Nuru.NuruApp` with `maxResults` 5, three warm pairs: first call median 6,874 ms (load plus search), second call median 25.9 ms (25.2–26.3). The load is gone and the search still runs. That 25.9 ms median is the uncached warm cost. The diagnostics 0.679 ms figure is the memo hit, not this cost.

The gap is large. A fresh process pays about 6 seconds for load plus the first compile. Keeping the solution loaded removes that. A repeated diagnostics call then pays under a millisecond because it hits the memo. A warm call that still does work, the `refs` search, pays about 26 ms. Daemon verdict: GREEN for a client that starts a process per command. Nice-to-have only for a host that already keeps the process.

SimCube `Roslynk-cli` 1.1.0 (`1.1.0+c0da0deeafe3c07871c6f5531a91737619f18ac5`), installed under `/tmp` for this measurement and stopped afterward. Same solution, `diagnostics --errors --timing`. The daemon was stopped before each cold run. Their stdout was `errors=0`, `warnings=0`, `infos=426`, `hidden=4380`. Ours with analyzers off was `errors=0`, `infos=0`, `hidden=187`. The queries are not the same amount of work.

| | Cold wall (s) | Their `request_ms` | Warm client `total_ms` | Warm `request_ms` |
| --- | --- | --- | --- | --- |
| Values | 20.35, 20.07, 20.22 | 19998, 19688, 19848 | 64.3, 56.6, 68.9, 75.5, 76.0 | 3.71, 4.04, 4.51, 4.82, 4.51 |
| Median | 20.22 | 19848 | 68.9 | 4.51 |

Warm connect time median was 64 ms. `server ping --timing` was 79 ms. Their warm request is a few milliseconds once the solution is loaded, plus a socket connect. Our in-process diagnostics warm call has no socket and, for this repeated query, hits the memo, so it is the sub-millisecond number above. The in-process `refs` warm call is about 26 ms with no socket. Both measurements say the same thing: the load dominates, and keeping the solution in a process removes it.

### How to validate

Smoke:

```bash
dotnet run -c Release --file spikes/roslynk-cli/roslynk-nuru.cs -- --capabilities
dotnet run -c Release --file spikes/roslynk-cli/roslynk-nuru.cs -- diagnostics --help
dotnet run -c Release --file spikes/roslynk-cli/schema/schema-report.cs
dotnet run -c Release --file spikes/roslynk-cli/binding/binding-probe.cs -- scalars --json-args '{"includeAnalyzers":false,"ids":["CS0246"],"tool":"find_references"}'
```

Expect:

- `--capabilities` is JSON whose `endpoints` are `diagnostics` and `refs`, and `invocation.jsonArgs` is `--json-args`.
- `diagnostics --help` contains `Returns compiler/analyzer diagnostics` and `Values may come from --json-args.`
- The schema report prints `tools=29`, `parameters.blocked=2`, and `parameters.boolDefaultTrue=6`, plus `boolDefaultTrue=` lines for `get_diagnostics.includeAnalyzers`, `get_members.includeMethods`, `get_members.includeFields`, `get_members.includeProperties`, `get_members.includeEvents`, and `get_members.includeNestedTypes`.
- The probe prints a line containing `includeAnalyzers=False`, `ids=CS0246`, and `tool=find_references`, and exits 0.

A solution load is slower. With `Morris.Roslynk.csproj` available, `diagnostics` against `timewarp-nuru.slnx` and `--json-args` setting `includeErrors` true and `includeAnalyzers` false exits 0 and prints `errors=0`.

### Review

Round 1 (general, effort 3): 0 bug, 2 suggestion, 1 nit. The fix pass addressed M1–M3 on this branch. Round 2 (general, effort 1) re-verified the fix delta, re-ran `schema-report` (`parameters.boolDefaultTrue=6`) and `ganda repo audit` (pass), and found nothing new. Final counts: bug 0, suggestion 2 fixed, nit 1 fixed, 0 open, 0 wontfix. Disposition: clean, 2 rounds. Ledger: `review/round-2/merged.md` (round 1: `review/round-1/merged.md`). Disposition: `review/disposition.md`. Framework: `review/review-framework.md`. M1: the 0.679 ms diagnostics warm time is a memo hit on `includeAnalyzers`, and the uncached warm cost is the `refs` median of 25.9 ms. Cold is load plus the first compile. M2: six clean `bool` parameters default to `true` and become `bool?`, with the handler supplying `true`; `schema-report` counts them. M3: option long forms and positional names are separate sentences.
