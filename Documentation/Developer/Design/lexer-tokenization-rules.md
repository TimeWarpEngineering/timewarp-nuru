# Lexer Tokenization Rules

## Purpose

The lexer is the first line of defense against malformed route patterns. It should identify and reject nonsensical character sequences that could never form valid patterns, rather than passing them to the parser to figure out.

## Token Types

The lexer produces the following tokens:

| Token | Symbol/Example | Valid Usage |
|-------|---------------|-------------|
| `Literal` | Plain text | Commands and identifiers |
| `Identifier` | `name`, `dry-run` | Parameter names, option names, types |
| `LeftBrace` | `{` | Opens parameter |
| `RightBrace` | `}` | Closes parameter |
| `Colon` | `:` | Type constraint separator |
| `Question` | `?` | Optional modifier |
| `Asterisk` | `*` | Catch-all/repeated modifier |
| `Pipe` | `|` | Description separator |
| `Comma` | `,` | Option alias separator |
| `DoubleDash` | `--` | Long option prefix |
| `SingleDash` | `-` | Short option prefix |
| `EndOfOptions` | `--` (standalone) | POSIX separator |
| `Description` | Text after `|` | Help text |
| `EndOfInput` | EOF | End marker |
| `Invalid` | Malformed | Error token |

## Valid Compound Identifiers

Identifiers can contain single dashes (`-`) within them:
- ✅ `dry-run`
- ✅ `no-edit`
- ✅ `save-dev`
- ✅ `my-long-command-name`

These are tokenized as single `Identifier` tokens.

## Option Tokenization

### Valid Options

Long options with compound names:
- ✅ `--dry-run` → `[DoubleDash] --`, `[Identifier] dry-run`
- ✅ `--no-edit` → `[DoubleDash] --`, `[Identifier] no-edit`
- ✅ `--save-dev` → `[DoubleDash] --`, `[Identifier] save-dev`

Short options:
- ✅ `-h` → `[SingleDash] -`, `[Identifier] h`
- ✅ `-v` → `[SingleDash] -`, `[Identifier] v`

## Invalid Patterns That Should Produce Invalid Tokens

### Double Dashes Within Text (No Spaces)

These are malformed and should produce `Invalid` tokens:
- ❌ `test--case` - Not a valid identifier, not a valid command+option
- ❌ `foo--bar--baz` - Multiple double dashes without spaces
- ❌ `my--option` - Double dash must be at start for options

**Rule**: Double dashes (`--`) are only valid:
1. At the start of a token (for options)
2. As a standalone token (for end-of-options)
3. Never in the middle of an identifier

### Trailing Dashes

These are incomplete/malformed:
- ❌ `test-` - Trailing single dash
- ❌ `test--` - Trailing double dash
- ❌ `foo---` - Multiple trailing dashes

**Rule**: Dashes at the end of a token (not followed by alphanumeric) should produce `Invalid` tokens.

### Leading Single Dash with Multi-Character

While `-h` is valid (short option), these are ambiguous:
- ⚠️ `-test` - Currently tokenized as `[SingleDash] -`, `[Identifier] test`
  - Could mean `-t est` or is it a typo for `--test`?
  - Consider making this `Invalid` if identifier after single dash is multi-character

## Special Cases

### End-of-Options Separator

The standalone `--` is the POSIX end-of-options marker:
- ✅ `git log --` → `[Identifier] git`, `[Identifier] log`, `[EndOfOptions] --`
- ✅ `exec -- {*args}` → `[Identifier] exec`, `[EndOfOptions] --`, ...

**Rule**: `--` followed by space or end-of-input becomes `EndOfOptions`, not `DoubleDash`.

### Angle Brackets (Already Invalid)

Using angle brackets instead of curly braces produces `Invalid`:
- ❌ `test<param>` → `[Identifier] test`, `[Invalid] <param>`
- This helps catch common mistakes early

## Tokenization Decision Tree

```
Character sequence starting with '--':
├── Followed by space/EOF? → EndOfOptions
├── Followed by alphanumeric? → DoubleDash + Identifier
└── In middle of text? → Invalid

Character sequence starting with '-':
├── Followed by single char then space/EOF? → SingleDash + Identifier
├── Followed by multi-char? → Consider Invalid (ambiguous)
└── At end of identifier? → Invalid

Identifier with dashes:
├── Single dash within (a-z)? → Valid compound Identifier
├── Double dash within? → Invalid
└── Trailing dash? → Invalid
```

## Benefits of Strict Lexing

1. **Early Error Detection**: Catch malformed patterns before parsing
2. **Clear Error Messages**: Can pinpoint exact character position of problem
3. **Simpler Parser**: Parser doesn't need to handle these edge cases
4. **Better IDE Support**: Analyzers can highlight invalid tokens immediately

## Implementation Notes

The lexer should be updated to:

1. Detect double dashes within identifiers and produce `Invalid` tokens
2. Detect trailing dashes and produce `Invalid` tokens
3. Consider making `-multichar` produce `Invalid` (or at least a warning)
4. Provide clear error messages indicating why a token is invalid

## Examples

| Input | Current Tokenization | Proposed Tokenization |
|-------|---------------------|----------------------|
| `test--case` | `[Id] test`, `[DD] --`, `[Id] case` | `[Invalid] test--case` |
| `test-` | `[Id] test`, `[SD] -` | `[Invalid] test-` |
| `foo--` | `[Id] foo`, `[DD] --` | `[Invalid] foo--` |
| `my--name` | `[Id] my`, `[DD] --`, `[Id] name` | `[Invalid] my--name` |
| `-test` | `[SD] -`, `[Id] test` | `[Invalid] -test` or warning |
| `--dry-run` | `[DD] --`, `[Id] dry-run` | No change (valid) |
| `dry-run` | `[Id] dry-run` | No change (valid) |