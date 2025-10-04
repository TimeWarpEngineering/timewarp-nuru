# Route Pattern Parsing Flow and Dependency Analysis

This document provides a detailed analysis of the method dependencies in the TimeWarp.Nuru route pattern parsing system.

## High-Level Flow

1. **NuruAppBuilder.AddRoute(pattern, handler)**
   - Calls `AddRouteInternal(pattern, handler, description)`

2. **NuruAppBuilder.AddRouteInternal()**
   - Calls `EndpointCollectionBuilder.AddRoute(pattern, handler, description)`

3. **DefaultEndpointCollectionBuilder.AddRoute()**
   - Calls `RoutePatternParser.Parse(routePattern)`
   - Creates a `RouteEndpoint` with the parsed route
   - Adds it to the `EndpointCollection`

4. **RoutePatternParser.Parse()**
   - Returns `ImprovedRoutePatternParser.Parse(routePattern)`

5. **ImprovedRoutePatternParser.Parse()**
   - Calls `Parser.Parse(routePattern)` (NewRoutePatternParser)
   - Calls `Builder.Build(result.Value)` (ParsedRouteBuilder)

6. **NewRoutePatternParser.Parse()**
   - Creates lexer and calls `lexer.Tokenize()`
   - Calls `ParsePattern()` to build AST
   - Returns AST

7. **Lexer.Tokenize()**
   - Breaks input into tokens (Identifier, DoubleDash, LeftBrace, etc.)

8. **ParsedRouteBuilder.Build()**
   - Visits AST nodes and builds ParsedRoute
   - Converts AST nodes to RouteSegments and OptionSegments

## Detailed Method Dependencies

### Lexer

**Class Dependencies:**
- `Token` class (creates instances)
- `TokenType` enum
- `System.Text.StringBuilder`
- `System.Collections.Generic.List<Token>`

**Method Dependencies:**

| Method | Calls |
|--------|-------|
| **Tokenize()** | `ScanToken()`, `IsAtEnd()`, `Token.EndOfInput()` |
| **ScanToken()** | `Advance()`, `Match()`, `AddToken()`, `ScanIdentifier()`, `ScanInvalidParameterSyntax()`, `IsAlphaNumeric()` |
| **ScanIdentifier()** | `IsAtEnd()`, `IsAlphaNumeric()`, `Peek()`, `Advance()`, `AddToken()` |
| **ScanInvalidParameterSyntax()** | `IsAtEnd()`, `Peek()`, `Advance()`, `AddToken()` |
| **Match()** | `IsAtEnd()` |
| **AddToken()** | `Token` constructor |
| **Advance()** | *(leaf method)* |
| **Peek()** | `IsAtEnd()` |
| **PeekNext()** | *(leaf method)* |
| **IsAtEnd()** | *(leaf method)* |
| **IsAlphaNumeric()** | *(leaf method)* |

### NewRoutePatternParser

**Class Dependencies:**
- `Lexer` class
- `Token` class
- `TokenType` enum
- AST node classes (`LiteralNode`, `ParameterNode`, `OptionNode`)
- `ParseError` class
- `ParseResult<T>` class
- `RoutePatternAst` class

**Method Dependencies:**

| Method | Calls |
|--------|-------|
| **Parse()** | `Lexer.Tokenize()`, `ParsePattern()` |
| **ParsePattern()** | `IsAtEnd()`, `ParseSegment()`, `Synchronize()` |
| **ParseSegment()** | `Peek()`, `ParseParameter()`, `ParseOption()`, `ParseLiteral()`, `ParseInvalidToken()` |
| **ParseLiteral()** | `Consume()`, `LiteralNode` constructor |
| **ParseParameter()** | `Consume()`, `Match()`, `ConsumeDescription()`, `ParameterNode` constructor |
| **ParseOption()** | `Current()`, `Advance()`, `Consume()`, `Match()`, `ConsumeDescription()`, `ParseParameter()`, `OptionNode` constructor |
| **ParseInvalidToken()** | `Advance()`, `AddError()` |
| **ConsumeDescription()** | `IsAtEnd()`, `Peek()`, `Advance()` |
| **Synchronize()** | `IsAtEnd()`, `Peek()`, `Advance()` |
| **Match()** | `Check()`, `Advance()` |
| **Check()** | `IsAtEnd()`, `Peek()` |
| **Consume()** | `Check()`, `Advance()`, `Peek()`, `AddError()` |
| **Advance()** | `IsAtEnd()`, `Previous()` |
| **IsAtEnd()** | `Peek()` |
| **Peek()** | `Token.EndOfInput()` |
| **Previous()** | *(leaf method)* |
| **Current()** | *(leaf method)* |
| **AddError()** | `ParseError` constructor |

### ParsedRouteBuilder

**Class Dependencies:**
- AST node classes (`RoutePatternAst`, `LiteralNode`, `ParameterNode`, `OptionNode`)
- Segment classes (`LiteralSegment`, `ParameterSegment`, `OptionSegment`)
- `ParsedRoute` class
- `RoutePatternVisitor<T>` base class

**Method Dependencies:**

| Method | Calls |
|--------|-------|
| **Build()** | `Accept()`, `BuildParsedRoute()` |
| **VisitPattern()** | `VisitSegment()` |
| **VisitSegment()** | `Visit()` |
| **Visit()** | `VisitLiteral()`, `VisitParameter()`, `VisitOption()` |
| **VisitLiteral()** | `LiteralSegment` constructor |
| **VisitParameter()** | `ParameterSegment` constructor |
| **VisitOption()** | `OptionSegment` constructor |
| **BuildParsedRoute()** | `ParsedRoute` constructor |

## Leaf Methods

These methods do not call any other methods in the parsing flow:

### Lexer
- **Advance()** - `return Input[Position++]`
- **PeekNext()** - `return Position + 1 >= Input.Length ? '\0' : Input[Position + 1]`
- **IsAtEnd()** - `return Position >= Input.Length`
- **IsAlphaNumeric()** - `return char.IsLetterOrDigit(c) || c == '_'`

### NewRoutePatternParser
- **Previous()** - `return Tokens[CurrentIndex - 1]`
- **Current()** - `return Tokens[CurrentIndex]`

### Constructors (all are leaf methods)
- **Token** constructor
- **LiteralNode** constructor
- **ParameterNode** constructor
- **OptionNode** constructor
- **ParseError** constructor
- **LiteralSegment** constructor
- **ParameterSegment** constructor
- **OptionSegment** constructor
- **ParsedRoute** constructor

### External .NET Dependencies
- `char.IsLetterOrDigit()`
- String operations
- List/Array operations

## Key Observations

1. The parsing flow follows a clear pipeline:
   - Lexer tokenizes the input string
   - Parser builds an AST from tokens
   - Builder converts AST to ParsedRoute

2. Most complexity is in the middle layers (parsing and AST traversal)

3. The leaf methods are simple primitives that:
   - Access array/string elements
   - Compare values
   - Construct objects

4. The lexer's leaf methods (`Advance()`, `IsAtEnd()`, etc.) form the foundation for all tokenization

5. The parser's leaf methods (`Previous()`, `Current()`) provide basic token navigation

This architecture allows for clear separation of concerns and makes the parsing logic easier to test and debug.