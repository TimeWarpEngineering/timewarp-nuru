# Dynamic Completion Test Plan

## Overview

This test plan covers dynamic shell completion for TimeWarp.Nuru - the `EnableDynamicCompletion()` feature that callbacks to the application at Tab-press time instead of using static completion scripts.

**Related**: See `completion-test-plan.md` for static completion tests (located in `Static/` folder).

## Dynamic Completion Architecture

Dynamic completion enables:
- **Runtime Data**: Complete from databases, APIs, configuration services
- **Context-Aware Suggestions**: Different completions based on previous arguments
- **Type-Based Registration**: `RegisterForType(typeof(Enum), source)` for reusable sources
- **Parameter-Specific Registration**: `RegisterForParameter("env", source)` for targeted completion

### Callback Protocol

When the user presses Tab, the shell executes:
```bash
myapp __complete 2 myapp deploy production
```

Where:
- `__complete` is the registered callback route
- `2` is the cursor position (index of word being completed)
- Remaining arguments are the words typed so far

The app outputs completion candidates to stdout:
```
production	Production environment (use with caution)
staging	Staging environment for final testing
:4
```

Format:
- One candidate per line (value\tdescription)
- Final line: directive code (`:4` = NoFileComp)
- Diagnostics to stderr (not visible to shell)

## Test Structure

Tests are implemented as single-file C# applications (.NET 10) following the repository's testing conventions. Each test file validates specific aspects of dynamic completion functionality.

Tests are located in `Tests/TimeWarp.Nuru.Completion.Tests/Dynamic/`.

## Core Components to Test

### 1. DynamicCompletionHandler
**File**: `Source/TimeWarp.Nuru.Completion/Completion/DynamicCompletionHandler.cs`

**Responsibilities:**
- Process `__complete` callback requests
- Detect parameter being completed from cursor position
- Look up completion sources from registry
- Output candidates in Cobra-compatible format

**Key Methods:**
- `HandleCompletion(int cursorIndex, string[] words, CompletionSourceRegistry registry, EndpointCollection endpoints)`
- `GetCompletions(CompletionContext context, CompletionSourceRegistry registry)`
- `TryGetParameterInfo(CompletionContext context, out string? parameterName, out Type? parameterType)`
- `TryMatchEndpoint(Endpoint endpoint, string[] typedWords, out string? parameterName, out Type? parameterType)`
- `GetParameterType(Endpoint endpoint, string? parameterName)`

### 2. CompletionSourceRegistry
**File**: `Source/TimeWarp.Nuru.Completion/Completion/CompletionSourceRegistry.cs`

**Responsibilities:**
- Store completion sources by parameter name or type
- Provide lookup methods for parameter-based and type-based sources
- Support overwriting registrations

**Key Methods:**
- `RegisterForParameter(string parameterName, ICompletionSource source)`
- `RegisterForType(Type type, ICompletionSource source)`
- `GetSourceForParameter(string parameterName)`
- `GetSourceForType(Type type)`

### 3. ICompletionSource Implementations

#### DefaultCompletionSource
**File**: `Source/TimeWarp.Nuru.Completion/Completion/Sources/DefaultCompletionSource.cs`

**Responsibilities:**
- Analyze registered routes to suggest commands and options
- Extract literal segments for command completion
- Extract option matchers for option completion
- Provide fallback when no custom sources registered

#### EnumCompletionSource<TEnum>
**File**: `Source/TimeWarp.Nuru.Completion/Completion/Sources/EnumCompletionSource.cs`

**Responsibilities:**
- Provide completions for enum types
- Extract DescriptionAttribute values
- Support case-insensitive mode
- AOT-compatible with DynamicallyAccessedMembers

### 4. Dynamic Script Generation
**File**: `Source/TimeWarp.Nuru.Completion/Completion/DynamicCompletionScriptGenerator.cs`

**Responsibilities:**
- Load embedded shell templates (bash, zsh, PowerShell, fish)
- Replace {{APP_NAME}} placeholder
- Generate scripts that invoke `__complete` callback
- Handle template loading errors

### 5. Integration - EnableDynamicCompletion
**File**: `Source/TimeWarp.Nuru.Completion/Extensions/NuruAppBuilderExtensions.cs`

**Responsibilities:**
- Register `__complete {index:int} {*words}` route
- Register `--generate-completion {shell}` route
- Invoke registry configuration callback
- Auto-detect app name from runtime environment

## Test Files

### Phase 1: Core Infrastructure Tests

#### completion-14-dynamic-handler.cs
**Purpose:** Validate `DynamicCompletionHandler.HandleCompletion()` main entry point

**Scenarios:**
- Should detect parameter name from cursor position
  - Input: `__complete 2 myapp deploy`
  - Expected: detects "env" parameter for route `deploy {env}`
- Should detect option parameter value
  - Input: `__complete 4 myapp deploy prod --version`
  - Expected: detects "tag" parameter for `--version {tag}`
- Should fall back to DefaultCompletionSource when no custom source
  - Input: `__complete 1 myapp`
  - Expected: returns commands from registered routes
- Should use parameter name-based source when registered
  - Registered: `RegisterForParameter("env", customSource)`
  - Expected: calls customSource.GetCompletions()
- Should use type-based source when parameter name not registered
  - Route: `deploy {env} --mode {mode}` where mode is DeploymentMode enum
  - Registered: `RegisterForType(typeof(DeploymentMode), enumSource)`
  - Expected: calls enumSource.GetCompletions() for mode parameter
- Should output correct Cobra-style format
  - Expected: `value\tdescription\n:4\n`
- Should write diagnostics to stderr
  - Expected: "Completion ended with directive: NoFileComp"
- Should return exit code 0 on success
- Should handle invalid cursor position gracefully
- Should handle empty word array
- Should handle routes with catch-all parameters

**Expected Results:**
- All parameter detection scenarios work correctly
- Source lookup follows priority: name-based → type-based → default
- Output format matches Cobra's `__complete` protocol
- Exit codes indicate success/failure appropriately

#### completion-15-completion-registry.cs
**Purpose:** Validate `CompletionSourceRegistry` registration and lookup

**Scenarios:**
- Should register and retrieve source by parameter name
  - Register: `RegisterForParameter("env", envSource)`
  - Retrieve: `GetSourceForParameter("env")` → returns envSource
- Should register and retrieve source by type
  - Register: `RegisterForType(typeof(DeploymentMode), enumSource)`
  - Retrieve: `GetSourceForType(typeof(DeploymentMode))` → returns enumSource
- Should overwrite existing parameter registration
  - Register: `RegisterForParameter("env", source1)`
  - Register: `RegisterForParameter("env", source2)`
  - Retrieve: should return source2
- Should overwrite existing type registration
  - Similar to parameter overwrite
- Should return null for unregistered parameter name
  - Retrieve: `GetSourceForParameter("unknown")` → null
- Should return null for unregistered type
  - Retrieve: `GetSourceForType(typeof(CustomType))` → null
- Should throw ArgumentNullException for null parameter name
  - Register/Get with null parameter name → throws
- Should throw ArgumentNullException for null type
  - Register/Get with null type → throws
- Should throw ArgumentNullException for null source
  - Register with null source → throws
- Should handle multiple registrations independently
  - Register multiple parameters and types
  - Each lookup returns correct source

**Expected Results:**
- Registry stores and retrieves sources correctly
- Overwrite behavior works as expected
- Null handling is robust
- No cross-contamination between parameter and type registrations

#### completion-16-default-source.cs
**Purpose:** Validate `DefaultCompletionSource` command and option extraction

**Scenarios:**
- Should return commands at root level
  - Context: cursor=1, args=["myapp"]
  - Routes: `status`, `deploy {env}`, `build {project}`
  - Expected: ["status", "deploy", "build"]
- Should return nested commands
  - Context: cursor=2, args=["myapp", "git"]
  - Routes: `git commit`, `git status`, `git push`
  - Expected: ["commit", "status", "push"]
- Should return options when current word starts with dash
  - Context: cursor=2, args=["myapp", "deploy", "--"]
  - Routes: `deploy {env} --version {ver} --force`
  - Expected: ["--version", "--force"]
- Should include both long and short option forms
  - Route: `test --verbose,-v --dry-run,-d`
  - Expected: ["--verbose", "-v", "--dry-run", "-d"]
- Should handle empty route collection
  - No routes registered
  - Expected: empty candidate list
- Should deduplicate commands
  - Routes: `deploy {env}`, `deploy {env} --force`
  - Expected: ["deploy"] (appears once)
- Should filter commands by typed prefix
  - Context: args includes partially typed command
  - Expected: only matching commands returned
- Should handle routes with only parameters (no literals)
  - Route: `{command} {arg}`
  - Expected: no commands suggested (parameter-only route)
- Should handle routes with only options
  - Route: `--help`, `--version`
  - Expected: options returned at root level
- Should sort results alphabetically
  - Expected: commands in alphabetical order

**Expected Results:**
- Command extraction works for simple and nested patterns
- Option extraction includes all forms
- Filtering and deduplication work correctly
- Edge cases handled gracefully

#### completion-17-enum-source.cs
**Purpose:** Validate `EnumCompletionSource<TEnum>` enum value extraction

**Scenarios:**
- Should return all enum values
  - Enum: `LogLevel { Debug, Info, Warning, Error }`
  - Expected: ["Debug", "Info", "Warning", "Error"]
- Should include descriptions from DescriptionAttribute
  - Enum value: `[Description("Fast deployment")] Fast`
  - Expected: candidate.Description == "Fast deployment"
- Should return values in alphabetical order
  - Expected: sorted by value name
- Should handle case-insensitive mode
  - Constructor: `new EnumCompletionSource<LogLevel>(includeCaseInsensitive: true)`
  - Expected: both "Debug" and "debug" candidates
- Should not duplicate case-insensitive values if enum already lowercase
  - Enum: `enum Size { small, medium, large }`
  - Expected: no duplicate "small", "small"
- Should use numeric value as fallback description
  - Enum value without DescriptionAttribute
  - Expected: candidate.Description == "Value: 0" (or actual numeric value)
- Should work with flags enums
  - Enum: `[Flags] FileAccess { Read = 1, Write = 2, Execute = 4 }`
  - Expected: all individual values returned
- Should handle enums with explicit numeric values
  - Enum: `Priority { Low = 1, Medium = 5, High = 10 }`
  - Expected: all values with correct numeric descriptions
- Should throw ArgumentNullException for null context
  - Input: null CompletionContext
  - Expected: ArgumentNullException
- Should be AOT-compatible
  - Generic type parameter has DynamicallyAccessedMembers attribute
  - Expected: works with PublishAot=true

**Expected Results:**
- All enum values extracted correctly
- Descriptions parsed from attributes
- Case-insensitive mode works as expected
- AOT compilation succeeds

#### completion-18-parameter-detection.cs
**Purpose:** Validate `TryGetParameterInfo()` parameter detection logic

**Scenarios:**
- Should detect positional parameter name and type
  - Route: `deploy {env}`
  - Context: cursor=2, args=["myapp", "deploy"]
  - Expected: paramName="env", paramType=typeof(string)
- Should detect typed positional parameter
  - Route: `delay {ms:int}`
  - Context: cursor=2, args=["myapp", "delay"]
  - Expected: paramName="ms", paramType=typeof(int)
- Should detect option parameter name and type
  - Route: `deploy {env} --version {tag}`
  - Context: cursor=4, args=["myapp", "deploy", "prod", "--version"]
  - Expected: paramName="tag", paramType=typeof(string)
- Should detect option parameter with short form
  - Route: `deploy {env} --version,-v {tag}`
  - Context: args end with "-v"
  - Expected: paramName="tag"
- Should detect enum parameter type
  - Route: `deploy {env} --mode {mode}` where mode is DeploymentMode enum
  - Expected: paramType=typeof(DeploymentMode)
- Should return false when completing literal segment
  - Route: `git commit -m {msg}`
  - Context: cursor=2, args=["myapp", "git"]
  - Expected: returns false (completing "commit" literal, not parameter)
- Should handle optional parameters
  - Route: `deploy {env} {tag?}`
  - Context: cursor after env is provided
  - Expected: detects "tag" parameter
- Should return false for routes with catch-all parameters
  - Route: `docker {*args}`
  - Expected: returns false (cannot determine specific parameter)
- Should handle multiple parameters
  - Route: `copy {src} {dest}`
  - Context: cursor=2, args=["myapp", "copy"]
  - Expected: detects "src" parameter
  - Context: cursor=3, args=["myapp", "copy", "file.txt"]
  - Expected: detects "dest" parameter
- Should return false when no endpoint matches
  - Context: args=["myapp", "unknown"]
  - Expected: returns false
- Should handle minimum args requirement
  - Context: args=["myapp"] (length < 2)
  - Expected: returns false early

**Expected Results:**
- Parameter name and type correctly extracted in all scenarios
- Positional and option parameters both supported
- Edge cases (catch-all, optional, no match) handled correctly
- Return value indicates detection success/failure

#### completion-19-endpoint-matching.cs
**Purpose:** Validate `TryMatchEndpoint()` route pattern matching

**Scenarios:**
- Should match route when all literals match
  - Route: `git commit -m {msg}`
  - Typed: ["git", "commit", "-m"]
  - Expected: true, paramName="msg"
- Should fail to match on literal mismatch
  - Route: `deploy {env}`
  - Typed: ["build"]
  - Expected: false
- Should handle partial match with remaining parameters
  - Route: `deploy {env} {tag}`
  - Typed: ["deploy", "production"]
  - Expected: true, paramName="tag" (next parameter to complete)
- Should consume parameter segments when matching
  - Route: `copy {src} {dest}`
  - Typed: ["copy", "file1.txt"]
  - Expected: true, consumes "file1.txt" for src, detects dest parameter
- Should return false for routes with catch-all
  - Route: `docker {*args}`
  - Expected: false (cannot determine specific parameter)
- Should match option matchers
  - Route: `deploy {env} --version {tag}`
  - Typed: ["deploy", "prod", "--version"]
  - Expected: true, paramName="tag"
- Should match option alternate forms
  - Route: `deploy {env} --version,-v {tag}`
  - Typed: ["deploy", "prod", "-v"]
  - Expected: true, paramName="tag"
- Should handle routes with only literals (no parameters)
  - Route: `status`
  - Typed: ["status"]
  - Expected: false (no parameter to complete)
- Should handle empty typed words array
  - Typed: []
  - Expected: false or early return
- Should detect parameter at specific position in route
  - Route: `git commit -m {msg} --amend`
  - Typed: ["git", "commit", "-m"]
  - Expected: true, paramName="msg" (not "amend")
- Should handle multiple option matchers
  - Route: `build --config {mode} --output {dir}`
  - Typed: ["build", "--config", "Release", "--output"]
  - Expected: true, paramName="dir"

**Expected Results:**
- Accurate route matching against typed words
- Parameter consumption logic works correctly
- Option matching supports both primary and alternate forms
- Catch-all and edge cases handled appropriately

### Phase 2: Handler and Integration Tests

#### completion-21-integration-enabledynamic.cs
**Purpose:** Validate `EnableDynamicCompletion()` API integration

**Scenarios:**
- Should register `__complete` route with correct pattern
  - Call: `builder.EnableDynamicCompletion()`
  - Expected: route `__complete {index:int} {*words}` registered
- Should register `--generate-completion` route
  - Expected: route `--generate-completion {shell}` registered
- Should invoke configure callback
  - Call: `builder.EnableDynamicCompletion(configure: registry => { /* test */ })`
  - Expected: configure action called with CompletionSourceRegistry
- Should auto-detect app name from runtime environment
  - Expected: uses Environment.ProcessPath, Process.GetCurrentProcess(), or Assembly name
- Should use custom app name when provided
  - Call: `builder.EnableDynamicCompletion("myapp", configure: ...)`
  - Expected: scripts use "myapp" as app name
- Should support fluent API chaining
  - Call: `builder.EnableDynamicCompletion(...).Map(...).Build()`
  - Expected: all methods chainable
- Should not interfere with existing routes
  - Register routes before and after EnableDynamicCompletion
  - Expected: all routes work correctly
- Should allow multiple completion source registrations
  - Configure: register multiple sources
  - Expected: all sources stored in registry
- Should validate shell parameter
  - Call: `--generate-completion invalid`
  - Expected: ArgumentException with helpful message
- Should support all 4 shells
  - Call: `--generate-completion bash|zsh|pwsh|fish`
  - Expected: each generates valid script
- Should work when called before Map
  - Call order: EnableDynamicCompletion → Map
  - Expected: works correctly
- Should work when called after Map
  - Call order: Map → EnableDynamicCompletion
  - Expected: works correctly

**Expected Results:**
- Both routes registered correctly
- Configure callback invoked properly
- App name detection works
- Fluent API fully functional
- No conflicts with other routes

#### completion-22-callback-protocol.cs
**Purpose:** Validate `__complete` route callback protocol

**Scenarios:**
- Should accept correct parameter types
  - Route: `__complete {index:int} {*words}`
  - Input: `myapp __complete 2 myapp deploy`
  - Expected: index=2, words=["myapp", "deploy"]
- Should output candidates to stdout
  - Expected: completion candidates written to Console.Out
- Should output tab-separated value and description
  - Expected format: `value\tdescription`
- Should output directive code on final line
  - Expected format: `:4` (or other directive)
- Should write diagnostics to stderr
  - Expected: "Completion ended with directive: NoFileComp" to Console.Error
- Should return exit code 0 on success
  - Expected: handler returns 0
- Should handle index out of range
  - Input: index=99, words=["myapp", "cmd"]
  - Expected: graceful handling (empty results or error)
- Should handle empty words array
  - Input: index=1, words=[]
  - Expected: graceful handling
- Should handle single word (app name only)
  - Input: index=1, words=["myapp"]
  - Expected: returns root-level commands
- Should pass correct context to completion sources
  - Expected: CompletionContext.Args and CursorPosition set correctly
- Should support multiple completion sources in sequence
  - Scenario: multiple parameters in one command line
  - Expected: each Tab-press calls __complete with updated index

**Expected Results:**
- Protocol follows Cobra's `__complete` specification
- Output format is shell-compatible
- Error handling is robust
- Context passing is accurate

### Phase 3: Advanced Features Tests

#### completion-23-custom-sources.cs
**Purpose:** Validate custom `ICompletionSource` implementations

**Scenarios:**
- Should use custom source implementing ICompletionSource
  - Implement: custom source returning specific candidates
  - Register: `RegisterForParameter("env", customSource)`
  - Expected: calls customSource.GetCompletions()
- Should pass CompletionContext with correct data
  - Custom source inspects context.Args
  - Expected: receives full argument array and cursor position
- Should support context-aware completion
  - Custom source: returns different results based on previous args
  - Example: `deploy prod <TAB>` vs `deploy staging <TAB>`
  - Expected: different candidates based on context
- Should handle exceptions in custom source
  - Custom source: throws exception
  - Expected: error logged or graceful fallback
- Should support multiple custom sources for different parameters
  - Register: envSource for "env", tagSource for "tag"
  - Expected: each parameter gets correct source
- Should prioritize parameter name over type registration
  - Register: source for "env" parameter AND source for string type
  - Expected: parameter-specific source takes priority
- Should fall back to type-based source when parameter source not found
  - Route: `deploy {env} --mode {mode}` where mode is DeploymentMode
  - Register: only RegisterForType(typeof(DeploymentMode), enumSource)
  - Expected: mode parameter uses enumSource
- Should support chaining/composition of sources
  - Custom source: calls another source internally
  - Expected: composition works correctly
- Should handle empty candidate list from custom source
  - Custom source: returns empty enumerable
  - Expected: no candidates shown (shell shows nothing)
- Should respect CompletionType in candidates
  - Custom source: returns candidates with different types
  - Expected: shell respects type hints (e.g., file completion)

**Expected Results:**
- Custom sources integrate seamlessly
- Context passing is accurate
- Priority system works (name > type > default)
- Error handling is robust

#### completion-24-context-aware.cs
**Purpose:** Validate context-aware completion scenarios

**Scenarios:**
- Should use previous arguments for context-aware completion
  - Route: `deploy {env} {tag}`
  - Context: args=["myapp", "deploy", "production"]
  - Custom source: queries production tags only
  - Expected: returns production-specific tags
- Should support conditional completion based on flags
  - Route: `test {file} --coverage`
  - Context: args include "--coverage"
  - Custom source: returns coverage-specific suggestions
  - Expected: different results when --coverage present
- Should support chained commands
  - Route: `git commit -m {msg}` followed by more args
  - Expected: completion works at each position
- Should handle complex route patterns
  - Route: `deploy {env} --version {tag} --region {region}`
  - Expected: each parameter gets correct context
- Should validate context data
  - Custom source: checks context.Endpoints
  - Expected: full endpoint collection available
- Should support real-world use case: environment-specific deployments
  - Example from DynamicCompletionExample
  - Expected: production env shows production tags, staging shows staging tags
- Should support real-world use case: type-aware completion
  - Example: file parameters show files, directory parameters show directories
  - Expected: type information used correctly

**Expected Results:**
- Context-aware sources work correctly
- Previous arguments accessible
- Complex scenarios supported
- Real-world use cases validated

#### completion-20-dynamic-script-gen.cs
**Purpose:** Validate `DynamicCompletionScriptGenerator` template generation

**Scenarios:**
- Should load bash template from embedded resource
  - Expected: bash-completion-dynamic.sh loaded
- Should load zsh template from embedded resource
  - Expected: zsh-completion-dynamic.zsh loaded
- Should load PowerShell template from embedded resource
  - Expected: pwsh-completion-dynamic.ps1 loaded
- Should load fish template from embedded resource
  - Expected: fish-completion-dynamic.fish loaded
- Should replace {{APP_NAME}} placeholder
  - Input: appName="myapp"
  - Expected: "myapp" appears in script where {{APP_NAME}} was
- Should throw on missing template resource
  - Scenario: template file missing from assembly
  - Expected: InvalidOperationException with helpful message
- Should generate valid bash syntax
  - Expected: script passes `bash -n` syntax check
- Should generate valid zsh syntax
  - Expected: script passes `zsh -n` syntax check
- Should generate valid PowerShell syntax
  - Expected: script passes `pwsh -NoProfile -Command "& { ... }"` check
- Should generate valid fish syntax
  - Expected: script passes `fish -n` syntax check
- Should include callback invocation in all scripts
  - Expected: all scripts contain `__complete` invocation
- Should pass correct parameters to __complete
  - Bash: uses COMP_CWORD, COMP_WORDS
  - Zsh: uses CURRENT, words array
  - PowerShell: uses $wordToComplete, $commandAst
  - Fish: uses __fish_complete_arguments

**Expected Results:**
- All 4 templates load successfully
- Placeholder replacement works
- Generated scripts are syntactically valid
- Callback invocation format is correct for each shell

#### completion-25-output-format.cs
**Purpose:** Validate completion output format (Cobra-style)

**Scenarios:**
- Should output tab-separated value and description
  - Input: CompletionCandidate(Value="prod", Description="Production")
  - Expected output: `prod\tProduction`
- Should output value only when description is null/empty
  - Input: CompletionCandidate(Value="staging", Description=null)
  - Expected output: `staging` (no tab)
- Should output directive code on final line
  - Directive: CompletionDirective.NoFileComp (4)
  - Expected output: `:4`
- Should handle special characters in value
  - Input: value with spaces, tabs, newlines
  - Expected: appropriate escaping or handling
- Should handle special characters in description
  - Input: description with tabs, newlines
  - Expected: tabs escaped or replaced
- Should support multiple directives (bitwise flags)
  - Directive: NoFileComp | NoSpace (4 | 8 = 12)
  - Expected output: `:12`
- Should output candidates in order provided
  - Expected: maintains order from completion source
- Should handle empty candidate list
  - Input: no candidates
  - Expected output: only `:4` (directive)
- Should handle very long descriptions
  - Input: description > 200 characters
  - Expected: truncation or full output (shell-dependent)
- Should output Unicode characters correctly
  - Input: value/description with emoji or Unicode
  - Expected: correct UTF-8 encoding
- Should separate multiple candidates with newlines
  - Input: 3 candidates
  - Expected: 3 lines + directive line

**Expected Results:**
- Output format matches Cobra's `__complete` protocol
- Special characters handled correctly
- Directive codes output properly
- Shell compatibility maintained

## Integration Tests

### Real-World Scenario: DynamicCompletionExample
**Purpose:** Validate end-to-end flow using the DynamicCompletionExample

**Test:**
1. Build DynamicCompletionExample with AOT
2. Test command completion: `myapp <TAB>` → deploy, list-environments, list-tags, status
3. Test parameter completion: `myapp deploy <TAB>` → production, staging, development, qa, demo
4. Test option parameter completion: `myapp deploy prod --version <TAB>` → v2.1.0, v2.0.5, latest
5. Test enum parameter completion: `myapp deploy prod --mode <TAB>` → Fast, Standard, BlueGreen, Canary
6. Verify descriptions are included
7. Verify directive codes are correct
8. Test in actual shell (bash)

**Expected:**
- All completion scenarios work end-to-end
- Performance is acceptable (~7-10ms per invocation)
- Shell integration works correctly

## Performance Considerations

Tests should verify:
- No excessive allocations during parameter detection
- Registry lookups are fast (Dictionary-based)
- Route matching is efficient (early returns)
- Output generation is minimal (direct Console.WriteLine)
- Cold start time < 10ms (AOT)

## Error Scenarios

Tests should validate proper error handling for:
- Invalid cursor index (out of range, negative)
- Null or empty words array
- Malformed route patterns
- Missing Method property on Endpoint (Mediator commands)
- Circular dependencies in custom sources
- Exceptions thrown by custom sources
- Invalid shell name in --generate-completion
- Missing template resources

## Success Criteria

All tests should:
- ✅ Execute as single-file .NET 10 applications
- ✅ Follow naming convention: `completion-##-description.cs`
- ✅ Include clear documentation of purpose and scenarios
- ✅ Use Shouldly assertions for readability
- ✅ Provide readable output with ✅/❌ indicators
- ✅ Exit with code 0 on success, non-zero on failure
- ✅ Be executable via `chmod +x` and direct execution
- ✅ Work with both `dotnet run` and direct execution

## Coverage Goals

- **DynamicCompletionHandler:** 95% - All core flows tested
- **CompletionSourceRegistry:** 100% - Small surface area, all methods tested
- **DefaultCompletionSource:** 90% - Command/option extraction tested
- **EnumCompletionSource:** 95% - All enum scenarios tested
- **Parameter Detection:** 95% - All detection scenarios tested
- **Endpoint Matching:** 90% - Route matching logic tested
- **Integration:** 95% - EnableDynamicCompletion API tested
- **Script Generation:** 85% - Template loading and generation tested

## Dependencies

Tests depend on:
- `TimeWarp.Nuru` - Core library with routing
- `TimeWarp.Nuru.Completion` - Completion functionality
- .NET 10 SDK - For single-file app support
- Shouldly - For readable assertions
- No traditional test frameworks (xUnit, NUnit, etc.)

## Running Tests

```bash
# Run all dynamic completion tests
cd Tests/TimeWarp.Nuru.Completion.Tests/Dynamic
for test in completion-*.cs; do
  echo "Running $test..."
  chmod +x "$test"
  ./"$test" || echo "FAILED: $test"
done

# Run specific test
./completion-15-completion-registry.cs

# Run via dotnet
dotnet run completion-17-enum-source.cs
```

## Related Documentation

- Static Completion Tests: `../Static/` folder
- Static Test Plan: `../completion-test-plan.md`
- Task 029: `/Kanban/InProgress/029_Implement-EnableDynamicCompletion.md`
- Dynamic Completion Example: `/Samples/DynamicCompletionExample/`
- Example Overview: `/Samples/DynamicCompletionExample/Overview.md`
