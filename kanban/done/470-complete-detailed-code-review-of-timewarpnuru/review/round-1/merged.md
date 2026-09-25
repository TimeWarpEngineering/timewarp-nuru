# Round 1 — merged findings
**Date:** 2026-09-04
**Sources:** core-runtime, repl-completion, analyzers-generators, parsing, aux, tests-infra, security
**Pinned tree:** origin-home `38480f57` / product `648369f6` (`3.0.0-beta.77`)

Orchestrator re-verified the highest-severity claims against the current tree (builder stubs, `HandleCharacter` slice, `ConsumeDescription` stop set, NuGet fail-open, `GetTypeInfo`-only `Map<T>`, history `WriteAllLines`, CI standalone list, search `--version` formatter, MCP `Uri` join). Parsing crash-safety: 50k patterns, seed `470001`, 0 uncaught exceptions.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 17 | 0 | 0 |
| suggestion | 15 | 0 | 1 |
| nit | 11 | 0 | 0 |

**454-019** is still present (`repl-console-reader.cs:279`, `:310`) and is **not** given a new M#. Tracked by `kanban/to-do/454-019-fix-redraw-of-lines-longer-than-terminal-width.md`.

## Issues

### M1 — Severity: bug — Status: open
- File: `source/timewarp-nuru/builders/nuru-app-builder/nuru-app-builder.configuration.cs:179-185`
- Description: `UseTelemetry(Action<NuruTelemetryOptions>)` is a compile-time stub that never invokes `configure`. DSL dispatch (`dsl-interpreter.cs:1424-1434`) only calls parameterless `UseTelemetry()`. Emitted setup reads `OTEL_*` env vars only (`telemetry-emitter.cs:42-48`). Documented `NuruTelemetryOptions` properties are never applied.
- Suggestion: Extract the lambda into IR, or obsolete the `Action<>` overload until options are wired.
- Source: core-runtime
- Disposition notes: filed as **470-001** (`--parent 470`)

### M2 — Severity: bug — Status: open
- File: `source/timewarp-nuru/help/help-options.cs:9-37`
- Description: `ConfigureHelp` mutates a builder field (`nuru-app-builder.cs:90-95`) but no emitter reads `ShowPerCommandHelpRoutes` / `ShowReplCommandsInCli` / `ShowCompletionRoutes` / `ExcludePatterns`. Help is now always enabled (`nuru-generator.cs:373-377` `HasHelp = true`). Generator locator/interpreter still look for removed `AddHelp` (`add-help-locator.cs:12`, `dsl-interpreter.cs:586`). User docs advertise `.ConfigureHelp(options => …)` (`documentation/user/reference/builder-api.md:203-206`).
- Suggestion: Wire `HelpOptions` into `HelpModel` + `HelpEmitter`, and locate `ConfigureHelp` instead of `AddHelp` — or stop advertising filtering as live.
- Source: core-runtime
- Disposition notes: filed as **470-001** (`--parent 470`)

### M3 — Severity: bug — Status: open
- File: `source/timewarp-nuru/builders/nuru-app-builder/nuru-app-builder.cs:26-37`
- Description: Public `Services` getter always throws, telling the caller to `Call AddDependencyInjection() first`. That method does not exist (opt-in is `UseMicrosoftDependencyInjection`). `ServiceCollection` is never assigned.
- Suggestion: Populate `ServiceCollection` when runtime DI is enabled and fix the exception text, or obsolete `Services` in favor of `ConfigureServices`.
- Source: core-runtime
- Disposition notes: filed as **470-001** (`--parent 470`)

### M4 — Severity: bug — Status: open
- File: `source/timewarp-nuru/repl/input/repl-console-reader.cs:226-228`
- Description: `HandleCharacter` replaces an active selection with unclamped `SelectionState.Start`/`End`. History, kill-ring, and undo/redo replace/shorten `UserInput` without clearing selection. Repro: select suffix, `Ctrl+K`, type → `ArgumentOutOfRangeException`. 454-020 clamped cut/paste/delete only.
- Suggestion: `GetClampedBounds` in `HandleCharacter`; clear selection on every non-selection buffer replace.
- Source: repl-completion
- Disposition notes: filed as **470-002** (`--parent 470`)

### M5 — Severity: bug — Status: open
- File: `source/timewarp-nuru/repl/input/repl-console-reader.selection.cs:231-233`
- Description: Windows clipboard paste leaves `\r` in `UserInput` and advances `CursorPosition` by raw length, disagreeing with the multiline `\n` linear domain (454-007 contract). Distinct from 454-007 (GetFullText/`SyncFromMultilineBuffer`).
- Suggestion: Normalize clipboard newlines to `\n` before updating `UserInput`/`CursorPosition`; sync from the multiline buffer after paste.
- Source: repl-completion
- Disposition notes: filed as **470-003** (`--parent 470`)

### M6 — Severity: bug — Status: open
- File: `source/timewarp-nuru-parsing/parsing/parser/parser.cs:180`
- Description: `ConsumeDescription(stopAtRightBrace: false)` does not stop on `EndOfOptions`. `--` after `|desc` is skipped. `run --flag|desc -- {*args}` fails; `cmd --opt | a -- b {x}` succeeds with a corrupted AST (EndOfOptions dropped, `{x}` attached as option value).
- Suggestion: Add `RouteTokenType.EndOfOptions` to the option-description stop set. Regression tests for both shapes.
- Source: parsing
- Disposition notes: filed as **470-004** (`--parent 470`)

### M7 — Severity: bug — Status: open
- File: `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:764`
- Description: `ExtractGenericTypeArgument` (`Map<T>`) uses only `GetTypeInfo().Type`. Same pattern at `AddBehavior(typeof)` (`:1289-1290`), `implements-extractor.cs:62-65`, `service-extractor.cs:541-555` and `:1027-1036`. Repo convention / 454-012 AddTypeConverter path uses `GetSymbolInfo` first because referenced-project types can yield null `GetTypeInfo`. Fail-soft then drops the endpoint.
- Suggestion: Mirror AddTypeConverter: `GetSymbolInfo` then `GetTypeInfo`, reject `TypeKind.Error`.
- Source: analyzers-generators
- Disposition notes: filed as **470-005** (`--parent 470`)

### M8 — Severity: bug — Status: open
- File: `source/timewarp-nuru-analyzers/generators/extractors/service-extractor.cs:409-419`
- Description: `GetDefaultValueExpression` wraps strings as `$"\"{s}\""` with no escaping; enum/other defaults use unqualified `ToString()`. Endpoint property defaults already use `SymbolDisplay.FormatLiteral` (`endpoint-extractor.cs:977-980`). Quote/backslash defaults emit non-compiling interceptor code.
- Suggestion: `SymbolDisplay.FormatPrimitive` / fully-qualified enum members. Generator-hosted regression.
- Source: analyzers-generators
- Disposition notes: filed as **470-006** (`--parent 470`)

### M9 — Severity: bug — Status: open
- File: `source/timewarp-nuru-devcli/content/any/services/nuget-version-service.cs:43-46`
- Description: `GetPackageVersionsAsync` returns `[]` for every non-success HTTP status. 429/5xx are treated as “never published” → check-version/release fail-open (`check-version-command.cs:117-120`, `release-command.cs:307-317`).
- Suggestion: Treat only 404 (maybe 400) as empty; other statuses fail-closed. Dispose `HttpResponseMessage`.
- Source: aux
- Disposition notes: filed as **470-007** (`--parent 470`)

### M10 — Severity: bug — Status: open
- File: `source/timewarp-nuru-devcli/content/any/endpoints/check-version-command.cs:202-203`
- Description: `GetVersionFromSource` does not `.Trim()` `<Version>`. `release-command.cs:464-465` and workflow `ReadPropsVersion` do. Whitespace in the element makes check-version miss a published version.
- Suggestion: Share one trimmed props-version reader.
- Source: aux
- Disposition notes: filed as **470-007** (`--parent 470`)

### M11 — Severity: bug — Status: open
- File: `source/timewarp-nuru-search/endpoints/search-query.cs:9-10,70`
- Description: `--version` is documented as “Show CLI version in results” but prints `result.Endpoint.Kind`. `SearchResult` has no version field (lives on `CliInfo` / `clis` table).
- Suggestion: Select `clis.version` onto `SearchResult` and print that.
- Source: aux
- Disposition notes: filed as **470-008** (`--parent 470`)

### M12 — Severity: bug — Status: open
- File: `source/timewarp-nuru-search/services/search-index.cs:295-309` (crash `:273`)
- Description: Residual of 454-023: `SanitizeFtsQuery` does not strip U+0000. `hello\0world` → FTS5 `unterminated string`; `SearchAsync` does not catch `SqliteException`.
- Suggestion: Strip/reject NUL (and C0 controls) in the sanitizer and/or catch `SqliteException` around MATCH.
- Source: aux
- Disposition notes: filed as **470-008** (`--parent 470`)

### M13 — Severity: bug — Status: open
- File: `source/timewarp-nuru-mcp/tools/get-example-tool.cs:105-110` (also `github-cache-service.cs:78-83`)
- Description: `new Uri(base + path)` with untrusted manifest `Path` lets `..` leave `…/master/` on `raw.githubusercontent.com`. Cache files use raw ids (`Path.Combine(CacheDirectory, $"{name}.cache")`) so `../` in an id can escape the cache dir. MCP is frozen for features; this is a correctness/security bug.
- Suggestion: Reject `..` / absolute URIs; require `samples/` (or allowlisted) prefix; assert resolved URI stays under this repo; hash cache names.
- Source: aux, security
- Disposition notes: filed as **470-009** (`--parent 470`)

### M14 — Severity: bug — Status: open
- File: `tests/ci-tests/Directory.Build.props:24-26,38` and `tests/ci-tests/run-ci-tests.cs:19-35`
- Description: `generator-17-local-function-config.cs` is `CiTestExcludes` with “run standalone” but is not in `standaloneTests`. 454-001 class: committed test never runs in CI.
- Suggestion: Add it to `standaloneTests` (or make the scenario multi-mode-safe).
- Source: tests-infra
- Disposition notes: filed as **470-010** (`--parent 470`)

### M15 — Severity: bug — Status: open
- File: `tests/timewarp-nuru-tests/devcli/check-version-04-endpoint-zero-package.cs:3-12`
- Description: Entire file is `#if !JARIBU_MULTI` so multi-mode compiles it as a no-op; CI second phase does not invoke it. Endpoint coverage for delimiter-only `--package` (458-005) never runs on the CI path.
- Suggestion: Append to `standaloneTests`; list in `CiTestExcludes` (or document the `#if` inert pattern).
- Source: tests-infra
- Disposition notes: filed as **470-010** (`--parent 470`)

### M16 — Severity: bug — Status: open
- File: `benchmarks/aot-benchmarks/run-benchmark.sh:13`
- Description: Script runs `publish/bench-nuru-full/bench-nuru-full`; on-disk project is `bench-nuru` (`AssemblyName` `bench-nuru`).
- Suggestion: Point the hyperfine entry at `publish/bench-nuru/bench-nuru`.
- Source: tests-infra
- Disposition notes: filed as **470-012** (`--parent 470`)

### M17 — Severity: bug — Status: open
- File: `source/timewarp-nuru/repl/repl-history.cs:187`
- Description: `File.WriteAllLines` with default umask `022` yields world-readable (`0644`) history under `~/.nuru/history/<app>`. `PersistHistory` defaults true. Residual secrets that miss ignore patterns are world-readable on multi-user hosts. Confirmed on this machine.
- Suggestion: Create the file with owner-only mode; `0700` on `~/.nuru` / `history` when creating them. `repl-03b` assertion on mode.
- Source: security
- Disposition notes: filed as **470-011** (`--parent 470`)

### M18 — Severity: suggestion — Status: open
- File: `source/timewarp-nuru/repl/key-bindings/default-key-binding-profile.cs:25` (missing from emacs/vi/vscode profiles)
- Description: `Shift+Enter` → `HandleAddLineAsync` is Default-profile only. On Emacs/Vi/VSCode the chord is swallowed.
- Suggestion: Bind the same chord on the other profiles (or document a substitute).
- Source: repl-completion
- Disposition notes: filed as **470-013** (`--parent 470`)

### M19 — Severity: suggestion — Status: open
- File: `source/timewarp-nuru/completion/completion/templates/bash-completion-dynamic.sh:31,37`
- Description: `COMPREPLY=($(compgen -W "${suggestions[*]}" -- "$cur"))` word-splits spaced/glob candidates. pwsh/fish were hardened in 454-030; bash was not.
- Suggestion: Quoting-safe candidate feed; no unquoted command substitution.
- Source: repl-completion
- Disposition notes: filed as **470-013** (`--parent 470`)

### M20 — Severity: suggestion — Status: wontfix
- File: `source/timewarp-nuru-analyzers/generators/emitters/service-resolver-emitter.cs:349-377`
- Description: Source-gen DI cannot resolve `IEnumerable<T>` multi-implementation constructor deps (epic 391 leftover). Runtime DI works; `generator-27` is runtime-DI-only.
- Suggestion: Emit aggregated arrays or an explicit diagnostic. Track under epic 391, not a 470 child.
- Source: analyzers-generators
- Disposition notes: wontfix on 470 — owned by in-progress epic 391 (`kanban/in-progress/391-epic-full-di-support-sourcegen-and-runtime.md`). Decided by review oracle 2026-09-04.

### M21 — Severity: suggestion — Status: open
- File: `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:771-778`
- Description: Emitted `FileInfo`/`DirectoryInfo` conversions catch only `ArgumentException`; constructors can also throw `PathTooLongException` / `NotSupportedException`.
- Suggestion: Catch the documented constructor exception set (or `Exception`) to keep the soft-fail path.
- Source: analyzers-generators
- Disposition notes: filed as **470-006** (`--parent 470`)

### M22 — Severity: suggestion — Status: open
- File: `source/timewarp-nuru-parsing/parsing/parser/parser.validation.cs:75-85`
- Description: Parameter names allow hyphens (`{my-param}` parses) contrary to documented “alphanumeric + underscore”. Type constraints already reject hyphens. Cannot bind to a legal C# parameter.
- Suggestion: Validate parameter names with `IsValidIdentifierFormat`; keep hyphens legal for option names only.
- Source: parsing
- Disposition notes: filed as **470-004** (`--parent 470`)

### M23 — Severity: suggestion — Status: open
- File: `source/timewarp-nuru-devcli/content/any/services/packable-project-service.cs:126-140,78-88`
- Description: MSBuild exit 0 with unparseable JSON silently omits the project from the packable set.
- Suggestion: Throw naming the project (same fail-loud posture as blank PackageId / duplicates).
- Source: aux
- Disposition notes: filed as **470-014** (`--parent 470`)

### M24 — Severity: suggestion — Status: open
- File: `source/timewarp-nuru-build/GenerateNuruJsonContextTask.cs:58-70`
- Description: Any exception in `ExecuteCore` is a warning + `return true` with empty `GeneratedFiles` (intentional fail-soft). Unexpected bugs become silent ToString fallback.
- Suggestion: Keep fail-soft for “no DSL”; fail or emit a highly visible diagnostic for unexpected exceptions.
- Source: aux
- Disposition notes: filed as **470-014** (`--parent 470`)

### M25 — Severity: suggestion — Status: open
- File: `tests/ci-tests/Directory.Build.props:60,66`
- Description: search/mcp test globs are non-recursive `*.cs` while the main tree is `**/*.cs`. Nested files would be silently skipped. New `tests/<name>-tests` roots still need a manual glob.
- Suggestion: Switch to `**/*.cs`; optional CI drift guard for unmatched `*-tests/**/*.cs`.
- Source: tests-infra
- Disposition notes: filed as **470-012** (`--parent 470`)

### M26 — Severity: suggestion — Status: open
- File: `source/timewarp-nuru/internals-visible-to.g.cs:2` (siblings under parsing/mcp)
- Description: Committed IVT lists miss 24 current test stems (including standalone `generator-39/40/42` and newer `devcli/*`). Generator banner still says `scripts/generate-internals-visible-to.cs` (actual `runfiles/`). Multi-mode uses assembly name `ci-tests` (present), so CI may still pass.
- Suggestion: Re-run `dotnet run runfiles/generate-internals-visible-to.cs` and commit; fix banner path.
- Source: tests-infra
- Disposition notes: filed as **470-012** (`--parent 470`)

### M27 — Severity: suggestion — Status: open
- File: `tests/scripts/run-nuru-tests.cs:58-71`; `run-all-tests.cs:138-139,206`; `run-mcp-tests.cs:32-36`
- Description: Legacy hand-list runners point at deleted dirs / omit mcp-06/07 / still run mcp-02. Official CI is `run-ci-tests.cs` only.
- Suggestion: Delete or rewrite to delegate to `run-ci-tests.cs`.
- Source: tests-infra
- Disposition notes: filed as **470-012** (`--parent 470`)

### M28 — Severity: suggestion — Status: open
- File: `tests/timewarp-nuru-tests/completion/engine/engine-01-input-tokenizer.cs:1-42`
- Description: References removed `ParsedInput`/`InputTokenizer` (#360). Does not compile standalone. 454-001 already said delete or rewrite.
- Suggestion: Delete the file and its `CiTestExcludes` entry, or rewrite against the current tokenizer.
- Source: tests-infra
- Disposition notes: filed as **470-012** (`--parent 470`)

### M29 — Severity: suggestion — Status: open
- File: `tests/timewarp-nuru-tests/generator/generator-19-group-filtering.cs:190-199`; `generator-20-parameterized-service-constructor.cs:148-149,306-308`
- Description: Individual methods gated `#if !JARIBU_MULTI` never run on CI’s standalone phase (files themselves are multi-included).
- Suggestion: Extract standalone-only cases into CiTestExcluded + standalone-listed files, or add the files to the second phase.
- Source: tests-infra
- Disposition notes: filed as **470-010** (`--parent 470`)

### M30 — Severity: suggestion — Status: open
- File: `source/timewarp-nuru-search/services/database-path.cs:15`
- Description: `~/.nuru/index.db` is created world-readable (same trust directory as history). Stores route patterns and capabilities JSON.
- Suggestion: Owner-only mode on the DB and `0700` on `~/.nuru`.
- Source: security
- Disposition notes: filed as **470-008** (`--parent 470`)

### M31 — Severity: suggestion — Status: open
- File: `source/timewarp-nuru-devcli/content/any/services/nuget-version-service.cs:40`
- Description: Package id is interpolated unescaped; `../evil` normalizes off `v3-flatcontainer`. Reachable from `--package`. Host stays `api.nuget.org`.
- Suggestion: Validate NuGet id grammar and/or `Uri.EscapeDataString` the segment.
- Source: security
- Disposition notes: filed as **470-007** (`--parent 470`)

### M32 — Severity: suggestion — Status: open
- File: `source/timewarp-nuru/options/repl-options.cs:92-100`
- Description: Default history ignore patterns miss `Bearer`, `Authorization`, `api_key`/`api-key`, `sk-…`. Combined with M17, missed lines are world-readable.
- Suggestion: Extend defaults; add `repl-03b` cases. Document best-effort.
- Source: security
- Disposition notes: filed as **470-011** (`--parent 470`)

### M33 — Severity: suggestion — Status: open
- File: `source/timewarp-nuru/telemetry/telemetry-behavior.cs:58-60` (generated twin `telemetry-emitter.cs:120`)
- Description: Failure path tags `error.message` with `ex.Message`. Opt-in OTLP export can leak argv/secrets in exception text.
- Suggestion: Prefer `error.type` by default, or redact/truncate `error.message`. Document that OTLP sinks must be trusted.
- Source: security
- Disposition notes: filed as **470-015** (`--parent 470`)

### M34 — Severity: nit — Status: open
- File: `source/timewarp-nuru/nuru-app.cs:135-147`
- Description: `CreateBuilder` XML crefs `AddHelp` (gone) and shows two-arg `Map`. `ConfigureServices` examples still mention `AddDependencyInjection()`.
- Suggestion: Update crefs/examples to current API.
- Source: core-runtime
- Disposition notes: filed as **470-001** (`--parent 470`)

### M35 — Severity: nit — Status: open
- File: `source/timewarp-nuru/services/empty-service-provider.cs:6-12`
- Description: `EmptyServiceProvider`, `NuruLoggingBuilder`, `NuruMetricsBuilder` are unreferenced.
- Suggestion: Delete or wire.
- Source: core-runtime
- Disposition notes: filed as **470-001** (`--parent 470`)

### M36 — Severity: nit — Status: open
- File: `source/timewarp-nuru/repl/input/multiline-buffer.cs:145-150`
- Description: `InsertText` treats `\r` and `\n` as separate breaks so `\r\n` inserts a blank line. `SetText` splits correctly.
- Suggestion: Mirror `SetText` splitting.
- Source: repl-completion
- Disposition notes: filed as **470-003** (`--parent 470`)

### M37 — Severity: nit — Status: open
- File: `source/timewarp-nuru/completion/completion/templates/zsh-completion-dynamic.zsh:18-23`
- Description: Dead numeric “exit code” strip; handler never emits a bare numeric stdout line.
- Suggestion: Remove; keep `:directive` handling only.
- Source: repl-completion
- Disposition notes: filed as **470-013** (`--parent 470`)

### M38 — Severity: nit — Status: open
- File: `source/timewarp-nuru-parsing/parsing/parser/parse-error.cs:63`
- Description: `InvalidTypeConstraintError.SupportedTypes` omits types `IsBuiltInType` accepts (`byte`, `sbyte`, `short`, …).
- Suggestion: One source of truth for the built-in list.
- Source: parsing
- Disposition notes: filed as **470-004** (`--parent 470`)

### M39 — Severity: nit — Status: open
- File: `source/timewarp-nuru-parsing/parsing/parser/parser.cs:147`
- Description: `AdjacentParametersError` spans only the `{` token (`pos` of `LeftBrace`, `len=1`).
- Suggestion: Widen to the full second parameter segment.
- Source: parsing
- Disposition notes: filed as **470-004** (`--parent 470`)

### M40 — Severity: nit — Status: open
- File: `source/timewarp-nuru-mcp/services/github-cache-service.cs:103-104`
- Description: Meta timestamps written with `"O"` but read with culture-sensitive `DateTime.TryParse`.
- Suggestion: `TryParse` with `InvariantCulture` + `RoundtripKind`.
- Source: aux
- Disposition notes: filed as **470-009** (`--parent 470`)

### M41 — Severity: nit — Status: open
- File: `source/timewarp-nuru-devcli/content/any/endpoints/self-install-command.cs:52-108`
- Description: Windows successful self-install leaves `dev.exe.old`.
- Suggestion: Best-effort delete after success.
- Source: aux
- Disposition notes: filed as **470-014** (`--parent 470`)

### M42 — Severity: nit — Status: open
- File: `samples/Directory.Build.props:12-13`
- Description: Comment still says TreatWarningsAsErrors is “Temporarily disabled … #365” though it is the long-term samples default.
- Suggestion: Match the permanent rationale at repo-root props.
- Source: tests-infra
- Disposition notes: filed as **470-012** (`--parent 470`)

### M43 — Severity: nit — Status: open
- File: `source/timewarp-nuru/timewarp-nuru.csproj:46`
- Description: `Microsoft.Extensions.Logging` sets `GeneratePathProperty="true"` but packing only uses the Abstractions pkg path.
- Suggestion: Drop unused `GeneratePathProperty`.
- Source: tests-infra
- Disposition notes: filed as **470-012** (`--parent 470`)

### M44 — Severity: nit — Status: open
- File: `source/timewarp-nuru/completion/completion/templates/pwsh-completion-dynamic.ps1:14`
- Description: `{{APP_PATH}}` is substituted unescaped inside a PowerShell string. Practical risk near-zero for normal install layouts.
- Suggestion: Escape `'`/`"`/`$`/`` ` `` in `APP_PATH`.
- Source: security
- Disposition notes: filed as **470-013** (`--parent 470`)

## Duplicates / conflicts

- MCP example `Uri` `../` + cache path: aux Issue 5 (bug) and security Issue 3 (suggestion) collapsed to **M13** at strongest severity (bug).
- NuGet `packageId` path confusion (security Issue 4) kept as **M31** (suggestion), separate from HTTP fail-open **M9**.
- FTS NUL residual (aux Issue 4) is **M12**; security correctly did not re-open 454-023’s original quote/wildcard bugs.
- 454-019 wrapped-line redraw noted only; no M#.
- Analyzer IEnumerable DI (**M20**) is wontfix on this task in favor of epic 391.

## 454 regression rollup

All cited 454-done items in the seven area files verified **not present**, except:

- **454-019** still open (note only).
- **454-001** partially regressed via **M14**/**M15** (glob is healthy; two committed tests still never run in CI).
- **454-023** quoting/LIKE remain fixed; residual NUL is **M12**.
- **454-032** IVT drift returned as **M26**.
