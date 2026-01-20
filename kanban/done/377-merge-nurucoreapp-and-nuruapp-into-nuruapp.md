# Merge NuruCoreApp and NuruApp into Single NuruApp Class

## Description

Consolidate `NuruCoreApp` and `NuruApp` into a single unified `NuruApp` class. First merge the factory method from `NuruApp` into `NuruCoreApp`, then rename the class.

## Requirements

- `CreateBuilder()` factory method moved from `NuruApp` to `NuruCoreApp`
- `NuruCoreApp` class renamed to `NuruApp`
- `nuru-app.cs` deleted (factory method merged)
- `NuruCoreAppBuilder` merged into `NuruAppBuilder` (flatten hierarchy)
- All references updated across source code, tests, samples, and documentation
- No type alias - clean rename only

## Checklist

### Phase 1: Merge (Move CreateBuilder into NuruCoreApp)
- [x] Open `source/timewarp-nuru/nuru-app.cs` and copy the `CreateBuilder()` method
- [x] Open `source/timewarp-nuru/nuru-core-app.cs` and paste `CreateBuilder()` into the `NuruCoreApp` class
- [x] Delete `source/timewarp-nuru/nuru-app.cs`

### Phase 2: Rename (NuruCoreApp → NuruApp)
- [x] F2 rename class `NuruCoreApp` → `NuruApp` in `nuru-core-app.cs`
- [x] Rename file `nuru-core-app.cs` → `nuru-app.cs`
- [x] Update `source/timewarp-nuru-analyzers/generators/locators/build-locator.cs` lines 43, 71:
  - Change `"NuruCoreApp"` to `"NuruApp"`
- [x] Fix compilation errors in:
  - `builders/nuru-core-app-builder.cs` - Build() return type
  - `builders/endpoint-builder.cs` - Build() return type
  - `io/nuru-test-context.cs` - delegate and method signatures
  - `repl/repl-session.cs` - delegate, field, and method signatures

### Phase 3: MCP Server Tools
- [x] Updated 3 MCP server tools (get-attributed-route-tool, get-type-converter-tool, get-behavior-tool)
- [x] `get-version-info-tool.cs` - `typeof(NuruCoreApp)` kept for assembly reflection

### Phase 4: Verification
- [x] Run `dotnet build timewarp-nuru.slnx -c Release` ✅ Build succeeded!
- [x] Run `dotnet run tests/ci-tests/run-ci-tests.cs` ✅ 1016/1023 passed (7 skipped)
- [x] Run samples to verify they work ✅ Aspire sample works!

### Phase 5: Clean up remaining references
- [x] Update 21 test/sample files with `NuruCoreApp app` → `NuruApp app`
- [x] Update static field declarations (`NuruCoreApp? App` → `NuruApp? App`)
- [x] Update XML doc comments referencing `NuruCoreApp`
- [x] Update code generators to recognize NuruApp instead of NuruCoreApp:
  - `behavior-emitter.cs` - Update type name checks
  - `handler-invoker-emitter.cs` - Update type name checks
  - `interceptor-emitter.cs` - Update generated code to use NuruApp parameter type
  - `repl-emitter.cs` - Update generated code to use NuruApp parameter type
  - `service-resolver-emitter.cs` - Rename IsNuruCoreAppType → IsNuruAppType
  - `dsl-interpreter.cs` - Remove NuruCoreApp from type name check

### Phase 6: Merge NuruCoreAppBuilder into NuruAppBuilder
- [x] Flatten builder hierarchy: move NuruCoreAppBuilder content into NuruAppBuilder
- [x] Move fields/methods from `builders/nuru-core-app-builder/nuru-core-app-builder.cs`
- [x] Move methods from `builders/nuru-core-app-builder/nuru-core-app-builder.configuration.cs`
- [x] Move methods from `builders/nuru-core-app-builder/nuru-core-app-builder.routes.cs`
- [x] Delete `builders/nuru-core-app-builder/` directory
- [x] Update extension methods: `NuruCoreAppBuilder<TBuilder>` → `NuruAppBuilder`
- [x] Update `EndpointBuilder<NuruCoreAppBuilder>` → `EndpointBuilder<NuruAppBuilder>`
- [x] Delete `nuru-core-app-builder.factory.cs`
- [x] Build verification ✅
- [x] Run CI tests ✅ 1016/1023 passed

### Phase 7: Remove Dead Code
- [x] Delete `NuruAppOptions` class (properties never read - dead code)
- [x] Delete `options/nuru-application-options.cs` file
- [x] Remove `args` parameter from `CreateBuilder()` (checked for null, never used)
- [x] Remove incomplete `IHostApplicationBuilder` interface from NuruAppBuilder
- [x] Remove `NuruHostEnvironment` class (only used for IHostApplicationBuilder)
- [x] Remove `IDisposable` implementation (not needed)
- [x] Build verification ✅
- [x] Run CI tests ✅ 1016/1023 passed

### Additional Changes Made
- Renamed `services/nuru-core-app-holder.cs` → `services/nuru-app-holder.cs`
- Deleted `nuru-app-static.cs` (conflicting static partial class)
- Restored `NuruApp.CreateBuilder()` factory method (was lost during rename, fixed in commit 1084c436)

## References that MUST remain as `NuruCoreApp`

These cannot be changed because they serve a specific purpose:

- `typeof(NuruCoreApp)` - for assembly reflection in get-version-info-tool

## Performance Results

**Benchmark comparison (2026-01-20):**
- Nuru is **nearly matching ConsoleAppFramework** in performance
- Nuru has **significantly more features** (source generators, behaviors, REPL, telemetry, completion)
- Trade-off: Same speed, way more functionality = win for Nuru!

See: `benchmarks/aot-benchmarks/results/`

## Notes

See analysis report: `.agent/workspace/2026-01-20T00-00-00_nuru-core-app-nuru-app-merge-analysis.md`

**Key files:**
- `source/timewarp-nuru/nuru-app.cs` - renamed from `nuru-core-app.cs`
- `source/timewarp-nuru-analyzers/generators/locators/build-locator.cs` - lines 43, 71

**What F2 will NOT catch:**
- MCP server tool code generation strings (manual updates needed)
- String literals in code (grep to find)
- Documentation markdown files (grep to find)
