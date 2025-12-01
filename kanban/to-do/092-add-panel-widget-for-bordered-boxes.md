# Add Panel Widget for Bordered Boxes

## Description

Implement a `Panel` widget for rendering bordered boxes with optional headers and styled content. This eliminates the need for Spectre.Console when CLI applications need to highlight important information in visually distinct boxes.

**GitHub Issue**: [#90](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/90)

**Analysis**: `.agent/workspace/2025-12-01T17-00-00_issue-90-panel-widget-analysis.md`

## Dependencies

- **Task #091** (Rule widget) should be implemented first to establish shared infrastructure (box characters, ANSI string utilities, LineStyle enum)

## Requirements

- Simple panel: `terminal.WritePanel("Content")`
- Panel with header: `terminal.WritePanel("Content", header: "Notice")`
- Fluent builder API for advanced configuration
- Support multiple border styles: Rounded, Square, Double, Heavy
- Handle multi-line content
- Configurable padding (horizontal and vertical)
- Respect terminal width or allow fixed width
- Graceful degradation when colors not supported

## Checklist

### Design
- [ ] Create `BorderStyle` enum (None, Rounded, Square, Double, Heavy)
- [ ] Create `BoxChars` static class with character constants for each style
- [ ] Design `Panel` class with properties (Header, Content, Border, Padding, Width)
- [ ] Design `PanelBuilder` for fluent configuration
- [ ] Plan extension methods for `ITerminal`

### Implementation
- [ ] Create `border-style.cs` with BorderStyle enum
- [ ] Create `box-chars.cs` with box-drawing character constants
- [ ] Create `ansi-string-utils.cs` with GetVisibleLength and StripAnsiCodes (if not created in #091)
- [ ] Create `panel-widget.cs` with Panel and PanelBuilder classes
- [ ] Create `terminal-panel-extensions.cs` with ITerminal extensions
- [ ] Implement multi-line content splitting and padding
- [ ] Implement header rendering in top border
- [ ] Handle terminal width detection via `ITerminal.WindowWidth`

### Testing
- [ ] Test simple panel without header
- [ ] Test panel with header
- [ ] Test panel with multi-line content
- [ ] Test panel with different BorderStyles
- [ ] Test panel with custom padding
- [ ] Test panel with fixed width
- [ ] Test panel with styled header and content
- [ ] Test graceful degradation when SupportsColor is false

### Documentation
- [ ] Add XML documentation to all public APIs
- [ ] Create sample demonstrating Panel widget usage

## Notes

### Proposed API

```csharp
// Simple panel with content
terminal.WritePanel("This is important information");

// With header
terminal.WritePanel("Content here", header: "Notice");

// Fluent builder
terminal.WritePanel(panel => panel
    .Header("💠 Ardalis".Cyan().Bold())
    .Content("Steve 'Ardalis' Smith\nSoftware Architect")
    .Border(BorderStyle.Rounded)
    .Padding(2, 1));
```

### Expected Output

```
╭─ Notice ─────────────────────────────╮
│                                      │
│  This is important information       │
│                                      │
╰──────────────────────────────────────╯
```

### Border Styles

| Style | TopLeft | TopRight | BottomLeft | BottomRight | Horizontal | Vertical |
|-------|---------|----------|------------|-------------|------------|----------|
| Rounded | `╭` | `╮` | `╰` | `╯` | `─` | `│` |
| Square | `┌` | `┐` | `└` | `┘` | `─` | `│` |
| Double | `╔` | `╗` | `╚` | `╝` | `═` | `║` |
| Heavy | `┏` | `┓` | `┗` | `┛` | `━` | `┃` |

### Implementation Location

New files in `source/timewarp-nuru-core/io/`:
- `border-style.cs`
- `box-chars.cs`
- `ansi-string-utils.cs` (shared with Rule widget)
- `panel-widget.cs`
- `terminal-panel-extensions.cs`

### Shared Infrastructure with Task #091

These components should be shared between Rule and Panel widgets:
- `BoxChars` class - Box-drawing character constants
- `AnsiStringUtils` - `GetVisibleLength()`, `StripAnsiCodes()`
- `LineStyle` enum may map to `BorderStyle` horizontal characters

### Panel Rendering Algorithm

1. Calculate effective width (terminal width or fixed)
2. Calculate content area width (width - 2 borders - 2×horizontal padding)
3. Split content into lines
4. Calculate total height (content lines + 2×vertical padding + 2 borders)
5. Render top border with optional header
6. Render vertical padding rows
7. Render content rows with side borders and horizontal padding
8. Render vertical padding rows
9. Render bottom border

### Evidence of Need

The codebase contains 33+ instances of manual box-drawing that would benefit from this widget:
- `samples/configuration/command-line-overrides.cs`
- `tests/scripts/run-repl-tests.cs`
- `samples/builtin-types-example.cs`
- `samples/custom-type-converter-example.cs`
