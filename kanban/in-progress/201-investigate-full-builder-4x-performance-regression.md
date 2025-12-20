# Investigate Full Builder 4x Performance Regression

## Description

The Full builder (`NuruApp.CreateBuilder()`) has a critical performance regression discovered in December 2025 benchmarks. Performance dropped from 34.42 ms (July 2025) to 131.96 ms (December 2025) - a **3.8x slowdown** that puts us in last place among CLI frameworks.

Meanwhile, `CreateEmptyBuilder()` performs excellently at 3.98 ms (2nd place), proving the core routing/parsing is fast.

## Priority

**HIGH** - This blocks Phase 3 (Unified Pipeline) and makes CreateBuilder() uncompetitive.

## Parent

148-epic-nuru-3-unified-route-pipeline

## Benchmark Data

### December 2025 Results

| Builder          | Time       | Memory     | Rank |
| ---------------- | ---------- | ---------- | ---- |
| Direct (Empty)   | 3.98 ms    | 14,776 B   | 2nd  |
| Full (Mediator)  | 131.96 ms  | 221,152 B  | 11th (last) |

### Comparison to July 2025

| Metric | July 2025  | December 2025 | Change |
| ------ | ---------- | ------------- | ------ |
| Time   | 34.42 ms   | 131.96 ms     | 3.8x slower |
| Memory | 18,576 B   | 221,152 B     | 12x more |
| Rank   | 6th        | 11th          | -5 positions |

## Checklist

### Phase 1: Cost Matrix Benchmark (DONE)
- [x] Create NuruBuilderCostBenchmark with feature toggles
- [x] Add DisableTelemetry, DisableRepl, DisableCompletion, DisableInteractiveRoute flags to NuruAppOptions
- [x] Update UseAllExtensions() to respect new flags
- [x] Fix benchmark toolchain (restore CsProjCoreToolchain, rename csproj to match assembly)
- [x] Run cost matrix benchmark to identify hotspots

### Phase 2: Telemetry Optimization (DONE)
- [x] Move ResourceBuilder.CreateDefault() after ShouldExportTelemetry check (-15 ms)
- [x] Make ActivitySource, Meter, and counters lazy (-8 ms additional)
- [x] Verify telemetry overhead is now near zero

### Phase 3: Configuration Optimization (DONE)
- [x] Remove `reloadOnChange: true` from all JSON config files - unnecessary for CLI apps
- [x] Eliminates FileSystemWatcher initialization overhead

### Phase 4: Remaining Investigation (IN PROGRESS)
- [ ] Investigate DI + Configuration cost (~35-40 ms gap between Empty and Full)
- [ ] Profile Mediator.AddMediator() overhead
- [ ] Investigate ServiceProvider.BuildServiceProvider() cost
- [ ] Consider if Configuration can be made optional/lazy

### Profiling (if needed)
- [ ] Profile `NuruApp.CreateBuilder()` cold start with dotnet-trace
- [ ] Capture flame graph of startup

## Notes

### Progress: December 20, 2025

#### Cost Matrix Results (After Optimizations)

| Method               | Time (ms) | Notes                              |
| -------------------- | --------- | ---------------------------------- |
| 1-Empty (baseline)   | 40.39     | No DI, no Mediator                 |
| 2-Full-NoExtensions  | 82.78     | DI + Config + Mediator             |
| 3-Full+Telemetry     | 82.00     | Now essentially zero overhead!     |
| 4-Full+Repl          | 83.03     | +7 routes                          |
| 5-Full+Completion    | 84.80     | +4 routes                          |
| 6-Full+Interactive   | 83.48     | +1 route                           |
| 7-Full+Version       | 86.26     | +1 route                           |
| 8-Full+CheckUpdates  | 82.91     | +1 route                           |
| 9-Full-AllExtensions | 90.02     | All extensions                     |

#### Improvements Made

1. **Telemetry ResourceBuilder fix**: Moved `ResourceBuilder.CreateDefault()` after `ShouldExportTelemetry` check
   - Saved ~15 ms by avoiding expensive OS/process info gathering when no OTLP endpoint configured

2. **Lazy telemetry instruments**: Made ActivitySource, Meter, counters lazy-initialized using `Lazy<T>`
   - Saved additional ~8 ms by deferring initialization until first use

3. **Configuration reloadOnChange fix**: Changed `reloadOnChange: true` to `false` for all JSON config sources
   - Eliminates FileSystemWatcher initialization overhead (unnecessary for CLI apps)
   - Saved ~10 ms

4. **Total improvement**: 131.96 ms → ~80 ms (**-52 ms, 39% faster**)

#### Current Benchmark Results (Single-run cold-start, matching July methodology)

| Method               | Time (ms) | Memory (KB) | Notes                    |
| -------------------- | --------- | ----------- | ------------------------ |
| 1-Empty (baseline)   | ~40       | 9.88        | No DI, no Mediator       |
| 2-Full-NoExtensions  | ~75-85    | ~122        | DI + Config + Mediator   |
| 9-Full-AllExtensions | ~80       | ~198        | All extensions           |

Note: Single-run benchmarks have inherent variance (~10-15 ms). The key finding is that extensions 
add minimal overhead (~5-10 ms total) - the bulk of cost is in DI/Config/Mediator initialization.

#### Remaining Bottleneck

The ~35-40 ms gap between Empty (40 ms) and Full-NoExtensions (~80 ms) is in:
- DI container setup (`AddDependencyInjection()`)
- Configuration loading (`AddConfiguration()`)
- Mediator source generator registration (`services.AddMediator()`)

#### Progress Summary

| Metric             | Original  | Current   | Improvement       |
| ------------------ | --------- | --------- | ----------------- |
| Full-AllExtensions | 131.96 ms | ~80 ms    | **-52 ms (39%)**  |
| vs July 2025 (34 ms) | 3.8x slower | 2.3x slower | Significant progress |

### Key Insight

The 33x gap between Direct (3.98 ms) and Full (131.96 ms) suggests the bottleneck is NOT in:
- Route parsing
- Pattern matching  
- Delegate invocation

The bottleneck IS in:
- ~~Telemetry initialization~~ (FIXED)
- DI container initialization (REMAINING)
- Configuration loading (REMAINING)
- Mediator handler registration (REMAINING)

### Files Modified

- `source/timewarp-nuru/nuru-app-options.cs` - Added DisableTelemetry, DisableRepl, DisableCompletion, DisableInteractiveRoute
- `source/timewarp-nuru/nuru-app-builder-extensions.cs` - UseAllExtensions() respects new flags
- `source/timewarp-nuru-telemetry/nuru-telemetry-extensions.cs` - Lazy initialization with Lazy<T>, early exit when no OTLP endpoint
- `source/timewarp-nuru-core/nuru-core-app-builder.configuration.cs` - Changed reloadOnChange to false
- `benchmarks/timewarp-nuru-benchmarks/nuru-builder-cost-benchmark.cs` - New cost matrix benchmark
- `benchmarks/timewarp-nuru-benchmarks/program.cs` - Added --cost flag, restored CsProjCoreToolchain
- `benchmarks/timewarp-nuru-benchmarks/TimeWarp.Nuru.Benchmarks.csproj` - Renamed from kebab-case to match assembly name
- `timewarp-nuru.slnx` - Updated benchmark project reference
