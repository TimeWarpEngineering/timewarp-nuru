---
name: tw-nuru-verify
description: Verify a shipped TimeWarp.Nuru release the way a consumer uses it - restore the package and the nuru CLI from a clean cache, drive every advertised builder API, compile the docs' snippets, audit the global NuGet cache, and write evidence a PR or release note can quote. Use after publishing a Nuru release, before promoting a beta, when docs or builder APIs changed, or when asked to "verify the release", "check Nuru as a consumer", or "run the consumer check".
---

# Nuru consumer verification

Repo tests build against project references, with this repo's `Directory.Build.props`. A consumer
gets none of that. They run `dotnet add package TimeWarp.Nuru` and copy a snippet from the README.
This skill does the same thing and records what happened.

It exists because of three failures that got past tests, the REPL and `--capabilities`:

| Failure | Class | What catches it here |
|---------|-------|----------------------|
| nuru#242 | Builder configure APIs advertised as live were stubs | **probes**: one app per advertised API, run with an expected effect |
| nuru#296 | Living docs taught pre-3.0 APIs | **docs**: every snippet that builds an app is compiled against the package |
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
| `--docs readme` | Only the README snippets |
| `--docs-ref HEAD` | Check this branch's docs against the shipped package |
| `--source <feed> --version <v>` | A candidate in a local feed. The version must not exist on nuget.org |
| `--global-cache <dir>` | Audit a cache other than `$NUGET_PACKAGES` / `~/.nuget/packages` |
| `--keep` | Keep the scratch folder to rebuild a probe by hand |

Exit codes: `0` every check passed, `1` a check failed, `2` doctor refused to run.

## What it does

**Doctor.** Before it runs, it refuses (exit 2) when:

- a `Directory.Build.*` or `Directory.Packages.props` sits above the scratch folder. MSBuild would
  import it and turn the consumer build into a repo build. A stale `/tmp/Directory.Build.props` from
  an agent session has done this before. Move the file or set `NURU_VERIFY_WORK`.
- `--source` is a local feed and the version is already on nuget.org. That is the state#647
  pattern. Pack candidates under a unique prerelease version instead.
- the version is not on nuget.org, or the docs tag is missing.

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
- *docs*: extracts every C# block that contains `CreateBuilder` from `readme.md` and
  `documentation/user/**`, and builds each one as a program. When a block is followed by
  `dotnet run -- <args>` and `# Output: <text>`, it runs the app and checks the output. Each
  snippet gets one of three results:
  - `compiles`
  - `broken`: the build fails on a Nuru API or on the package, for example a `NuruAppBuilder` member
    that does not exist, `NURU_S999 Unrecognized DSL method`, a route the analyzer rejects, or an
    inaccessible package type
  - `fragment`: it only lacks names it never defined, such as its own handlers, services or types.
    Generator errors that follow from those missing names count as fragment too.

**Evidence.** By default it writes to `artifacts/nuru-verify/<version>/`:

```
summary.md                    Markdown table: step, check, PASS/FAIL, evidence file, detail
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

`documentation/user/reference/builder-api.md` and the README are the source of truth for what is
advertised. Each advertised API has a probe or a stated reason it has none:

| API | Probe | Observable effect |
|-----|-------|-------------------|
| `Map().WithHandler().AsQuery/AsCommand/AsIdempotentCommand().Done()`, typed, optional, option and catch-all parameters, exit codes | core-routing | output, exit code, `--capabilities` kind |
| `WithName`, `WithDescription`, `--help`, `--version`, `--capabilities` | core-routing | text in output |
| `WithGroupPrefix` (nested) | group-prefix | grouped route runs |
| `ConfigureServices` | configure-services | injected service answers |
| `AddConfiguration` | add-configuration | env var reaches `IConfiguration` |
| `AddBehavior` | add-behavior | marker lines before and after the handler |
| `AddTypeConverter` | type-converter | custom type binds |
| `EnableCompletion` | enable-completion | `--generate-completion bash`, `__complete` |
| `ConfigureHelp` (row toggles, `ExcludePatterns`) | help-hide-rows, help-show-rows, help-exclude | `--help` changes with the options |
| `[NuruRoute]` + `Map<T>()` (README Endpoint DSL) | endpoint-dsl | handler output |
| `AddRepl(options)` | repl | PTY session output |
| `UseTelemetry(options)` + `TelemetryBehavior` | telemetry | service name in the OTLP export |
| `UseLogging` | use-logging | handler's `ILogger<T>` reaches the factory |
| `DiscoverEndpoints()` | none, docs step | README snippet compiles |
| `UseTerminal` | none | test-only API, covered by the repo's `TestTerminal` tests |

To add a probe, drop `probes/<name>.cs` (a complete `Program.cs`) and add rows to
`probes/cases.tsv`:

```
probe  api  mode(run|pty|otlp)  env(KEY=VAL|-)  args  stdin(\n-separated|-)  exit  contains  not_contains
```

## Where it is invoked

- **tw-release, after publish (required).** Once `dev release` has pushed version V, run
  `nuru-consumer-check.sh --version V`. Paste `summary.md` into the release notes. A FAIL means the
  next beta fixes it. A beta whose check fails is not promoted to stable.
- **tw-release, before promotion (required).** Before a stable release, the beta being promoted
  must pass. That beta is the last shipped build, so the check covers what stable will contain.
- **Doc-changing tasks (optional).** `--steps docs --docs-ref HEAD` checks a branch's docs against
  the latest shipped package. A snippet for an API that has not shipped yet fails until it ships.

It is not a per-task ganda walk step. A walk changes source and the check needs a published
package. A run also costs about 90 seconds and network access, and catches nothing the branch can
change, except docs.

## Troubleshooting

| Symptom | Cause |
|---------|-------|
| doctor: `Directory.Build.props would be imported` | Move the stray file, or set `NURU_VERIFY_WORK` to a folder with no MSBuild files above it |
| every probe fails with CS9137 interceptors | The package does not enable `InterceptorsNamespaces` for consumers. This is a real package defect |
| REPL case times out | The REPL needs a terminal; the pty mode waits 3s for startup. Raise the sleeps in the script on a slow machine |
| telemetry case: missing service name | No `nc`, or the app sent nothing. Check `probes/telemetry.otlp-capture.bin` |
