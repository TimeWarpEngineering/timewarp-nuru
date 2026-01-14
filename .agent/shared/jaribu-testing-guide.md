# How to Write Jaribu Tests

## Overview

Jaribu is a lightweight, attribute-based testing framework specifically designed for .NET 10 file-based apps (runfiles). Unlike traditional testing frameworks like xUnit or NUnit, Jaribu tests are standalone executable C# files that leverage .NET 10's native single-file app support.

**Jaribu supports two execution modes:**
1. **Standalone Mode** - Each test file runs as an independent executable
2. **Multi-Mode** - Multiple test files compiled into a single assembly for faster CI execution

## Key Concepts

### What is Jaribu?

- **NOT a traditional test framework** - No xUnit, NUnit, or MSTest
- **Single-file executable tests** - Each test file is a standalone .NET app
- **Attribute-driven** - Uses attributes like `[Input]`, `[TestTag]`, `[ModuleInitializer]`
- **Built for .NET 10 runfiles** - Uses the `#!/usr/bin/dotnet --` shebang
- **Package-based** - Available as `TimeWarp.Jaribu` NuGet package
- **Dual-mode compatible** - Tests can run standalone OR compiled together in multi-mode

### Test File Structure (Multi-Mode Compatible)

Every Jaribu test file should follow this pattern to support both standalone and multi-mode execution:

```csharp
#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace Your.Namespace.Here
{

[TestTag("CategoryName")]
public class YourTestClassName
{
    [ModuleInitializer]
    internal static void Register() => RegisterTests<YourTestClassName>();

    public static async Task Should_describe_expected_behavior()
    {
        // Arrange
        // Act
        // Assert
        await Task.CompletedTask;
    }
}

} // namespace Your.Namespace.Here
```

### Legacy Standalone-Only Structure (Deprecated)

The old pattern without multi-mode support:

```csharp
#!/usr/bin/dotnet --

return await RunTests<YourTestClassName>(clearCache: true);

[TestTag("CategoryName")]
[ClearRunfileCache]
public class YourTestClassName
{
    public static async Task Should_describe_expected_behavior()
    {
        // Arrange
        // Act
        // Assert
        await Task.CompletedTask;
    }
}
```

**Note:** New tests should use the multi-mode compatible format.

## Writing Your First Test

### Step 1: Create the Test File

Create a new file with a descriptive name following the naming convention:
- `category-##-feature-description.cs`
- Examples: `lexer-01-basic-token-types.cs`, `routing-05-option-matching.cs`

### Step 2: Add the Shebang and Multi-Mode Guard

```csharp
#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif
```

The `#if !JARIBU_MULTI` guard ensures:
- **Standalone mode**: `RunAllTests()` executes all tests in the file
- **Multi-mode**: The top-level statement is skipped (tests discovered via `[ModuleInitializer]`)

### Step 3: Add a Namespace

Use a unique namespace to avoid type conflicts when multiple test files are compiled together:

```csharp
namespace TimeWarp.Nuru.Tests.YourCategory.YourFeature
{
```

### Step 4: Define Your Test Class with Registration

```csharp
[TestTag("MyCategory")]
public class MyTests
{
    [ModuleInitializer]
    internal static void Register() => RegisterTests<MyTests>();

    // Test methods go here
}
```

The `[ModuleInitializer]` attribute ensures your test class is automatically registered when compiled in multi-mode.

### Step 5: Close the Namespace

```csharp
} // namespace TimeWarp.Nuru.Tests.YourCategory.YourFeature
```

### Step 6: Write Test Methods

Test methods must:
- Be `public static async Task`
- Have descriptive names starting with `Should_`
- Return `await Task.CompletedTask` at the end

```csharp
public static async Task Should_perform_expected_action()
{
    // Arrange - Set up test conditions
    string input = "test value";
    
    // Act - Execute the code being tested
    string result = ProcessInput(input);
    
    // Assert - Verify the outcome
    result.ShouldBe("expected value");
    
    await Task.CompletedTask;
}
```

### Complete Multi-Mode Compatible Example

```csharp
#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.MyFeature
{

[TestTag("MyCategory")]
public class MyFeatureTests
{
    [ModuleInitializer]
    internal static void Register() => RegisterTests<MyFeatureTests>();

    public static async Task Should_perform_expected_action()
    {
        // Arrange
        string input = "test value";
        
        // Act
        string result = input.ToUpper();
        
        // Assert
        result.ShouldBe("TEST VALUE");
        
        await Task.CompletedTask;
    }

    public static async Task Should_handle_empty_input()
    {
        // Arrange
        string input = "";
        
        // Act
        string result = input.ToUpper();
        
        // Assert
        result.ShouldBeEmpty();
        
        await Task.CompletedTask;
    }
}

} // namespace TimeWarp.Nuru.Tests.MyFeature
```

### Step 5: Optional Setup and CleanUp Methods

Jaribu automatically calls `Setup()` before each test and `CleanUp()` after each test if they are defined:

```csharp
[TestTag("MyCategory")]
[ClearRunfileCache]
public class MyTests
{
    public static async Task Setup()
    {
        // Named Setup: invoked before EACH test
        WriteLine("Setup invoked - preparing test environment");
        // Initialize test data, create temp files, etc.
        await Task.CompletedTask;
    }

    public static async Task CleanUp()
    {
        // Named CleanUp: invoked after EACH test
        WriteLine("CleanUp invoked - cleaning test environment");
        // Delete temp files, reset state, etc.
        await Task.CompletedTask;
    }
    
    public static async Task Should_test_something()
    {
        // Setup is called before this test
        // CleanUp will be called after this test
        await Task.CompletedTask;
    }
}

## Testing Patterns

### Setup and CleanUp Pattern

Jaribu supports automatic setup and cleanup for tests. These methods are called automatically by the framework:
- `Setup()` - Called before EACH test method
- `CleanUp()` - Called after EACH test method

```csharp
public class DatabaseTests
{
    private static TestDatabase? _database;
    
    public static async Task Setup()
    {
        // Called before EACH test
        WriteLine("Setting up test database...");
        _database = new TestDatabase();
        await _database.InitializeAsync();
        await _database.SeedTestDataAsync();
    }
    
    public static async Task CleanUp()
    {
        // Called after EACH test
        WriteLine("Cleaning up test database...");
        if (_database != null)
        {
            await _database.ClearAsync();
            await _database.DisposeAsync();
            _database = null;
        }
    }
    
    public static async Task Should_query_test_data()
    {
        // Setup has been called, fresh database ready
        QueryResult result = await _database!.QueryAsync("SELECT * FROM Users");
        result.Count.ShouldBe(5);
        
        await Task.CompletedTask;
        // CleanUp will be called after this
    }
    
    public static async Task Should_insert_new_record()
    {
        // Fresh database from new Setup call
        await _database!.InsertAsync("Users", new { Name = "Test" });
        int count = await _database.CountAsync("Users");
        count.ShouldBe(6); // 5 seeded + 1 new
        
        await Task.CompletedTask;
        // CleanUp ensures next test starts fresh
    }
}
```

**Important Notes about Setup/CleanUp:**
- Both methods are optional - define only what you need
- They must be `public static async Task` methods
- Setup is called before EACH test, ensuring test isolation
- CleanUp is called after EACH test, even if the test fails
- Each test gets a fresh state from Setup
- Use them for test-specific initialization and cleanup

**For One-Time Class-Level Setup:**
If you need expensive one-time initialization, use static field initializers:

```csharp
public class PerformanceTests
{
    // One-time expensive initialization
    private static readonly ExpensiveResource SharedResource = InitializeExpensiveResource();
    
    private static ExpensiveResource InitializeExpensiveResource()
    {
        WriteLine("One-time initialization");
        return new ExpensiveResource();
    }
    
    public static async Task Setup()
    {
        // Per-test setup (if needed)
        WriteLine("Preparing test-specific state");
        await Task.CompletedTask;
    }
}

### Basic Test Pattern

```csharp
public static async Task Should_validate_simple_behavior()
{
    // Arrange
    string input = "hello";
    StringProcessor processor = new();
    
    // Act
    string result = processor.ToUpper(input);
    
    // Assert
    result.ShouldBe("HELLO");
    
    await Task.CompletedTask;
}
```

### Data-Driven Tests with [Input] Attribute

The `[Input]` attribute allows running the same test with different inputs:

```csharp
[Input("status")]
[Input("version")]
[Input("help")]
[Input("build")]
public static async Task Should_tokenize_plain_identifiers(string pattern)
{
    // Arrange
    Lexer lexer = CreateLexer(pattern);
    
    // Act
    IReadOnlyList<Token> tokens = lexer.Tokenize();
    
    // Assert
    tokens.Count.ShouldBe(2);
    tokens[0].Type.ShouldBe(TokenType.Identifier);
    tokens[0].Value.ShouldBe(pattern);
    
    await Task.CompletedTask;
}
```

### Testing with Captured State

For testing CLI frameworks like Nuru, capture state during execution:

```csharp
public static async Task Should_bind_string_parameter()
{
    // Arrange
    string? boundName = null;
    NuruApp app = new NuruAppBuilder()
        .AddRoute("greet {name}", (string name) => 
        { 
            boundName = name; 
            return 0; 
        })
        .Build();
    
    // Act
    int exitCode = await app.RunAsync(["greet", "Alice"]);
    
    // Assert
    exitCode.ShouldBe(0);
    boundName.ShouldBe("Alice");
    
    await Task.CompletedTask;
}
```

## Assertions with Shouldly

Jaribu uses the Shouldly assertion library for readable assertions:

```csharp
// Basic assertions
result.ShouldBe(expectedValue);
result.ShouldNotBe(unexpectedValue);
result.ShouldBeNull();
result.ShouldNotBeNull();

// Boolean assertions
flag.ShouldBeTrue();
flag.ShouldBeFalse();

// Numeric comparisons
count.ShouldBeGreaterThan(0);
value.ShouldBeLessThanOrEqualTo(100);

// String assertions
text.ShouldContain("substring");
text.ShouldStartWith("prefix");
text.ShouldEndWith("suffix");
text.ShouldBeEmpty();
text.ShouldNotBeEmpty();

// Collection assertions
list.Count.ShouldBe(5);
list.ShouldContain(item);
list.ShouldNotContain(item);
list.ShouldBeEmpty();

// Exception assertions
Should.Throw<ArgumentException>(() => MethodThatThrows());
Should.NotThrow(() => SafeMethod());
```

## Test Organization

### Directory Structure

```
Tests/
  TimeWarp.Nuru.Tests/
    Lexer/
      lexer-01-basic-token-types.cs
      lexer-02-valid-options.cs
      LexerTestHelper.cs
    Parser/
      parser-01-basic-parameters.cs
      parser-02-typed-parameters.cs
    Routing/
      routing-01-basic-matching.cs
      routing-02-parameter-binding.cs
  TimeWarp.Nuru.Completion.Tests/
    Static/
      completion-01-command-extraction.cs
    Dynamic/
      completion-14-dynamic-handler.cs
  Scripts/
    run-nuru-tests.cs
    run-all-tests.cs
```

### Test Naming Conventions

1. **File Names**: `category-##-feature.cs`
   - Category: lexer, parser, routing, completion, etc.
   - Number: 01, 02, 03... for logical ordering
   - Feature: descriptive name with hyphens

2. **Test Method Names**: `Should_<expected_behavior>_<context>`
   - Examples:
     - `Should_tokenize_plain_identifiers`
     - `Should_bind_string_parameter_greet_Alice`
     - `Should_not_match_missing_required_option`

3. **Test Class Names**: `<Feature>Tests`
   - Examples: `BasicTokenTypesTests`, `ParameterBindingTests`

## Test Attributes

### TestTag Attribute

Tags can be applied at class or method level for filtering:

```csharp
[TestTag("Lexer")]
public class LexerTests 
{
    [TestTag("Critical")]
    public static async Task Should_handle_critical_scenario() { }
}
```

### Skip Attribute

Skip tests with a reason:

```csharp
[Skip("Not implemented yet")]
public static async Task Should_handle_future_feature() 
{
    // This test won't run
    await Task.CompletedTask;
}
```

### Timeout Attribute

Set a timeout for long-running tests:

```csharp
[Timeout(5000)] // 5 seconds
public static async Task Should_complete_within_timeout()
{
    await SomeLongRunningOperation();
    await Task.CompletedTask;
}
```

### ClearRunfileCache Attribute (Standalone Only)

Clear the .NET runfile cache before tests run (only applies to standalone mode):

```csharp
[ClearRunfileCache] // Clears cache to ensure latest code changes
public class MyTests { }

// Or control it programmatically:
return await RunTests<MyTests>(clearCache: true);
```

**Note:** In multi-mode compatible tests, `[ClearRunfileCache]` is not needed because `RunAllTests()` handles cache clearing automatically.

### ModuleInitializer Attribute (Multi-Mode Required)

Register test classes for multi-mode discovery:

```csharp
[TestTag("MyCategory")]
public class MyTests
{
    [ModuleInitializer]
    internal static void Register() => RegisterTests<MyTests>();
    
    // Tests...
}
```

The `[ModuleInitializer]` ensures the test class is automatically registered when the assembly loads in multi-mode.

## Test Tags and Filtering

### Filtering Tests by Tag

Tests can be filtered using environment variables or parameters:

```bash
# Via environment variable
JARIBU_FILTER_TAG=Lexer ./run-nuru-tests.cs

# Via parameter in RunTests
return await RunTests<MyTests>(filterTag: "Critical");
```

Tag filtering rules:
- Class-level tags apply to all methods in the class
- Method-level tags can further filter within a tagged class
- If no methods match the filter, tests are skipped with a message

## Helper Classes and Utilities

### Creating Test Helpers

For shared test functionality, create helper classes:

```csharp
// LexerTestHelper.cs
public static class LexerTestHelper
{
    internal static Lexer CreateLexer(string pattern)
    {
        ILogger<Lexer>? logger = null;
        
        if (Environment.GetEnvironmentVariable("TRACE_LEXER") == "1")
        {
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddConsole();
            });
            logger = loggerFactory.CreateLogger<Lexer>();
        }
        
        return new Lexer(pattern, logger);
    }
    
    internal static IReadOnlyList<Token> Tokenize(string pattern)
    {
        Lexer lexer = CreateLexer(pattern);
        return lexer.Tokenize();
    }
}
```

### Using Helpers via Directory.Build.props

Configure helpers to be available in all test files:

```xml
<ItemGroup>
  <Compile Include="$(MSBuildThisFileDirectory)Lexer/LexerTestHelper.cs" 
           Link="Shared/LexerTestHelper.cs" />
</ItemGroup>

<ItemGroup>
  <Using Include="TimeWarp.Nuru.Tests.Lexer.LexerTestHelper" Static="true" />
</ItemGroup>
```

## Running Tests

### Individual Test Execution

```bash
# Make executable and run
chmod +x lexer-01-basic-token-types.cs
./lexer-01-basic-token-types.cs

# Or run with dotnet
dotnet run lexer-01-basic-token-types.cs
```

### Running Test Suites

Create runner scripts for test suites:

```csharp
#!/usr/bin/dotnet --
#:package TimeWarp.Amuru

using TimeWarp.Amuru;

string[] testFiles = [
    "lexer-01-basic-token-types.cs",
    "lexer-02-valid-options.cs",
    // ... more test files
];

int totalTests = 0;
int passedTests = 0;

foreach (string testFile in testFiles)
{
    totalTests++;
    WriteLine($"Running: {Path.GetFileName(testFile)}");
    
    CommandOutput result = await Shell.Builder(fullPath)
        .WithWorkingDirectory(Path.GetDirectoryName(fullPath)!)
        .WithNoValidation()
        .CaptureAsync();
    
    if (result.Success)
    {
        passedTests++;
        WriteLine("✅ PASSED");
    }
    else
    {
        WriteLine("❌ FAILED");
        WriteLine(result.Stdout);
    }
}

WriteLine($"Results: {passedTests}/{totalTests} test files passed");
return passedTests == totalTests ? 0 : 1;
```

## Dependencies Configuration

### Directory.Build.props Setup

Configure common dependencies for all test projects:

```xml
<Project>
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <NoWarn>$(NoWarn);CA1303;CA1307;CA2007</NoWarn>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- Testing packages -->
    <PackageReference Include="TimeWarp.Jaribu" />
    <PackageReference Include="Shouldly" />
    <PackageReference Include="Microsoft.Extensions.Logging" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" />
    
    <!-- Global usings -->
    <Using Include="Shouldly" />
    <Using Include="System.Console" Static="true" />
    <Using Include="TimeWarp.Jaribu" />
    <Using Include="TimeWarp.Jaribu.TestRunner" Static="true" />
  </ItemGroup>
  
  <ItemGroup>
    <!-- Project references -->
    <ProjectReference Include="../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj" />
  </ItemGroup>
</Project>
```

## Advanced Testing Patterns

### Testing with Project References

For tests that need access to internal types:

```csharp
#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru.Mcp/TimeWarp.Nuru.Mcp.csproj

return await RunTests<McpTests>(clearCache: true);

public class McpTests
{
    public static async Task Should_access_internal_types()
    {
        // Can now test internal classes from referenced project
        InternalComponent internalComponent = new();
        // ... test implementation
    }
}
```

### Testing with Mock Data

```csharp
public static async Task Should_handle_complex_scenario()
{
    // Arrange - Create test data
    string[] testRoutes =
    [
        "git commit -m {message}",
        "git push --force",
        "git pull --rebase"
    ];
    
    NuruAppBuilder builder = new();
    foreach (string route in testRoutes)
    {
        builder.AddRoute(route, () => 0);
    }
    
    // Act
    NuruApp app = builder.Build();
    int result = await app.RunAsync(["git", "commit", "-m", "test"]);
    
    // Assert
    result.ShouldBe(0);
    
    await Task.CompletedTask;
}
```

### Performance Testing

```csharp
public static async Task Should_complete_within_time_limit()
{
    // Arrange
    Stopwatch stopwatch = Stopwatch.StartNew();
    
    // Act
    for (int i = 0; i < 1000; i++)
    {
        Lexer lexer = new($"command-{i}");
        lexer.Tokenize();
    }
    
    stopwatch.Stop();
    
    // Assert
    stopwatch.ElapsedMilliseconds.ShouldBeLessThan(100);
    
    await Task.CompletedTask;
}
```

## Best Practices

### DO:
- ✅ Use descriptive test names that explain the expected behavior
- ✅ Follow the Arrange-Act-Assert pattern
- ✅ Keep tests focused on a single behavior
- ✅ Use helper methods to reduce duplication
- ✅ Include both positive and negative test cases
- ✅ Test edge cases and boundary conditions
- ✅ Use `await Task.CompletedTask` in all test methods
- ✅ Use Setup/CleanUp for per-test initialization and cleanup
- ✅ Clean up resources in CleanUp method (it runs even if test fails)
- ✅ Use static field initializers for expensive one-time setup

### DON'T:
- ❌ Don't use traditional test frameworks (xUnit, NUnit, MSTest)
- ❌ Don't make tests dependent on each other
- ❌ Don't test multiple behaviors in a single test method
- ❌ Don't hard-code absolute paths
- ❌ Don't ignore compilation warnings
- ❌ Don't skip the `ClearRunfileCache` attribute for consistency
- ❌ Don't put expensive operations in Setup if they can be shared (use static initializers instead)
- ❌ Don't forget that Setup/CleanUp run for EACH test

## Debugging Tests

### Enable Trace Logging

```bash
# Enable lexer trace logging
TRACE_LEXER=1 ./lexer-01-basic-token-types.cs

# Enable parser trace logging  
TRACE_PARSER=1 ./parser-01-basic-parameters.cs
```

### Interactive Debugging

Since tests are executable apps, you can debug them like any .NET application:

```bash
# Attach debugger
dotnet run --project lexer-01-basic-token-types.cs --debug

# Or use VS Code launch configuration
```

## Common Patterns for Nuru Tests

### Testing Route Patterns

```csharp
public static async Task Should_match_route_pattern()
{
    // Arrange
    bool routeExecuted = false;
    NuruApp app = new NuruAppBuilder()
        .AddRoute("deploy {env} --tag {version}", 
            (string env, string version) => 
            {
                routeExecuted = true;
                env.ShouldBe("prod");
                version.ShouldBe("v1.0");
                return 0;
            })
        .Build();
    
    // Act
    int exitCode = await app.RunAsync(["deploy", "prod", "--tag", "v1.0"]);
    
    // Assert
    exitCode.ShouldBe(0);
    routeExecuted.ShouldBeTrue();
    
    await Task.CompletedTask;
}
```

### Testing Error Conditions

```csharp
public static async Task Should_handle_missing_required_parameter()
{
    // Arrange
    NuruApp app = new NuruAppBuilder()
        .AddRoute("deploy {env}", (string env) => 0)
        .Build();
    
    // Act
    int exitCode = await app.RunAsync(["deploy"]); // Missing env parameter
    
    // Assert
    exitCode.ShouldBe(1); // Should fail
    
    await Task.CompletedTask;
}
```

### Testing Type Conversions

```csharp
public static async Task Should_convert_parameter_types()
{
    // Arrange
    int? capturedPort = null;
    NuruApp app = new NuruAppBuilder()
        .AddRoute("listen {port:int}", (int port) => 
        {
            capturedPort = port;
            return 0;
        })
        .Build();
    
    // Act
    int exitCode = await app.RunAsync(["listen", "8080"]);
    
    // Assert
    exitCode.ShouldBe(0);
    capturedPort.ShouldBe(8080);
    
    await Task.CompletedTask;
}
```

## Test Execution Flow

Understanding how Jaribu executes tests helps write better test suites:

```
1. RunTests<TestClass>() called
   ↓
2. Check for ClearRunfileCache attribute/parameter
   ↓
3. Clear cache if needed (skips current test executable)
   ↓
4. Check class-level TestTag filter
   ↓
5. For each public static async Task method:
   - Check if it's Setup/CleanUp (skip as test)
   - Check Skip attribute (skip with reason)
   - Check TestTag filter (skip if no match)
   - Check Input attributes (run once per input)
   - Call Setup() if defined
   - Run test with timeout if specified
   - Call CleanUp() if defined (even if test fails)
   - Track pass/fail
   ↓
6. Display summary and return exit code
```

**Per-Test Lifecycle:**
```
Setup() → Test Method → CleanUp()
         ↓ (if fails)
         CleanUp() still runs
```

## Summary

Jaribu testing provides a simple, executable approach to testing that aligns with .NET 10's file-based app philosophy. By following these patterns and conventions, you can write clear, maintainable tests that are easy to run and debug.

Key takeaways:
1. Each test is a standalone executable file
2. Setup/CleanUp run before/after EACH test for proper isolation
3. Use static initializers for expensive one-time setup
4. Use attributes for test control (Skip, Timeout, TestTag, Input)
5. Tests can be parameterized with [Input] attributes
6. Follow consistent naming conventions
7. Leverage Shouldly for readable assertions
8. Organize tests by feature/layer
9. Use helper classes for shared functionality
10. Clear runfile cache when testing code changes

Happy testing with Jaribu!