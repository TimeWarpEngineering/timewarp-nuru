# Parser Classes: Syntax vs Semantics

This document clarifies which parser classes represent **syntax** (the literal representation as written) versus **semantics** (the meaning or interpretation).

## Lexer Layer (Pure Syntax)

| Class | Type | Description |
|-------|------|-------------|
| `Token` | **Syntax** | Raw textual tokens from input (e.g., `-`, `m`, `{`, `}`) |
| `TokenType` | **Syntax** | Categories of raw tokens (SingleDash, DoubleDash, Identifier, etc.) |
| `Lexer` | **Syntax** | Breaks input string into syntactic tokens |

## AST Layer (Mixed)

| Class | Type | Description |
|-------|------|-------------|
| `RoutePatternAst` | **Structure** | Container for parsed segments |
| `SegmentNode` | **Structure** | Base class for AST nodes |
| `LiteralNode` | **Semantic** | Represents a literal string value to match |
| `ParameterNode` | **Semantic** | Represents a parameter with name, type, optional flag |
| `OptionNode` | **Mixed** | Contains both semantic names (LongName/ShortName) and structural info |

### OptionNode Details
- `LongName` (e.g., "verbose") - **Semantic**: the option name without dashes
- `ShortName` (e.g., "v") - **Semantic**: the short form name without dash
- The node itself represents the semantic concept of "an option"

## Route Segments (Runtime Representation)

| Class | Type | Description |
|-------|------|-------------|
| `ParsedRoute` | **Mixed** | Runtime representation with both syntax and semantics |
| `RouteSegment` | **Structure** | Base class for route matching |
| `LiteralSegment` | **Semantic** | Value to match literally |
| `ParameterSegment` | **Semantic** | Parameter definition with name, type, constraints |
| `OptionSegment` | **Syntax** | Option as it appears on command line |

### OptionSegment Details
- `Name` (e.g., "-v", "--verbose") - **Syntax**: includes dashes as typed
- `ExpectsValue` - **Semantic**: whether option takes a parameter
- `ValueParameterName` - **Semantic**: name of the parameter
- `ShortAlias` (e.g., "-v") - **Syntax**: alternative form with dash

## Parser Classes

| Class | Type | Description |
|-------|------|-------------|
| `NewRoutePatternParser` | **Transformation** | Converts syntax (tokens) to semantics (AST) |
| `ParsedRouteBuilder` | **Transformation** | Converts semantic AST to runtime representation |
| `ImprovedRoutePatternParser` | **Facade** | Orchestrates parsing pipeline |

## Key Distinctions

### Syntax-focused classes deal with:
- How things are written (`-m` vs `--message`)
- Raw text representation
- Tokens and lexical analysis
- Command-line appearance

### Semantic-focused classes deal with:
- What things mean (option named "message")
- Logical structure
- Type information
- Behavioral properties

### Mixed classes:
- Contain both representations
- Often used at boundaries between layers
- Support runtime matching which needs both syntax (to match input) and semantics (to extract values)

## Example: Option Parsing Flow

1. **Input**: `"git commit -m {message}"`
2. **Tokens** (Syntax): `[git] [commit] [-] [m] [{] [message] [}]`
3. **AST** (Semantic): `OptionNode(ShortName="m", Parameter=...)`
4. **Route** (Syntax): `OptionSegment(Name="-m", ...)`

Note how the option's semantic name "m" is stored without dash in the AST, but the runtime OptionSegment uses the syntactic form "-m" for matching.