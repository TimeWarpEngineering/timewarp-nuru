# Tokenization Algorithm

How the Lexer converts route pattern strings into tokens.

## Algorithm Design

### Single-Pass Scanning

The Lexer uses a **character-by-character, left-to-right scanning algorithm**:

1. **Single Pass**: Input processed exactly once, no backtracking
2. **Greedy Matching**: Consumes maximum characters per token
3. **Lookahead**: Peeks ahead to distinguish multi-char tokens
4. **Fault Tolerant**: Invalid sequences become Invalid tokens (no exceptions)
5. **Position Tracking**: Every token records start position and length

**Implementation:** `RoutePatternLexer` in `Source/TimeWarp.Nuru.Parsing/Parsing/Lexer/RoutePatternLexer.cs`

## Core Operations

### Advance
Consumes current character and moves position forward. Used when we've decided to accept the character.

### Peek
Non-destructive read of current character. Returns `\0` if at end. Used for lookahead decisions without commitment.

### Match
Conditional consume: checks if current character matches expected, advances only if true. Combines check + consume in one operation.

### IsAtEnd
Boundary check that prevents reading past input length. Guards all peek and advance operations.

## Main Algorithm Flow

```
1. Initialize (clear tokens, position = 0)
2. WHILE not at end:
   a. Advance to get current character
   b. Dispatch based on character:
      - Whitespace → skip (no token)
      - Single-char delimiter → emit token immediately
      - Dash → check for single/double/end-of-options
      - Alphanumeric → scan identifier
      - Angle bracket → scan invalid parameter syntax
      - Unknown → emit Invalid token
3. Append EndOfInput token
4. Return token list
```

## Character Dispatch Strategy

**Immediate Emission** (single-character tokens):
- Delimiters: `{` `}` `:` `|` `,`
- Modifiers: `?` `*`

**Whitespace Handling**:
- Spaces, tabs, newlines consumed but not emitted
- Acts as token separator only

**Complex Patterns** (require lookahead):
- **Dash**: Could be `-` (SingleDash), `--` (DoubleDash), or `-- ` (EndOfOptions)
- **Identifier**: Alphanumeric with internal dashes allowed
- **Angle Brackets**: Error case for `<param>` instead of `{param}`

## Dash Token Decision Tree

Dashes require lookahead to distinguish three cases:

```
Input starts with '-':
├─ Next char is '-'?
│  ├─ YES: Followed by space/end?
│  │  ├─ YES → EndOfOptions ("--" standalone)
│  │  └─ NO → DoubleDash ("--" prefix)
│  └─ NO → SingleDash ("-" prefix)
```

**Examples:**
- `-h` → SingleDash + Identifier
- `--verbose` → DoubleDash + Identifier
- `exec -- {*args}` → ... + EndOfOptions + ...

## Identifier Scanning Rules

**Valid Patterns:**
- Alphanumeric characters (letters, digits, underscore)
- Internal single dashes: `dry-run`, `no-edit`, `my-long-name`

**Invalid Patterns** (emit Invalid token):
- Trailing dashes: `test-`, `foo--`
- Double dashes within: `test--case`
- Dash before non-alphanumeric: `test-}`

**Algorithm:**
1. Consume first alphanumeric (already advanced)
2. WHILE not at end:
   - If alphanumeric → consume and continue
   - If dash:
     - Peek ahead to check what follows
     - If end/double-dash/non-alphanum → mark Invalid
     - If alphanumeric → consume dash and continue
   - Else → end of identifier
3. Emit Identifier or Invalid token

## Error Handling Strategy

### No Exceptions for Syntax Errors

**Design Decision**: Lexer produces Invalid tokens instead of throwing exceptions.

**Rationale:**
- **Best-effort tokenization**: Process as much as possible
- **Better error reporting**: Parser sees full context
- **IDE-friendly**: Can provide suggestions, continue analysis
- **Multiple errors**: Report all issues, not just first

**Invalid Token Cases:**
1. Unknown characters: `@`, `#`, etc.
2. Malformed identifiers: `test-`, `foo--bar`
3. Wrong delimiters: `<param>` instead of `{param}`

### Invalid Parameter Syntax Detection

Special case for common mistake: angle brackets instead of curly braces.

**Algorithm:**
1. Detect `<` character
2. Scan until matching `>` or end of input
3. Capture entire sequence as single Invalid token
4. Continue scanning (don't stop on error)

**Example:** `deploy <env>` → `[Identifier("deploy")]` `[Invalid("<env>")]`

## Position Tracking

Each token records:
- **Position**: Zero-based start index in input
- **Length**: Number of characters in token

**Purpose:**
- Error messages show exact location
- IDE can highlight specific tokens
- Debugging shows tokenization visually

**Position Calculation:**
- Single-char tokens: `position - value.Length`
- Multi-char tokens: Explicit start position tracked during scan

## Whitespace Handling

**Design**: Whitespace consumed but not emitted as tokens.

**Rationale:**
- Simplifies parser (no whitespace tokens to skip)
- Whitespace serves only as token separator
- Position tracking maintains accuracy

**Whitespace Characters:**
- Space (` `)
- Tab (`\t`)
- Carriage return (`\r`)
- Newline (`\n`)

## Algorithm Properties

### Time Complexity
**O(n)** where n = input length
- Single pass through input
- Each character examined constant times
- No backtracking or nested loops over input

### Space Complexity
**O(t)** where t = number of tokens
- Token list grows with token count
- Temporary StringBuilder for multi-char tokens
- No recursive calls (iterative algorithm)

### Deterministic
- Same input → same tokens (always)
- No randomness or external state
- Reproducible for testing

### Greedy
- Consumes maximum characters per token
- No backtracking if greedy choice fails
- Example: `dry-run-test` → one identifier (not split)

## Design Rationale

### Why Character-by-Character vs Regex?

**Character-by-Character** (chosen):
- Simple mental model (linear flow)
- Easy to debug (step through character by character)
- Precise error positions
- Full control over lookahead and error handling

**Regex** (rejected):
- More concise but harder to debug
- Error positions less precise
- Harder to customize error recovery
- Context-dependent tokens difficult (e.g., `--` vs `-- `)

### Why No Exceptions for Errors?

**No Exceptions** (chosen):
- Fault tolerance (process entire input)
- Better UX (multiple errors shown at once)
- Parser has full context for error messages

**Throw Exceptions** (rejected):
- Fail-fast is less useful for syntax errors
- User only sees first error
- Harder to build IDE integrations

### Why Lookahead Instead of Buffering?

**Lookahead** (chosen):
- Flexible (peek 1-2 chars as needed)
- Simple state (just position pointer)
- Context-dependent decisions easy

**Buffering** (rejected):
- More complex state management
- Overkill for 1-2 char lookahead
- Harder to reason about

## Related Concepts

**From [token-types.md](token-types.md):**
- All TokenType definitions
- Token validation rules
- Invalid token categories

**From [ubiquitous-language.md](../ubiquitous-language.md):**
- Lexer, Token, Tokenization definitions
- Operations: Scan, Advance, Peek, Match
- Position, Input, End of Input concepts
