---
name: tw-nuru-verify
description: Verify a shipped TimeWarp.Nuru release the way a consumer uses it - restore the package and the nuru CLI from a clean cache, drive the shipped public builder API, audit the global NuGet cache, report which docs are out of date with the source, and write evidence a PR or release note can quote. Use after publishing a Nuru release, before promoting a beta, when docs or builder APIs changed, or when asked to "verify the release", "check Nuru as a consumer", or "run the consumer check".
---

# Nuru consumer verification

Repo tests build against project references, with this repo's `Directory.Build.props`. A consumer
gets none of that. They run `dotnet add package TimeWarp.Nuru` and copy a snippet from the README.
This skill does the same thing and records what happened.
(why: task 523, Steven approved the consumer-use check 2026-10-23)
(why: Steven 2026-10-23 docs are not the source of truth)

## The source is the authority

The source code, with the `#region Purpose` and `#region Design` blocks in each file, defines what
Nuru is meant to do. The docs and the README do not. So:

- Probe coverage comes from the **shipped public API** (public builder and extension methods in
  `source/`, as compiled into the package), plus Purpose/Design regions where they exist.
- A doc snippet that fails to build is a **doc defect**: the docs are out of date with the source.
  It is reported in its own section and does not fail the release verdict.
- The exception: when the source public API says the snippet should work, the failure is a
  **code defect**. The probe for that API shows it and fails the verdict. Example on
  4.0.0-beta.5: `AddCheckUpdatesRoute()` is public in source but the package gives `CS0122`;
  `UseLogging` and `UseConsoleLogging` are public in source but the generator answers
  `NURU_S999`.

It exists because of three failures that got past tests, the REPL and `--capabilities`:

| Failure | Class | What catches it here |
|---------|-------|----------------------|
| nuru#242 | Builder configure APIs advertised as live were stubs | **probes**: one app per shipped public API, run with an expected effect |
| nuru#296 | Living docs taught pre-3.0 APIs | **docs**: every snippet that builds an app is compiled against the package and reported as a doc defect |
| state#647 | A local pack poisoned the global NuGet cache under the release version | **cache**: every restored package is compared with the global cache entry |

## Run it

```bash
skills/tw-nuru-verify/scripts/nuru-consumer-check.sh                         # latest on nuget.org
skills/tw-nuru-verify/scripts/nuru-consumer-check.sh --version 4.0.0-beta.5  # a given release
```

Run it from a timewarp-nuru checkout with tags fetched (`git fetch --tags`). The docs step reads
the docs at tag `v<version>`, which are the docs a consumer of that version reads. The full run
takes about 90 seconds after the first restore.

| Option | Use |
|--------|-----|
| `--steps cache,probes,cli,docs` | Run a subset (launch always runs) |
| `--docs-strict` | Doc defects fail the run. Use for doc-changing tasks |
| `--docs readme` | Only the README snippets |
| `--docs-ref HEAD` | Check this branch's docs against the shipped package |
| `--source <feed> --version <v>` | A candidate in a local feed. The version must not exist on nuget.org |
| `--global-cache <dir>` | Audit a cache other than `$NUGET_PACKAGES` / `~/.nuget/packages` |
| `--keep` | Keep the scratch folder to rebuild a probe by hand |

Exit codes: `0` means launch, cache, probes and cli all ran and passed. Doc defects do not change
the exit code unless you pass `--docs-strict`, which makes them failing too. `1` means one of those
checks failed. `2` means doctor refused to run. No check is skipped: a check that cannot run in
this environment is a doctor refusal.

## What it does

**Doctor.** Before it runs, it refuses (exit 2) when:

- a `Directory.Build.*` or `Directory.Packages.props` sits above the scratch folder. MSBuild would
  import it and turn the consumer build into a repo build. A stale `/tmp/Directory.Build.props` from
  an agent session has done this before. Move the file or set `NURU_VERIFY_WORK`.
- `--source` is a local feed and the version is already on nuget.org. That is the state#647
  pattern. Pack candidates under a unique prerelease version instead.
- the version is not on nuget.org, or the docs tag is missing.
- a selected check cannot run: `script(1)` is missing for the REPL (pty) cases, `nc` is missing
  for the telemetry (otlp) case, or `cli` is selected without `probes` (it searches the
  core-routing probe app).

**Launch.** It creates a scratch folder under `~/.cache/nuru-verify/` with its own `nuget.config`
(`<clear/>` plus one source), `NUGET_PACKAGES` and HTTP cache. Then it runs `dotnet new console`
and `dotnet add package TimeWarp.Nuru --version <v>`, and records which source the package came
from. Nothing in the run writes to the global cache.

**Drive.**

- *cache*: compares the content hash of every package in the private cache (TimeWarp.Nuru and its
  whole dependency graph) with the global cache entry for the same id and version. A mismatch is
  reported as `POISONED`, with the global entry's source and the folder to delete.
- *probes*: builds each `probes/*.cs` as its own consumer project, in one parallel solution build.
  Then it runs the cases in `probes/cases.tsv`. Modes: `run` (plain process), `pty` (`script(1)`
  types into the REPL with `-i`), `otlp` (`nc` listens on the probe's literal endpoint and the
  capture must contain the service name).
- *cli*: `dotnet tool install TimeWarp.Nuru.Search` into the scratch folder, `nuru --version`, then
  `nuru index rebuild` and `nuru search` against the core-routing probe app.
- *docs* (a doc-defect report, not part of the release verdict): extracts every C# block that contains `CreateBuilder` from `readme.md` and
  `documentation/user/**`, and builds each one as a program. A block that calls
  `builder.Map("pattern", handler)` without `CreateBuilder` is built too, after a
  `var builder = NuruApp.CreateBuilder();` line, so the pre-3.0 two-argument `Map` meets a real
  receiver (`context` = `builder` in `snippets.tsv`). When a block is followed by
  `dotnet run -- <args>` and `# Output: <text>`, it runs the app and checks the output. Each
  snippet gets one of three results, and `broken` is a DOC-DEFECT row with `file:line`:
  - `compiles`
  - `broken`: the build fails on a Nuru API or on the package. Examples: a `NuruAppBuilder` member
    that does not exist, `NURU_S999 Unrecognized DSL method`, a route the analyzer rejects, an
    inaccessible package type, or a call shape that no longer exists. `CS1501` (wrong argument
    count, such as `Map` with 2 arguments) is always broken. `CS1061` and `CS0117` (missing member)
    are broken unless the receiver is a host extension point such as `IServiceCollection` or
    `ILoggingBuilder`, where docs call their own or another package's extensions.
  - `fragment`: it only lacks names it never defined, such as its own handlers, services or types.
    Generator errors that follow from those missing names count as fragment too.

**Evidence.** By default it writes to `artifacts/nuru-verify/<version>/`:

```
summary.md                    Release verdict table (launch, cache, probes, cli), then a separate
                              "Docs out of date with source" section: DOC-DEFECT rows with file:line
environment.txt               version, source, docs ref, dotnet, scratch paths
launch/*.log                  dotnet new / dotnet add package transcripts
launch/timewarp.nuru.nupkg.metadata
cache/global-cache-audit.tsv  per package: match / POISONED / not-cached, both hashes
probes/build.log              the solution build
probes/NN-<probe>.log         per case: "$ command", stdout, stderr, exit code
probes/<probe>.build-errors.txt
cli/*.log
docs/build.log, docs/snippets.tsv, docs/<snippet>.run.log
```

Every log has the command, stdout, stderr and the exit code. Paste `summary.md` into the PR or the
release notes.

**Cleanup.** It deletes the scratch folder unless you pass `--keep`. It does not change the global
cache. When the cache step reports `POISONED`, delete the folder it names, then restore again.

## Builder API coverage

Coverage comes from the shipped public API in `source/timewarp-nuru/`, not from the docs. Each
probe cites the source type and method it exercises. A public API without a probe is listed as
uncovered below, never silently omitted.

| Source API (file, type, method) | Probe | Observable effect |
|---------|-------|-------------------|
| `builders/nuru-app-builder/nuru-app-builder.routes.cs`, `NuruAppBuilder.Map`; `builders/endpoint-builder.cs`, `EndpointBuilder` `WithHandler`/`AsQuery`/`AsCommand`/`AsIdempotentCommand`/`Done`; typed, optional, option and catch-all parameters, exit codes | core-routing | output, exit code, `--capabilities` kind |
| `nuru-app-builder.cs`, `NuruAppBuilder.WithName`/`WithDescription`; built-in `--help`, `--version`, `--capabilities` | core-routing | text in output |
| `nuru-app-builder.routes.cs`, `NuruAppBuilder.WithGroupPrefix`; `builders/group-builder.cs`, `GroupBuilder.WithGroupPrefix` (nested) | group-prefix | grouped route runs |
| `nuru-app-builder.configuration.cs`, `NuruAppBuilder.ConfigureServices` | configure-services | injected service answers |
| `nuru-app-builder.configuration.cs`, `NuruAppBuilder.AddConfiguration` | add-configuration | env var reaches `IConfiguration` |
| `nuru-app-builder.cs`, `NuruAppBuilder.AddBehavior` | add-behavior | marker lines before and after the handler |
| `nuru-app-builder.routes.cs`, `NuruAppBuilder.AddTypeConverter` | type-converter | custom type binds |
| `completion/nuru-app-builder-extensions.cs`, `EnableCompletion` | enable-completion | `--generate-completion bash`, `__complete` |
| `nuru-app-builder.cs`, `NuruAppBuilder.ConfigureHelp` (row toggles, `ExcludePatterns`) | help-hide-rows, help-show-rows, help-exclude | `--help` changes with the options |
| `nuru-app-builder.routes.cs`, `NuruAppBuilder.Map<TEndpoint>()` with `[NuruRoute]` | endpoint-dsl | handler output |
| `nuru-app-builder.routes.cs`, `NuruAppBuilder.DiscoverEndpoints()` | discover-endpoints | two `[NuruRoute]` classes, no `Map<T>()`, both routes run |
| `nuru-app-builder.routes.cs`, `NuruAppBuilder.AddRepl(Action<ReplOptions>)` | repl | PTY session output |
| `nuru-app-builder.configuration.cs`, `NuruAppBuilder.UseTelemetry(Action<NuruTelemetryOptions>)` | telemetry | service name in the OTLP export |
| `nuru-app-builder.configuration.cs`, `NuruAppBuilder.UseLogging(ILoggerFactory)` | use-logging | handler's `ILogger<T>` reaches the factory |
| `logging/nuru-logging-extensions.cs`, `UseConsoleLogging` | use-console-logging | builds, route runs |
| `nuru-app-builder-extensions.updates.cs`, `NuruAppBuilderExtensions.AddCheckUpdatesRoute` | add-check-updates-route | builds, route runs |

Uncovered public APIs (no probe yet):

| Source API | Why uncovered |
|------------|---------------|
| `nuru-app-builder.configuration.cs`, `NuruAppBuilder.UseTerminal(ITerminal)` | test-only API, covered by the repo's `TestTerminal` tests |
| `logging/nuru-logging-extensions.cs`, `ConfigureLogging`, `UseDebugLogging` | not yet probed |
| `nuru-app-builder.configuration.cs`, `UseTelemetry()` (no options) | not yet probed |
| `nuru-app-builder.routes.cs`, `AddRepl()` (no options), `DiscoverEndpoints(params Type[])` | not yet probed |
| Public APIs in `timewarp-nuru-mcphost` and `timewarp-nuru-daemon` (`AddMcpServer`, `AddServeRoute`, ...) | separate packages; the probe projects reference `TimeWarp.Nuru` only |

When a source file has `#region Purpose` / `#region Design`, read it before adding or judging a
probe: it says what the API is meant to do.

To add a probe, drop `probes/<name>.cs` (a complete `Program.cs`) and add rows to
`probes/cases.tsv`:

```
probe  api  mode(run|pty|otlp)  env(KEY=VAL|-)  args  stdin(\n-separated|-)  exit  contains  not_contains
```

## Where it is invoked

- **tw-release, after publish (required).** Once `dev release` has pushed version V, run
  `nuru-consumer-check.sh --version V`. Paste `summary.md` into the release notes. A FAIL in the
  release verdict means the next beta fixes it. Doc defects go to a docs task; they do not block. A beta whose check fails is not promoted to stable.
- **tw-release, before promotion (required).** Before a stable release, the beta being promoted
  must pass. That beta is the last shipped build, so the check covers what stable will contain.
- **Doc-changing tasks (optional).** `--steps docs --docs-ref HEAD --docs-strict` checks a branch's
  docs against the latest shipped package and fails on doc defects. A snippet for an API that has not shipped yet fails until it ships.

It is not a per-task ganda walk step. A walk changes source and the check needs a published
package. A run also costs about 90 seconds and network access, and catches nothing the branch can
change, except docs.

## Troubleshooting

| Symptom | Cause |
|---------|-------|
| doctor: `Directory.Build.props would be imported` | Move the stray file, or set `NURU_VERIFY_WORK` to a folder with no MSBuild files above it |
| every probe fails with CS9137 interceptors | The package does not enable `InterceptorsNamespaces` for consumers. This is a real package defect |
| doctor: `script(1)` / `nc` is not on PATH | Install util-linux (`script`) or netcat (`nc`). Without them the REPL and telemetry cases cannot run, and a run that skipped them would not prove anything |
| REPL case times out | The REPL needs a terminal; the pty mode waits 3s for startup. Raise the sleeps in the script on a slow machine |
| telemetry case: missing service name | The app sent nothing to the listener. Check `probes/telemetry.otlp-capture.bin` |
