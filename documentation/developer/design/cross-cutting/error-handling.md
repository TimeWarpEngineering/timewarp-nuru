# Error Handling Philosophy

Design principles and approach for error handling in TimeWarp.Nuru.

## Core Philosophy

Nuru follows a **"fail fast with clear messages"** approach to error handling.

## Design Principles

### 1. Simplicity First
Avoid complex error recovery mechanisms in favor of clear, predictable failures. The framework should not try to guess user intent or automatically recover from errors.

### 2. Clear Communication
Provide specific, actionable error messages that tell users:
- What went wrong
- Where it went wrong (parameter names, values)
- How to fix it (when possible)

### 3. Graceful Degradation
When commands are invalid or ambiguous:
- Show available commands via automatic help generation
- Suggest similar commands when appropriate
- Provide usage examples

### 4. Stream Separation
Maintain strict separation between output streams:
- **stdout**: Normal command output and results only
- **stderr**: All error messages and diagnostic information

This enables proper piping and scripting workflows where errors don't contaminate data streams.

### 5. Standard Exit Codes
Use conventional exit codes for compatibility with shell scripts and CI/CD:
- `0` = Success
- `1` = General error (default for all failures)
- Future: Consider specific exit codes for different error types

## Error Detection Pipeline

Route patterns go through multiple validation stages before execution:

### Stage 1: Lexical Analysis (Lexer)

**Input**: Route pattern string
**Output**: Token stream or invalid token markers
**Errors**: None thrown - invalid syntax becomes `InvalidToken` in stream

The Lexer performs character-by-character scanning to produce tokens. It doesn't throw exceptions; instead, it marks invalid syntax with `InvalidToken` types for the parser to handle.

**Example:**
```csharp
// Invalid: angle brackets instead of curly braces
"deploy <env>"
// Lexer produces: Identifier("deploy"), InvalidToken("<env>")
```

### Stage 2: Parsing (Parser)

**Input**: Token stream
**Output**: Abstract Syntax Tree (AST) + ParseError[]
**Errors**: **NURU_P001-P007** - Parse Errors

The Parser builds an AST from tokens and validates syntax correctness. Parse errors indicate malformed syntax that cannot be parsed into a valid structure.

**Parse Error Types (NURU_P###):**
- **NURU_P001**: Invalid parameter syntax (`<param>` instead of `{param}`)
- **NURU_P002**: Unbalanced braces (`deploy {env` missing `}`)
- **NURU_P003**: Invalid option format (`-verbose` should be `--verbose`)
- **NURU_P004**: Invalid type constraint (`{id:integer}` should be `{id:int}`)
- **NURU_P005**: Invalid character (unsupported characters in pattern)
- **NURU_P006**: Unexpected token (parser encountered unexpected syntax)
- **NURU_P007**: Null route pattern (pattern string is null)

**Exception**: Throws `PatternException` with `ParseErrors` collection

**Example:**
```csharp
// Missing closing brace
"deploy {env"
// Parser throws: PatternException with UnbalancedBracesError (NURU_P002)
```

### Stage 3: Semantic Validation (SemanticValidator)

**Input**: Valid AST
**Output**: Validated route + SemanticError[]
**Errors**: **NURU_S001-S008** - Semantic Errors

The SemanticValidator checks logical correctness of syntactically valid patterns. Semantic errors indicate patterns that create ambiguity or conflicts.

**Semantic Error Types (NURU_S###):**
- **NURU_S001**: Duplicate parameter names in route
- **NURU_S002**: Conflicting optional parameters (multiple consecutive)
- **NURU_S003**: Catch-all parameter not at end of route
- **NURU_S004**: Mixed catch-all with optional parameters
- **NURU_S005**: Option with duplicate alias
- **NURU_S006**: Optional parameter before required parameter
- **NURU_S007**: Invalid end-of-options separator
- **NURU_S008**: Options after end-of-options separator

**Exception**: Throws `PatternException` with `SemanticErrors` collection

**Example:**
```csharp
// Optional before required - syntactically valid but semantically ambiguous
"copy {source?} {dest}"
// SemanticValidator throws: PatternException with OptionalBeforeRequiredError (NURU_S006)
```

### Stage 4: Compile-Time Analysis (Roslyn Analyzer)

**Input**: C# source code with route patterns
**Output**: Build diagnostics
**Errors**: Both NURU_P### and NURU_S### reported as compiler diagnostics

The Roslyn Analyzer validates route patterns at compile time, providing the same error codes as runtime validation. This catches errors before the code runs.

**Build Integration:**
- Errors appear in Error List window
- Build fails if any errors are present
- Same error codes (NURU_P###, NURU_S###) as runtime

**Example:**
```csharp
// Build-time error
builder.AddRoute("deploy {env?} {version?}", handler);
// Compiler diagnostic: NURU_S002 - Conflicting optional parameters
```

---

## Error Categories

### Parse-Time Errors (NURU_P###)
Detected during route pattern parsing (Stage 2). Should fail immediately with clear syntax error messages indicating exactly what syntax is malformed.

### Semantic-Time Errors (NURU_S###)
Detected during semantic validation (Stage 3). Should fail immediately with clear messages explaining what logical conflict or ambiguity exists.

### Bind-Time Errors
Detected during parameter binding at runtime. Should indicate which parameter failed and why (type conversion error, missing required value, etc.).

### Runtime Errors
Exceptions during handler execution. Should preserve original error information while adding context about which route and parameters were being processed.

## Non-Goals

- **Error Recovery**: Nuru does not attempt to recover from errors or guess corrections
- **Retry Logic**: Failed commands should be re-run by the user, not retried automatically
- **Complex Error Codes**: Keep it simple with 0/1 rather than elaborate error code schemes