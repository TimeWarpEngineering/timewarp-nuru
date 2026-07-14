# Address 2026-07-06 Full Code Review Findings

## Description

Full-repo code review performed 2026-07-06 by six parallel review agents covering:
core runtime (routing/binding + REPL/terminal/completion), analyzers/source generators,
route-pattern parsing, auxiliary projects (MCP server, search, build, devcli), and
repository infrastructure (MSBuild, packaging, CI, test health). ~51k lines of C# across
413 files. The parsing library was additionally fuzzed with 200,000 random malformed
patterns (zero uncaught exceptions).

Overall: the codebase is well-architected — clean pipeline separation, AOT-conscious,
parameterized SQL, no command-injection exposure, correct analyzer packaging, modern
`GetInterceptableLocation` usage. The findings below are the exceptions, grouped by
severity. Each is independently fixable; consider splitting the HIGHs into child tasks.

## HIGH Severity

### H1. CI silently excludes 16+ committed test files
`tests/ci-tests/Directory.Build.props` enumerates test files by hand; later-added tests
were never wired in. Excluded but tracked and substantial:
- `tests/timewarp-nuru-tests/auto/` (entire dir, e.g. `endpoint-nullable-option-01.cs`)
- `tests/timewarp-nuru-tests/group-options/` (entire dir, 492-line `group-options-01-basic.cs`)
- `tests/timewarp-nuru-tests/generator/generator-{16,17,18,19,20-issue184,20-parameterized,21,22,24,25}.cs` + `temp-iconfig-test.cs`
- `tests/timewarp-nuru-tests/completion/completion-27-endpoint-protocol.cs`
- `tests/timewarp-nuru-tests/repl/repl-38-auto-start-when-empty.cs`, `repl-39-no-duplicate-options.cs`
- ALL 6 `tests/timewarp-nuru-mcp-tests/mcp-0*.cs` (glob commented out at line 116) — MCP server has zero CI coverage

Impact: group filtering, nullable options, duplicate-option suppression, endpoint-protocol
completion, and the entire MCP server can regress with green CI. Prior report
`.agent/workspace/2026-01-17T00-00-00_ci-excluded-tests-report.md` flagged this pattern;
drift has continued since. Consider replacing the hand-maintained list with globs + an
explicit exclusion list, or a CI check that fails when an unreferenced test file exists.

### H2. Help emitter generates non-compiling code when description contains braces
`source/timewarp-nuru-analyzers/generators/emitters/help-emitter.cs:60` — description is
emitted into a generated interpolated string but `EscapeString` doesn't escape `{`/`}`.
A description like `"greet {name}"` (users describe route syntax!) emits
`WriteLine($"  greet {name}");` → CS0103, generated code won't compile. The inner `$` is
unnecessary — drop interpolation or escape braces.

### H3. DSL interpreter: self-referential initializer causes StackOverflow (kills IDE/compiler process)
`source/timewarp-nuru-analyzers/interpreter/dsl-interpreter.cs:440-454` —
`ResolveIdentifier` writes the cache only after evaluating the initializer, so `var x = x;`
(ordinary mid-typing state) recurses forever → uncatchable `StackOverflowException` in the
analyzer host. Write a sentinel into the cache before recursing.

### H4. False NURU_H002 error on valid handlers using named arguments
`source/timewarp-nuru-analyzers/validation/handler-validator.cs:258-310` (and ~:410 for
anonymous methods) — closure detector doesn't skip the identifier in a `NameColon`.
`(string name) => Console.WriteLine(format: name)` resolves `format` to WriteLine's
parameter and reports it as a captured variable → error diagnostic on valid code.

### H5. Parser rejects multi-char single-dash options the lexer explicitly supports
`source/timewarp-nuru-parsing/parser/parser.segments.cs:175-185` vs `lexer/lexer.cs:141-144` —
lexer tokenizes `-bl`, `-verbosity` (with dedicated test lexer-05), but `ParseOptionForms`
unconditionally errors when shortForm.Length > 1, with the self-contradictory message
"options must start with '--' or '-'". Feature is half-implemented; decide (support in
parser, or remove from lexer + fix error message).

### H6. REPL undo-stack trim reverses history order
`source/timewarp-nuru/repl/input/undo-stack.cs:170-189` — `TrimToCapacity()` reverses,
trims, then reverses AGAIN (line 184) before re-pushing: `[A,B,C,D]` (newest→oldest),
max=3 → `[C,B,A]` instead of `[A,B,C]`. After >100 edits on one line, Ctrl+Z walks history
in inverted order. Fix: delete the second `items.Reverse()`.

### H7. Windows `\r\n` breaks REPL multiline cursor mapping
`source/timewarp-nuru/repl/input/multiline-buffer.cs:313,329,349` +
`repl-console-reader.multiline.cs:70-71` — `GetFullText(Environment.NewLine)` joins with
2-char `\r\n` on Windows but `CursorToPosition`/`PositionToCursor`/`TotalLength` assume
1-char newlines. After Shift+Enter, cursor lands on wrong column; `UserInput[..CursorPosition]`
can throw `ArgumentOutOfRangeException`. Windows only.

## MEDIUM Severity

### Core runtime — type conversion
- **M1.** `type-conversion/converters/enum-type-converter.cs:23` — `Enum.TryParse` accepts
  undefined numerics (`"999"` → `(TEnum)999`); this IS the generated AOT binding path
  (route-matcher-emitter.cs:1040,1052). Add `IsDefined` check for non-Flags enums.
- **M2.** `type-conversion/default-type-converters.cs:21-146` + all public converter classes —
  current-culture `TryParse`, contradicting the generated invariant-culture path and the
  parity claim in `type-conversion-map.cs:5`. `double.TryParse("3.14")` misparses under de-DE.
- **M3.** `type-conversion/converters/bool-type-converter.cs:5-38` — advertises
  `yes/no/1/0/on/off/enabled/disabled` but the real binding path only accepts `true/false`.

### Analyzers / generators
- **M4.** `generators/nuru-generator.cs:121` — raw `Compilation` combined into output stage;
  heavy `InterceptorEmitter.Emit` re-runs every keystroke. Narrow to what REPL enum
  resolution needs.
- **M5.** Pipeline model records hold `ImmutableArray<T>` (reference equality) —
  `models/generator-model.cs:13-19`, app-model.cs, route-definition.cs, etc. Incrementality
  largely defeated. Standard fix: `EquatableArray<T>`.
- **M6.** `interpreter/dsl-interpreter.cs:91,162,227` — fail-soft catch is only
  `InvalidOperationException`; NRE/ArgumentException from partial syntax escape → AD0001.
- **M7.** `extractors/builders/route-definition-builder.cs:228` — handler param with no
  matching route segment throws → generic NURU_S999 and ALL other routes in the block
  dropped. Should be a targeted param-mismatch diagnostic.
- **M8.** `extractors/endpoint-extractor.cs:506-508,832-834` — `typeName.Contains("IEnumerable")`
  string heuristic for isRepeated; `MyApp.IListManager` wrongly treated as repeated option.
  Violates repo SemanticModel convention. Same class of issue: `handler-extractor.cs:623-632`
  (`IsServiceType` via `Contains("ILogger")`/"I+uppercase") and `dsl-interpreter.cs:1475`
  (`objectCreation.Type.ToString()` fallback).
- **M9.** `dsl-interpreter.cs` — `DispatchWithDescription` (~942), `DispatchWithAlias` (~1500),
  `DispatchWithGroupPrefix` (~887) lack the `IsDslBuilderMethod` guard that
  `DispatchWithName` (~963) has; unrelated `x.WithDescription(...)` → bogus NURU_S999.
- **M10.** `validation/overlap-validator.cs:397` — descending-specificity sort makes the
  `higher >= lower` guard always true; `list {filter?}` vs `list --all` (both reduce to
  `list`) → false NURU_R003 unreachable-route warning.

### Parsing
- **M11.** `runtime/matchers/option-matcher.cs:79-85` — grouped short-option matching uses
  `arg.Contains(shortChar)` anywhere in the arg: `-e` matches `-help`. Verify chars are
  standalone grouped flags. (Also: `ToString()` alloc per call at :84.)
- **M12.** `lexer/lexer.cs:130` — end-of-options `--` recognized only before ASCII space;
  `--\tx` lexes as long option `--x` and parses successfully.
- **M13.** `validation/semantic-validator.cs:280-302` — duplicate LONG-form options not
  validated (`build --verbose --verbose` compiles to two identical matchers); short forms
  are rejected.
- **M14.** `lexer/lexer.cs:94-99` — whitespace fully discarded; `greet {a}{b}` parses
  identically to `greet {a} {b}` — a near-certain typo silently accepted.

### REPL / terminal
- **M15.** `repl/repl-session.cs:316-321` — Ctrl+C only sets `Running = false`; no linked
  CancellationTokenSource, so an in-flight command cannot be aborted.
- **M16.** `repl/input/repl-console-reader.clipboard.cs:96` — Windows clipboard SET glues
  the whole PowerShell invocation into one argv element; silently does nothing. (Read path
  at :86 is correct.)
- **M17.** `repl/input/repl-console-reader.cs:279,310` — `RedrawLine` clears one row only
  and cursor positioning skipped when `desiredLeft >= WindowWidth`; editing lines longer
  than terminal width is visually broken.
- **M18.** `repl-console-reader.yank-arg.cs:59-63,103-106` — Alt+. past the last
  args-bearing history entry deletes yanked text without redraw/re-insert → buffer/screen
  desync, silent text loss.
- **M19.** `repl-console-reader.search.cs:162-164,200,217` — extending an i-search pattern
  jumps off a still-matching current entry (diverges from readline).
- **M20.** `repl-console-reader.selection.cs:194,226,253` — cut/paste/delete slice with
  unclamped `SelectionState.End` → `ArgumentOutOfRangeException` on stale selection.
- **M21.** `completion/completion/sources/enum-completion-source.cs:81` — `Convert.ToInt32`
  overflows for `enum : long/ulong` members outside Int32 range, crashing `__complete`.

### Aux projects
- **M22.** `source/timewarp-nuru-devcli/content/any/endpoints/check-version-command.cs:161-171` —
  release gate only compares against the single HIGHEST published version; if source is
  1.2.0 and NuGet has [1.2.0, 1.3.0], it reports "safe to release". Test membership in the
  full versions list.
- **M23.** `source/timewarp-nuru-search/services/search-index.cs:290-314` — FTS sanitizer
  produces malformed MATCH expressions: searching `(` or `***` throws uncaught
  `SqliteException` instead of "No results found". Wrap tokens as FTS5 quoted strings.
- **M24.** `source/timewarp-nuru-mcp/services/github-cache-service.cs:9,39,50,59` — static
  `Dictionary` cache read/mutated concurrently by MCP tool calls with no lock. Use
  `ConcurrentDictionary`.

### Infrastructure
- **M25.** Stale scratch files committed: `optimization-results.md` (root),
  `tests/test-status-report.md` (hand-maintained pass/fail table, guaranteed to rot),
  `tests/temp-test-chained.cs` (bug-#295 repro at tests/ root). Delete or relocate.
- **M26.** `source/timewarp-nuru/timewarp-nuru.csproj:101` packs
  `lib/net9.0/Microsoft.Extensions.Logging.Abstractions.dll` into analyzers/dotnet/cs while
  `timewarp-nuru-analyzers.csproj:58` packs `lib/net10.0/` — inconsistent; net9.0 path is a
  latent break if the TFM is dropped.

## LOW Severity

- Core: dead `TypeConverterRegistry` field in `nuru-app-builder.cs:9` (AddTypeConverter is a
  silent no-op); 7 never-instantiated public converter classes duplicating
  DefaultTypeConverters with divergent culture behavior; `compiled-route-builder.cs:235-245`
  ToCamelCase doc example wrong (`dry-run` → `dryrun`, not `dryRun`) and two divergent
  camelCase algorithms coexist (runtime vs `csharp-identifier-utils.cs:59`);
  `app-name-detector.cs:19-25` returns "dotnet" for framework-dependent runs;
  `telemetry-behavior.cs:49,67` integer-ms duration loses sub-ms precision.
- Analyzers: six duplicated `EscapeString` helpers with inconsistent coverage
  (`telemetry-emitter.cs:88/101/113` escapes only `"`; `behavior-emitter.cs:443` omits
  newlines) — consolidate; `endpoint-extractor.cs:880,896` property defaults emitted via raw
  `ToString()` (using-scope breakage); `handler-validator.cs:164-189` method-group path
  misses IsStatic check (NURU_H001); non-generic `ValueTask` handlers not recognized as
  awaitable (`handler-extractor.cs:489-508`); `service-validator.cs:221,341,365` diagnostics
  report at `Location.None`; `endpoint-extractor.cs:137-154` alias index math wrong for
  multi-word `[NuruRouteGroup("git remote")]`; NURU_DEBUG* hidden diagnostics and unused
  `IsBuilderType` linger.
- Parsing: `InvalidModifierCombinationError` span excludes offending modifiers
  (`parser.segments.cs:68-74`); dead code — write-only `ValidationContext.OptionAliases`,
  unused locals in `ValidateDuplicateParameters` (semantic-validator.cs:108-120), uncalled
  `Lexer.PeekNext()` (lexer.cs:273-276).
- REPL/completion: unreachable `DetectShell()` (`install-completion-handler.cs:90-138`);
  `repl-history.cs:119,155` last-writer-wins clobbering between two REPL instances and
  `Load` duplicates entries on second call; pwsh completion template corrupts tokens with
  spaces, fish template drops a legitimate `0` candidate; UTF-16 char-based cursor math
  splits surrogate pairs in word ops; `repl-session.cs:34,101,111` mutable static
  `CurrentSession` + narrow exception filter tears down REPL on unexpected exceptions.
- Aux: `github-cache-service.cs:132-142` disk-cache filename collision across directories;
  `nuget-version-service.cs:59-82` `CompareVersions` ranks `1.0.0-beta` above `1.0.0`;
  `search-index.cs:254-255` group filter passes raw LIKE wildcards.
- Infra: BannedSymbols `AdditionalFiles` ItemGroup duplicated 3x in root
  `Directory.Build.props:35-38,51-54,87-90`; misleading "Treat all warnings as errors"
  comment above `TreatWarningsAsErrors=false` (root props:58-59); orphan `docs/` root with a
  single file vs the real `documentation/` tree; committed `internals-visible-to.g.cs` files
  still reference removed scratch tests.

## Checklist

Findings are broken out into child tasks 454-001 … 454-032. HIGHs map 1:1; MEDIUMs are
grouped into coherent fix batches; LOWs are per-area sweeps. Related LOW items are folded
into the MEDIUM task that touches the same files (noted in each task).

HIGH:
- [x] 454-001 Fix CI test file inclusion drift (H1) — done; CI now 1271 tests green, spawned 454-033
- [x] 454-002 Fix help emitter brace escaping (H2) — done; regression test help-07 added
- [x] 454-003 Fix DSL interpreter self reference stack overflow (H3) — done; crashes on VALID code fixed, Roslyn-hosted test added
- [x] 454-004 Fix NURU H002 false positive on named arguments (H4) — done; also fixed property-pattern/anonymous-type shapes
- [x] 454-005 Resolve multi char single dash option support (H5) — done; multi-char shorts supported, grouping removed
- [x] 454-006 Fix undo stack trim order reversal (H6) — done; one-line fix + regression test
- [x] 454-007 Fix Windows CRLF multiline cursor mapping (H7) — done; \n contract pinned by tests, Windows human check pending

MEDIUM:
- [x] 454-008 Reject undefined enum values in enum type converter (M1) — done by agent, reviewed OK
- [x] 454-009 Align runtime type converters with invariant culture path (M2, M3 + dead converters) — done by agent, reviewed OK
- [ ] 454-010 Restore generator incrementality (M4, M5)
- [ ] 454-011 Harden DSL interpreter against invalid user code (M6, M7, M9)
- [ ] 454-012 Replace string type heuristics with SemanticModel checks (M8 + property defaults)
- [x] 454-013 Fix false NURU R003 unreachable route warning (M10) — done; bound flags optional, unbound flags required discriminators, IsFlagBound + skip guard + required signature, PatternSyntax fix, 10 tests
- [x] 454-014 Fix grouped short option over matching (M11) — resolved by 454-005 (heuristic removed)
- [x] 454-015 Fix lexer whitespace and end of options handling (M12, M14) — done; char.IsWhiteSpace for --, AdjacentParametersError for {a}{b}, 11 tests
- [x] 454-016 Validate duplicate long form options (M13) — done; short+long form dup detection, dead OptionAliases removed, 5 tests
- [ ] 454-017 Wire Ctrl C cancellation into REPL command execution (M15)
- [ ] 454-018 Fix Windows clipboard set in REPL (M16)
- [ ] 454-019 Fix redraw of lines longer than terminal width (M17)
- [ ] 454-020 Fix REPL reader state desync bugs (M18, M19, M20)
- [x] 454-021 Fix enum completion overflow for wide underlying types (M21) — done; Convert.ToInt32 → value.ToString("D"), 3 regression tests added
- [x] 454-022 Fix release gate already published version check (M22 + CompareVersions) — done; full SemVer 2.0 §11 CompareVersions, IsVersionPublished full-list check, 5 service files wired into CI (not endpoint), 11 tests
- [x] 454-023 Fix FTS query sanitizer malformed match expressions (M23 + LIKE wildcards) — done; FTS5 double-quoted tokens, empty-query guard, EscapeLikePattern + ESCAPE clause, 13 tests
- [x] 454-024 Make MCP GitHub cache thread safe (M24 + cache filename collision) — done; ConcurrentDictionary swap, full-path filename fix, also fixed broken InternalsVisibleTo generator, 3 tests
- [x] 454-025 Remove stale scratch files from repo (M25) — done; 4 scratch files + 1 stale nested .g.cs deleted, CiTestExcludes cleaned, .g.cs regenerated, CI 1360 green
- [x] 454-026 Unify analyzer packaging TFM for logging abstractions (M26) — done; net9.0→net10.0 in timewarp-nuru.csproj, SHA-256 confirmed both nupkg ship identical DLL

MEDIUM (discovered during 454-001):
- [ ] 454-033 Fix MCP examples manifest drift and endpoint syntax regions

Cross-cutting:
- [ ] Human REPL verification session (single batch, after 454-017/018/019/020 land):
      Ctrl+C cancels in-flight command; Windows clipboard cut/copy; long-line redraw;
      Alt+. cycling past last args entry; i-search extension; stale-selection ops;
      Windows Shift+Enter multiline cursor (454-007 leftover). Each task's Results
      lists its exact manual steps.

LOW sweeps:
- [ ] 454-027 Sweep core runtime low severity findings
- [ ] 454-028 Sweep analyzer low severity findings
- [ ] 454-029 Sweep parsing low severity findings
- [ ] 454-030 Sweep REPL and completion low severity findings
- [ ] 454-031 Sweep aux project low severity findings (mostly verification of folded items)
- [ ] 454-032 Sweep infrastructure low severity findings

## Notes

- Review method: six parallel agents (core runtime ×2, analyzers, parsing, aux projects,
  infrastructure), each required to cite verifiable file:line evidence.
- Parsing crash-safety was fuzz-verified: 200k random malformed patterns, zero uncaught
  exceptions — the "never throw inside a Roslyn analyzer" requirement holds for the parser.
- Explicitly NOT findings: SQLite CVE-2025-6965 suppression in timewarp-nuru-search
  (known/documented), root `bin/dev*` (gitignored local output), committed
  `internals-visible-to.g.cs` pattern itself (deliberate).
- No SQL injection (parameterized throughout), no command injection (Amuru argv builders),
  no resource leaks found in search/MCP/telemetry paths.

## Reprioritization (2026-07-14)

The remaining backlog was re-assessed against how this framework is actually consumed now
(AI-authored code + a curated Nuru skill), rather than treating this review list as the roadmap:

- **MCP server frozen/deprecated.** The `timewarp-nuru` MCP server's example/syntax/validation
  tools duplicate what `skills/nuru/SKILL.md` + `samples/` now deliver in-context and more
  reliably. It is frozen (docs + `.agent/local/nuru-specific.md` updated); no further MCP work.
  - **454-033 → CLOSED won't-do, archived** (`kanban/archived/`). Its manifest-drift and
    endpoint-syntax-region work would invest in the frozen component.
- **REPL batch parked for human verification.** 454-017/018/019/020 and 454-030 carry
  "Status: PARKED — needs human interactive verification" banners. Their value is real but
  *human-user* facing (key handling, cursor/redraw, clipboard, cancellation) and cannot be
  verified by an automated agent in a non-interactive shell. Held for a keyboard-verify pass.
- **Everything else is done.** The analyzer trio (011/012/010), analyzer sweep (028), and the
  core/parsing/aux/infra sweeps (027/029/031/032) all landed green. The high-value items in
  this list are exhausted; further forward investment (diagnostic message quality for AI
  fix-loops, skill accuracy, cold-build/AOT) is proactive work, not review remediation.

## Session

- Created: 2026-07-06 (full-repo review session)
