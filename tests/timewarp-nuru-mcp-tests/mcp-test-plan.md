# MCP Test Plan

> **See also**: [Test Plan Overview](../TimeWarp.Nuru.Tests/test-plan-overview.md) for the three-layer testing architecture and shared philosophy.

This test plan covers **MCP (Model Context Protocol) Tools** - providing TimeWarp.Nuru code examples, syntax documentation, route validation, and handler generation to AI assistants.

## Scope

The MCP layer is responsible for:

1. **Example Retrieval** - Fetching and caching production-quality code samples
2. **Syntax Documentation** - Providing route pattern syntax references
3. **Route Validation** - Validating route patterns and providing detailed feedback
4. **Handler Generation** - Generating delegate and mediator handler code
5. **Error Documentation** - Documenting error handling architecture and scenarios
6. **Cache Management** - Managing disk and memory caches for GitHub-fetched content
7. **Server Integration** - Exposing tools via Model Context Protocol (JSON-RPC 2.0)

Tests use numbered files (mcp-01, mcp-02, etc.) for systematic coverage, with 7 sections covering ~40 test scenarios.

---

## Section 1: Example Retrieval (GetExampleTool)

**Purpose**: Verify code example fetching, caching, and GitHub manifest integration.

### Test Cases

1. **List All Examples**
   - Method: `GetExampleTool.ListExamplesAsync()`
   - Expected: Returns formatted list of available examples
   - Should contain: `basic`, `mixed`, `delegate`, `mediator`, `console-logging`, `serilog`

2. **Get Specific Example (basic)**
   - Method: `GetExampleTool.GetExampleAsync("basic")`
   - Expected: Returns calc-mixed.cs content with description header
   - Content length: > 500 characters

3. **Get Specific Example (delegate)**
   - Method: `GetExampleTool.GetExampleAsync("delegate")`
   - Expected: Returns calc-delegate.cs content
   - Should demonstrate: Pure delegate routing, AddAutoHelp, named parameters

4. **Get Specific Example (mediator)**
   - Method: `GetExampleTool.GetExampleAsync("mediator")`
   - Expected: Returns calc-mediator.cs content
   - Should demonstrate: Mediator pattern, DI, AddAutoHelp

5. **Get Specific Example (mixed)**
   - Method: `GetExampleTool.GetExampleAsync("mixed")`
   - Expected: Returns calc-mixed.cs content
   - Should demonstrate: Hybrid approach combining delegate and mediator

6. **Get Specific Example (console-logging)**
   - Method: `GetExampleTool.GetExampleAsync("console-logging")`
   - Expected: Returns console logging integration example

7. **Get Specific Example (serilog)**
   - Method: `GetExampleTool.GetExampleAsync("serilog")`
   - Expected: Returns Serilog integration example

8. **Unknown Example Handling**
   - Method: `GetExampleTool.GetExampleAsync("nonexistent")`
   - Expected: Returns error message with available examples list

9. **Force Refresh**
   - Method: `GetExampleTool.GetExampleAsync("basic", forceRefresh: true)`
   - Expected: Bypasses cache, fetches from GitHub
   - Result should match cached version

10. **Memory Cache Hit**
    - First call: Fetch example (cache miss)
    - Second call: Same example (cache hit)
    - Expected: Both return identical content, second is faster

11. **Disk Cache Persistence**
    - Check cache directory existence
    - Verify .cache and .meta files created
    - Expected: Files in `~/.local/share/TimeWarp.Nuru.Mcp/cache/examples/`

12. **Cache TTL Expiration**
    - Verify cache entries have timestamps
    - Expected: 1-hour TTL for example cache, 24-hour for manifest

13. **GitHub Unavailable Fallback**
    - Simulate network failure
    - Expected: Uses fallback examples from FallbackExamples dictionary

14. **List Command**
    - Method: `GetExampleTool.GetExampleAsync("list")`
    - Expected: Returns same result as ListExamplesAsync()

---

## Section 2: Syntax Documentation (GetSyntaxTool)

**Purpose**: Verify route pattern syntax reference retrieval.

### Test Cases

1. **Get All Syntax**
   - Method: `GetSyntaxTool.GetSyntax("all")`
   - Expected: Complete route pattern syntax reference
   - Should contain: "Route Pattern Syntax Reference" header

2. **Get Literals Syntax**
   - Method: `GetSyntaxTool.GetSyntax("literals")`
   - Expected: Documentation for literal segments
   - Should contain: Examples like `status`, `git commit`

3. **Get Parameters Syntax**
   - Method: `GetSyntaxTool.GetSyntax("parameters")`
   - Expected: Documentation for `{name}` syntax
   - Should contain: Basic, typed, optional, catch-all examples

4. **Get Types Syntax**
   - Method: `GetSyntaxTool.GetSyntax("types")`
   - Expected: Documentation for `:int`, `:double`, `:bool`
   - Should contain: Type constraint examples

5. **Get Optional Syntax**
   - Method: `GetSyntaxTool.GetSyntax("optional")`
   - Expected: Documentation for `?` modifier
   - Should contain: Nullability-based optionality explanation

6. **Get Catch-All Syntax**
   - Method: `GetSyntaxTool.GetSyntax("catchall")`
   - Expected: Documentation for `{*args}` syntax
   - Should contain: Array parameter examples

7. **Get Options Syntax**
   - Method: `GetSyntaxTool.GetSyntax("options")`
   - Expected: Documentation for `--long`, `-short` flags
   - Should contain: Required/optional, value/flag, repeated, aliases

8. **Get Descriptions Syntax**
   - Method: `GetSyntaxTool.GetSyntax("descriptions")`
   - Expected: Documentation for `|Description` syntax
   - Should contain: AddAutoHelp integration

9. **Partial Match (param)**
   - Method: `GetSyntaxTool.GetSyntax("param")`
   - Expected: Matches "parameters" element

10. **Partial Match (opt)**
    - Method: `GetSyntaxTool.GetSyntax("opt")`
    - Expected: Matches "optional" or "options" element

11. **Partial Match (catch)**
    - Method: `GetSyntaxTool.GetSyntax("catch")`
    - Expected: Matches "catchall" element

12. **Unknown Element Handling**
    - Method: `GetSyntaxTool.GetSyntax("foobar")`
    - Expected: Error message with available elements list

13. **Pattern Examples (basic)**
    - Method: `GetSyntaxTool.GetPatternExamples("basic")`
    - Expected: Simple pattern examples with code blocks

14. **Pattern Examples (typed)**
    - Method: `GetSyntaxTool.GetPatternExamples("typed")`
    - Expected: Type constraint examples

15. **Pattern Examples (optional)**
    - Method: `GetSyntaxTool.GetPatternExamples("optional")`
    - Expected: Optional parameter examples

16. **Pattern Examples (catchall)**
    - Method: `GetSyntaxTool.GetPatternExamples("catchall")`
    - Expected: Catch-all parameter examples

17. **Pattern Examples (options)**
    - Method: `GetSyntaxTool.GetPatternExamples("options")`
    - Expected: Option flag and value examples

18. **Pattern Examples (complex)**
    - Method: `GetSyntaxTool.GetPatternExamples("complex")`
    - Expected: Real-world multi-feature examples

---

## Section 3: Route Validation (ValidateRouteTool)

**Purpose**: Verify route pattern validation and feedback.

### Test Cases

1. **Valid Simple Literal**
   - Pattern: `status`
   - Expected: ✅ Valid, no errors

2. **Valid Multi-Literal**
   - Pattern: `git commit`
   - Expected: ✅ Valid, no errors

3. **Valid Parameter**
   - Pattern: `deploy {env}`
   - Expected: ✅ Valid, shows parameter info

4. **Valid Optional Parameter**
   - Pattern: `deploy {env} {tag?}`
   - Expected: ✅ Valid, shows optional parameter

5. **Valid Typed Parameter**
   - Pattern: `delay {ms:int}`
   - Expected: ✅ Valid, shows type constraint

6. **Valid Catch-All**
   - Pattern: `docker {*args}`
   - Expected: ✅ Valid, shows array parameter

7. **Valid Boolean Flag**
   - Pattern: `build --verbose`
   - Expected: ✅ Valid, shows flag info

8. **Valid Option with Value**
   - Pattern: `build --config {mode}`
   - Expected: ✅ Valid, shows option parameter

9. **Valid Complex Pattern**
   - Pattern: `deploy {env|Environment} --dry-run,-d|Preview`
   - Expected: ✅ Valid, shows descriptions and aliases

10. **Invalid Unclosed Brace**
    - Pattern: `deploy {env`
    - Expected: ❌ Parse error, shows error details

11. **Invalid Angle Brackets**
    - Pattern: `prompt <input>`
    - Expected: ❌ Invalid syntax (angle brackets not supported)

12. **Invalid Duplicate Parameter**
    - Pattern: `test {env} {env}`
    - Expected: ❌ Semantic error NURU_S001

13. **Invalid Optional Before Required**
    - Pattern: `deploy {env?} {tag}`
    - Expected: ❌ Semantic error NURU_S002

14. **Validation Output Format**
    - Expected format:
      - Pattern: `{pattern}`
      - Status: Valid/Invalid
      - Segments: Detailed breakdown
      - Specificity: Numeric score
      - Errors (if any)

---

## Section 4: Handler Generation (GenerateHandlerTool)

**Purpose**: Verify delegate and mediator handler code generation.

### Test Cases

1. **Generate Delegate for Simple Literal**
   - Pattern: `status`
   - Mode: Delegate (useMediator: false)
   - Expected: `builder.Map("status", () => { ... });`

2. **Generate Delegate for Parameter**
   - Pattern: `greet {name}`
   - Mode: Delegate
   - Expected: Handler with `(string name)` parameter

3. **Generate Delegate for Optional Parameter**
   - Pattern: `deploy {env} {tag?}`
   - Mode: Delegate
   - Expected: Handler with `(string env, string? tag)` parameters

4. **Generate Delegate for Typed Parameter**
   - Pattern: `wait {seconds:int}`
   - Mode: Delegate
   - Expected: Handler with `(int seconds)` parameter

5. **Generate Delegate for Boolean Flag**
   - Pattern: `build --verbose`
   - Mode: Delegate
   - Expected: Handler with `(bool verbose)` parameter

6. **Generate Delegate for Option with Value**
   - Pattern: `test {project} --filter {pattern}`
   - Mode: Delegate
   - Expected: Handler with `(string project, string filter)` parameters

7. **Generate Delegate for Catch-All**
   - Pattern: `docker {*args}`
   - Mode: Delegate
   - Expected: Handler with `(string[] args)` parameter

8. **Generate Delegate for Complex Pattern**
   - Pattern: `backup {source} --output,-o {dest} --compress,-c`
   - Mode: Delegate
   - Expected: Handler with all parameters and option aliases shown

9. **Generate Mediator for Simple Pattern**
   - Pattern: `deploy {env}`
   - Mode: Mediator (useMediator: true)
   - Expected: IRequest record, IRequestHandler class, Map registration

10. **Generate Mediator for Optional Parameter**
    - Pattern: `backup {source} {dest?}`
    - Mode: Mediator
    - Expected: Record with nullable parameter

11. **Generate Mediator for Complex Pattern**
    - Pattern: `test {project} --verbose --filter {pattern}`
    - Mode: Mediator
    - Expected: Full mediator pattern with all parameters

12. **Error Handling for Invalid Pattern (missing literal)**
    - Pattern: `{missing-literal}`
    - Expected: Error comment in generated code

13. **Error Handling for Nested Braces**
    - Pattern: `test {param {nested}`
    - Expected: Error comment about parse failure

14. **Error Handling for Invalid Option**
    - Pattern: `invalid --`
    - Expected: Error comment about syntax error

---

## Section 5: Error Documentation (ErrorHandlingTool)

**Purpose**: Verify error handling documentation retrieval.

### Test Cases

1. **Get Error Handling Overview**
   - Method: `ErrorHandlingTool.GetErrorHandlingInfoAsync("overview")`
   - Expected: High-level error handling philosophy

2. **Get Error Handling Architecture**
   - Method: `ErrorHandlingTool.GetErrorHandlingInfoAsync("architecture")`
   - Expected: Error handling layers and flow

3. **Get Error Handling Philosophy**
   - Method: `ErrorHandlingTool.GetErrorHandlingInfoAsync("philosophy")`
   - Expected: Design principles for error handling

4. **Unknown Area Handling**
   - Method: `ErrorHandlingTool.GetErrorHandlingInfoAsync("invalid-area")`
   - Expected: Error message with available areas

5. **Get Parsing Error Scenarios**
   - Method: `ErrorHandlingTool.GetErrorScenariosAsync("parsing")`
   - Expected: Parse-time error scenarios and codes

6. **Get Binding Error Scenarios**
   - Method: `ErrorHandlingTool.GetErrorScenariosAsync("binding")`
   - Expected: Parameter binding error scenarios

7. **Get Conversion Error Scenarios**
   - Method: `ErrorHandlingTool.GetErrorScenariosAsync("conversion")`
   - Expected: Type conversion error scenarios

8. **Get Execution Error Scenarios**
   - Method: `ErrorHandlingTool.GetErrorScenariosAsync("execution")`
   - Expected: Runtime execution error scenarios

9. **Get Matching Error Scenarios**
   - Method: `ErrorHandlingTool.GetErrorScenariosAsync("matching")`
   - Expected: Route matching error scenarios

10. **Get All Error Scenarios**
    - Method: `ErrorHandlingTool.GetErrorScenariosAsync("all")`
    - Expected: Complete error scenario reference

11. **Unknown Scenario Handling**
    - Method: `ErrorHandlingTool.GetErrorScenariosAsync("invalid-scenario")`
    - Expected: Error message with available scenarios

12. **Get Best Practices**
    - Method: `ErrorHandlingTool.GetErrorHandlingBestPracticesAsync()`
    - Expected: Best practices documentation from GitHub

13. **Cache Hit on Second Call**
    - First call: Fetch documentation (cache miss)
    - Second call: Same documentation (cache hit)
    - Expected: Both return identical content

14. **Force Refresh**
    - Method: `ErrorHandlingTool.GetErrorHandlingInfoAsync("overview", forceRefresh: true)`
    - Expected: Bypasses cache, fetches fresh content

---

## Section 6: Cache Management (CacheManagementTool)

**Purpose**: Verify cache status reporting and clearing.

### Test Cases

1. **Cache Status (Empty)**
   - Method: `CacheManagementTool.CacheStatus()`
   - Context: Before any examples fetched
   - Expected: Shows empty cache, disk cache location

2. **Cache Status (After Fetching)**
   - Fetch example first
   - Method: `CacheManagementTool.CacheStatus()`
   - Expected: Shows cached entries with timestamps

3. **Clear Cache**
   - Method: `CacheManagementTool.ClearCache()`
   - Expected: Removes all cached examples, confirms deletion

4. **Cache Status (After Clearing)**
   - Clear cache
   - Method: `CacheManagementTool.CacheStatus()`
   - Expected: Shows empty cache again

5. **Disk Cache Directory**
   - Expected location: `~/.local/share/TimeWarp.Nuru.Mcp/cache/examples/`
   - Expected files: `{example-name}.cache` and `{example-name}.meta`

6. **Memory Cache Lifecycle**
   - Fetch example (populates memory cache)
   - Verify subsequent calls are instant (memory cache hit)
   - Clear cache (invalidates memory cache)
   - Next fetch is slower (cache miss)

---

## Section 7: Server Integration (MCP Protocol)

**Purpose**: Verify MCP server JSON-RPC 2.0 protocol compliance.

**Note**: This test file (`mcp-06-server-integration.cs`) remains a console-style integration test rather than Jaribu framework due to Process spawning and async I/O requirements.

### Test Cases

1. **Server Initialization**
   - Send: `initialize` request with protocol version
   - Expected: Success response with server capabilities

2. **List Available Tools**
   - Send: `tools/list` request
   - Expected: Returns all 6 tool definitions (get_example, list_examples, get_syntax, validate_route, generate_handler, etc.)

3. **Call list_examples Tool**
   - Send: `tools/call` with `list_examples`
   - Expected: Returns formatted list of available examples

4. **Call get_example Tool (basic)**
   - Send: `tools/call` with `get_example` and `name: "basic"`
   - Expected: Returns calc-mixed.cs content

5. **Call cache_status Tool**
   - Send: `tools/call` with `cache_status`
   - Expected: Returns cache status information

6. **Call clear_cache Tool**
   - Send: `tools/call` with `clear_cache`
   - Expected: Confirms cache cleared

7. **Graceful Shutdown**
   - Close stdin
   - Expected: Server exits within 2 seconds

8. **JSON-RPC 2.0 Compliance**
   - All requests: `jsonrpc: "2.0"`, `method`, `id`
   - All responses: `jsonrpc: "2.0"`, `result` or `error`, matching `id`

9. **Error Handling**
   - Invalid method name
   - Expected: JSON-RPC error response

10. **Timeout Handling**
    - All operations: 5-second timeout
    - Expected: No hangs or deadlocks

---

## Test Organization

### Naming Convention

All test files follow a numbered naming scheme for systematic coverage:

- **MCP**: `mcp-01-example-retrieval.cs`, `mcp-02-syntax-documentation.cs`, ...

### File Structure

Each test file uses the Jaribu test framework (except mcp-06 integration test):

```csharp
#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru.Mcp/TimeWarp.Nuru.Mcp.csproj

return await RunTests<ExampleRetrievalTests>(clearCache: true);

[TestTag("MCP")]
[ClearRunfileCache]
public class ExampleRetrievalTests
{
  public static async Task Should_list_all_available_examples()
  {
    // Arrange
    // Act
    string result = await GetExampleTool.ListExamplesAsync();

    // Assert
    result.ShouldContain("basic");
    result.ShouldContain("delegate");
    result.ShouldContain("mediator");
    result.ShouldContain("mixed");
    result.ShouldContain("console-logging");
    result.ShouldContain("serilog");

    await Task.CompletedTask;
  }
}
```

### Test Runner

MCP tests (01-05) are executed via `Tests/Scripts/run-mcp-tests.cs`, which:
- Runs all mcp-01 through mcp-05 tests sequentially
- Reports aggregate pass/fail counts
- Skips mcp-06 (server integration runs separately)

---

## Design Document References

**MCP Tools**:
- `source/timewarp-nuru.Mcp/README.md` - MCP server architecture
- Model Context Protocol specification - https://modelcontextprotocol.io

**Related Core Features**:
- `documentation/developer/design/parser/syntax-rules.md` - Route pattern syntax
- `documentation/developer/guides/route-pattern-syntax.md` - User-facing syntax guide
- `documentation/developer/design/cross-cutting/error-handling.md` - Error handling architecture

---

## Coverage Goals

### Completeness

- ✅ All 6 MCP tools tested (GetExample, GetSyntax, ValidateRoute, GenerateHandler, ErrorHandling, CacheManagement)
- ✅ All example IDs verified (basic, mixed, delegate, mediator, console-logging, serilog)
- ✅ All syntax elements accessible (literals, parameters, types, optional, catchall, options, descriptions)
- ✅ Cache behavior validated (memory, disk, TTL, force refresh)
- ✅ Error handling tested (unknown examples, invalid patterns, network failures)
- ✅ MCP protocol compliance (JSON-RPC 2.0, tool discovery, tool invocation)

### Real-World Validation

MCP tools are tested against production-quality calculator samples:
- **calc-delegate.cs**: Pure delegate routing for maximum performance
- **calc-mediator.cs**: Mediator pattern with DI for testability
- **calc-mixed.cs**: Hybrid approach combining both patterns

### Integration Testing

Server integration test (mcp-06) validates:
- Process spawning and lifecycle management
- JSON-RPC 2.0 protocol compliance
- Tool discovery and invocation
- Graceful shutdown on stdin close

---

## Success Criteria

A test suite is considered complete when:

1. **Coverage**: All 6 MCP tools have comprehensive test coverage
2. **Clarity**: Each test clearly demonstrates one tool capability
3. **Isolation**: Tests don't depend on external state (except GitHub for fallback testing)
4. **Performance**: Test suite runs in < 10 seconds (excluding server integration)
5. **Reliability**: Zero flaky tests (100% deterministic, except network-dependent tests have fallbacks)
6. **Documentation**: Test names and comments explain "why" not just "what"
7. **MCP Compliance**: Server integration test validates protocol conformance

---

## Contributing New Tests

When adding new MCP tools or features:

1. **Add tool tests** in appropriate numbered file (or create new mcp-0X file)
2. **Update test plan** to document new test sections
3. **Update run-mcp-tests.cs** to include new test files
4. **Run full suite** via `Tests/Scripts/run-mcp-tests.cs`
5. **Test server integration** via `mcp-06-server-integration.cs`

Each tool's tests provide detailed validation of that tool's specific capabilities and error handling.
