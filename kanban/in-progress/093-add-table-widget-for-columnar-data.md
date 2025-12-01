# Add Table Widget for Columnar Data

## Description

Implement a `Table` widget for rendering formatted columnar data with headers, alignment, and borders. This is the most complex widget in the widget system, building on shared infrastructure from Rule and Panel widgets. Tables are a fundamental CLI output pattern, and this feature eliminates the primary remaining dependency on Spectre.Console for terminal UI.

**GitHub Issue**: [#91](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/91)

**Analysis**: `.agent/workspace/2025-12-01T17-30-00_issue-91-table-widget-analysis.md`

## Dependencies

- **Task #091** (Rule widget) - Provides `LineStyle`, `AnsiStringUtils`
- **Task #092** (Panel widget) - Provides `BorderStyle`, `BoxChars` (needs extension for table-specific characters)

## Requirements

- Column headers with optional styling
- Column alignment: Left, Center, Right
- Auto-sizing columns based on content
- Optional max column width with text wrapping
- Border styles: Rounded, Square, Double, Heavy, None
- Row separators (optional)
- Styled cell content (colors, bold, etc.)
- Handle ANSI codes in content when calculating widths
- Headerless tables
- Expandable columns to fill terminal width

## Checklist

### Design
- [x] Create `Alignment` enum (Left, Center, Right)
- [x] Create `TableColumn` class (Header, Alignment, MaxWidth, HeaderColor)
- [x] Design `Table` class with columns and rows
- [x] Design `TableBuilder` for fluent configuration
- [x] Plan extension methods for `ITerminal`
- [x] Extend `BoxChars` with T-junction and cross characters

### Implementation
- [x] Create `alignment.cs` with Alignment enum
- [x] Create `table-column.cs` with TableColumn class
- [x] Create `table-widget.cs` with Table class
- [x] Create `table-builder.cs` with TableBuilder class
- [x] Create `terminal-table-extensions.cs` with ITerminal extensions
- [x] Extend `box-chars.cs` with TopT, BottomT, LeftT, RightT, Cross for each style
- [x] Implement column width calculation (ANSI-aware)
- [x] Implement column alignment (left-pad, right-pad, center)
- [x] Implement text truncation for max column width (ellipsis)
- [x] Implement header separator row rendering
- [x] Implement optional row separators
- [x] Implement expandable columns

### Testing
- [x] Test basic table (2 columns, 2 rows)
- [x] Test right-aligned column
- [x] Test center-aligned column
- [x] Test multi-column table (5+ columns)
- [x] Test max column width with text truncation
- [x] Test styled content (ANSI colors in cells)
- [x] Test headerless table
- [x] Test row separators
- [x] Test expandable columns
- [x] Test empty table (headers only)
- [x] Test all border styles (Rounded, Square, Double, Heavy, None)
- [ ] Test long content (exceeds terminal width) - deferred
- [ ] Test graceful degradation when SupportsColor is false - deferred

### Documentation
- [x] Add XML documentation to all public APIs
- [x] Create sample demonstrating Table widget usage

## Notes

### Proposed API

```csharp
// Simple table
var table = new Table()
    .AddColumn("Name")
    .AddColumn("Stars", Alignment.Right)
    .AddColumn("Description")
    .AddRow("CleanArchitecture", "16.5k", "Clean Architecture template")
    .AddRow("GuardClauses", "3.2k", "Guard clause library");

terminal.WriteTable(table);

// Fluent inline
terminal.WriteTable(t => t
    .AddColumns("Package", "Downloads", "Version")
    .AddRow("Ardalis.GuardClauses", "12M", "5.0.0")
    .AddRow("Ardalis.Result", "8M", "10.0.0")
    .Border(TableBorder.Rounded));
```

### Expected Output

```
┌────────────────────┬─────────┬─────────────────────────────┐
│ Name               │   Stars │ Description                 │
├────────────────────┼─────────┼─────────────────────────────┤
│ CleanArchitecture  │   16.5k │ Clean Architecture template │
│ GuardClauses       │    3.2k │ Guard clause library        │
└────────────────────┴─────────┴─────────────────────────────┘
```

### Additional Box-Drawing Characters for Tables

Tables require T-junctions and crosses not needed by Panel:

| Style | TopT | BottomT | LeftT | RightT | Cross |
|-------|------|---------|-------|--------|-------|
| Square/Rounded | `┬` | `┴` | `├` | `┤` | `┼` |
| Double | `╦` | `╩` | `╠` | `╣` | `╬` |
| Heavy | `┳` | `┻` | `┣` | `┫` | `╋` |

### Table Rendering Algorithm

```
1. MEASURE PHASE
   - For each column: calculate max visible length (header vs cells)
   - Apply max column width constraints
   - If expandable: distribute remaining terminal width

2. RENDER PHASE
   a. Top border:    ┌───┬───┬───┐
   b. Header row:    │ H │ H │ H │
   c. Header sep:    ├───┼───┼───┤
   d. Data rows:     │ D │ D │ D │
   e. (Optional row separators between data rows)
   f. Bottom border: └───┴───┴───┘
```

### Implementation Location

New files in `source/timewarp-nuru-core/io/`:
- `alignment.cs`
- `table-column.cs`
- `table-widget.cs`
- `table-builder.cs`
- `terminal-table-extensions.cs`

Extend existing file:
- `box-chars.cs` - Add T-junction and cross characters

### Shared Infrastructure from Previous Tasks

| Component | Source Task | Usage |
|-----------|-------------|-------|
| `AnsiStringUtils.GetVisibleLength()` | #091 | Column width calculation |
| `AnsiStringUtils.StripAnsiCodes()` | #091 | Measure styled content |
| `BorderStyle` enum | #092 | Reuse as `TableBorder` |
| `BoxChars` class | #092 | Extend with table characters |
| `ITerminal.WindowWidth` | Existing | Expandable columns |

### Column Width Calculation

```csharp
int CalculateColumnWidth(TableColumn column, IEnumerable<string> cellValues)
{
    int headerWidth = AnsiStringUtils.GetVisibleLength(column.Header);
    int maxCellWidth = cellValues.Max(c => AnsiStringUtils.GetVisibleLength(c));
    int naturalWidth = Math.Max(headerWidth, maxCellWidth);
    
    return column.MaxWidth.HasValue 
        ? Math.Min(naturalWidth, column.MaxWidth.Value)
        : naturalWidth;
}
```

### Future Enhancement (Out of Scope)

Data source integration for later:
```csharp
// From IEnumerable<T>
terminal.WriteTable(users, u => new { u.Name, u.Email });
```
