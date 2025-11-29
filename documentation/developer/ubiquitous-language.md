# Ubiquitous Language

**Status:** Living Document
**Last Updated:** 2025-10-02

## Purpose

This document defines the **canonical terminology** used throughout the TimeWarp.Nuru project. These terms form our **Ubiquitous Language** (from Domain-Driven Design) - the shared vocabulary used consistently in:

- Design Documents
- Source Code (class names, method names, namespaces)
- Tests
- API Documentation
- Developer Communication

**Critical Rule:** If a term is not defined here, it should not be introduced into design documents or code without first adding it to this document.

## Dependency Flow

```
Architectural Vision + Ubiquitous Language (this document)
     ↓
Design Documents (use ONLY UL terms)
     ↓
Source Code (implements Design using UL terms)
     ↓
Reference Documentation (describes implementation using UL terms)
```

The Ubiquitous Language is an **input** to Design - when we change UL, we cascade those changes through Design, then Code.

---

## Processing Pipeline Overview

The Nuru framework processes CLI commands through a pipeline of distinct stages:

```
Route Pattern String → [Lexer] → Tokens → [Parser] → AST → [Compiler] → Matchers → [Resolver] → Matched Endpoint → [Executor] → Result
```

Each stage has its own ubiquitous language defined below.

---

# Lexer Subsystem

The **Lexer** is the first stage of the processing pipeline. It converts a route pattern string into a sequence of tokens.

## Primary Concepts

### Lexer
**Definition:** Component that converts a route pattern string into a sequence of tokens through the process of tokenization.

**Code Artifact:** `Lexer` class

**Example:**
```csharp
Lexer lexer = new Lexer("deploy {env} --force");
IReadOnlyList<Token> tokens = lexer.Tokenize();
```

**Related Terms:** Tokenization, Token, Input

---

### Token
**Definition:** Atomic unit produced by the Lexer, representing a meaningful symbol or sequence of characters from the input. Each token has a type, value, position, and length.

**Code Artifact:** `Token` record

**Structure:**
- `TokenType Type` - Classification of the token
- `string Value` - The actual text
- `int Position` - Zero-based start index in input
- `int Length` - Number of characters

**Example:**
```csharp
// Input: "deploy {env}"
// Produces tokens:
Token { Type = Identifier, Value = "deploy", Position = 0, Length = 6 }
Token { Type = LeftBrace, Value = "{", Position = 7, Length = 1 }
Token { Type = Identifier, Value = "env", Position = 8, Length = 3 }
Token { Type = RightBrace, Value = "}", Position = 11, Length = 1 }
Token { Type = EndOfInput, Value = "", Position = 12, Length = 0 }
```

**Related Terms:** TokenType, Position, Value

---

### TokenType
**Definition:** Enumeration classifying what kind of token was recognized by the Lexer. Determines how the Parser interprets the token.

**Code Artifact:** `TokenType` enum

**Categories:**

**Structural Delimiters:**
- `LeftBrace` - `{` - Opens parameter
- `RightBrace` - `}` - Closes parameter
- `Colon` - `:` - Separates parameter from type constraint
- `Pipe` - `|` - Separates parameter/option from description
- `Comma` - `,` - Separates option aliases

**Operators:**
- `Question` - `?` - Marks parameter or option as optional
- `Asterisk` - `*` - Marks catch-all parameter

**Dash Sequences:**
- `SingleDash` - `-` - Short option prefix
- `DoubleDash` - `--` - Long option prefix
- `EndOfOptions` - `--` (standalone) - POSIX end-of-options separator

**Content:**
- `Identifier` - Alphanumeric sequence (parameter names, option names, literals)
- `Description` - Text following a pipe character (not currently emitted as separate token type)

**Special:**
- `EndOfInput` - Marks end of input string
- `Invalid` - Malformed or unrecognized character sequence

**Related Terms:** Token, Delimiter, Modifier

---

### Tokenization
**Definition:** The process of breaking an input string into a sequence of tokens. This is what the Lexer does.

**Code Artifact:** `Tokenize()` method

**Synonym:** Lexical Analysis (more formal/academic term used in logging)

**Example:**
```csharp
// Tokenization process:
Input: "git commit -m {msg}"
  ↓ [Lexer.Tokenize()]
Output: [
  Token(Identifier, "git", 0, 3),
  Token(Identifier, "commit", 4, 6),
  Token(SingleDash, "-", 11, 1),
  Token(Identifier, "m", 12, 1),
  Token(LeftBrace, "{", 14, 1),
  Token(Identifier, "msg", 15, 3),
  Token(RightBrace, "}", 18, 1),
  Token(EndOfInput, "", 19, 0)
]
```

**Related Terms:** Lexer, Token, Scan

---

## Input/Output Concepts

### Input
**Definition:** The route pattern string being tokenized by the Lexer. This is the raw character sequence before processing.

**Code Artifact:** `string Input` field in `Lexer`

**Example:**
```csharp
string input = "deploy {env} --force";
Lexer lexer = new Lexer(input);
```

**Related Terms:** Route Pattern, Position

---

### Route Pattern
**Definition:** A string template that defines CLI command syntax. This is a **user-facing concept** - what developers write when defining routes.

**Note:** This is the INPUT to the Lexer, not a Lexer concept itself. The Lexer doesn't understand "Route Pattern" semantics, it only sees it as "Input to tokenize".

**Examples:**
- `"git status"`
- `"deploy {env} --tag {version?}"`
- `"docker run {*args}"`

**Related Terms:** Input (from Lexer perspective), Parameter, Option (from user perspective)

---

### Position
**Definition:** Zero-based character index in the input string. Used to track where each token begins and to report error locations.

**Code Artifact:** `int Position` field in `Lexer`, `int Position` property in `Token`

**Example:**
```csharp
// Input: "deploy {env}"
//        0123456789...
// Token "deploy" starts at Position 0
// Token "{" starts at Position 7
// Token "env" starts at Position 8
```

**Related Terms:** Token, Input, Length

---

### End of Input
**Definition:** Special condition indicating no more characters remain to process. Marked by a special EndOfInput token.

**Code Artifact:** `TokenType.EndOfInput`

**Characteristic:** Always the last token in the sequence, always has Length = 0

**Example:**
```csharp
// Every tokenization ends with:
Token { Type = EndOfInput, Value = "", Position = <end>, Length = 0 }
```

**Related Terms:** Token, TokenType

---

## Token Categories

### Delimiter
**Definition:** Structural tokens that separate or group other tokens. These define the syntax structure.

**Token Types:**
- `LeftBrace` (`{`)
- `RightBrace` (`}`)
- `Colon` (`:`)
- `Pipe` (`|`)
- `Comma` (`,`)

**Purpose:** Allow Parser to recognize boundaries between syntactic elements

**Example:**
```csharp
// Input: "{env:string|Environment name}"
// Delimiters: { : | }
```

**Related Terms:** TokenType, Modifier

---

### Identifier
**Definition:** Alphanumeric sequence (including underscores and internal single dashes) representing names - could be parameter names, option names, type names, or literal segments.

**Code Artifact:** `TokenType.Identifier`

**Valid Characters:**
- Letters (Unicode letter categories)
- Digits
- Underscore (`_`)
- Single dash (`-`) when followed by alphanumeric

**Examples:**
```csharp
// Simple identifiers:
"deploy" "status" "env" "int" "version"

// Compound identifiers (with internal dashes):
"dry-run" "no-edit" "save-dev"

// With underscores:
"my_command" "param_name"
```

**Related Terms:** Compound Identifier, Token, TokenType

---

### Modifier
**Definition:** Symbols that modify the meaning of adjacent tokens. Modifiers change parameter behavior or requirements.

**Token Types:**
- `Question` (`?`) - Marks parameter or option as optional
- `Asterisk` (`*`) - Marks parameter as catch-all/variadic

**Example:**
```csharp
// Input: "{env?} {*args}"
// Modifiers: ? (optional), * (catch-all)
```

**Related Terms:** TokenType, Delimiter, Parameter Modifiers, Option Modifiers

---

### Dash Sequence
**Definition:** Single or double dashes used to mark command-line options.

**Token Types:**
- `SingleDash` (`-`) - Prefix for short options
- `DoubleDash` (`--`) - Prefix for long options
- `EndOfOptions` (`--` standalone) - POSIX end-of-options separator

**Distinction:**
```csharp
// DoubleDash: followed by identifier
"--verbose" → [DoubleDash("--"), Identifier("verbose")]

// EndOfOptions: followed by space or end of input
"exec -- {*args}" → [Identifier("exec"), EndOfOptions("--"), ...]
```

**Related Terms:** TokenType, End-of-Options Separator

---

### Invalid Token
**Definition:** Token produced when the Lexer encounters unrecognized or malformed character sequences that violate tokenization rules.

**Code Artifact:** `TokenType.Invalid`

**Causes:**
- Double dashes within identifiers: `test--case`
- Trailing dashes: `foo-`, `bar--`
- Wrong delimiters: `<param>` instead of `{param}`

**Purpose:** Early error detection - catch nonsensical patterns before Parser sees them

**Example:**
```csharp
// Input: "test--case"
// Output: Token { Type = Invalid, Value = "test--case", Position = 0, Length = 10 }
```

**Related Terms:** Malformed Sequence, Invalid Syntax, Error Handling

---

## Special Cases

### End-of-Options Separator
**Definition:** Standalone `--` (double dash followed by space or end of input) that marks the end of option parsing per POSIX standard. Everything after this separator is treated as positional arguments, even if it starts with dashes.

**Code Artifact:** `TokenType.EndOfOptions`

**Purpose:** Allow literal dash-prefixed values without them being interpreted as options

**Example:**
```csharp
// Input: "exec -- --not-a-flag file.txt"
// The "--not-a-flag" after the separator is NOT tokenized as DoubleDash + Identifier
// It's part of the positional arguments

Tokens:
  Identifier("exec")
  EndOfOptions("--")
  Identifier("--not-a-flag")  // Treated as literal after separator
  Identifier("file.txt")
```

**Contrast:**
```csharp
// WITHOUT separator:
"exec --verbose --dry-run"
  → DoubleDash, Identifier("verbose"), DoubleDash, Identifier("dry-run")

// WITH separator:
"exec -- --verbose --dry-run"
  → Identifier("exec"), EndOfOptions, Identifier("--verbose"), Identifier("--dry-run")
```

**Related Terms:** Dash Sequence, POSIX, TokenType

---

### Compound Identifier
**Definition:** Identifier containing one or more internal single dashes, commonly used in option names like `--dry-run` or `--no-edit`.

**Still classified as:** `TokenType.Identifier` (single token, not multiple)

**Rules:**
- Dash must be followed by alphanumeric character
- Multiple single dashes allowed: `my-long-option-name`
- Double dashes NOT allowed within: `test--case` is Invalid
- Trailing dashes NOT allowed: `test-` is Invalid

**Examples:**
```csharp
// Valid compound identifiers:
"dry-run" → Token(Identifier, "dry-run", ...)
"no-edit" → Token(Identifier, "no-edit", ...)
"my-very-long-command-name" → Token(Identifier, "my-very-long-command-name", ...)

// Invalid (produce Invalid tokens):
"test--case" → Token(Invalid, "test--case", ...)
"foo-" → Token(Invalid, "foo-", ...)
```

**Related Terms:** Identifier, Dash Sequence, Invalid Token

---

### Whitespace
**Definition:** Spaces, tabs, carriage returns, and newlines that separate tokens but are not themselves emitted as tokens. Whitespace is consumed and discarded during tokenization.

**Characters:**
- Space (` `)
- Tab (`\t`)
- Carriage return (`\r`)
- Newline (`\n`)

**Behavior:** Acts as token separator but doesn't produce tokens

**Example:**
```csharp
// Input: "git   status\t--verbose\n"
// Whitespace consumed, not emitted as tokens
Tokens:
  Identifier("git")
  Identifier("status")
  DoubleDash("--")
  Identifier("verbose")
  EndOfInput
```

**Related Terms:** Input, Scan, Delimiter

---

## Lexer Operations

### Scan
**Definition:** The operation of examining the current character in the input and producing the appropriate token(s). This is the core Lexer algorithm.

**Code Artifact:** `ScanToken()` method

**Process:**
1. Read current character via `Advance()`
2. Determine token type based on character
3. Collect additional characters if needed (for multi-char tokens)
4. Emit token via `AddToken()`

**Example:**
```csharp
// When scanning '{':
ScanToken() → Advance() returns '{' → AddToken(TokenType.LeftBrace, "{")

// When scanning identifier start:
ScanToken() → Advance() returns 'd' → ScanIdentifier() collects "deploy" → AddToken(Identifier, "deploy")
```

**Related Terms:** Advance, Tokenization, Token

---

### Advance
**Definition:** Move the position pointer forward by one character and return the character at the previous position. This consumes input.

**Code Artifact:** `Advance()` method

**Signature:** `char Advance()`

**Side Effect:** Increments `Position`

**Example:**
```csharp
// Input: "git"
// Position = 0

char c = Advance();  // c = 'g', Position = 1
c = Advance();       // c = 'i', Position = 2
c = Advance();       // c = 't', Position = 3
```

**Related Terms:** Peek, Position, Scan

---

### Peek
**Definition:** Look at the current or next character in the input WITHOUT consuming it (non-destructive read). Used for lookahead decisions.

**Code Artifacts:**
- `Peek()` - Look at current character
- `PeekNext()` - Look one character ahead

**Signatures:**
- `char Peek()` - Returns current char or `\0` if at end
- `char PeekNext()` - Returns next char or `\0` if beyond end

**Side Effect:** None - Position unchanged

**Example:**
```csharp
// Input: "git"
// Position = 0

char current = Peek();      // 'g', Position still 0
char next = PeekNext();     // 'i', Position still 0
char consumed = Advance();  // 'g', Position now 1
current = Peek();           // 'i', Position still 1
```

**Use Case:**
```csharp
// Decide if we have DoubleDash or SingleDash:
if (c == '-') {
  if (Peek() == '-') {  // Lookahead without consuming
    // It's DoubleDash
  } else {
    // It's SingleDash
  }
}
```

**Related Terms:** Advance, Scan, Lookahead

---

### Match
**Definition:** Check if the current character equals an expected character. If true, advance past it. If false, don't advance. Combines conditional check with consume.

**Code Artifact:** `Match(char expected)` method

**Signature:** `bool Match(char expected)`

**Returns:** `true` if matched and consumed, `false` otherwise

**Example:**
```csharp
// Input: "git"
// Position = 0

Advance();              // Consume 'g', Position = 1
bool matched = Match('i');  // true, Position = 2
matched = Match('x');   // false, Position still 2 (no match, no advance)
matched = Match('t');   // true, Position = 3
```

**Use Case:**
```csharp
// Check for double dash:
if (c == '-' && Match('-')) {
  // We have '--', position advanced past both dashes
  AddToken(TokenType.DoubleDash, "--");
}
```

**Related Terms:** Peek, Advance, Scan

---

## Error Handling

### Malformed Sequence
**Definition:** Character pattern that violates the Lexer's tokenization rules. Produces an Invalid token.

**Examples:**
- `test--case` - Double dash within identifier
- `foo-` - Trailing dash
- `bar---baz` - Multiple dashes

**Lexer Behavior:** Scan the entire malformed sequence and emit single Invalid token

**Related Terms:** Invalid Token, Invalid Syntax

---

### Invalid Syntax
**Definition:** Use of incorrect delimiters or characters that don't match route pattern syntax.

**Examples:**
- `<param>` - Angle brackets instead of curly braces
- `[name]` - Square brackets instead of curly braces
- Random special characters

**Lexer Behavior:** Emit Invalid token

**Example:**
```csharp
// Input: "deploy <env>"
Tokens:
  Identifier("deploy")
  Invalid("<env>")  // Entire angle-bracket sequence
  EndOfInput
```

**Related Terms:** Invalid Token, Malformed Sequence

---

### Trailing Dash
**Definition:** Dash character at the end of an identifier, not followed by an alphanumeric character. This is a specific type of malformed sequence.

**Examples:**
- `test-` followed by space or end of input
- `foo-` followed by `}`

**Why Invalid:** Dash must be internal (connecting two alphanumeric parts) or a prefix for options, never trailing

**Lexer Behavior:** Include the trailing dash in the Invalid token

**Example:**
```csharp
// Input: "test- {env}"
Tokens:
  Invalid("test-")  // Includes the dash
  LeftBrace("{")
  Identifier("env")
  RightBrace("}")
```

**Related Terms:** Malformed Sequence, Compound Identifier

---

## Rejected Terms

These terms are **NOT** part of our Ubiquitous Language. Do not use them in design documents or code.

| Rejected Term | Why Rejected | Use Instead |
|---------------|-------------|-------------|
| **Scanner** | Not used in codebase, ambiguous in industry | **Lexer** |
| **Tokenizer** | Verbose, not our convention | **Lexer** (the component) or **Tokenization** (the process) |
| **Lexical Analyzer** | Too academic/verbose for a class name | **Lexer** (for the component)<br>**Lexical Analysis** (acceptable for describing the process in documentation) |
| **Symbol** | Too vague, unclear distinction from Token | **Token** |
| **Character Stream** | Implementation detail, unnecessarily low-level | **Input** |
| **Lexeme** | Academic term for "token value", unnecessary distinction | **Token.Value** or just **Token** |
| **Literal Token** | Confusing - "Literal" is a Parser/semantic concept, not Lexer | **Identifier** (from Lexer) which becomes **Literal Segment** (in Parser) |
| **Operator** | Implies arithmetic/logical operations (confusing) | **Modifier** (`?` and `*` modify parameters/options) |

---

# Parser Subsystem

**Status:** Defined

The Parser converts token streams into structured representations (CompiledRoute) ready for runtime matching.

> **Reference**: [Route Pattern Anatomy](design/parser/route-pattern-anatomy.md) - Complete visual breakdown and terminology

---

### Route Pattern

**Definition:** The template string that defines what command-line input will match and how values are extracted.

**User-Facing Term:** Yes - used in documentation and error messages

**Code Artifact:** `string` (passed to `Map()`)

**Example:** `"docker run {image} --env,-e {var}* --verbose"`

**Components:**
- Positional Section (literals and parameters before options)
- Options Section (flags and value-bearing options)
- Optional End-of-Options separator (`--`)

**Related Terms:** Pattern, Template, Route Template

---

### Positional Section

**Definition:** The ordered sequence of literals and parameters that must appear before options.

**User-Facing Term:** Technical (used in docs)

**Example:** `docker run {image} {tag?}` (the `docker run {image} {tag?}` part)

**Characteristics:**
- Position matters (must match in order)
- Contains: Literal Segments, Parameter Segments, optional Catch-All

**Related Terms:** Positional Arguments, Positional Matchers

---

### Literal Segment

**Definition:** Fixed text in a route pattern that must match exactly.

**Code Artifact:** `LiteralMatcher`

**User-Facing Term:** "Literal" (in error messages)

**Example:** `docker` and `run` in `"docker run {image}"`

**Characteristics:**
- Case-sensitive exact match
- Increases route specificity (100 points each)
- Commonly used for command/subcommand names

**Anti-Pattern:** Don't confuse with Lexer's "Identifier" token

**Related Terms:** Command, Subcommand, Keyword

---

### Parameter Segment

**Definition:** A placeholder in a route pattern that captures a value from command-line input.

**Code Artifact:** `ParameterMatcher`

**User-Facing Term:** "Parameter" (in error messages and docs)

**Syntax:** `{name}`, `{name:type}`, `{name?}`, `{name:type?}`, `{*name}`

**Examples:**
- `{image}` - required untyped parameter
- `{port:int}` - required typed parameter
- `{tag?}` - optional parameter
- `{*args}` - catch-all parameter

**Components:**
- Parameter Name (required)
- Type Constraint (optional)
- Optionality Modifier `?` (optional)
- Catch-All Modifier `*` (optional)

**Related Terms:** Placeholder, Variable, Argument Capture

---

### Type Constraint

**Definition:** Optional type specification in a parameter for validation and conversion.

**Syntax:** `:` followed by type name (e.g., `:int`, `:string`)

**Supported Types:** `string`, `int`, `double`, `bool`, `DateTime`, `Guid`, `long`, `decimal`, `TimeSpan`

**Effect:**
- Validates input can convert to type
- Increases route specificity
- Provides type safety

**Example:** `{port:int}` only matches numeric input

**Related Terms:** Type Annotation, Type Hint

---

### Optionality Modifier

**Definition:** The `?` symbol that marks a parameter or option as optional (nullable).

**Syntax:** `?` suffix after parameter name or type

**Examples:**
- `{tag?}` - optional parameter
- `{port:int?}` - optional typed parameter
- `--config?` - optional option

**Effect:**
- Value can be omitted
- Handler parameter must be nullable
- Reduces route specificity

**Related Terms:** Optional Marker, Nullable Modifier

---

### Catch-All Modifier

**Definition:** The `*` symbol that marks a parameter to capture all remaining arguments.

**Syntax:** `*` prefix inside parameter braces: `{*args}`

**Effect:**
- Captures 0 or more remaining positional arguments
- Handler parameter must be `string[]`
- Must be last positional segment

**Use Cases:** Shell passthrough, variable-length argument lists

**Related Terms:** Variadic, Rest Parameter, Spread

---

### Option

**Definition:** A named argument prefixed with `--` (long form) or `-` (short form) that modifies command behavior.

**Code Artifact:** `OptionMatcher`

**User-Facing Term:** "Option" (primary), "Flag" (for boolean options)

**Types:**
- **Boolean Option** (Flag) - No value, just presence/absence
- **Value-Bearing Option** - Requires a parameter

**Examples:**
- `--verbose` - boolean option (flag)
- `--port {num:int}` - value-bearing option
- `--env,-e {var}*` - with alias and repetition

**Characteristics:**
- Order-independent (unlike positional section)
- Can have short-form aliases
- Can be required or optional
- Can be repeated (with `*` modifier)

**Related Terms:** Flag (for boolean), Switch, Named Argument

---

### Boolean Option (Flag)

**Definition:** An option that represents a binary choice without a value parameter.

**Code Artifact:** `OptionMatcher` where `ExpectsValue = false`

**User-Facing Term:** "Flag" (preferred), "Boolean Option" (formal)

**Syntax:** `--name` or `-x` (no parameter)

**Examples:** `--verbose`, `--force`, `--dry-run`, `-v`, `-f`

**Characteristics:**
- **Always optional** (presence = true, absence = false)
- Never takes a value
- Maps to `bool` parameter in handler

**Anti-Pattern:** "Required flag" - flags are always optional by definition

**Terminology Note:** Use "flag" colloquially, "boolean option" in formal contexts

**Related Terms:** Switch, Toggle, Boolean Flag

---

### Value-Bearing Option

**Definition:** An option that requires a parameter value.

**Code Artifact:** `OptionMatcher` where `ExpectsValue = true`

**Syntax:** `--name {param}` or `--name,-alias {param:type}`

**Examples:**
- `--port {num:int}` - typed parameter
- `--env,-e {var}*` - with alias and repetition
- `--config? {mode?}` - optional option with optional value

**Characteristics:**
- Must have a parameter
- Can be required or optional (based on `?` modifier or parameter nullability)
- Parameter follows all standard parameter rules

**Related Terms:** Parameterized Option, Option with Value

---

### Option Alias

**Definition:** Alternative name for an option, typically short form for long form.

**Syntax:** Comma-separated in pattern: `--long,-s`

**Examples:**
- `--verbose,-v` (long: `--verbose`, short: `-v`)
- `--env,-e` (both forms bind to same parameter)

**Effect:**
- Both forms match the same option
- Users can use either interchangeably

**Code Artifact:** `OptionMatcher.AlternateForm`

**Related Terms:** Short Form, Long Form, Synonym

---

### Repetition Modifier

**Definition:** The `*` symbol on option parameters that allows collecting multiple values.

**Syntax:** `*` after parameter closing brace: `{var}*`

**Example:** `--env {var}*` allows `--env A --env B --env C`

**Effect:**
- Option can appear multiple times
- Handler parameter must be array type (`string[]`, `int[]`)
- Option becomes implicitly optional (0 or more values)

**Code Artifact:** `OptionMatcher.IsRepeated`

**Related Terms:** Multi-Valued, Repeated, Array Option

---

### CompiledRoute

**Definition:** The parsed and validated representation of a route pattern, ready for runtime matching.

**Code Artifact:** `CompiledRoute` class

**Components:**
- `PositionalMatchers` - List of `RouteMatcher` (literals and parameters)
- `OptionMatchers` - List of `OptionMatcher`
- `CatchAllParameterName` - Name of catch-all if present
- `Specificity` - Integer score for route ordering

**Purpose:** Efficient matching against command-line arguments

**Related Terms:** Compiled Pattern, Route AST

---

### RouteMatcher

**Definition:** Base class for all positional route components (literals, parameters).

**Code Artifact:** `RouteMatcher` abstract class

**Subclasses:**
- `LiteralMatcher` - Matches exact text
- `ParameterMatcher` - Captures values

**Method:** `TryMatch(string arg, out string? extractedValue)`

**Related Terms:** Matcher, Segment Matcher

---

### OptionMatcher

**Definition:** Represents an option in a compiled route.

**Code Artifact:** `OptionMatcher` class (extends `RouteMatcher`)

**Key Properties:**
- `MatchPattern` - Primary option name (e.g., `--verbose`)
- `AlternateForm` - Alias if present (e.g., `-v`)
- `ExpectsValue` - Whether option takes a parameter
- `ParameterName` - Name of parameter if value-bearing
- `IsOptional` - Whether option can be omitted
- `IsRepeated` - Whether option can appear multiple times

**Related Terms:** Option Descriptor, Option Definition

---

### End-of-Options Separator

**Definition:** POSIX standard `--` (standalone double dash) that stops option parsing.

**Syntax:** `--` (must be standalone, not part of option name)

**Effect:**
- All subsequent arguments treated as literals
- Prevents `--verbose` after separator from being parsed as option

**Example Usage:**
```
docker run -- --not-an-option file.txt
```

**Code Artifact:** `TokenType.EndOfOptions`

**Related Terms:** POSIX Separator, Argument Separator

---

# Compiler Subsystem

**Status:** To Be Defined

The Compiler converts the AST into runtime matchers used for route matching.

**Placeholder concepts:**
- Compiler, Compilation, Matcher, Compiled Route

---

# Resolver Subsystem

**Status:** To Be Defined

The Resolver matches command-line arguments against compiled routes using specificity scoring.

**Placeholder concepts:**
- Resolver, Route Matching, Specificity, Specificity Score, Match Result

---

# Executor Subsystem

**Status:** To Be Defined

The Executor runs the matched route's handler with bound parameters.

**Placeholder concepts:**
- Executor, Execution, Handler, Parameter Binding, Result

---

# Cross-Cutting Concerns

**Status:** To Be Defined

Terms that apply across multiple subsystems.

**Placeholder concepts:**
- Error Handling, Logging, Validation, Type Conversion

---

## Document Evolution

This is a **living document**. When adding new terms:

1. Add to appropriate subsystem section
2. Include: Definition, Code Artifact, Examples, Related Terms
3. Update "Rejected Terms" if applicable
4. Notify team of changes
5. Update dependent design documents to use new terms

When changing existing terms:
1. Update all occurrences in this document
2. Update design documents
3. Update code (may require refactoring)
4. Update tests
5. Update API documentation

---

**Last Updated:** 2025-10-02
**Next Review:** When defining Parser UL (Phase 2)
