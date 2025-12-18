# Add Source Generation Verification Tests for Attributed Routes

## Description

Create tests that verify the `NuruAttributedRouteGenerator` source generator produces correct `CompiledRouteBuilder` code. Current tests only verify registration happened; these tests verify the generated source code is correct.

## Parent

150-implement-attributed-routes-phase-1

## Checklist

### Test Utilities (shared file)
- [ ] Create `tests/timewarp-nuru-analyzers-tests/auto/attributed-route-test-helpers.cs`
- [ ] Add to `Directory.Build.props` as shared compile include
- [ ] Implement `RunAttributedRouteGenerator(string source)` helper
- [ ] Implement `CreateCompilationWithNuruAttributes(string source)` helper
- [ ] Reuse patterns from `nuru-invoker-generator-01-basic.cs`

### Test File
- [ ] Create `tests/timewarp-nuru-analyzers-tests/auto/attributed-route-generator-02-source.cs`

### Literal Generation Tests
- [ ] Test simple literal route: `[NuruRoute("status")]` -> `.WithLiteral("status")`
- [ ] Test empty default route: `[NuruRoute("")]` -> no `.WithLiteral()` call
- [ ] Test multi-word literal: `[NuruRoute("docker compose up")]` -> three `.WithLiteral()` calls

### Parameter Generation Tests
- [ ] Test required parameter: `[Parameter] string Name` -> `.WithParameter("name")` without `isOptional`
- [ ] Test optional parameter from nullability: `string?` -> `isOptional: true`
- [ ] Test typed parameter: `int Count` -> `type: "int"`
- [ ] Test catch-all parameter: `IsCatchAll=true` -> `.WithCatchAll("args")`
- [ ] Test parameter with description

### Option Generation Tests
- [ ] Test bool flag: `[Option("force", "f")] bool` -> `.WithOption("force", shortForm: "f")` without `expectsValue`
- [ ] Test valued option: `string Config` -> `expectsValue: true`
- [ ] Test optional valued option: `string?` -> `parameterIsOptional: true`
- [ ] Test typed valued option: `int Replicas` -> `parameterType: "int"`
- [ ] Test long form only (no short): no `shortForm` parameter
- [ ] Test repeated option: `IsRepeated=true` -> `isRepeated: true`
- [ ] Test option with description

### Group and Alias Generation Tests
- [ ] Test group prefix: `[NuruRouteGroup("docker")]` -> `.WithLiteral("docker")` before route literals
- [ ] Test group options: `[GroupOption("debug")]` -> `.WithOption("debug")` on child routes
- [ ] Test alias routes: `[NuruRouteAlias("bye", "cya")]` -> separate `__Route_*_Alias_*` constants

### Infrastructure Generation Tests
- [ ] Test module initializer: `[ModuleInitializer]` attribute present
- [ ] Test pattern string generation: correct pattern like `"deploy {env} --force,-f"`
- [ ] Test route description in `Register()` call

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
