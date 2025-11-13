# Shell Completion Test Plan

## Overview

This test plan covers the TimeWarp.Nuru.Completion library, which provides shell tab completion support for CLI applications built with TimeWarp.Nuru.

## Test Structure

Tests are implemented as single-file C# applications (new in .NET 10) following the repository's testing conventions. Each test file validates specific aspects of completion functionality.

## Core Components to Test

### 1. CompletionProvider
- Command extraction from routes
- Option extraction (long and short forms)
- Parameter detection and type awareness
- Enum value completion
- File/Directory type handling
- Cursor position context analysis

### 2. CompletionScriptGenerator
- Bash script generation
- Zsh script generation
- PowerShell script generation
- Fish script generation
- Template placeholder replacement
- Invalid shell handling

### 3. CompletionContext
- Argument parsing
- Cursor position tracking
- Route matching up to cursor

### 4. Integration with NuruAppBuilder
- EnableShellCompletion() extension method
- Auto-registration of --generate-completion route
- App name customization

## Test Files

### completion-01-command-extraction.cs
**Purpose:** Validate that CompletionProvider correctly extracts command literals from routes

**Scenarios:**
- Single command route: `status`
- Multiple command routes: `create`, `createorder`, `delete`
- Commands with parameters: `deploy {env}`, `build {project}`
- Commands with options: `test --verbose`
- Nested commands: `git commit`, `git status`
- Duplicate command handling (should deduplicate)

**Expected Results:**
- All unique command literals extracted
- Commands from LiteralMatcher segments only
- No parameters or options included
- Case-sensitive matching

### completion-02-option-extraction.cs
**Purpose:** Validate that CompletionProvider correctly extracts options from routes

**Scenarios:**
- Long-form options: `--verbose`, `--force`, `--dry-run`
- Short-form options: `-v`, `-f`, `-d`
- Options with aliases: `--verbose,-v`, `--output,-o`
- Multiple options in one route: `--force --dry-run`
- Options with values: `--config {mode}`, `--output {file}`
- Mixed option styles

**Expected Results:**
- All unique options extracted
- Both long and short forms included
- No duplicate options
- Proper MatchPattern and AlternateForm handling

### completion-03-parameter-type-detection.cs
**Purpose:** Validate type-aware parameter completion

**Scenarios:**
- String parameters: `{name}` → CompletionType.Parameter
- Int parameters: `{count:int}` → CompletionType.Parameter
- Enum parameters: `{level:LogLevel}` → CompletionType.Enum with values
- File parameters: `{file:FileInfo}` → CompletionType.File
- Directory parameters: `{dir:DirectoryInfo}` → CompletionType.Directory
- Custom type parameters
- Optional parameters: `{tag?}`
- Catch-all parameters: `{*args}`

**Expected Results:**
- Correct CompletionType assigned
- Enum values extracted for enum types
- File/Directory delegation to shell
- Type converter integration working

### completion-04-cursor-context.cs
**Purpose:** Validate cursor position awareness in completion

**Scenarios:**
- Cursor at start: `|deploy` → show all commands
- Cursor mid-command: `dep|loy` → show matching commands
- Cursor after command: `deploy |` → show parameters/options
- Cursor in parameter: `deploy pr|` → show parameter help
- Cursor after option: `deploy --ver|` → complete to `--version`
- Cursor in option value: `--config |` → show config options

**Expected Results:**
- Context-aware suggestions
- Proper filtering based on cursor position
- Correct identification of what's being completed

### completion-05-bash-script-generation.cs
**Purpose:** Validate bash completion script generation

**Scenarios:**
- Generate script with multiple commands
- Generate script with options
- Verify complete -F function registration
- Verify COMP_WORDS and COMPREPLY usage
- Test placeholder replacement ({{APP_NAME}}, {{COMMANDS}}, {{OPTIONS}})
- Verify compgen integration

**Expected Results:**
- Valid bash syntax
- All commands included
- All options included
- Proper quoting and escaping
- Script is executable

### completion-06-zsh-script-generation.cs
**Purpose:** Validate zsh completion script generation

**Scenarios:**
- Generate script with _arguments function
- Verify command list format
- Verify option descriptions
- Test compsys integration
- Validate mutual exclusion handling
- Check completion function naming

**Expected Results:**
- Valid zsh syntax
- Proper _arguments specification
- Option descriptions included
- Command completion working
- Script follows zsh conventions

### completion-07-powershell-script-generation.cs
**Purpose:** Validate PowerShell completion script generation

**Scenarios:**
- Generate Register-ArgumentCompleter scriptblock
- Verify CompletionResult objects
- Test parameter name handling
- Validate value and description separation
- Check CompletionResultType usage

**Expected Results:**
- Valid PowerShell syntax
- Proper ScriptBlock structure
- CompletionResult objects correctly formed
- Works with both pwsh and powershell aliases

### completion-08-fish-script-generation.cs
**Purpose:** Validate fish completion script generation

**Scenarios:**
- Generate complete -c commands
- Verify declarative syntax
- Test -a (argument) and -l (long option) flags
- Validate -d (description) inclusion
- Check file path handling

**Expected Results:**
- Valid fish syntax
- One complete command per completion
- Proper flag usage
- Descriptions included
- Works in fish shell

### completion-09-integration-enablecompletion.cs
**Purpose:** Validate EnableShellCompletion() integration

**Scenarios:**
- Call EnableShellCompletion() without app name (use default)
- Call EnableShellCompletion("myapp") with custom name
- Verify --generate-completion route is registered
- Test route with valid shells: bash, zsh, pwsh, fish
- Test route with invalid shell → ArgumentException
- Verify script output to stdout

**Expected Results:**
- Route auto-registered in EndpointCollection
- App name defaults to assembly name
- Custom app name used when provided
- Invalid shells throw helpful errors
- Scripts output correctly

### completion-10-route-analysis.cs
**Purpose:** Validate comprehensive route pattern analysis

**Scenarios:**
- Simple literal route: `status`
- Route with parameters: `greet {name}`
- Route with typed parameters: `delay {ms:int}`
- Route with optional parameters: `deploy {env} {tag?}`
- Route with options: `build --config {mode}`
- Route with catch-all: `docker {*args}`
- Route with multiple options: `test --verbose --dry-run`
- Complex route: `deploy {env} --version {ver} --force`

**Expected Results:**
- All route components extracted
- Proper segment type identification
- Correct parameter type detection
- Options and parameters separated
- Catch-all handling

### completion-11-enum-completion.cs
**Purpose:** Validate enum value completion

**Scenarios:**
- Define enum: LogLevel { Debug, Info, Warning, Error }
- Route with enum parameter: `log {level:LogLevel}`
- Extract enum values via ITypeConverterRegistry
- Generate completion candidates with enum values
- Test enum in script generation

**Expected Results:**
- Enum values extracted: Debug, Info, Warning, Error
- CompletionType.Enum assigned
- Values included in completion candidates
- Works with custom enums
- Type converter integration

### completion-12-edge-cases.cs
**Purpose:** Validate edge cases and error handling

**Scenarios:**
- Empty route collection
- Route with no literal commands (only parameters)
- Route with only options
- Very long route patterns
- Special characters in commands
- Unicode in route patterns
- Null/empty app name handling
- Missing template resource handling

**Expected Results:**
- Graceful handling of edge cases
- Appropriate error messages
- No crashes or exceptions
- Sensible defaults

### completion-13-template-loading.cs
**Purpose:** Validate embedded resource template loading

**Scenarios:**
- Load bash-completion.sh template
- Load zsh-completion.zsh template
- Load pwsh-completion.ps1 template
- Load fish-completion.fish template
- Handle missing template (should throw)
- Verify template content structure

**Expected Results:**
- Templates load successfully
- Content contains expected placeholders
- Missing templates throw InvalidOperationException
- Resource names match convention

## Integration Tests

### Issue #30 Validation
**Purpose:** Verify the specific use case from Issue #30

**Scenario:**
```csharp
builder.AddRoute("createorder {product} {quantity:int}", ...);
builder.AddRoute("create {item}", ...);
builder.EnableShellCompletion("myapp");
```

**Test:**
1. Generate bash completion
2. Verify "createorder" and "create" both present
3. Verify typing `cre<TAB>` would show both options
4. Verify typing `createo<TAB>` would complete to `createorder`

**Expected:**
- Both commands in generated script
- Proper filtering by bash completion function
- Issue #30 fully resolved

## Performance Considerations

Tests should verify:
- No excessive allocations during completion analysis
- Template loading is cached (or minimal overhead)
- Route analysis is efficient for large endpoint collections
- Script generation completes quickly (< 100ms for typical apps)

## Error Scenarios

Tests should validate proper error handling for:
- Invalid shell name: `--generate-completion invalid`
- Malformed route patterns
- Missing type converters
- Null endpoint collections
- Invalid template resources
- Circular dependencies

## Success Criteria

All tests should:
- ✅ Execute as single-file .NET 10 applications
- ✅ Follow repository naming conventions: `completion-##-description.cs`
- ✅ Include clear documentation of purpose and scenarios
- ✅ Provide readable output with pass/fail indicators
- ✅ Exit with code 0 on success, non-zero on failure
- ✅ Be executable via `chmod +x` and direct execution
- ✅ Work with both `dotnet run` and direct execution

## Coverage Goals

- **Command Extraction:** 100% - All LiteralMatcher scenarios
- **Option Extraction:** 100% - All OptionMatcher scenarios
- **Parameter Detection:** 100% - All parameter types
- **Script Generation:** 100% - All 4 shells
- **Integration:** 100% - EnableShellCompletion API
- **Edge Cases:** 90% - Common edge cases covered

## Dependencies

Tests depend on:
- `TimeWarp.Nuru` - Core library with routing
- `TimeWarp.Nuru.Completion` - Completion functionality
- .NET 10 SDK - For single-file app support
- No traditional test frameworks (xUnit, NUnit, etc.)

## Running Tests

```bash
# Run all completion tests
cd Tests/TimeWarp.Nuru.Completion.Tests
for test in completion-*.cs; do
  echo "Running $test..."
  chmod +x "$test"
  ./"$test" || echo "FAILED: $test"
done

# Run specific test
./completion-01-command-extraction.cs

# Run via dotnet
dotnet run completion-05-bash-script-generation.cs
```

## Related Documentation

- Main Test Plan: `/Tests/test-plan-overview.md`
- Lexer Tests: `/Tests/TimeWarp.Nuru.Tests/Lexer/lexer-test-plan.md`
- Parser Tests: `/Tests/TimeWarp.Nuru.Tests/Parsing/Parser/parser-test-plan.md`
- Routing Tests: `/Tests/TimeWarp.Nuru.Tests/Routing/routing-test-plan.md`
- Task 025: `/Kanban/InProgress/025_Implement-Shell-Tab-Completion.md`
- Issue #30: https://github.com/TimeWarpEngineering/timewarp-nuru/issues/30
