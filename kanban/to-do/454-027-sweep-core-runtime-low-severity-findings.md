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

- [ ] Fix ToCamelCase doc example; decide on algorithm convergence
- [ ] AppNameDetector handles dotnet-host case
- [ ] Telemetry uses TotalMilliseconds
- [ ] CI tests green
