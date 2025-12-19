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

### Profiling
- [ ] Profile `NuruApp.CreateBuilder()` cold start with dotnet-trace
- [ ] Identify top 10 time-consuming operations
- [ ] Capture flame graph of startup
- [ ] Compare to `NuruCoreApp.CreateEmptyBuilder()` profile

### Analysis
- [ ] Compare July 2025 vs December 2025 codebase changes
- [ ] Identify what was added to DI container
- [ ] Check Mediator source generator output size
- [ ] Review `UseAllExtensions()` overhead
- [ ] Check Configuration loading time

### Hypotheses to Test
- [ ] Is Mediator handler scanning slow?
- [ ] Is Configuration loading slow?
- [ ] Is DI container setup slow?
- [ ] Are source generators adding startup cost?
- [ ] Is there excessive reflection despite source gen?

### Fixes (TBD based on findings)
- [ ] Implement lazy initialization where possible
- [ ] Optimize hot paths
- [ ] Consider tiered startup
- [ ] Reduce memory allocations

## Notes

### Key Insight

The 33x gap between Direct (3.98 ms) and Full (131.96 ms) suggests the bottleneck is NOT in:
- Route parsing
- Pattern matching
- Delegate invocation

The bottleneck IS likely in:
- DI container initialization
- Mediator handler registration
- Configuration loading
- Extension method overhead

### Previous Benchmark Context

In July 2025, we had:
- Reflection-based delegate invocation (slower)
- Less source generation
- Simpler DI setup

Yet we were at 34.42 ms (middle of pack).

Now with MORE source generation, we're at 131.96 ms (last place).

**Something is very wrong with our startup path.**

### Files to Investigate

- `source/timewarp-nuru/nuru-app-builder.cs` - Full builder setup
- `source/timewarp-nuru/nuru-app-builder-extensions.cs` - UseAllExtensions()
- `source/timewarp-nuru-core/service-collection-extensions.cs` - DI registration
- Generated Mediator code in `artifacts/generated/`
