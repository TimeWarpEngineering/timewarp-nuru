# Implement Proper Route Pattern Parser

## Description

Replace the current regex-based route pattern parsing with a proper hand-written recursive descent parser. This will provide better error messages, compile-time validation support, and a reusable parsing infrastructure for route patterns throughout the Nuru ecosystem.

## Problem Statement

The current `RoutePatternParser` uses complex regex patterns and manual tokenization with state tracking. This approach is:
- Fragile and hard to maintain
- Provides poor error messages (e.g., when user types `<input>` instead of `{input}`)
- Difficult to extend with new syntax
- Not reusable for compile-time analysis

## Requirements

- Clean, maintainable parser implementation using recursive descent
- Produces an Abstract Syntax Tree (AST) for route patterns
- Provides detailed error messages with exact positions
- Reusable across runtime parsing, compile-time analysis, and tooling
- Maintains backward compatibility with existing route syntax
- Performance should be comparable to or better than current regex approach

## Implementation Plan

### Phase 1: Define AST and Interfaces

1. Create AST node types:
   ```csharp
   public abstract record RoutePatternNode;
   public record RoutePatternAst(IReadOnlyList<SegmentNode> Segments) : RoutePatternNode;
   public abstract record SegmentNode : RoutePatternNode;
   public record LiteralNode(string Value, int Position) : SegmentNode;
   public record ParameterNode(string Name, bool IsCatchAll, bool IsOptional, 
       string? Type, string? Description, int Position) : SegmentNode;
   public record OptionNode(string LongName, string? ShortName, 
       string? Description, ParameterNode? Parameter, int Position) : SegmentNode;
   ```

2. Define parser interfaces:
   ```csharp
   public interface IRoutePatternParser
   {
       ParseResult<RoutePatternAst> Parse(string pattern);
   }
   
   public class ParseResult<T>
   {
       public T? Value { get; init; }
       public bool Success { get; init; }
       public IReadOnlyList<ParseError> Errors { get; init; }
   }
   
   public record ParseError(string Message, int Position, int Length, string? Suggestion = null);
   ```

3. Define visitor interface for AST processing:
   ```csharp
   public interface IRoutePatternVisitor<T>
   {
       T VisitPattern(RoutePatternAst pattern);
       T VisitLiteral(LiteralNode literal);
       T VisitParameter(ParameterNode parameter);
       T VisitOption(OptionNode option);
   }
   ```

### Phase 2: Implement Lexer/Tokenizer

1. Define token types:
   ```csharp
   public enum TokenType
   {
       Literal, LeftBrace, RightBrace, Colon, Question, Pipe, 
       Asterisk, DoubleDash, SingleDash, Comma, Identifier, 
       Description, EndOfInput
   }
   
   public record Token(TokenType Type, string Value, int Position, int Length);
   ```

2. Implement tokenizer that handles:
   - Context-sensitive tokenization (spaces in descriptions vs separators)
   - Proper handling of special characters
   - Position tracking for error reporting

### Phase 3: Implement Recursive Descent Parser

1. Parser structure:
   ```csharp
   public class RoutePatternParser : IRoutePatternParser
   {
       private List<Token> tokens;
       private int current;
       private List<ParseError> errors;
       
       // Main parsing methods
       private RoutePatternAst ParsePattern();
       private SegmentNode ParseSegment();
       private ParameterNode ParseParameter();
       private OptionNode ParseOption();
       
       // Helper methods
       private bool Match(TokenType type);
       private Token Consume(TokenType type, string errorMessage);
       private void SynchronizeAfterError();
   }
   ```

2. Implement grammar rules:
   ```
   pattern        → segment*
   segment        → literal | parameter | option
   literal        → IDENTIFIER
   parameter      → '{' '*'? IDENTIFIER optional? type? description? '}'
   option         → ('--' | '-') IDENTIFIER aliases? description? parameter?
   optional       → '?'
   type           → ':' IDENTIFIER optional?
   description    → '|' DESCRIPTION
   aliases        → (',' '-' IDENTIFIER)+
   ```

### Phase 4: Error Handling and Recovery

1. Implement specific error types:
   - Invalid parameter syntax (e.g., `<input>` instead of `{input}`)
   - Unmatched braces
   - Invalid characters in identifiers
   - Missing required elements

2. Error recovery strategies:
   - Synchronize to next segment after error
   - Provide helpful suggestions (e.g., "Did you mean {input}?")
   - Continue parsing to find multiple errors

3. Example error messages:
   ```
   Error at position 7: Invalid parameter syntax '<input>'. 
   Use curly braces for parameters: {input}
   
   Error at position 15: Unmatched closing brace '}'.
   
   Error at position 23: Expected type name after ':' in parameter {count:}.
   ```

### Phase 5: Create AST-to-ParsedRoute Converter

1. Implement visitor that converts AST to existing `ParsedRoute` structure:
   ```csharp
   public class ParsedRouteBuilder : IRoutePatternVisitor<ParsedRoute>
   {
       // Convert AST nodes to RouteSegment, ParameterSegment, etc.
   }
   ```

2. Ensure backward compatibility with existing code

### Phase 6: Integrate with Existing Code

1. Replace `RoutePatternParser.Parse()` implementation
2. Update `NuruAppBuilder` to use new parser
3. Maintain existing public API

### Phase 7: Create Roslyn Analyzer

1. Create analyzer project that uses the parser
2. Analyze `AddRoute` calls at compile time
3. Report diagnostics for invalid patterns:
   - NURU001: Invalid parameter syntax
   - NURU002: Unbalanced braces
   - NURU003: Invalid option format

### Phase 8: Testing

1. Unit tests for lexer
2. Unit tests for parser (valid and invalid patterns)
3. Integration tests with existing route patterns
4. Performance benchmarks vs regex implementation
5. Analyzer tests

## Example Usage

```csharp
// Runtime parsing
var parser = new RoutePatternParser();
var result = parser.Parse("deploy {env|Environment} --dry-run,-d|Preview");

if (!result.Success)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error at {error.Position}: {error.Message}");
        if (error.Suggestion != null)
            Console.WriteLine($"  Suggestion: {error.Suggestion}");
    }
    return;
}

// Use the AST
var route = new ParsedRouteBuilder().Visit(result.Value);
```

## Benefits

1. **Better Error Messages**: Clear, actionable errors with positions and suggestions
2. **Maintainability**: Grammar-based approach is easier to understand and modify
3. **Reusability**: Parser can be used in analyzer, runtime, help generation, etc.
4. **Extensibility**: New syntax can be added by updating grammar rules
5. **Compile-Time Safety**: Analyzer can catch errors before runtime

## Technical Considerations

- Parser should be allocation-efficient for runtime use
- Consider caching parsed results for repeated patterns
- Ensure thread safety if parser instances are reused
- Profile performance vs current regex implementation

## Dependencies

- No external parser libraries (hand-written)
- Roslyn for analyzer implementation
- Existing Nuru types for backward compatibility

## Success Criteria

- All existing route patterns parse correctly
- Invalid patterns produce helpful error messages
- Analyzer catches common mistakes at compile time
- Performance is within 10% of current implementation
- Code is well-documented and maintainable

This will create the foundation for robust route pattern handling throughout the Nuru ecosystem.