# Merge NuruCoreApp and NuruApp into Single NuruApp Class

## Description

Consolidate `NuruCoreApp` and `NuruApp` into a single unified `NuruApp` class. First merge the factory method from `NuruApp` into `NuruCoreApp`, then rename the class.

## Requirements

- `CreateBuilder()` factory method moved from `NuruApp` to `NuruCoreApp`
- `NuruCoreApp` class renamed to `NuruApp`
- `nuru-app.cs` deleted (factory method merged)
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
- [ ] Run `dotnet run tests/ci-tests/run-ci-tests.cs`
- [ ] Run samples to verify they work

### Phase 5: Clean up remaining references
- [x] Update 21 test/sample files with `NuruCoreApp app` → `NuruApp app`
- [x] Update static field declarations (`NuruCoreApp? App` → `NuruApp? App`)
- [x] Update XML doc comments referencing `NuruCoreApp`

### Additional Changes Made
- Renamed `services/nuru-core-app-holder.cs` → `services/nuru-app-holder.cs`
- Deleted `nuru-app-static.cs` (conflicting static partial class)

## Remaining References (intentionally not changed)

These remain as `NuruCoreApp` because they must for code generation correctness:

- `NuruCoreApplicationOptions` - separate options class (not being renamed)
- `NuruCoreAppBuilder` - separate builder class (not being renamed)
- `typeof(NuruCoreApp)` - for assembly reflection in get-version-info-tool
- String literals in generator emitters that output `NuruCoreApp` in generated code
- Comments in generator code explaining generated code behavior

## Notes

See analysis report: `.agent/workspace/2026-01-20T00-00-00_nuru-core-app-nuru-app-merge-analysis.md`

**Key files:**
- `source/timewarp-nuru/nuru-app.cs` - renamed from `nuru-core-app.cs`
- `source/timewarp-nuru-analyzers/generators/locators/build-locator.cs` - lines 43, 71

**What F2 will NOT catch:**
- MCP server tool code generation strings (manual updates needed)
- String literals in code (grep to find)
- Documentation markdown files (grep to find)
