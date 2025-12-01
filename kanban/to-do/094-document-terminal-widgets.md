# Document Terminal Widgets

## Description

Create comprehensive documentation for the new terminal widget system (Rule, Panel, Table) that was implemented in Tasks #091, #092, and #093. Update existing documentation to reflect that TimeWarp.Nuru now includes table support as a Spectre.Console alternative.

**Related Issues**: [#89](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/89), [#90](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/90), [#91](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/91)

**Depends On**: Tasks #091, #092, #093 (all completed)

## Requirements

- Create new `widgets.md` documentation file
- Update `terminal-abstractions.md` to reference widgets
- Update `features/overview.md` to list widgets as a feature
- Fix outdated "Tables/Progress: Not included" statement

## Checklist

### New Documentation
- [ ] Create `documentation/user/features/widgets.md`
  - [ ] Overview of widget system
  - [ ] Rule widget documentation with examples
  - [ ] Panel widget documentation with examples
  - [ ] Table widget documentation with examples
  - [ ] API reference for all widget classes
  - [ ] Border/line style reference tables

### Update Existing Documentation
- [ ] Update `documentation/user/features/terminal-abstractions.md`
  - [ ] Fix line ~395: "Tables/Progress: Not included" → now included
  - [ ] Add reference to new widgets.md
  - [ ] Update Spectre.Console comparison table
- [ ] Update `documentation/user/features/overview.md`
  - [ ] Add "Terminal Widgets" section with link to widgets.md

## Notes

### Widgets Implemented

| Widget | Class | Builder | Extension Method |
|--------|-------|---------|------------------|
| Rule | `Rule` | `RuleBuilder` | `terminal.WriteRule()` |
| Panel | `Panel` | `PanelBuilder` | `terminal.WritePanel()` |
| Table | `Table` | `TableBuilder` | `terminal.WriteTable()` |

### Shared Infrastructure

- `LineStyle` enum (Thin, Doubled, Heavy)
- `BorderStyle` enum (None, Rounded, Square, Doubled, Heavy)
- `Alignment` enum (Left, Center, Right)
- `BoxChars` static class with character constants
- `AnsiStringUtils` for ANSI-aware string operations

### Sample Files to Reference

- `samples/rule-widget-demo/rule-widget-demo.cs`
- `samples/panel-widget-demo/panel-widget-demo.cs`
- `samples/table-widget-demo/table-widget-demo.cs`

### Key Documentation Points

1. **Rule Widget**: Horizontal divider lines with optional centered text
2. **Panel Widget**: Bordered boxes with headers and multi-line content
3. **Table Widget**: Columnar data with alignment, auto-sizing, and border styles
4. **Fluent API**: All widgets support builder pattern for configuration
5. **Testability**: Works with TestTerminal for unit testing
6. **ANSI-aware**: Properly handles styled content in width calculations
