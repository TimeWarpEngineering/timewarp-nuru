# Add Source Generation Verification Tests for Attributed Routes

## Description

Create tests that verify the `NuruAttributedRouteGenerator` source generator produces correct `CompiledRouteBuilder` code. Current tests only verify registration happened; these tests verify the generated source code is correct.

## Parent

150-implement-attributed-routes-phase-1

## Checklist

### Test Utilities (shared file)
- [x] Create `tests/timewarp-nuru-analyzers-tests/auto/attributed-route-test-helpers.cs`
- [x] Add to `Directory.Build.props` as shared compile include
- [x] Implement `RunAttributedRouteGenerator(string source)` helper
- [x] Implement `CreateCompilationWithNuruAttributes(string source)` helper
- [x] Reuse patterns from `nuru-invoker-generator-01-basic.cs`

### Test File
- [x] Create `tests/timewarp-nuru-analyzers-tests/auto/attributed-route-generator-02-source.cs`

### Literal Generation Tests
- [x] Test simple literal route: `[NuruRoute("status")]` -> `.WithLiteral("status")`
- [x] Test empty default route: `[NuruRoute("")]` -> no `.WithLiteral()` call
- [x] Test multi-word literal: `[NuruRoute("docker compose up")]` -> three `.WithLiteral()` calls

### Parameter Generation Tests
- [x] Test required parameter: `[Parameter] string Name` -> `.WithParameter("name")` without `isOptional`
- [x] Test optional parameter from nullability: `string?` -> `isOptional: true`
- [x] Test typed parameter: `int Count` -> `type: "int"`
- [x] Test catch-all parameter: `IsCatchAll=true` -> `.WithCatchAll("args")`
- [x] Test parameter with description

### Option Generation Tests
- [x] Test bool flag: `[Option("force", "f")] bool` -> `.WithOption("force", shortForm: "f")` without `expectsValue`
- [x] Test valued option: `string Config` -> `expectsValue: true`
- [x] Test optional valued option: `string?` -> `parameterIsOptional: true`
- [x] Test typed valued option: `int Replicas` -> `parameterType: "int"`
- [x] Test long form only (no short): no `shortForm` parameter
- [x] Test repeated option: `IsRepeated=true` -> `isRepeated: true`
- [x] Test option with description

### Group and Alias Generation Tests
- [x] Test group prefix: `[NuruRouteGroup("docker")]` -> `.WithLiteral("docker")` before route literals
- [x] Test group options: `[GroupOption("debug")]` -> `.WithOption("debug")` on child routes
- [x] Test alias routes: `[NuruRouteAlias("bye", "cya")]` -> separate `__Route_*_Alias_*` constants

### Infrastructure Generation Tests
- [x] Test module initializer: `[ModuleInitializer]` attribute present
- [x] Test pattern string generation: correct pattern like `"deploy {env} --force,-f"`
- [x] Test route description in `Register()` call

## Notes

### Implementation Pattern

```csharp
string source = """
  using TimeWarp.Nuru;
  using Mediator;
  
  [NuruRoute("status")]
  public sealed class StatusRequest : IRequest { }
  """;

GeneratorDriverRunResult result = RunAttributedRouteGenerator(source);
SyntaxTree? generated = result.GeneratedTrees
  .FirstOrDefault(t => t.FilePath.Contains("GeneratedAttributedRoutes"));

string code = generated.GetText().ToString();
code.ShouldContain(".WithLiteral(\"status\")");
```

### Reference

- Existing generator test: `tests/timewarp-nuru-analyzers-tests/auto/nuru-invoker-generator-01-basic.cs`
- Generator source: `source/timewarp-nuru-analyzers/analyzers/nuru-attributed-route-generator.cs`

## Results

- Created `attributed-route-test-helpers.cs` with shared utilities
- Created `attributed-route-generator-02-source.cs` with 20 passing tests
- Added `Microsoft.CodeAnalysis.CSharp` package to Directory.Build.props
- All tests pass (21/21)
- CI tests unaffected (same 12 pre-existing failures)
