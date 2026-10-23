# Nuru verification skill: launch and drive the real CLI as a consumer, with evidence

## Description

Write a hand-authored verification skill for Nuru, modelled on the pstack `/create-verification-skill` pattern (Launch, Drive, Evidence, with Doctor and Cleanup only if needed). Do not run pstack's meta-skill and do not install the pstack plugin. Source: timewarp-intel evaluation 2026-10-21 poteto pstack guide, action B (Busara verdict REWORK), and research 2026-10-23 sdlc-factory-vs-ganda-walk (gap 1).

Busara's condition for B was to name one real failure that slipped past the existing checks (tests, REPL, `--capabilities` checks), and to build the skill around that flow. Gap 1 names the failures:

- nuru#242: builder configure APIs were advertised as live but were stubs.
- nuru#296: living docs still taught pre-3.0 APIs.
- state#647 (2026-10-13): local-feed packs poison the global NuGet cache under the release version, a consumer-side break that diff review cannot see.

Scope added 2026-10-23 on Steven's yes: a "use it like a consumer" check. An agent installs the shipped package and CLI the way a consumer would (from a clean NuGet cache or a clean temp feed, never from a local pack under the release version), then uses it end to end and records evidence. The check must be able to catch each of the three failure classes above:

- Advertised-but-stubbed APIs: call every public builder API the docs and README advertise and confirm it does something real.
- Stale docs: build and run the snippets from the README and living docs against the packed package and confirm they compile and behave as written.
- Cache poisoning: verify the consumer restore resolves the package from the intended source and version, and that a local pack does not shadow the release version in the global NuGet cache.

## Requirements

- The skill lives in `skills/` of this repo next to `tw-nuru` and `tw-cli-search`, follows the skill conventions, and is loadable by any bot.
- Launch: restore and run the shipped package and CLI from an isolated cache location, no repo project references.
- Drive: run real commands with a PTY or plain process, covering the three failure classes above.
- Evidence: write transcripts and exit codes to a path a PR description can quote.
- Doctor and Cleanup only if a real flow needs them.
- Decide where the check is invoked: as a ganda task-walk step, as a pre-release gate in `tw-release`, or both. Write the decision and its reason in Notes.

## Goal (implementer restatement)

Give any bot a hand-written skill, kept in this repo's `skills/`, that checks a *shipped* Nuru
release the way a consumer uses it. It installs the published package and the `nuru` CLI from a
clean, private NuGet cache, never from a repo project or a local pack under the release version.
It uses them for real and writes transcripts and exit codes a PR can quote.

Acceptance:

1. The check catches all three failure classes that got past tests: stubbed builder APIs
   (nuru#242), stale docs (nuru#296), and a poisoned global NuGet cache (state#647).
2. Proof it can fail: at least one of those reproduced against a shipped build, with evidence.
3. It runs on the current release, with the evidence attached.
4. Doctor and Cleanup appear only because a real flow needs them.
5. Notes record where it is invoked (task walk, tw-release, or both) and why.

## Checklist

- [x] Reproduce one of nuru#242, nuru#296 or state#647 against a shipped build as proof the check can fail (all three)
- [x] Draft the skill under `skills/` using only the steps it needs
- [x] Run it on the current release and attach the evidence
- [x] Confirm it catches the reproduced failure
- [x] Decide and document where it is invoked
- [ ] Open a PR with the evidence in the description (host open-pr node)

## Session

- Created: 2156863 (2026-10-23)
- 2026-10-23 implementer-claude (ganda task work, headless): wrote `skills/tw-nuru-verify/`
  (SKILL.md, `scripts/nuru-consumer-check.sh`, 14 probes plus `probes/cases.tsv`). Ran it on
  4.0.0-beta.5 (current) and 3.0.0-beta.76 (before the #242 and #296 fixes), and on a scratch
  cache poisoned the state#647 way. Evidence is under `evidence/`. `ganda repo audit` passed
  (30/0/1 skip). Nothing to fix.

## Notes

- Origin: Steven approved the consumer-use scope on 2026-10-23, relayed by Amina; Steven confirmed publishing this task in chat the same day.
- The earlier REWORK verdict said to park if no missed failure could be named. nuru#242, nuru#296 and state#647 satisfy that condition.
- Authoring the skill itself still needs an owner and Steven's go-ahead before the task is claimed for work.
  (Dispatched with `ganda task work 523` on 2026-10-23, which is the go-ahead.)
- **Where it is invoked: tw-release, not the task walk.** Run it after `dev release` publishes V
  (`--version V`), paste `summary.md` into the release notes, and do not promote a beta whose check
  fails. The check needs a *published* package, and a walk only has source. It costs about 90 s plus
  network, and a code branch cannot change its result. The exception is docs, so doc-changing
  tasks may run `--steps docs --docs-ref HEAD` (optional). The tw-release skill is authored in
  timewarp-flow, so wiring the step in there is a follow-up in that repo.
- **Why bash, not a runfile.** The driver must not pick up this repo's MSBuild context (a runfile
  under `skills/` would import `Directory.Build.props` and `Directory.Packages.props` with CPM). It
  also has to run wherever `ganda skills sync` copies the skill. Every step is a `dotnet` CLI call
  a consumer would type anyway.
- **Doctor and Cleanup earn their place.** Doctor refuses when a stray `Directory.Build.*` sits
  above the scratch folder (the stale `/tmp` props seen in 508-013), and when a local feed would
  serve a version nuget.org already has (the state#647 pattern). Cleanup removes the scratch
  folder. The global cache is never written.

## Results

### What shipped

- `skills/tw-nuru-verify/SKILL.md`: when to run it, Launch / Drive / Evidence / Doctor / Cleanup,
  the builder API coverage table, where it is invoked, troubleshooting.
- `skills/tw-nuru-verify/scripts/nuru-consumer-check.sh`: the driver. Steps:
  - **launch**: `dotnet new console` and `dotnet add package` into a private `NUGET_PACKAGES` with a
    `<clear/>` nuget.config, and a record of the source the package resolved from.
  - **cache**: compares content hashes, private cache against global cache, for the whole graph.
  - **probes**: one consumer app per advertised builder API, built in parallel, run as a plain
    process, as a PTY via `script(1)` for the REPL, or against an `nc` OTLP listener for telemetry.
  - **cli**: `dotnet tool install TimeWarp.Nuru.Search`, `nuru --version`, `index rebuild`,
    `search`.
  - **docs**: compiles every `CreateBuilder` snippet in `readme.md` and `documentation/user/**`
    at tag `v<version>`, and checks `dotnet run -- …` / `# Output:` pairs.
- `skills/tw-nuru-verify/probes/*.cs` + `cases.tsv`: 14 probes, 31 cases.
- `evidence/` in this task folder: summaries and TSVs from the three runs below.

### Proof the check fails on each known failure (shipped builds)

| Failure | Run | What the check reported |
|---------|-----|-------------------------|
| nuru#242 stub configure APIs | `--version 3.0.0-beta.76` | `help-hide-rows`, `help-show-rows`: `NURU_S999: Unrecognized DSL method: ConfigureHelp` (the interpreter fix in #242). `help-exclude`: `CS8852 init-only HelpOptions.ExcludePatterns` (the setter fix in #242) |
| nuru#296 stale docs | `--version 3.0.0-beta.76` (docs at `v3.0.0-beta.76`) | 104 broken snippets: `CreateBuilder(args)` (CS1501), `NuruAppOptions` (CS0246), `TimeWarp.Nuru.Logging` / `.Completion` (CS0234), `WithGroup` (CS1061) |
| state#647 cache poisoning | repo packed as 4.0.0-beta.5 into a scratch feed, restored into a scratch global cache, `--steps cache --global-cache <scratch>` | `cache FAIL: global cache timewarp.nuru 4.0.0-beta.5 … global entry came from …/feed`. The audit TSV shows both hashes. Doctor also refuses `--source <feed> --version 4.0.0-beta.5` (exit 2) |

The beta.76 run also showed that every consumer build failed with `CS9137 interceptors feature is
not enabled`: the package did not set `InterceptorsNamespaces` for consumers. Repo tests could
not see this, because the repo's `Directory.Build.props` sets the property. 4.0.0-beta.5 no longer
has the problem (core probes compile).

### Current release: 4.0.0-beta.5, 36 passed / 18 failed

Passed: launch from nuget.org; cache (44 packages match); 29 of 31 probe cases (routing,
groups, DI, configuration, behaviors, converters, completion, ConfigureHelp ×3, Endpoint DSL, REPL
over PTY, telemetry export); the CLI install / version / index / search; README quick start
`dotnet run -- add 15 25` prints `15 + 25 = 40`.

The failures are real consumer-visible defects in the current release. Each was confirmed by
hand, not just reported by the script:

1. **`UseLogging` / `UseConsoleLogging` don't compile.** They are advertised in builder-api.md,
   logging.md and output-handling.md, and the generator rejects them with `NURU_S999 Unrecognized
   DSL method` (probe `use-logging` plus 6 doc snippets). This is the #242 class.
2. **The MCP hosting snippet doesn't compile.** mcp-hosting.md#L50
   (`builder.Services.AddMcpServer()…WithNuruTools(nuruApp)`) fails with `NURU_S999: Unrecognized
   DSL method: AddMcpServer`, even in a real `Microsoft.NET.Sdk.Web` project with
   TimeWarp.Nuru.McpHost and ModelContextProtocol.AspNetCore. The generator misreads the ASP.NET
   builder chain.
3. **`AddCheckUpdatesRoute()` doesn't compile for a consumer** (built-in-routes.md#L157):
   `CS0122 CheckUpdatesJsonSerializerContext is inaccessible`, because the generated code uses an
   internal type.
4. **The README still teaches `NuruApp.CreateBuilder(args)` and `EnableStaticCompletion()`**
   (readme.md#L176, #L246). The #296 sweep fixed `documentation/` but missed the README.
5. **Other doc bugs:**
   - best-practices.md#L28 uses `WithGroup`, which does not exist.
   - routing.md#L130 shows `Map("")` next to `{*args}`, which NURU_R003 rejects as unreachable.
   - supported-types.md#L28/#L49/#L109 name handler parameters differently from their route
     segments (`{message}` vs `msg`, NURU_H005).

Found while building the probes. These are not FAILs in the summary; they are recorded for
follow-up:

6. `UseTelemetry(o => o.OtlpEndpoint = someVariable)` is silently ignored. The generator only
   reads string literals and falls back to `OTEL_EXPORTER_OTLP_ENDPOINT`, with no diagnostic.
7. The telemetry.md `TelemetryBehavior` sample uses `new ActivitySource("MyApp.Commands")`. The
   generated tracer only subscribes `TimeWarp.Nuru` and `TimeWarp.Nuru.Behavior`, so a reader who
   follows the sample exports no spans. The shipped `TimeWarp.Nuru.TelemetryBehavior` does work
   (the probe uses it).
8. `nuru search --cli <exe>` filters by the app's `WithName`, not the executable name. Indexing
   `core-routing` (named `probe`) and then searching `--cli core-routing` finds nothing.
9. `TimeWarp.Build.Tasks` flows transitively into consumers. Outside a git repo it warns `Failed
   to retrieve git metadata` (the build still succeeds, even with `TreatWarningsAsErrors`).
10. REPL type-ahead sent before startup loses its Enter key. A person typing after the prompt
    appears is fine.
11. When `HOME` points at a folder that does not exist, Nuru writes `.nuru/` (REPL history,
    search index) into the current directory: `GetFolderPath(UserProfile)` returns "". The first
    commit on this branch picked those files up from a run. The script now creates its scratch
    HOME, and a follow-up commit removed the files.

Suggested follow-up tasks, not filed from this headless walk: one for 1+2+3 (generator /
package), one docs sweep for 4+5+7, and one each for 6, 8 and 9. Also wire the release step into
tw-release (timewarp-flow), and run `ganda skills add tw-nuru-verify <path>` after merge so the
skill syncs.

### How to validate

**Smoke**

```bash
cd /home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/task-523-nuru-verification-skill-launch-and-drive-the-real
git fetch --tags
skills/tw-nuru-verify/scripts/nuru-consumer-check.sh --version 4.0.0-beta.5; echo "exit=$?"
skills/tw-nuru-verify/scripts/nuru-consumer-check.sh --version 3.0.0-beta.76 --steps cache,probes,docs; echo "exit=$?"
skills/tw-nuru-verify/scripts/nuru-consumer-check.sh --version 4.0.0-beta.5 --source /tmp --steps cache; echo "exit=$?"
```

**Expect**

- Run 1: `exit=1`, `artifacts/nuru-verify/4.0.0-beta.5/summary.md` shows 36 passed / 18 failed.
  The FAILs are exactly items 1–5 above (`use-logging`, logging/output-handling/mcp-hosting/
  built-in-routes/routing/best-practices/supported-types/readme snippets). Launch, cache, cli, the
  REPL and telemetry probes PASS.
- Run 2: `exit=1`. The summary lists `help-hide-rows` / `help-show-rows` failing on `NURU_S999
  ConfigureHelp`, `help-exclude` on `CS8852`, and about 100 broken doc snippets (`CreateBuilder`
  takes 1 arguments, `NuruAppOptions`).
- Run 3: `exit=2`, with `doctor: 4.0.0-beta.5 is a released version on nuget.org; pack local
  candidates under a unique prerelease version`.
- `~/.nuget/packages` is unchanged by all three runs. `~/.cache/nuru-verify/` is empty afterwards.
