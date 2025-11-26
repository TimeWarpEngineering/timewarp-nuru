# Update Benchmark Analysis Documentation

## Description

The benchmark analysis documentation is outdated due to API changes and feature additions since the initial optimization work. The analysis files reference old API names and outdated benchmark data that no longer reflect the current state of the framework.

## Requirements

- Run fresh benchmarks to capture current performance and memory characteristics
- Update all analysis files to use current API names (`Map` instead of `AddRoute`, `NuruAppBuilder` instead of `DirectAppBuilder`, etc.)
- Document current memory allocation breakdown accurately
- Update performance rankings and comparisons

## Checklist

### Analysis
- [ ] Run benchmarks with current codebase
- [ ] Capture new memory allocation data
- [ ] Capture new performance timing data

### Documentation Updates
- [ ] Update `Benchmarks/TimeWarp.Nuru.Benchmarks/Analysis/Memory-Analysis.md`
- [ ] Update `Benchmarks/TimeWarp.Nuru.Benchmarks/Analysis/Performance-Analysis.md`
- [ ] Verify `Benchmarks/TimeWarp.Nuru.Benchmarks/Results/2025-07-25-Analysis.md` accuracy

## Notes

### Files Requiring Updates

**Memory-Analysis.md** contains:
- References to `DirectAppBuilder` (now `NuruAppBuilder`)
- References to `AddRoute` (now `Map`)
- References to `DirectApp` (now `NuruApp`)
- References to removed classes (`RouteParameter`, `AppBuilder`)
- References to `NuruCli` class
- Memory figures from July 2025 optimization work

**Performance-Analysis.md** contains:
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
- Various features were added that may affect performance/memory

The current benchmark commands in `Commands/NuruDirectCommand.cs` and `Commands/NuruMediatorCommand.cs` use the correct current API, but the analysis documentation does not match.
