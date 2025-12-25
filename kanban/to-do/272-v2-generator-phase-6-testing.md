# V2 Generator Phase 6: Testing

## Description

Create tests to verify the V2 generator works end-to-end. Start with a minimal test case and incrementally expand to cover all features.

## Parent

#265 Epic: V2 Source Generator Implementation

## Checklist

### Commit 6.1: Create minimal test
- [ ] Create `tests/timewarp-nuru-core-tests/routing/temp-minimal-intercept-test.cs`
- [ ] Single route, no parameters
- [ ] Verify interceptor is generated
- [ ] Verify route matches and handler executes

### Incremental test expansion
- [ ] Route with parameter `{name}`
- [ ] Route with typed parameter `{count:int}`
- [ ] Multiple routes
- [ ] Nested groups (`WithGroupPrefix`)
- [ ] Options (`--flag`, `--config {value}`)
- [ ] Service injection (`ILogger<T>`)
- [ ] Attributed routes
- [ ] Mini-language patterns
- [ ] Full `dsl-example.cs`

### AOT validation
- [ ] Publish with `PublishAot=true`
- [ ] Verify no trimmer warnings
- [ ] Verify no runtime reflection

## Notes

### Minimal Test Case
```csharp
#!/usr/bin/env dotnet run
// Minimal test: single route, verify interceptor works

using TestTerminal terminal = new();

NuruCoreApp app = NuruApp.CreateBuilder(args)
    .UseTerminal(terminal)
    .Map("ping")
        .WithHandler(() => "pong")
        .AsQuery()
        .Done()
    .Build();

int exitCode = await app.RunAsync(["ping"]);

exitCode.ShouldBe(0);
terminal.OutputContains("pong").ShouldBeTrue();

WriteLine("Minimal intercept test passed");
return 0;
```

### Test Expansion Path
1. Single route, no params (verifies basic interception)
2. Single param `{name}` (verifies parameter extraction)
3. Typed param `{count:int}` (verifies type conversion)
4. Multiple routes (verifies route table generation)
5. Nested groups (verifies prefix handling)
6. Options (verifies flag/value parsing)
7. Services (verifies DI integration)
8. Attributed (verifies attribute scanning)
9. Mini-language (verifies PatternParser integration)
10. Full DSL example (verifies complete feature set)

### Debugging Generator Output
To inspect generated code:
```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)\GeneratedFiles</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Generated files appear in `obj/GeneratedFiles/`.
