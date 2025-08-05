# Recommended Parser Restructure

## Current Problems

1. **Inconsistent naming**: `RoutePatternAst` has "Ast" suffix, but `LiteralNode`, `ParameterNode` don't
2. **Mixed organization**: `ParsedRoute` is in `/Parsing/` alongside lexer, not with segments
3. **Confusing class names**: `ParsedRoute` vs `RouteSegment` vs `SegmentNode`
4. **Property naming**: Mixing syntax/semantics (e.g., OptionNode has semantic names, OptionSegment has syntactic)

## Proposed Directory Structure

```
/Source/TimeWarp.Nuru/
├── /Parsing/
│   ├── /Lexer/
│   │   ├── Token.cs
│   │   ├── TokenType.cs
│   │   └── RouteLexer.cs
│   │
│   ├── /Syntax/              # AST nodes (pure structure)
│   │   ├── SyntaxNode.cs     # Base class
│   │   ├── RouteSyntax.cs    # Root (was RoutePatternAst)
│   │   ├── LiteralSyntax.cs  # Was LiteralNode
│   │   ├── ParameterSyntax.cs
│   │   └── OptionSyntax.cs
│   │
│   ├── /Parser/
│   │   ├── IRouteParser.cs
│   │   ├── RouteParser.cs    # Was NewRoutePatternParser
│   │   └── ParseException.cs
│   │
│   ├── /Compiler/            # Syntax → Runtime transformation
│   │   └── RouteCompiler.cs  # Was ParsedRouteBuilder
│   │
│   └── /Runtime/             # Runtime matching structures
│       ├── CompiledRoute.cs  # Was ParsedRoute
│       ├── /Matchers/
│       │   ├── RouteMatcher.cs    # Base
│       │   ├── LiteralMatcher.cs  # Was LiteralSegment
│       │   ├── ParameterMatcher.cs # Was ParameterSegment
│       │   └── OptionMatcher.cs    # Was OptionSegment
│       └── RouteMatchResult.cs
```

## Proposed Class Renames

### Lexer Layer
- Keep as is (already clear)

### Syntax Tree (AST)
- `RoutePatternAst` → `RouteSyntax`
- `SegmentNode` → `SyntaxNode`
- `LiteralNode` → `LiteralSyntax`
- `ParameterNode` → `ParameterSyntax`
- `OptionNode` → `OptionSyntax`

### Parser
- `NewRoutePatternParser` → `RouteParser`
- `ImprovedRoutePatternParser` → Remove (just use RouteParser directly)
- `RoutePatternParser` → `RouteParserFacade` (if needed for compatibility)

### Runtime
- `ParsedRoute` → `CompiledRoute`
- `RouteSegment` → `RouteMatcher`
- `LiteralSegment` → `LiteralMatcher`
- `ParameterSegment` → `ParameterMatcher`
- `OptionSegment` → `OptionMatcher`

## Proposed Property Renames

### OptionSyntax (was OptionNode)
```csharp
public record OptionSyntax(
    string? LongForm,      // Was LongName - just the name part
    string? ShortForm,     // Was ShortName - just the letter
    string? Description,
    ParameterSyntax? Parameter) : SyntaxNode;
```

### OptionMatcher (was OptionSegment)
```csharp
public class OptionMatcher : RouteMatcher
{
    public string MatchPattern { get; }      // Was Name - e.g., "-m", "--verbose"
    public bool ExpectsValue { get; }        // Keep as is
    public string? ParameterName { get; }    // Was ValueParameterName
    public string? AlternateForm { get; }    // Was ShortAlias
    public string? Description { get; }      // Keep as is
}
```

### CompiledRoute (was ParsedRoute)
```csharp
public class CompiledRoute
{
    public IReadOnlyList<RouteMatcher> PositionalMatchers { get; }  // Was PositionalTemplate
    public IReadOnlyList<OptionMatcher> OptionMatchers { get; }     // Was OptionSegments
    public IReadOnlyList<string> RequiredOptionPatterns { get; }    // Was RequiredOptions
    public string? CatchAllParameterName { get; }                   // Keep as is
    public int Specificity { get; }                                  // Keep as is
}
```

## Benefits

1. **Clear separation**: Lexer → Syntax → Parser → Compiler → Runtime
2. **Consistent naming**: All AST nodes end with "Syntax", all runtime matchers end with "Matcher"
3. **Better organization**: Related classes grouped in subdirectories
4. **Clearer semantics**: "Compiled" route makes it clear it's processed and ready for runtime
5. **No ambiguity**: "Matcher" clearly indicates runtime matching, "Syntax" clearly indicates AST

## Migration Path

1. Create new directory structure
2. Copy classes with new names
3. Mark old classes as obsolete
4. Update references gradually
5. Remove old classes once all references updated