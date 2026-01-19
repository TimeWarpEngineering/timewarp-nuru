# Fix benchmark project missing InterceptorsNamespaces configuration

## Description

The `timewarp-nuru-benchmarks` project fails to build because the interceptors feature
is not enabled for the generated code namespace.

## Error

```
error CS9137: The 'interceptors' feature is not enabled in this namespace. 
Add '<InterceptorsNamespaces>$(InterceptorsNamespaces);TimeWarp.Nuru.Generated</InterceptorsNamespaces>' 
to your project.
```

## Affected Files

- `benchmarks/timewarp-nuru-benchmarks/timewarp-nuru-benchmarks.csproj`

## Checklist

- [x] Add `InterceptorsNamespaces` property to benchmark csproj
- [x] Verify benchmark project builds
- [ ] Run benchmarks to ensure they work (skipped - not blocking)

## Results

Fixed multiple issues in the benchmark project:

1. **Added InterceptorsNamespaces** to `timewarp-nuru-benchmarks.csproj` - fixes CS9137 error
2. **Fixed NuruCoreApp.CreateBuilder** → `NuruApp.CreateBuilder` in `nuru-direct-command.cs`
3. **Fixed handler return** - changed `() => { }` to `() => 0` for generator compatibility
4. **Replaced NuruMediatorCommand** with `NuruDirectCommand` in `cli-framework-benchmark.cs`
5. **Removed NuruBuilderCostBenchmark** reference in `program.cs` (benchmark no longer exists)

## Notes

Found during #341 migration. The Nuru source generator emits interceptors in 
`TimeWarp.Nuru.Generated` namespace, which requires explicit opt-in via the
`InterceptorsNamespaces` MSBuild property.

### Fix

Add to `.csproj`:
```xml
<PropertyGroup>
  <InterceptorsNamespaces>$(InterceptorsNamespaces);TimeWarp.Nuru.Generated</InterceptorsNamespaces>
</PropertyGroup>
```
