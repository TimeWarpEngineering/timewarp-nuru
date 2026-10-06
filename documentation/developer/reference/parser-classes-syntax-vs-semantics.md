# Parser Classes: Syntax vs Semantics

This document clarifies which parser classes represent **syntax** (the literal representation as written) versus **semantics** (the meaning or interpretation).

All types below live in `source/timewarp-nuru-parsing/parsing/`.

## Lexer Layer (Pure Syntax)

| Class | Type | Description |
|-------|------|-------------|
| `Token` | **Syntax** | Raw textual token from input (type, value, position, length) |
| `RouteTokenType` | **Syntax** | Categories of raw tokens (`SingleDash`, `DoubleDash`, `Identifier`, `LeftBrace`, etc.) |
| `Lexer` | **Syntax** | Breaks the input string into syntactic tokens via `Tokenize()` |

## Syntax Tree Layer (Mixed)

| Class | Type | Description |
|-------|------|-------------|
| `Syntax` | **Structure** | Container for the parsed segments (`Segments`) |
| `SyntaxNode` | **Structure** | Abstract base record for all syntax tree nodes |
| `SegmentSyntax` | **Structure** | Abstract base record for segments; carries `Position` and `Length` in the original input |
| `LiteralSyntax` | **Semantic** | A literal string value (`Value`) to match |
| `ParameterSyntax` | **Semantic** | A parameter with `Name`, `Type`, `IsOptional`, `IsCatchAll`, `IsRepeated`, `Description` |
| `OptionSyntax` | **Mixed** | Contains semantic names (`LongForm`/`ShortForm`) and structural info (`IsOptional`, `Parameter`, `Description`) |
| `EndOfOptionsSyntax` | **Structure** | The standalone `--` end-of-options marker |

### OptionSyntax Details
- `LongForm` (e.g., "verbose") - **Semantic**: the long option name, stored **without** the leading `--`
- `ShortForm` (e.g., "v") - **Semantic**: the short option name, stored **without** the leading `-`
- `Parameter` - the associated `ParameterSyntax` for options that take a value
- The node itself represents the semantic concept of "an option"

## Runtime Representation

`Compiler` converts the syntax tree into a `CompiledRoute` whose `Segments` are `RouteMatcher` objects.

| Class | Type | Description |
|-------|------|-------------|
| `CompiledRoute` | **Mixed** | Runtime representation with both syntax and semantics (`Segments`, `Specificity`, `CatchAllParameterName`) |
| `RouteMatcher` | **Structure** | Abstract base class for matching |
| `LiteralMatcher` | **Semantic** | Value to match literally |
| `ParameterMatcher` | **Semantic** | Parameter definition with name, constraint, optional/catch-all flags |
| `OptionMatcher` | **Syntax** | Option as it appears on the command line |

### OptionMatcher Details
- `MatchPattern` (e.g., "--verbose", or "-v" when there is no long form) - **Syntax**: includes dashes as typed
- `AlternateForm` (e.g., "-v") - **Syntax**: the short form with its dash, set only when both long and short forms exist
- `ExpectsValue` - **Semantic**: whether the option takes a parameter
- `ParameterName` - **Semantic**: name of the value parameter
- `IsOptional`, `IsRepeated`, `ParameterIsOptional` - **Semantic**: optionality and repetition flags

## Parser Classes

| Class | Type | Description |
|-------|------|-------------|
| `Parser` | **Transformation** | Converts syntax (tokens) to a `Syntax` tree; returns a `ParseResult<Syntax>` |
| `SemanticValidator` | **Validation** | Checks the `Syntax` tree for meaning-level errors (duplicate parameters, catch-all position, etc.) |
| `Compiler` | **Transformation** | Converts the `Syntax` tree to the runtime `CompiledRoute` |
| `PatternParser` | **Facade** | Static entry point (`Parse`, `TryParse`) that runs `Parser` and `Compiler` |

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
2. **Tokens** (Syntax): `Identifier(git)`, `Identifier(commit)`, `SingleDash`, `Identifier(m)`, `LeftBrace`, `Identifier(message)`, `RightBrace`, `EndOfInput`
3. **Syntax tree** (Semantic): `OptionSyntax(LongForm=null, ShortForm="m", Parameter=ParameterSyntax(Name="message"))`
4. **Compiled route** (Syntax): `OptionMatcher(MatchPattern="-m", ExpectsValue=true, ParameterName="message")`

Note how the option's semantic name "m" is stored without a dash in `OptionSyntax`, but `Compiler.VisitOption` re-adds the dashes, so the runtime `OptionMatcher` holds the syntactic form "-m" in `MatchPattern` (and the short form in `AlternateForm` when a long form such as `--message,-m` is also declared).
