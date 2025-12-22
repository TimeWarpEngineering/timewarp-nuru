# Benchmark cold start and update documentation

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Measure and document the cold start improvement achieved by compile-time generation. Update user documentation to reflect the new source-generated approach.

## Requirements

- Benchmark cold start before/after
- Document improvements with numbers
- Update user guides for new patterns
- Update developer docs explaining architecture

## Checklist

- [ ] Create benchmark comparing old vs new startup time
- [ ] Measure with various app sizes (few routes, many routes)
- [ ] Measure AOT binary size difference
- [ ] Document results in optimization-results.md
- [ ] Update user guide: getting started
- [ ] Update user guide: attributed routes
- [ ] Update user guide: configuration
- [ ] Update developer guide: architecture
- [ ] Update developer guide: source generators
- [ ] Add migration guide for existing users
- [ ] Update README with performance claims

## Notes

### Benchmark Approach

```csharp
// Cold start benchmark
for (int i = 0; i < 100; i++)
{
    var sw = Stopwatch.StartNew();
    
    // Spawn new process to ensure cold start
    var result = await Shell.Builder("./myapp")
        .WithArguments("--version")
        .CaptureAsync();
    
    times.Add(sw.ElapsedMilliseconds);
}

Console.WriteLine($"Avg: {times.Average():F1}ms");
Console.WriteLine($"P50: {Percentile(times, 50):F1}ms");
Console.WriteLine($"P99: {Percentile(times, 99):F1}ms");
```

### Expected Results

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Cold start (simple app) | ~50ms | ~5ms | 10x |
| Cold start (complex app) | ~100ms | ~10ms | 10x |
| AOT binary size | Xmb | Ymb | Z% smaller |
| Build() time | ~Xms | <1ms | Nx |

### Documentation Updates

- `documentation/user/guides/getting-started.md`
- `documentation/user/guides/attributed-routes.md`
- `documentation/developer/guides/source-generators.md`
- `documentation/developer/design/compile-time-generation.md` (new)
- `README.md` - performance section
