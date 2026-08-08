# Sweep Core Runtime Low Severity Findings

Parent: 454 (2026-07-06 full code review). Severity: LOW (batch).

## Description

Low-severity core-runtime findings from the 2026-07-06 review (type-converter dead code
is handled in 454-009, not here):

1. `source/timewarp-nuru/builders/compiled-route-builder.cs:235-245` (and WithOption
   remark ~:106) — ToCamelCase doc example says `"dry-run"` → `"dryRun"` but the
   implementation strips dashes and lowercases only the first char → `"dryrun"`. It
   matches runtime `Compiler.ToCamelCase`
   (`timewarp-nuru-parsing/parsing/compiler/compiler.cs:197`), while the analyzer
   emitters use a different algorithm (`csharp-identifier-utils.cs:59`) producing
   `"dryRun"`. Each world is internally consistent, but fix the doc example and consider
   converging the two algorithms (maintenance trap).
2. `source/timewarp-nuru/extensions/app-name-detector.cs:19-25` — uses
   `Environment.ProcessPath`, so framework-dependent runs (`dotnet myapp.dll`) detect the
   app name as "dotnet". Feeds REPL history file naming and shell-completion naming.
   Fall back to entry-assembly name when the process is the dotnet host.
3. `source/timewarp-nuru/telemetry/telemetry-behavior.cs:49,67` — records integer
   `stopwatch.ElapsedMilliseconds` into a double-ms histogram; sub-millisecond commands
   bucket to 0. Use `stopwatch.Elapsed.TotalMilliseconds`.

## Checklist

- [x] Fix ToCamelCase doc example; decide on algorithm convergence
- [x] AppNameDetector handles dotnet-host case
- [x] Telemetry uses TotalMilliseconds
- [x] CI tests green (1386/1379/0)

## Resolution (2026-07-14)

- **#1** — Fixed the misleading `WithOption` remark ("dry-run" becomes "dryRun" → "dryrun",
  matching the actual `ToCamelCase`). **Decision: do NOT converge** the runtime
  (builder/Compiler → "dryrun") and source-gen (`csharp-identifier-utils` → "dryRun")
  algorithms — convergence is a runtime parameter-binding behavior change, out of scope for a
  LOW doc sweep. Documented the intentional divergence in the remark; each path is internally
  consistent.
- **#2** — `AppNameDetector` now skips the dotnet host: when `Environment.ProcessPath` /
  process name is "dotnet" (framework-dependent `dotnet myapp.dll` runs), it falls back to the
  entry-assembly name so REPL history and completion get the real app name.
- **#3** — Telemetry histogram records `stopwatch.Elapsed.TotalMilliseconds` (double) instead
  of integer `ElapsedMilliseconds`, so sub-millisecond commands no longer bucket to 0.
