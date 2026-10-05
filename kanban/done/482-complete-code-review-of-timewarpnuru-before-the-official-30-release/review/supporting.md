# Search, build, dev CLI, MCP, tests, and docs

Baseline `5a06e900`. Re-checked against the tree on 2026-10-05. S-5 is the same finding as T-1. S-7 through S-10 are the original doc samples; D-1 is the wider living-doc inventory those samples belong to.

## S-1 — `nuru search` prints a JSON array after the listing

- Disposition: **fix-now** (482-009)
- Evidence: `SearchQuery` is `IQuery<SearchResult[]>` (`source/timewarp-nuru-search/endpoints/search-query.cs`). The handler writes the human listing, including `No results found.`, and then `return [.. results]` (or `return []`). The invoker serializes a non-null return value (`generators/emitters/handler-invoker-emitter.cs`, `JsonSerializer.Serialize(result, NuruUserTypesJsonContext.Default.Options)`).
- Why it matters: TimeWarp.Nuru.Search is a shipped tool. Every successful or empty search appends a JSON array after the text the handler already printed.

## S-2 — `--group` does not match the dotted group filter Nuru forwards

- Disposition: **fix-now** (482-010)
- Evidence: Indexed `group_path` is `string.Join(" ", endpoint.GroupPath)` (`services/search-index.cs`). Search filters with `e.group_path LIKE $groupPath || '%'`. Capabilities filtering splits the filter on `'.'` and compares segments (`generators/emitters/capabilities-emitter.cs`: `groupFilter.Split('.')` against `GroupPath`, which is the group prefix split on spaces). A nested filter such as `docker.remote` therefore does not prefix-match the stored `docker remote`.
- Why it matters: `--capabilities --group docker.remote` and `nuru search --group docker.remote` do not speak the same path. Nested groups are how the capabilities document tells callers to filter.

## S-3 — Rebuild does not remember the executable path

- Disposition: **fix-now** (482-011)
- Evidence: `IndexRebuildCommand` with `--all` calls `TryIndexCliAsync(cli.Name)` (`endpoints/index-rebuild-command.cs`). The index row stores `capabilities.Name`, which the emitter sets from `WithName` or `Assembly.GetEntryAssembly().GetName().Name` (`capabilities-emitter.cs`). `--cli` rejects anything `File.Exists` does not see, so a PATH name such as `nuru` fails, while a full path is stored under the assembly name. `search --cli` then filters on that name, not the path that was indexed.
- Why it matters: `index rebuild --all` cannot re-run a CLI whose capabilities name is not an executable on PATH. The tool command for this package is `nuru` (`timewarp-nuru-search.csproj`); the assembly name is `TimeWarp.Nuru.Search`.

## S-4 — MCP syntax and handler tools do not match the 3.0 API

- Disposition: **fix-now** as an exclude, not a rewrite (482-012). Do not rewrite MCP on this id.
- Evidence: `timewarp-nuru-mcp.csproj` is `PackAsTool` with `PackageId` `TimeWarp.Nuru.Mcp`. `GetSyntax("all")` extracts `MCP:endpoint-*` regions first (`tools/get-syntax-tool.cs`). The only embedded sample is `samples/fluent/03-syntax/fluent-syntax-examples.cs`, whose regions are `MCP:fluent-*`. Missing endpoint regions return `Error: Region 'MCP:…' not found`. `GenerateHandler` emits `[NuruRoute("<full fluent pattern>", Description = ...)]` and a nested `Handler` that does not implement `ICommandHandler` (`tools/generate-handler-tool.cs`). `[NuruRoute]` allows only one literal (`endpoint-extractor.cs`, NURU_A001).
- Why it matters: Not a blocker for the TimeWarp.Nuru library. It is a blocker for shipping `TimeWarp.Nuru.Mcp` under the 3.0 banner. Freeze remains `79706ebf` (2026-07-14) plus the path-traversal fix `34ee7300` (2026-09-23). The security fix does not make the tool's 3.0 answers correct. **Exclude the package from the 3.0 ship set.**

## S-5 / T-1 — Seven multi-mode test files call unfiltered `DiscoverEndpoints()`

- Disposition: **post-3.0**. Overlaps task 219. Not migrated on this id. CI on the baseline is green.
- Evidence: `tests/ci-tests/Directory.Build.props` compiles `tests/timewarp-nuru-tests/**/*.cs` into the `JARIBU_MULTI` assembly except `CiTestExcludes`. In these files `#if !JARIBU_MULTI` only wraps the standalone entrypoint. The test methods still call `.DiscoverEndpoints()` with no type filter:
  - `tests/timewarp-nuru-tests/group-options/group-options-01-basic.cs`
  - `tests/timewarp-nuru-tests/auto/endpoint-nullable-option-01.cs`
  - `tests/timewarp-nuru-tests/generator/generator-11-endpoints.cs`
  - `tests/timewarp-nuru-tests/generator/generator-18-partial-class-options.cs`
  - `tests/timewarp-nuru-tests/routing/routing-26-optional-int-skip.cs`
  - `tests/timewarp-nuru-tests/routing/routing-27-nested-route-groups.cs`
  - `tests/timewarp-nuru-tests/routing/routing-28-group-alias.cs`
- `generator-19-group-filtering.cs` and `generator-20-parameterized-service-constructor.cs` guard the unfiltered call inside `#if !JARIBU_MULTI`. `generator-49` and `generator-52` are in `CiTestExcludes`.
- Why it matters: Those seven files run inside the shared CI assembly and collect every `[NuruRoute]` in that compilation. They pass today. Cleaning them up is the 219 test-file work, not a 3.0 release gate.

## S-6 — `tw-nuru` skill told agents to return `ValueTask`

- Disposition: **fixed on this task**
- Evidence: `skills/tw-nuru/SKILL.md` now says handlers return `Task<T>`, names `TimeWarp.Mediator` as the contract namespace, and points agents away from `ValueTask<T>`. Pipeline behaviors remain `ValueTask` in `source/timewarp-nuru/abstractions/behavior-interfaces.cs`. That part of the skill matches the code.

## S-7 — AOT guide is the removed 2.x builder API

- Disposition: **fix-now** under D-1 (482-013)
- Evidence: `documentation/developer/guides/aot-compilation.md` pins `#:package TimeWarp.Nuru@2.1.0-beta.17` and uses `new NuruAppBuilder()`, `AddAutoHelp()`, and `Map(pattern, delegate)`. `NuruAppBuilder()` is internal. There is no `AddAutoHelp` under `source/`. `Map` is `Map(string)` and `Map(Func<…>)` only.

## S-8 — User routing docs still document `MapDefault` and `CreateBuilder(args)`

- Disposition: **fix-now** under D-1 (482-013)
- Evidence: `documentation/user/features/routing.md` section "Default Route (MapDefault)" shows `.MapDefault().WithHandler(...)`. No `MapDefault` method exists under `source/timewarp-nuru`. `NuruApp.CreateBuilder` takes no arguments (`nuru-app.cs`). An empty route is `Map("")`. The migration guide already says `MapDefault(...)` becomes `Map("")`.

## S-9 — Logging docs reference a namespace and package that are not in the product

- Disposition: **fix-now** under D-1 (482-013)
- Evidence: `documentation/user/features/logging.md` uses `using TimeWarp.Nuru.Logging`, `CreateBuilder(args)`, and tells users to `dotnet add package TimeWarp.Nuru.Logging`. `UseConsoleLogging` is `TimeWarp.Nuru.NuruLoggingExtensions`. There is no `TimeWarp.Nuru.Logging` project. `documentation/developer/guides/logging.md` repeats that package.

## S-10 — Deployment runfile pins TimeWarp.Nuru 1.0.0

- Disposition: **fix-now** under D-1 (482-013)
- Evidence: `documentation/user/guides/deployment.md` sample is `#:package TimeWarp.Nuru@1.0.0`. The call is `NuruApp.CreateBuilder()` (no args). The package version is still 1.0.0.

## D-1 — Living docs still show removed 2.x APIs

- Disposition: **fix-now** (482-013). Includes P-1, S-7, S-8, S-9, S-10, the error-handling guide (R-2), and the attribute remark below.
- This pass compared living docs with the 3.0 surface (`CreateBuilder` arity, `MapDefault`, `AddAutoHelp`, `TimeWarp.Nuru.Logging`, package pins, parser type names, `Map(string, delegate)`). It is not a line-by-line prose review of every page. `docs-accuracy-validator` was not spawned. 482-013 owns the rewrite.
- Files still teaching a removed or nonexistent API (current user and developer docs, not historical posts):
  - `documentation/user/getting-started.md`, `documentation/user/use-cases.md`
  - `documentation/user/features/routing.md`, `auto-help.md`, `logging.md`, `configuration.md`, `shell-completion.md`, `repl-key-bindings.md`, `terminal-abstractions.md`, `telemetry.md`, `widgets.md`, `output-handling.md`, `pipeline-behaviors.md`, `built-in-routes.md`
  - `documentation/user/reference/builder-api.md`, `nuru-app-options.md`, `supported-types.md`
  - `documentation/user/guides/deployment.md`, `using-repl-mode.md`, `architecture-choices.md`, `subset-publishing.md`
  - `documentation/developer/guides/aot-compilation.md`, `logging.md`, `debugging.md`, `using-analyzers.md`, `route-pattern-syntax.md`
  - `documentation/developer/reference/error-handling.md` (stderr catch, `Error executing handler`, thrown `Cannot convert...`; the generator writes binding errors with `Terminal.WriteLine` and does not catch handler exceptions — R-2)
  - `documentation/developer/reference/parser-classes-syntax-vs-semantics.md`, `parsing-flow-dependency-analysis.md` (P-1)
  - `documentation/developer/reference/glossary.md`, `documentation/developer/design/source-generators/endpoint-generator.md`
  - `source/timewarp-nuru/attributes/nuru-route-attribute.cs` remarks say space-separated multi-word literals (`"docker compose up"`). `endpoint-extractor.cs` rejects anything but one literal or `""` (NURU_A001).
  - `source/timewarp-nuru-devcli/readme.md` still shows `NuruApp.CreateBuilder(args)`.
- Out of this child: `documentation/posts/` (historical blips and presentations). Do not rewrite them to look like the 3.0 guide.
- `documentation/user/guides/migrating-to-3.0.md` is the page that already names the removals. The other pages should agree with it.

## Checked, not a 3.0 blocker

- `source/timewarp-nuru-build` is `IsPackable` false. No correctness bug verified.
- `source/timewarp-nuru-devcli` is a source-only package (`IncludeBuildOutput` false, content packed). No logic bug verified. The stale readme is D-1.
- Samples: no `MapDefault`, `AddReplOptions`, `TimeWarp.Nuru.Unit`, or `ValueTask` handlers in the sample sources checked with the runtime pass. Two comments only (`samples/endpoints/01-hello-world/endpoint-hello-world.cs`, `samples/endpoints/04-async/async.cs`). **post-3.0** comment cleanup. No per-sample `dotnet run` in this pass.
- Symbols: embedded PDB + SourceLink. No snupkg. **wont-fix** (Notes).
- Changelog gap beta.20 through beta.77 is 482-015, not a code defect. Last dated section is `3.0.0-beta.19` (`changelog.md`). Unreleased points at the migration guide.
