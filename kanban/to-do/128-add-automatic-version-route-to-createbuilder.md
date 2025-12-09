# Add automatic --version route to CreateBuilder

## Description

Add a built-in `--version,-v` route that is automatically registered when using `NuruApp.CreateBuilder()`. This route should display version information including commit hash and date when available (via TimeWarp.Build.Tasks). The route should NOT be added for `CreateSlimBuilder` or `CreateEmptyBuilder` to maintain their minimal/AOT-focused design.

## Requirements

1. **Add version route in `UseAllExtensions()`** (`source/timewarp-nuru/nuru-app-builder-extensions.cs`)
   - Register `--version,-v` route that displays:
     - Assembly informational version (or simple version as fallback)
     - Commit hash (if available from `AssemblyMetadataAttribute` with key "CommitHash")
     - Commit date (if available from `AssemblyMetadataAttribute` with key "CommitDate")
   - Only added when `CreateBuilder()` is used (not Slim/Empty)

2. **Add disable option to `NuruAppOptions`** (`source/timewarp-nuru/nuru-app-options.cs`)
   - Add `bool DisableVersionRoute { get; set; } = false;` property
   - Document that setting to `true` prevents auto-registration of `--version` route

3. **Add transitive TimeWarp.Build.Tasks dependency** (`source/timewarp-nuru/timewarp-nuru.csproj`)
   - Add `<PackageReference Include="TimeWarp.Build.Tasks" PrivateAssets="none" />` 
   - This automatically injects commit hash/date into user assemblies at build time

4. **AOT Compatibility**
   - Using `Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()` is AOT-safe (metadata baked at compile time)
   - No dynamic type instantiation required

5. **Tests**
   - Add test verifying `--version` route is registered by `CreateBuilder()`
   - Add test verifying `--version` route is NOT registered by `CreateSlimBuilder()`
   - Add test verifying `DisableVersionRoute = true` prevents registration

## Checklist

### Implementation
- [ ] Add `DisableVersionRoute` property to `NuruAppOptions`
- [ ] Add version route registration in `UseAllExtensions()`
- [ ] Add TimeWarp.Build.Tasks package reference with `PrivateAssets="none"`
- [ ] Implement version output handler using `Assembly.GetEntryAssembly()`

### Testing
- [ ] Test `--version` route is registered by `CreateBuilder()`
- [ ] Test `--version` route is NOT registered by `CreateSlimBuilder()`
- [ ] Test `DisableVersionRoute = true` prevents registration

### Documentation
- [ ] Document the `--version` route in user documentation
- [ ] Document `DisableVersionRoute` option

## Notes

- The version route should return exit code 0
- Use `Assembly.GetEntryAssembly()` to get the user's application assembly (not Nuru's assembly)
- Format output simply: version on first line, optional commit info on subsequent lines
- `AssemblyMetadataAttribute` lookup is AOT-safe since metadata is baked at compile time
