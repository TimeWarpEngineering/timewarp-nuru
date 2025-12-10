# Document Built-in Routes

## Description

Document the automatic routes registered by `NuruApp.CreateBuilder()`:
- `--version,-v` - Display version information
- `--check-updates` - Check GitHub for newer versions
- `--help,-h` - Show help (already documented)
- `--interactive,-i` - Enter REPL mode

## Requirements

- Create new section or page for built-in routes
- Document each route's behavior and output
- Show how to disable via `NuruAppOptions`
- Include prerequisites (e.g., `RepositoryUrl` for check-updates)

## Checklist

- [ ] Add built-in routes section to getting-started.md or features/overview.md
- [ ] Document --version route behavior and output format
- [ ] Document --check-updates route behavior and prerequisites
- [ ] Document DisableVersionRoute and DisableCheckUpdatesRoute options
- [ ] Add example output for each route

## Notes

Source files:
- `source/timewarp-nuru/nuru-app-builder-extensions.cs` (lines 77-156)
- `source/timewarp-nuru/nuru-app-options.cs`
