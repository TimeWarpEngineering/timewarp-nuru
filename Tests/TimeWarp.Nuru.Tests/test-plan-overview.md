# Test Plan Overview

This document provides the overarching philosophy and structure for all TimeWarp.Nuru test plans.

## Three-Layer Testing Architecture

The test suite mirrors the CLI processing pipeline, with each layer responsible for a distinct phase:

```
User Input (text) → Lexer → Parser → Router → Handler
                      ↓        ↓        ↓
                   Tokens  CompiledRoute  Match + Bound Parameters
```

### Layer 1: Lexer (Tokenization)

**Location**: `Tests/TimeWarp.Nuru.Tests/Lexer/`
**Test Plan**: [lexer-test-plan.md](Lexer/lexer-test-plan.md)
**Test Files**: `lexer-01-*.cs` through `lexer-14-*.cs`

**Responsibility**: Convert route pattern text into tokens

```
Input:  "deploy {env} --tag {version}"
Output: [Identifier("deploy"), LeftBrace, Identifier("env"), RightBrace,
         DoubleDash, Identifier("tag"), LeftBrace, Identifier("version"), RightBrace]
```

**What it tests**:
- Token identification (identifiers, braces, dashes, symbols)
- Valid option formats (`--long`, `-short`)
- Invalid character sequences (rejection of nonsensical patterns)
- Whitespace handling
- Edge cases (empty input, special characters)

**Key principle**: The lexer rejects nonsensical character sequences early, making the parser's job simpler.

---

### Layer 2: Parser (Compilation & Validation)

**Location**: `Tests/TimeWarp.Nuru.Tests/Parsing/Parser/`
**Test Plan**: [parser-test-plan.md](Parsing/Parser/parser-test-plan.md)
**Test Files**: `parser-01-*.cs` through `parser-15-*.cs`

**Responsibility**: Build structured routes from token streams and validate semantic rules

```
Input:  [Identifier("deploy"), LeftBrace, Identifier("env"), RightBrace, ...]
Output: CompiledRoute {
          PositionalMatchers = [LiteralMatcher("deploy"), ParameterMatcher("env")],
          OptionMatchers = [OptionMatcher("--tag", parameterName: "version")],
          Specificity = 125
        }
```

**What it tests**:
- Parameter parsing (basic, typed, optional, catch-all)
- Option parsing (flags, values, aliases, repeated)
- Semantic validation rules (NURU_S001-S008):
  - Duplicate parameter detection
  - Optional parameter ordering
  - Catch-all positioning
  - End-of-options separator rules
- Specificity calculation
- Complex pattern integration
- Error reporting (both parse and semantic errors)

**Key principle**: Parser validates compile-time rules. If parsing succeeds, the route is semantically valid and ready for matching.

---

### Layer 3: Routing (Matching & Binding)

**Location**: `Tests/TimeWarp.Nuru.Tests/Routing/`
**Test Plan**: [routing-test-plan.md](Routing/routing-test-plan.md)
**Test Files**: `routing-01-*.cs` through `routing-11-*.cs`

**Responsibility**: Match input arguments against compiled routes and bind values to handler parameters

```
Input:  CompiledRoute + ["deploy", "prod", "--tag", "v1.0"]
Output: RouteMatch {
          Route = compiledRoute,
          BoundParameters = { env: "prod", version: "v1.0" },
          Handler = deployHandler
        }
```

**What it tests**:
- Route matching (exact, partial, no match)
- Parameter binding and type conversion (string, int, double, bool, arrays)
- Optional parameter handling (nullability-based)
- Catch-all argument capture
- Option matching (required, optional, repeated, aliases)
- Route selection (specificity-based when multiple routes match)
- End-of-options separator runtime behavior
- Complex integration scenarios
- Error handling (type conversion failures, missing required values)
- Delegate vs Mediator implementation consistency

**Key principle**: Routing validates runtime rules. Type conversions, nullability checks, and argument counts are validated here, not at parse time.

---

## Test Organization

### Naming Convention

All test files follow a numbered naming scheme for systematic coverage:

- **Lexer**: `lexer-01-basic-token-types.cs`, `lexer-02-valid-options.cs`, ...
- **Parser**: `parser-01-basic-parameters.cs`, `parser-02-typed-parameters.cs`, ...
- **Routing**: `routing-01-basic-matching.cs`, `routing-02-parameter-binding.cs`, ...

### File Structure

Each test file uses the Kijaribu test framework:

```csharp
#!/usr/bin/dotenv --

return await RunTests<TestClassName>(clearCache: true);

[TestTag("Lexer")] // or "Parser" or "Routing"
[ClearRunfileCache]
public class TestClassName
{
  public static async Task Should_describe_what_is_being_tested()
  {
    // Arrange
    // Act
    // Assert
    await Task.CompletedTask;
  }
}
```

### Test Runner

All tests are executed via `Tests/Scripts/run-kijaribu-tests.cs`, which:
- Runs all lexer tests sequentially
- Runs all parser tests sequentially
- Runs all routing tests sequentially
- Reports aggregate pass/fail counts

---

## Design Document References

Each layer references specific design documents:

**Lexer**:
- `documentation/developer/design/lexer-tokenization-rules.md`

**Parser**:
- `design/parser/syntax-rules.md` - Route pattern syntax and validation
- `design/resolver/specificity-algorithm.md` - Specificity calculation
- `design/cross-cutting/parameter-optionality.md` - Nullability-based optionality

**Routing**:
- `design/resolver/specificity-algorithm.md` - Route selection
- `design/cross-cutting/parameter-optionality.md` - Runtime nullability checks
- `guides/building-new-cli-apps.md` - Best practices

---

## Coverage Goals

### Completeness

- ✅ All token types recognized by lexer
- ✅ All semantic validation rules (NURU_S001-S008)
- ✅ All parameter types and modifiers
- ✅ All option formats and combinations
- ✅ All specificity rules
- ✅ All error conditions

### Real-World Validation

Each layer includes "integration" or "complex" test sections that validate realistic CLI patterns:

- Docker: `docker run -i -t --env {e}* -- {*cmd}`
- Git: `git commit --message,-m {msg} --amend --no-verify`
- Kubectl: `kubectl create {resource} {name} --namespace,-n {ns?}`

### Implementation Parity

Routing tests verify both Direct Delegate and Mediator implementations produce identical results for all scenarios.

---

## Progressive Complexity Model

Tests within each layer follow a progression:

1. **Basic** - Single feature in isolation
2. **Compound** - Multiple features combined
3. **Invalid** - Error cases and boundary conditions
4. **Integration** - Real-world complex patterns

Example for parameters:
1. Basic: `{name}` (single string parameter)
2. Typed: `{age:int}` (type constraint)
3. Optional: `{tag?}` (nullability)
4. Complex: `{env} {tag?} {*args}` (mixed required/optional/catch-all)

---

## Success Criteria

A test suite is considered complete when:

1. **Coverage**: All features documented in design docs have corresponding tests
2. **Clarity**: Each test clearly demonstrates one concept or rule
3. **Isolation**: Tests don't depend on each other (can run in any order)
4. **Performance**: Test suite runs in < 30 seconds
5. **Reliability**: Zero flaky tests (100% deterministic)
6. **Documentation**: Test names and comments explain "why" not just "what"

---

## Contributing New Tests

When adding new features to Nuru:

1. **Start with lexer tests** if adding new syntax
2. **Add parser tests** for compile-time validation rules
3. **Add routing tests** for runtime behavior
4. **Update test plans** to document new test sections
5. **Run full suite** via `Tests/Scripts/run-kijaribu-tests.cs`

Each layer's test plan provides detailed section-by-section guidance for that layer's specific concerns.
