# Fix table output wrapping in repo list command

## Description

The `dev-setup repo list` command displays a table that stretches beyond terminal width when paths are long. The table doesn't respect terminal width constraints, causing horizontal scrolling and poor readability.

## Checklist

- [x] Investigate how Spectre.Console table rendering handles terminal width
- [x] Implement path truncation or ellipsis for long paths
- [x] Consider showing only the relevant portion of paths (e.g., last N segments)
- [x] Add terminal width detection to constrain table output
- [x] Test with various terminal widths

## Implementation Notes

Implemented `Table.Shrink` property (default `true`) that automatically shrinks columns to fit terminal width:

- **Files modified:**
  - `source/timewarp-terminal/widgets/table-column.cs` - Added `MinWidth` and `TruncateMode` properties
  - `source/timewarp-terminal/widgets/table-widget.cs` - Added `Shrink` property, shrinking logic, and `TruncateMode` support
  - `source/timewarp-terminal/widgets/table-builder.cs` - Added `Shrink(bool value = true)` fluent method
  - `source/timewarp-terminal/widgets/truncate-mode.cs` - New enum for truncation modes

- **New test file:**
  - `tests/timewarp-nuru-core-tests/table-widget-05-shrink.cs` - 16 tests covering shrink and truncate functionality

- **Sample updated:**
  - `samples/terminal/table-widget.cs` - Added examples 10, 11, and 12 demonstrating shrink and TruncateMode

**Key features:**
- Wider columns shrink more aggressively than narrower ones (proportional shrinking)
- Respects `TableColumn.MinWidth` (defaults to 4 for "..." truncation)
- Content automatically truncated with ellipsis when column shrinks
- Disable with `table.Shrink = false` or `.Shrink(false)` in builder
- `TruncateMode` controls where ellipsis appears:
  - `TruncateMode.End` (default): `"long text..."` - shows beginning
  - `TruncateMode.Start`: `"...long text"` - shows end (ideal for paths)
  - `TruncateMode.Middle`: `"long...text"` - shows both ends

**Example usage for paths:**
```csharp
.AddColumn(new TableColumn("Path") { TruncateMode = TruncateMode.Start })
```

## Notes

**Current behavior:**
- Table extends far beyond typical terminal width (80-120 chars)
- Long worktree paths like `/home/steventcramer/worktrees/github.com/TimeWarpEngineering/timewarp-state/Cramer-2025-08-12-032migrate-from-mediatr-to-timewarp-mediator` cause issues

**Possible solutions:**
1. Use Spectre.Console's `Expand(false)` to prevent table expansion
2. Truncate paths with ellipsis (show start...end of path)
3. Show only the worktree branch portion of the path
4. Use `~` for home directory to shorten paths
5. Add a `--full-path` flag for when users want complete paths

**Related code:**
- `dev-setup` is a sample/tool that uses TimeWarp.Nuru
- Likely uses Spectre.Console for table rendering
