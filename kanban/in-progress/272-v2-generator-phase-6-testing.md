# V2 Generator Phase 6: Testing

## Description

Create tests to verify the V2 generator works end-to-end. Start with a minimal test case and incrementally expand to cover all features.

## Parent

#265 Epic: V2 Source Generator Implementation

## Key References

**IMPORTANT: Read these before starting:**

1. **Architecture Document:**
   `.agent/workspace/2024-12-25T14-00-00_v2-source-generator-architecture.md`
   - Full pipeline design (Locate → Extract → Emit)
   - Expected generated code structure
   - Three DSL support patterns

2. **Test Framework Guide:**
   `.agent/how-to/how-to-write-jaribu-tests.md`
   - Jaribu test patterns (NOT xUnit/NUnit)
   - Multi-mode compatible structure
   - `[ModuleInitializer]` registration pattern
   - Setup/CleanUp lifecycle

3. **DSL Reference Implementation:**
   `tests/timewarp-nuru-core-tests/routing/dsl-example.cs`
   - Complete fluent DSL example
   - Full feature demonstration
   - Expected terminal output validation

4. **Existing Generator Tests:**
   `tests/timewarp-nuru-analyzers-tests/auto/nuru-invoker-generator-01-basic.cs`
   - Source generator test patterns
   - `CreateCompilationWithNuru()` helper
   - `RunGenerator()` / `RunGeneratorWithCompilation()` patterns
   - Assembly loading and generator discovery

5. **Generator Entry Point (Phase 5):**
   `source/timewarp-nuru-analyzers/generators/nuru-generator.cs`
   - The generator being tested
   - Produces `NuruGenerated.g.cs`

6. **Emitters (Phase 4):**
   `source/timewarp-nuru-analyzers/generators/emitters/`
   - `interceptor-emitter.cs` - Main output format
   - Expected generated code patterns

7. **Models (Phase 1):**
   `source/timewarp-nuru-analyzers/generators/models/`
   - `app-model.cs` - IR structure
   - Understanding what should be extracted

## Technical Notes

### Test Framework
- Use **Jaribu** (NOT xUnit/NUnit/MSTest)
- Tests are standalone executable files
- Use multi-mode compatible structure with `[ModuleInitializer]`
- Use Shouldly for assertions

### Test File Structure
```csharp
#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.V2
{

[TestTag("V2Generator")]
public class MinimalInterceptTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<MinimalInterceptTests>();

  public static async Task Should_intercept_simple_route()
  {
    // Test implementation
    await Task.CompletedTask;
  }
}

} // namespace
```

### Debugging Generator Output
To inspect generated code, add to project file:
```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)\GeneratedFiles</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```
Generated files appear in `obj/GeneratedFiles/`.

### TestTerminal Pattern
Use `TestTerminal` for capturing CLI output:
```csharp
using TestTerminal terminal = new();

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .UseTerminal(terminal)
  // ... routes
  .Build();

int exitCode = await app.RunAsync(["ping"]);

exitCode.ShouldBe(0);
terminal.OutputContains("pong").ShouldBeTrue();
```

## Checklist

### Commit 6.1: Create minimal test ✅
- [x] Create `tests/timewarp-nuru-core-tests/routing/temp-minimal-intercept-test.cs`
- [x] Single route, no parameters
- [x] Verify interceptor is generated
- [x] Verify route matches and handler executes

### Commit 6.2: Parameter tests ✅
- [x] Route with parameter `{name}`
- [x] Route with typed parameter `{count:int}`
- [x] Multiple typed parameters `{a:int} {b:int}`
- [ ] Optional parameter `{tag?}` (TODO)

### Commit 6.3: Multiple routes and specificity ✅
- [x] Multiple routes
- [x] Route specificity ordering (literal routes before parameterized)
- [x] Route-unique variable names to prevent conflicts
- [ ] Nested groups (`WithGroupPrefix`) (TODO)

### Commit 6.4: Options tests ✅
- [x] Boolean flag (`--verbose`)
- [x] Option with value (`--config {value}`)
- [x] Short form aliases (`--force,-f`)
- [x] Option with alias and value (`--output,-o {file}`)
- [x] Multiple options together

**Note:** Tests use unique command prefixes to avoid route conflicts when multiple tests share the same compilation unit.

### Commit 6.5: Advanced features
- [ ] Service injection (`ILogger<T>`)
- [ ] Attributed routes (`[NuruRoute]`)
- [ ] Mini-language patterns
- [ ] Full `dsl-example.cs` validation

### Commit 6.6: AOT validation
- [ ] Publish with `PublishAot=true`
- [ ] Verify no trimmer warnings
- [ ] Verify no runtime reflection

## Detailed Design

### Test Expansion Path

Each test builds on the previous, verifying incremental features:

| Test # | Feature | Verifies |
|--------|---------|----------|
| 1 | Single route, no params | Basic interception works |
| 2 | Single param `{name}` | Parameter extraction |
| 3 | Typed param `{count:int}` | Type conversion |
| 4 | Multiple routes | Route table generation |
| 5 | Nested groups | Prefix handling |
| 6 | Options | Flag/value parsing |
| 7 | Services | DI integration |
| 8 | Attributed | Attribute scanning |
| 9 | Mini-language | PatternParser integration |
| 10 | Full DSL | Complete feature set |

### Minimal Test Case

```csharp
#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.V2.Minimal
{

[TestTag("V2Generator")]
public class MinimalInterceptTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<MinimalInterceptTests>();

  public static async Task Should_intercept_single_route()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("ping")
        .WithHandler(() => "pong")
        .AsQuery()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["ping"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("pong").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_help_flag()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddHelp()
      .Map("ping")
        .WithHandler(() => "pong")
        .AsQuery()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("ping").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_return_error_for_unknown_command()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("ping")
        .WithHandler(() => "pong")
        .AsQuery()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["unknown"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.OutputContains("Unknown command").ShouldBeTrue();

    await Task.CompletedTask;
  }
}

} // namespace
```

### Parameter Test Case

```csharp
public static async Task Should_bind_string_parameter()
{
  // Arrange
  using TestTerminal terminal = new();

  NuruCoreApp app = NuruApp.CreateBuilder([])
    .UseTerminal(terminal)
    .Map("greet {name}")
      .WithHandler((string name) => $"Hello, {name}!")
      .AsQuery()
      .Done()
    .Build();

  // Act
  int exitCode = await app.RunAsync(["greet", "World"]);

  // Assert
  exitCode.ShouldBe(0);
  terminal.OutputContains("Hello, World!").ShouldBeTrue();

  await Task.CompletedTask;
}

public static async Task Should_bind_typed_int_parameter()
{
  // Arrange
  using TestTerminal terminal = new();
  int? capturedCount = null;

  NuruCoreApp app = NuruApp.CreateBuilder([])
    .UseTerminal(terminal)
    .Map("repeat {count:int}")
      .WithHandler((int count) => 
      {
        capturedCount = count;
        return $"Count: {count}";
      })
      .AsQuery()
      .Done()
    .Build();

  // Act
  int exitCode = await app.RunAsync(["repeat", "42"]);

  // Assert
  exitCode.ShouldBe(0);
  capturedCount.ShouldBe(42);
  terminal.OutputContains("Count: 42").ShouldBeTrue();

  await Task.CompletedTask;
}
```

### Group Test Case

```csharp
public static async Task Should_handle_nested_group_prefix()
{
  // Arrange
  using TestTerminal terminal = new();

  NuruCoreApp app = NuruApp.CreateBuilder([])
    .UseTerminal(terminal)
    .WithGroupPrefix("admin")
      .Map("status")
        .WithHandler(() => "admin status")
        .AsQuery()
        .Done()
      .WithGroupPrefix("config")
        .Map("get {key}")
          .WithHandler((string key) => $"config value for {key}")
          .AsQuery()
          .Done()
        .Done() // end config
      .Done() // end admin
    .Build();

  // Act - test nested route
  int exitCode = await app.RunAsync(["admin", "config", "get", "debug"]);

  // Assert
  exitCode.ShouldBe(0);
  terminal.OutputContains("config value for debug").ShouldBeTrue();

  await Task.CompletedTask;
}
```

### Options Test Case

```csharp
public static async Task Should_parse_boolean_flag()
{
  // Arrange
  using TestTerminal terminal = new();
  bool? capturedForce = null;

  NuruCoreApp app = NuruApp.CreateBuilder([])
    .UseTerminal(terminal)
    .Map("deploy {env}")
      .WithHandler((string env, bool force) =>
      {
        capturedForce = force;
        return force ? "Force deploying" : "Normal deploy";
      })
      .WithOption("--force", "-f", "Skip confirmation")
      .AsCommand()
      .Done()
    .Build();

  // Act
  int exitCode = await app.RunAsync(["deploy", "prod", "--force"]);

  // Assert
  exitCode.ShouldBe(0);
  capturedForce.ShouldBe(true);
  terminal.OutputContains("Force deploying").ShouldBeTrue();

  await Task.CompletedTask;
}
```

### Generator Output Verification Test

For testing the actual generated code:

```csharp
public static async Task Should_generate_interceptor_source()
{
  // Arrange
  const string code = """
    using TimeWarp.Nuru;
    
    var app = NuruApp.CreateBuilder([])
      .Map("ping")
        .WithHandler(() => "pong")
        .AsQuery()
        .Done()
      .Build();
    
    await app.RunAsync(args);
    """;

  // Act
  GeneratorDriverRunResult result = RunGenerator(code);

  // Assert - check that NuruGenerated.g.cs was created
  SyntaxTree? generatedTree = result.GeneratedTrees
    .FirstOrDefault(t => t.FilePath.Contains("NuruGenerated"));

  generatedTree.ShouldNotBeNull();

  string content = generatedTree.GetText().ToString();
  
  // Verify key elements in generated code
  content.ShouldContain("[InterceptsLocation");
  content.ShouldContain("RunAsync_Intercepted");
  content.ShouldContain("args is [\"ping\"]");
  content.ShouldContain("pong");

  await Task.CompletedTask;
}
```

## Testing Strategy

### Test Levels

1. **Unit Tests** - Test individual extractors/emitters in isolation
2. **Integration Tests** - Test full generator pipeline with synthetic code
3. **E2E Tests** - Test generated code actually works at runtime

### AOT Validation

```bash
# Publish with AOT
dotnet publish -c Release -r linux-x64 /p:PublishAot=true

# Check for warnings
# Output should have:
# - No IL2XXX warnings (trimming)
# - No IL3XXX warnings (AOT)
# - No runtime reflection calls
```

### Performance Considerations

- Generator should complete in < 100ms for typical apps
- Incremental compilation should skip unchanged files
- Generated code should have minimal runtime overhead

## Notes

### Expected Generated Output

For a simple `ping` route, the generated code should look like:

```csharp
// <auto-generated/>
#nullable enable

namespace TimeWarp.Nuru.Generated;

using System.Runtime.CompilerServices;

file static class GeneratedInterceptor
{
  [InterceptsLocation(@"path/to/file.cs", line: X, character: Y)]
  public static async Task<int> RunAsync_Intercepted(
    this NuruCoreApp app,
    string[] args,
    CancellationToken cancellationToken = default)
  {
    // Built-in: --version
    if (args is ["--version"])
    {
      PrintVersion(app.Terminal);
      return 0;
    }

    // Built-in: --capabilities
    if (args is ["--capabilities"])
    {
      PrintCapabilities(app.Terminal);
      return 0;
    }

    // Route: ping
    if (args is ["ping"])
    {
      string result = "pong";
      app.Terminal.WriteLine(result);
      return 0;
    }

    // No route matched
    app.Terminal.WriteError("Unknown command. Use --help for usage.");
    return 1;
  }

  private static void PrintVersion(ITerminal terminal) { ... }
  private static void PrintCapabilities(ITerminal terminal) { ... }
}
```

### Common Test Failures

1. **Generator not found** - Ensure analyzer assembly is built
2. **No output generated** - Check for null `InterceptSiteModel`
3. **Wrong interception** - Verify `RunAsync` call site detection
4. **Missing routes** - Check `FluentChainExtractor` chain walking
