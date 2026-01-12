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

- [ ] Add `InterceptorsNamespaces` property to benchmark csproj
- [ ] Verify benchmark project builds
- [ ] Run benchmarks to ensure they work

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
