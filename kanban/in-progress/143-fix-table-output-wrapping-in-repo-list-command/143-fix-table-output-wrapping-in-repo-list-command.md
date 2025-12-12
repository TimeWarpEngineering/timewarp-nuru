# Fix table output wrapping in repo list command

## Description

The `dev-setup repo list` command displays a table that stretches beyond terminal width when paths are long. The table doesn't respect terminal width constraints, causing horizontal scrolling and poor readability.

## Checklist

- [ ] Investigate how Spectre.Console table rendering handles terminal width
- [ ] Implement path truncation or ellipsis for long paths
- [ ] Consider showing only the relevant portion of paths (e.g., last N segments)
- [ ] Add terminal width detection to constrain table output
- [ ] Test with various terminal widths

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
