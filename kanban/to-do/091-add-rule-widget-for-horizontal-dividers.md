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
- [ ] Create `LineStyle` enum (Single, Double, Heavy)
- [ ] Design `Rule` class with properties (Title, Style, Color, Width)
- [ ] Design `RuleBuilder` for fluent configuration
- [ ] Plan extension methods for `ITerminal`

### Implementation
- [ ] Create `line-style.cs` with LineStyle enum
- [ ] Create `rule-widget.cs` with Rule and RuleBuilder classes
- [ ] Create `terminal-rule-extensions.cs` with ITerminal extensions
- [ ] Implement ANSI code stripping for visible length calculation
- [ ] Handle terminal width detection via `ITerminal.WindowWidth`
- [ ] Handle color support detection via `ITerminal.SupportsColor`

### Testing
- [ ] Test simple rule without title
- [ ] Test rule with plain title (centered)
- [ ] Test rule with styled title
- [ ] Test rule with different LineStyles
- [ ] Test rule with custom width
- [ ] Test graceful degradation when SupportsColor is false
- [ ] Test with various terminal widths

### Documentation
- [ ] Add XML documentation to all public APIs
- [ ] Create sample demonstrating Rule widget usage

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
