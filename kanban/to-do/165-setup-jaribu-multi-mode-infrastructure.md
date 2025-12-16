# Setup Jaribu multi-mode infrastructure

## Description

Create the infrastructure needed for Jaribu multi-mode test execution. This is Phase 1 of task 164 (Migrate tests to Jaribu multi-mode for faster execution).

This task sets up the framework that allows all test files to be compiled together once, reducing execution time from ~15 minutes to ~10-30 seconds.

## Checklist

- [ ] Update `tests/Directory.Build.props` - Add `<Using Include="System.Runtime.CompilerServices" />` to enable `[ModuleInitializer]` attribute
- [ ] Create `tests/ci-tests/` directory
- [ ] Create `tests/ci-tests/Directory.Build.props` with JARIBU_MULTI define and file includes
- [ ] Create `tests/ci-tests/run-ci-tests.cs` orchestrator entry point
- [ ] Verify orchestrator compiles (even if no tests register yet)

## Implementation Details

### 1. Update tests/Directory.Build.props

Add to the `<ItemGroup>` section:
```xml
<Using Include="System.Runtime.CompilerServices" />
```

### 2. Create tests/ci-tests/ directory structure

```
tests/ci-tests/
├── Directory.Build.props    # Defines JARIBU_MULTI, includes all test files
└── run-ci-tests.cs          # Orchestrator entry point
```

### 3. Create tests/ci-tests/Directory.Build.props

```xml
<Project>
  <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />
  <PropertyGroup>
    <DefineConstants>$(DefineConstants);JARIBU_MULTI</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <!-- Test files will be added here as they are migrated to multi-mode pattern -->
    <!-- Example (do not add until files are migrated):
    <Compile Include="../timewarp-nuru-tests/lexer/*.cs" Exclude="../timewarp-nuru-tests/lexer/*helper*.cs" />
    -->
  </ItemGroup>
</Project>
```

**Note:** Do NOT include any test files yet. Files will be added to this ItemGroup as each category is migrated in subsequent tasks.

### 4. Create tests/ci-tests/run-ci-tests.cs

```csharp
#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj
#:project ../../source/timewarp-nuru-completion/timewarp-nuru-completion.csproj
#:project ../../source/timewarp-nuru-mcp/timewarp-nuru-mcp.csproj

// Multi-mode Test Runner
// Test classes are auto-registered via [ModuleInitializer] when compiled with JARIBU_MULTI.

WriteLine("TimeWarp.Nuru Multi-Mode Test Runner");
WriteLine();

return await RunAllTests();
```

## Parent Task

This is Phase 1 of [task 164](../in-progress/164-migrate-tests-to-jaribu-multi-mode-for-faster-execution.md).

## Notes

- The orchestrator will show "No test classes registered" until test files are migrated in subsequent tasks
- Do NOT add any test file includes yet - files are added as each category is migrated
- Helper files will be excluded via `*helper*.cs` pattern when includes are added
- All project references are included in the orchestrator to support tests across all categories
