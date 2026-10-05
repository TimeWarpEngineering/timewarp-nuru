# Route Pattern Parsing Flow and Dependency Analysis

This document provides an analysis of the method dependencies in the TimeWarp.Nuru route pattern parsing system, located in `source/timewarp-nuru-parsing/parsing/`.

## High-Level Flow

Route patterns declared with `Map("pattern").WithHandler(...).AsCommand().Done()` are parsed with the same pipeline that the source generator uses at compile time (the analyzer calls `PatternParser.TryParse`).

1. **PatternParser.Parse() / PatternParser.TryParse()**
   - Creates a `Parser` and a `Compiler`
   - Calls `Parser.Parse(routePattern)`; throws `PatternException` (or returns the errors from `TryParse`) when the result is not successful
   - Calls `Compiler.Compile(result.Value)` and returns the `CompiledRoute`

2. **Parser.Parse()**
   - Creates a `Lexer` and calls `lexer.Tokenize()`
   - Calls `ParsePattern()` to build the `Syntax` tree
   - Calls `SemanticValidator.Validate(ast)`
   - Returns a `ParseResult<Syntax>` holding the tree, `ParseErrors`, and `SemanticErrors`

3. **Lexer.Tokenize()**
   - Breaks input into tokens (`Identifier`, `DoubleDash`, `LeftBrace`, etc.) and appends an `EndOfInput` token

4. **Compiler.Compile()**
   - Visits the `Syntax` tree via `VisitPattern()` (`Compiler` extends `SyntaxVisitor<object?>`)
   - Converts syntax nodes to `RouteMatcher` objects (`LiteralMatcher`, `ParameterMatcher`, `OptionMatcher`) and computes specificity
   - Returns a `CompiledRoute`

## Detailed Method Dependencies

### Lexer

**Class Dependencies:**
- `Token` record (creates instances)
- `RouteTokenType` enum
- `System.Collections.Generic.List<Token>`

**Methods:**

| Method | Calls |
|--------|-------|
| **Tokenize()** | `ScanToken()`, `IsAtEnd()`, `Token.EndOfInput()` |
| **ScanToken()** | `Advance()`, `Match()`, `AddToken()`, `ScanIdentifier()`, `ScanInvalidParameterSyntax()`, `IsAlphaNumeric()` |
| **ScanIdentifier()** | `IsAtEnd()`, `IsAlphaNumeric()`, `Peek()`, `Advance()`, `AddToken()` |
| **ScanInvalidParameterSyntax()** | `IsAtEnd()`, `Peek()`, `Advance()`, `AddToken()` |
| **Match()** | `IsAtEnd()` |
| **AddToken()** | `Token` constructor |
| **Peek()** | `IsAtEnd()` |
| **Advance()** | *(leaf method)* |
| **IsAtEnd()** | *(leaf method)* |
| **IsAlphaNumeric()** | *(leaf method)* |

### Parser

The `Parser` class is split across partial files: `parser.cs`, `parser.segments.cs`, `parser.navigation.cs`, and `parser.validation.cs`.

**Class Dependencies:**
- `Lexer` class
- `Token` record
- `RouteTokenType` enum
- Syntax node records (`Syntax`, `SegmentSyntax`, `LiteralSyntax`, `ParameterSyntax`, `OptionSyntax`, `EndOfOptionsSyntax`)
- `SemanticValidator` class
- `ParseError` record (and subclasses such as `UnexpectedTokenError`)
- `ParseException` class
- `ParseResult<T>` class

**Methods:**

| Method | Calls |
|--------|-------|
| **Parse()** | `Lexer.Tokenize()`, `ParsePattern()`, `SemanticValidator.Validate()` |
| **ParsePattern()** | `IsAtEnd()`, `ParseSegment()`, `Synchronize()` |
| **ParseSegment()** | `Peek()`, `Previous()`, `ParseParameter()`, `ParseOption()`, `ParseEndOfOptions()`, `ParseLiteral()`, `ParseInvalidToken()`, `HandleUnexpectedRightBrace()`, `HandleUnexpectedToken()`, `AddParseError()` |
| **ParseOption()** | `Current()`, `Advance()`, `Consume()`, `ParseOptionForms()`, `Match()`, `ParseOptionDescription()`, `ParseOptionParameter()`, `Previous()` |
| **ParseOptionForms()** | `Match()`, `Consume()` |
| **Synchronize()** | `IsAtEnd()`, `Peek()`, `Advance()` |
| **Match()** | `Check()`, `Advance()` |
| **Check()** | `IsAtEnd()`, `Peek()` |
| **Consume()** | `Check()`, `Advance()`, `Peek()`, `AddParseError()` |
| **Advance()** | `IsAtEnd()`, `Previous()` |
| **IsAtEnd()** | `Peek()` |
| **Peek()** | `Token.EndOfInput()` |
| **Previous()** | *(leaf method)* |
| **Current()** | *(leaf method)* |

`ParseLiteral()`, `ParseParameter()`, `ParseOptionDescription()`, `ParseOptionParameter()`, and `ConsumeDescription()` are also in the parser; they use the same navigation helpers (`Consume()`, `Match()`, `Advance()`, `Peek()`).

### SemanticValidator

`SemanticValidator.Validate(Syntax)` collects segment metadata into a `ValidationContext` and runs the checks `ValidateDuplicateParameters`, `ValidateOptionalBeforeRequired`, `ValidateConsecutiveOptionalParameters`, `ValidateCatchAllPosition`, `ValidateEndOfOptionsSeparator`, `ValidateDuplicateOptionAliases`, and `ValidateMixedCatchAllWithOptional`, producing `SemanticError` values.

### Compiler

**Class Dependencies:**
- Syntax node records (`Syntax`, `LiteralSyntax`, `ParameterSyntax`, `OptionSyntax`, `EndOfOptionsSyntax`)
- Matcher classes (`LiteralMatcher`, `ParameterMatcher`, `OptionMatcher`) deriving from `RouteMatcher`
- `CompiledRoute` class
- `SyntaxVisitor<T>` base class

**Methods:**

| Method | Calls |
|--------|-------|
| **Compile()** | `VisitPattern()`, `CompiledRoute` constructor |
| **VisitPattern()** *(from `SyntaxVisitor<T>`)* | `Visit()` |
| **Visit()** *(from `SyntaxVisitor<T>`)* | `VisitLiteral()`, `VisitParameter()`, `VisitOption()`, `VisitEndOfOptions()` |
| **VisitLiteral()** | `LiteralMatcher` constructor |
| **VisitParameter()** | `ParameterMatcher` constructor |
| **VisitOption()** | `OptionMatcher` constructor |
| **VisitEndOfOptions()** | *(adds no matcher; structural marker only)* |

## Leaf Methods

These methods do not call any other methods in the parsing flow:

### Lexer
- **Advance()** - `return Input[Position++]`
- **IsAtEnd()** - `return Position >= Input.Length`
- **IsAlphaNumeric()** - `return char.IsLetterOrDigit(c) || c == '_'`

### Parser
- **Previous()** - `return Tokens[CurrentIndex - 1]`
- **Current()** - `return Tokens[CurrentIndex]`

### Constructors (all are leaf methods)
- **Token** constructor
- **LiteralSyntax**, **ParameterSyntax**, **OptionSyntax** constructors
- **LiteralMatcher**, **ParameterMatcher**, **OptionMatcher** constructors
- **CompiledRoute** constructor

### External .NET Dependencies
- `char.IsLetterOrDigit()`
- String operations
- List/Array operations

## Key Observations

1. The parsing flow follows a clear pipeline:
   - `Lexer` tokenizes the input string
   - `Parser` builds a `Syntax` tree from tokens
   - `SemanticValidator` validates the tree
   - `Compiler` converts the tree to a `CompiledRoute`

2. Most complexity is in the middle layers (parsing and tree traversal)

3. The leaf methods are simple primitives that:
   - Access array/string elements
   - Compare values
   - Construct objects

4. The lexer's leaf methods (`Advance()`, `IsAtEnd()`, etc.) form the foundation for all tokenization

5. The parser's leaf methods (`Previous()`, `Current()`) provide basic token navigation

This architecture allows for clear separation of concerns and makes the parsing logic easier to test and debug.
