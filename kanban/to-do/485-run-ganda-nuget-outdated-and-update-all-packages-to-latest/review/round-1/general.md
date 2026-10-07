# Round 1 — general
**Date:** 2026-10-07
**Scope reviewed:** branch `task/485-run-ganda-nuget-outdated-and-update-all-packages-t` vs `origin/master` — `Directory.Packages.props`, `changelog.md`, and the analyzer, build-task, and Spectre benchmark call sites those versions feed.

## Summary

Roslynator.Analyzers, Roslynator.CodeAnalysis.Analyzers, and Roslynator.Formatting.Analyzers move 5.0.0 → 5.0.1. Spectre.Console.Cli moves 0.55.0 → 0.57.2. `Microsoft.CodeAnalysis.CSharp` stays at 5.6.0 and `Microsoft.Build.Utilities.Core` stays at 18.9.6. Both pins match the constraints in the task. `ganda nuget outdated --dry-run` on this tree checks 57 packages and reports only those two (5.6.0 → 5.9.0, 18.9.6 → 18.10.1). Installed SDK compilers are Roslyn 5.6.0 on 10.0.301 and 10.0.302, and Roslyn 5.9.0 on 10.0.400, so taking 5.9.0 would stop the analyzer loading on 10.0.3xx. The NuGet cache shows 18.10.1 `lib/` is `net11.0` and `net472` only; 18.9.6 still has `lib/net10.0`, which is what `timewarp-nuru-build` packs under `build/net10.0`. Spectre.Console.Cli is referenced only by `bench-spectreconsole` and the solution-excluded `timewarp-nuru-benchmarks`. Releases 0.56.0 and 0.57.0 do not change `Command.Execute`. `dotnet build benchmarks/aot-benchmarks/bench-spectreconsole/bench-spectreconsole.csproj -c Release` is 0 warnings, 0 errors. Roslynator 5.0.1 keeps the same `roslyn3.8` / `roslyn4.7` / `roslyn5.0` analyzer layout as 5.0.0, and `Directory.Build.props` marks those references `PrivateAssets=all`. The Unreleased changelog line records the bumps and that the consumer Roslyn minimum is unchanged. No bugs, suggestions, or nits.

## Issues

<!-- none -->
