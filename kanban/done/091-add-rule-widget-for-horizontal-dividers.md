# Add Rule Widget for Horizontal Divider Lines

## Description

Implement a `Rule` widget for rendering horizontal divider lines in the terminal, optionally with centered text. This eliminates the need for Spectre.Console when CLI applications need visual separation between sections.

**GitHub Issue**: [#89](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/89)

## Requirements

- Simple horizontal line: `terminal.WriteRule()`
- Centered title: `terminal.WriteRule("Section Title")`
- Styled title support: `terminal.WriteRule("Results".Cyan().Bold())`
- Fluent builder API for advanced configuration
- Support multiple line styles: Single (`─`), Double (`═`), Heavy (`━`)
- Respect terminal width automatically
- Graceful degradation when colors not supported

## Checklist

### Design
- [x] Create `LineStyle` enum (Thin, Doubled, Heavy)
- [x] Design `Rule` class with properties (Title, Style, Color, Width)
- [x] Design `RuleBuilder` for fluent configuration
- [x] Plan extension methods for `ITerminal`

### Implementation
- [x] Create `line-style.cs` with LineStyle enum
- [x] Create `rule-widget.cs` with Rule and RuleBuilder classes
- [x] Create `terminal-rule-extensions.cs` with ITerminal extensions
- [x] Implement ANSI code stripping for visible length calculation
- [x] Handle terminal width detection via `ITerminal.WindowWidth`
- [x] Handle color support detection via `ITerminal.SupportsColor`

### Testing
- [x] Test simple rule without title
- [x] Test rule with plain title (centered)
- [x] Test rule with styled title
- [x] Test rule with different LineStyles
- [x] Test rule with custom width
- [x] Test graceful degradation when SupportsColor is false
- [x] Test with various terminal widths

### Documentation
- [x] Add XML documentation to all public APIs
- [x] Create sample demonstrating Rule widget usage

## Notes

### Proposed API

```csharp
// Simple horizontal line
terminal.WriteRule();

// With centered title
terminal.WriteRule("Section Title");

// With styling (uses existing AnsiColorExtensions)
terminal.WriteRule("Results".Cyan().Bold());

// Fluent builder
terminal.WriteRule(rule => rule
    .Title("Configuration")
    .Style(LineStyle.Double)
    .Color(AnsiColors.Cyan));
```

### Expected Output

```
────────────────────────────────────────
─────────── Section Title ──────────────
═══════════ Configuration ══════════════
```

### Implementation Location

New files in `source/timewarp-nuru-core/io/`:
- `line-style.cs`
- `rule-widget.cs`
- `terminal-rule-extensions.cs`

### Key Dependencies (Already Available)

- `ITerminal.WindowWidth` - For calculating rule width
- `ITerminal.SupportsColor` - For graceful degradation
- `AnsiColors` class - All color constants
- `AnsiColorExtensions` - Fluent string coloring
- `TestTerminal` - For unit testing with settable WindowWidth

### Box-Drawing Characters

| Style | Character | Unicode |
|-------|-----------|---------|
| Single | `─` | U+2500 |
| Double | `═` | U+2550 |
| Heavy | `━` | U+2501 |

### Centered Title Algorithm

```csharp
int totalWidth = terminal.WindowWidth;
int titleLength = GetVisibleLength(title);  // Strip ANSI codes
int leftPadding = (totalWidth - titleLength - 2) / 2;  // -2 for spaces
int rightPadding = totalWidth - leftPadding - titleLength - 2;
// Output: [left chars] [space] [title] [space] [right chars]
```

## Implementation Notes

### Files Created

**Source files** in `source/timewarp-nuru-core/io/widgets/`:
- `line-style.cs` - `LineStyle` enum (Thin, Doubled, Heavy) and `LineChars` helper
- `ansi-string-utils.cs` - `AnsiStringUtils` class for ANSI-aware string operations
- `rule-widget.cs` - `Rule` class and `RuleBuilder` for fluent configuration
- `terminal-rule-extensions.cs` - Extension methods for `ITerminal`

**Test files** in `tests/timewarp-nuru-core-tests/`:
- `rule-widget-01-basic.cs` - 8 tests for Rule class
- `rule-widget-02-terminal-extensions.cs` - 9 tests for terminal extensions
- `ansi-string-utils-01-basic.cs` - 12 tests for AnsiStringUtils

**Sample** in `samples/rule-widget-demo/`:
- `rule-widget-demo.cs` - Comprehensive demo of all Rule widget features

### API Changes

Due to CA1720 analyzer rule (identifiers cannot contain type names), enum values were renamed:
- `LineStyle.Single` → `LineStyle.Thin`
- `LineStyle.Double` → `LineStyle.Doubled`

### Test Results

All 29 tests pass:
- Rule widget basic: 8/8 passed
- Terminal extensions: 9/9 passed
- AnsiStringUtils: 12/12 passed
