# Update Benchmark Analysis Documentation

## Description

The benchmark analysis documentation is outdated due to API changes and feature additions since the initial optimization work. The analysis files reference old API names and outdated benchmark data that no longer reflect the current state of the framework.

**Update (December 2025):** Fresh benchmarks have been run revealing a critical performance regression in the Full builder.

## Requirements

- ~~Run fresh benchmarks to capture current performance and memory characteristics~~ ✅ Done Dec 20, 2025
- Update all analysis files to use current API names (`Map` instead of `AddRoute`, `NuruAppBuilder` instead of `DirectAppBuilder`, etc.)
- Document current memory allocation breakdown accurately
- Update performance rankings and comparisons

## December 2025 Benchmark Results

New results file created: `results/2025-12-20-analysis.md`

### Key Findings

| Builder | July 2025 | December 2025 | Change |
| ------- | --------- | ------------- | ------ |
| Direct (Empty) | Not tested | **3.98 ms (2nd place!)** | Excellent! |
| Full (Mediator) | 34.42 ms (6th) | **131.96 ms (last!)** | **3.8x regression** |

### Critical Issue Discovered

The Full builder with Mediator has regressed from 34ms to 132ms - a nearly 4x slowdown. This needs investigation before Phase 3 can proceed.

## Checklist

### Analysis
- [x] Run benchmarks with current codebase (Dec 20, 2025)
- [x] Capture new memory allocation data
- [x] Capture new performance timing data
- [x] Create `results/2025-12-20-analysis.md`

### Documentation Updates
- [ ] Update `analysis/memory-analysis.md` with current API names and data
- [ ] Update `analysis/performance-analysis.md` with current API names and data
- [ ] Archive `results/2025-07-25-analysis.md` as historical reference

### Investigation (New - High Priority)
- [ ] Profile CreateBuilder() startup to find bottlenecks
- [ ] Compare July vs December DI/Mediator initialization code
- [ ] Identify source of 4x regression
- [ ] Create task for performance fix

## Notes

### Files Requiring Updates

**memory-analysis.md** contains:
- References to `DirectAppBuilder` (now `NuruAppBuilder`)
- References to `AddRoute` (now `Map`)
- References to `DirectApp` (now `NuruApp`)
- References to removed classes (`RouteParameter`, `AppBuilder`)
- References to `NuruCli` class
- Memory figures from July 2025 optimization work

**performance-analysis.md** contains:
- References to `AppBuilder.AddRoute()` (now `NuruAppBuilder.Map()`)
- References to `NuruCli` class
- Performance figures from July 2025 (34.422 ms, 6th place)
- Outdated DI architecture descriptions

### Context

The original benchmark optimization was performed in July 2025. Since then:
- API was refactored (`AddRoute` -> `Map`, class name changes)
- REPL support was added
- Tab completion was added
- Shell completion was added
- Fluent API migration (`Map().WithHandler().AsCommand().Done()`)
- Source generators added for delegate commands
- Various features were added that may affect performance/memory

### Benchmark Infrastructure Fixed

The benchmark `program.cs` was updated to use `InProcessEmitToolchain` to work around BenchmarkDotNet's issue with kebab-case project names not matching assembly names.
