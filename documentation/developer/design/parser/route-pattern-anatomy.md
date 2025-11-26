# Route Pattern Anatomy

**Purpose:** Define consistent terminology for all route pattern syntax elements with a comprehensive reference example.

## The Kitchen Sink Example

Here's a route pattern using every possible syntax element:

```csharp
builder.Map(
  "docker run {image} {tag:string?} --env,-e {var}* --port {num:int} --detach? --verbose -- {*args}",
  (string image, string? tag, string[] var, int num, bool detach, bool verbose, string[] args) => ...
);
```

Let's break down every component:

---

## Pattern Components Hierarchy

```
Route Pattern (entire string)
├── Positional Section (before options)
│   ├── Literal Segments
│   ├── Parameter Segments
│   └── Catch-All Segment (if present, always last)
├── Options Section (flags and their parameters)
│   ├── Boolean Options (flags)
│   └── Value-Bearing Options
└── End-of-Options Separator (optional)
    └── Arguments After Separator
```

---

## Complete Terminology Reference

### 1. Route Pattern

**Definition:** The entire template string that defines what command-line input will match.

**Example:** `"docker run {image} {tag:string?} --env,-e {var}* --port {num:int} --detach? --verbose -- {*args}"`

**Components:** Positional section + Options section + (optional) End-of-Options separator

---

### 2. Positional Section

**Definition:** The ordered sequence of literals and parameters before any options.

**Example from pattern:** `docker run {image} {tag:string?}`

**Components:**
- `docker` - Literal Segment
- `run` - Literal Segment
- `{image}` - Required Parameter Segment
- `{tag:string?}` - Optional Typed Parameter Segment

**Rules:**
- Must appear in order (position matters)
- Required segments must come before optional segments
- Catch-all (if present) must be last

---

### 3. Literal Segment

**Definition:** Fixed text that must match exactly.

**Syntax:** Plain text without braces or dashes

**Examples from pattern:**
- `docker` - command name
- `run` - subcommand

**Matching:** Case-sensitive, must match exactly

**Code Representation:** `LiteralMatcher`

---

### 4. Parameter Segment

**Definition:** A placeholder that captures a value from the command-line arguments.

**Syntax:** `{name}` or `{name:type}` or `{name?}` or `{name:type?}` or `{*name}`

**Examples from pattern:**
- `{image}` - Required untyped parameter
- `{tag:string?}` - Optional typed parameter
- `{*args}` - Catch-all parameter

**Components:**
- **Parameter Name** - identifier inside braces (`image`, `tag`, `args`)
- **Type Constraint** (optional) - type after colon (`:string`, `:int`)
- **Optionality Modifier** (optional) - question mark (`?`)
- **Catch-All Modifier** (optional) - asterisk (`*`)

**Code Representation:** `ParameterMatcher`

#### 4.1 Parameter Name

**Definition:** The identifier that will bind to the handler parameter.

**Syntax:** Alphanumeric + underscore, must start with letter

**Examples:** `image`, `tag`, `var`, `num`, `args`

**Matching:** Must correspond to handler parameter name (case-sensitive)

#### 4.2 Type Constraint

**Definition:** Optional type specification for compile-time validation.

**Syntax:** `:` followed by type name

**Supported Types:** `string`, `int`, `double`, `bool`, `DateTime`, `Guid`, `long`, `decimal`, `TimeSpan`

**Examples from pattern:**
- `:string` in `{tag:string?}`
- `:int` in `{num:int}`

**Effect:**
- Validates input can convert to specified type
- Increases route specificity (typed routes match before untyped)

#### 4.3 Optionality Modifier (`?`)

**Definition:** Marks a parameter as optional (nullable).

**Syntax:** `?` suffix after parameter name or type

**Examples from pattern:**
- `{tag:string?}` - optional typed parameter

**Effect:**
- Parameter can be omitted from input
- Handler parameter must be nullable type
- Reduces route specificity (optional routes match after required)

**Rules:**
- Optional parameters must come after required parameters
- Cannot use `?` on catch-all parameters (they're implicitly optional)

#### 4.4 Catch-All Modifier (`*`)

**Definition:** Captures all remaining positional arguments into an array.

**Syntax:** `*` prefix inside braces: `{*name}`

**Example from pattern:** `{*args}`

**Effect:**
- Collects 0 or more arguments
- Handler parameter must be `string[]` type
- Must be the last positional segment

**Use Cases:**
- Passthrough to shell commands
- Variable-length argument lists
- Unknown remaining arguments

---

### 5. Options Section

**Definition:** Named arguments prefixed with `--` or `-` that can appear in any order.

**Example from pattern:** `--env,-e {var}* --port {num:int} --detach? --verbose`

**Components:**
- `--env,-e {var}*` - Repeated value-bearing option with alias
- `--port {num:int}` - Required value-bearing option with typed parameter
- `--detach?` - Optional boolean option
- `--verbose` - Boolean option (always optional)

**Characteristics:**
- Order-independent (unlike positional section)
- Can be required or optional
- Can have short-form aliases
- Can be repeated to collect multiple values

---

### 6. Option

**Definition:** A named flag or switch that modifies command behavior.

**Syntax:** `--long-name`, `-s`, `--name,-alias`

**Types:**
- **Boolean Option** - No parameter, just presence/absence
- **Value-Bearing Option** - Takes a parameter value

**Examples from pattern:**
- `--env,-e {var}*` - Value-bearing with alias and repetition
- `--port {num:int}` - Value-bearing with typed parameter
- `--detach?` - Optional boolean
- `--verbose` - Boolean (implicitly optional)

**Code Representation:** `OptionMatcher`

#### 6.1 Boolean Option (Flag)

**Definition:** An option that represents a boolean choice (on/off, true/false).

**Syntax:** `--name` or `-x` (no parameter)

**Examples from pattern:**
- `--detach?` - optional boolean
- `--verbose` - boolean (always optional)

**Characteristics:**
- Never takes a value
- **Always optional** (presence = true, absence = false)
- Maps to `bool` parameter in handler

**Anti-Pattern:** "Required flag" - flags are always optional by definition

#### 6.2 Value-Bearing Option

**Definition:** An option that requires a parameter value.

**Syntax:** `--name {param}` or `--name,-alias {param}`

**Examples from pattern:**
- `--env,-e {var}*` - with alias and repetition
- `--port {num:int}` - with type constraint

**Characteristics:**
- Must have a parameter
- Can be required or optional (based on parameter nullability or `?` modifier)
- Parameter follows all standard parameter rules (type, optionality, etc.)

#### 6.3 Option Name

**Definition:** The identifier for the option (long form and/or short form).

**Syntax:**
- **Long Form:** `--` + multi-character name (`--verbose`, `--env`)
- **Short Form:** `-` + single character (`-v`, `-e`)

**Rules:**
- Long form: 2+ characters, lowercase, hyphens allowed
- Short form: exactly 1 character
- Both forms case-sensitive

#### 6.4 Option Alias

**Definition:** Alternative name for an option (typically short form for long form).

**Syntax:** Comma-separated in pattern: `--long,-s`

**Examples from pattern:**
- `--env,-e` - long form `--env`, short form `-e`

**Effect:**
- Both forms match the same option
- Users can use either form
- Both bind to same parameter

##### 6.4.1 Option Alias with Optionality Modifier

**Definition:** When combining aliases with the optionality modifier (`?`), the `?` is placed **after the alias group** and applies to **both forms**.

**Canonical Syntax:** `--long,-short? {param}`

**Examples:**

**Optional Boolean Flag with Alias:**
```csharp
// Pattern
builder.Map("build --verbose,-v?", (bool verbose) => ...);

// All valid invocations:
// build              → verbose = false
// build --verbose    → verbose = true  (long form)
// build -v           → verbose = true  (short form)
```

**Optional Flag with Required Value:**
```csharp
// Pattern
builder.Map("backup {source} --output,-o? {file}",
    (string source, string? file) => ...);

// Valid invocations:
// backup /data                    → file = null (flag omitted)
// backup /data --output result.tar → file = "result.tar" (long form)
// backup /data -o result.tar       → file = "result.tar" (short form)

// Invalid - flag present requires value:
// backup /data --output            → Error
// backup /data -o                  → Error
```

**Optional Flag with Optional Value:**
```csharp
// Pattern
builder.Map("build --config,-c? {mode?}",
    (string? mode) => ...);

// All valid invocations:
// build                → mode = null (flag omitted)
// build --config       → mode = null (flag present, value omitted)
// build --config debug → mode = "debug" (both present)
// build -c             → mode = null (short form, value omitted)
// build -c release     → mode = "release" (short form with value)
```

**Placement Rule:**
- ✅ `--output,-o? {file}` - Correct: `?` after alias applies to both forms
- ❌ `--output?,-o {file}` - Incorrect: ambiguous placement
- ❌ `--output?,-o? {file}` - Incorrect: redundant `?` modifiers

**See Also:** [Optional Flag Alias Syntax](optional-flag-alias-syntax.md) for complete design rationale

#### 6.5 Option Modifiers

**Definition:** Symbols that modify option behavior.

**Types:**
- **Optionality Modifier (`?`)** - Makes option itself optional
- **Repetition Modifier (`*`)** - Allows option to appear multiple times

##### 6.5.1 Option Optionality Modifier (`?`)

**Definition:** Makes the option itself optional (can be omitted entirely).

**Syntax:** `?` after option name, before parameter

**Examples from pattern:**
- `--detach?` - optional boolean option

**Effect:**
- Route matches even if option is absent
- For value-bearing options: `--port? {num}` means port can be omitted OR provided with value

**Note:** Boolean options are always optional, `?` is redundant but allowed

##### 6.5.2 Option Repetition Modifier (`*`)

**Definition:** Allows option to appear multiple times, collecting values into an array.

**Syntax:** `*` after parameter closing brace

**Example from pattern:**
- `--env,-e {var}*` - can use `--env A --env B -e C`

**Effect:**
- Handler parameter must be array type (`string[]`, `int[]`, etc.)
- Collects all values from repeated occurrences
- Option becomes implicitly optional (0 or more values)

---

### 7. End-of-Options Separator

**Definition:** POSIX standard `--` that stops option parsing.

**Syntax:** `--` (double dash, standalone)

**Example from pattern:** `-- {*args}` (everything after `--` goes to catch-all)

**Effect:**
- All subsequent arguments treated as literals, not options
- `--verbose` after separator is literal string, not an option
- Useful for passing options to nested commands

**Example Usage:**
```bash
docker run -- --not-an-option file.txt
# --not-an-option is captured as literal in {*args}
```

---

## Visual Pattern Breakdown

```
docker run {image} {tag:string?} --env,-e {var}* --port {num:int} --detach? --verbose -- {*args}
└─┬──┘ └─┬ └──┬──┘ └────┬─────┘ └──────┬──────┘ └────┬────┘ └────┬───┘ └──┬─┘ └──┬──┘ └─┬┘ └──┬───┘
  │      │    │         │               │              │           │        │      │     │    │
  │      │    │         │               │              │           │        │      │     │    └─ Catch-all parameter
  │      │    │         │               │              │           │        │      │     └───── End-of-options separator
  │      │    │         │               │              │           │        │      └─────────── Boolean option (always optional)
  │      │    │         │               │              │           │        └────────────────── Optional boolean option
  │      │    │         │               │              │           └─────────────────────────── Required value-bearing option
  │      │    │         │               │              └─────────────────────────────────────── Typed parameter (int)
  │      │    │         │               └────────────────────────────────────────────────────── Repeated value-bearing option
  │      │    │         └────────────────────────────────────────────────────────────────────── Repetition modifier
  │      │    └──────────────────────────────────────────────────────────────────────────────── Option alias (short form)
  │      └───────────────────────────────────────────────────────────────────────────────────── Optional typed parameter
  └──────────────────────────────────────────────────────────────────────────────────────────── Literal segments (subcommand)
```

---

## Terminology Summary Table

| Term | Syntax Example | Category | Code Class | Description |
|------|----------------|----------|------------|-------------|
| **Route Pattern** | `"docker run {image}"` | Top-level | String | Entire template string |
| **Positional Section** | `docker run {image}` | Container | - | Ordered literals and parameters |
| **Options Section** | `--env {var} --verbose` | Container | - | Named options and flags |
| **Literal Segment** | `docker`, `run` | Positional | `LiteralMatcher` | Fixed text to match exactly |
| **Parameter Segment** | `{image}`, `{tag?}` | Positional | `ParameterMatcher` | Value capture placeholder |
| **Parameter Name** | `image`, `tag` | Component | String | Identifier for binding |
| **Type Constraint** | `:int`, `:string` | Modifier | String | Type validation |
| **Optionality Modifier** | `?` | Modifier | Boolean flag | Makes parameter optional |
| **Catch-All Modifier** | `*` | Modifier | Boolean flag | Captures remaining args |
| **Catch-All Parameter** | `{*args}` | Positional | `ParameterMatcher` | Collects all remaining positional args |
| **Option** | `--verbose`, `--env {var}` | Options | `OptionMatcher` | Named argument |
| **Boolean Option** | `--verbose`, `--detach` | Option type | `OptionMatcher` | Flag without value |
| **Value-Bearing Option** | `--env {var}` | Option type | `OptionMatcher` | Option with parameter |
| **Option Name** | `--env`, `-e` | Component | String | Option identifier |
| **Option Alias** | `--env,-e` | Component | String | Alternative form |
| **Option Optionality Modifier** | `--port?` | Modifier | Boolean flag | Makes option itself optional |
| **Option Repetition Modifier** | `{var}*` | Modifier | Boolean flag | Allows multiple occurrences |
| **End-of-Options Separator** | `--` | Special | Token | Stops option parsing |

---

## Formal Grammar (EBNF-style)

```
RoutePattern     = PositionalSection [ OptionsSection ] [ EndOfOptions [ CatchAll ] ]
PositionalSection = Segment { Segment } [ CatchAll ]
OptionsSection   = { Option }
Segment          = Literal | Parameter
Literal          = Identifier
Parameter        = "{" [ "*" ] Name [ ":" Type ] [ "?" ] "}"
Option           = OptionName [ "," OptionAlias ] [ "?" ] [ "{" Name [ ":" Type ] [ "?" ] "}" [ "*" ] ]
OptionName       = "--" MultiCharIdent | "-" SingleChar
Name             = Identifier
Type             = "string" | "int" | "double" | "bool" | "DateTime" | "Guid" | "long" | "decimal" | "TimeSpan"
CatchAll         = "{" "*" Name "}"
EndOfOptions     = "--"
```

**Note on Option Grammar:** The `[ "?" ]` modifier appears **after** the optional alias (`[ "," OptionAlias ]`) and **before** the optional parameter. This means:
- `--flag?` - Optional boolean flag (no parameter)
- `--flag,-f?` - Optional flag with alias (? applies to both forms)
- `--flag? {value}` - Optional flag with required value
- `--flag,-f? {value?}` - Optional flag with alias and optional value

---

## Related Documents

- [Syntax Rules](syntax-rules.md) - Validation rules and error codes
- [Parameter Optionality](../cross-cutting/parameter-optionality.md) - Required vs optional semantics
- [Ubiquitous Language](../../ubiquitous-language.md) - Domain terminology
