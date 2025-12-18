# Extract Terminal/Console functionality into TimeWarp.Nuru.Terminal NuGet package

## Description

Extract the terminal and console functionality from `timewarp-nuru-core` into a new standalone NuGet package `TimeWarp.Nuru.Terminal` (`timewarp-nuru-terminal.csproj`). This will allow consumers to use the terminal/console abstractions and widgets independently without requiring the full Nuru CLI framework.

## Checklist

### Project Setup
- [ ] Create `source/timewarp-nuru-terminal/timewarp-nuru-terminal.csproj`
- [ ] Configure NuGet package metadata (TimeWarp.Nuru.Terminal)
- [ ] Add to solution file `timewarp-nuru.slnx`
- [ ] Configure `Directory.Build.props` as needed

### Move Core Terminal Abstractions
- [ ] Move `io/iconsole.cs` (IConsole interface)
- [ ] Move `io/iterminal.cs` (ITerminal interface)
- [ ] Move `io/nuru-console.cs` (NuruConsole implementation)
- [ ] Move `io/nuru-terminal.cs` (NuruTerminal implementation)

### Move ANSI Support
- [ ] Move `io/ansi-colors.cs`
- [ ] Move `io/ansi-color-extensions.cs`
- [ ] Move `io/ansi-hyperlink-extensions.cs`
- [ ] Move `io/terminal-hyperlink-extensions.cs`

### Move Widget System
- [ ] Move `io/widgets/` directory (entire widget system)
  - alignment.cs
  - ansi-string-utils.cs
  - border-style.cs
  - box-chars.cs
  - line-style.cs
  - panel-widget.cs
  - rule-widget.cs
  - table-builder.cs
  - table-column.cs
  - table-widget.cs
  - terminal-panel-extensions.cs
  - terminal-rule-extensions.cs
  - terminal-table-extensions.cs

### Move Test Infrastructure
- [ ] Move `io/test-console.cs`
- [ ] Move `io/test-terminal.cs`
- [ ] Move `io/test-terminal-context.cs`
- [ ] Move `io/nuru-test-context.cs`

### Update Dependencies
- [ ] Add `timewarp-nuru-terminal` as dependency to `timewarp-nuru-core`
- [ ] Update any internal references in timewarp-nuru-core
- [ ] Ensure `response-display.cs` still works (may need to stay in core or reference terminal)

### Testing
- [ ] Create `tests/timewarp-nuru-terminal-tests/` project
- [ ] Move relevant tests from `timewarp-nuru-core-tests`
- [ ] Verify all existing tests still pass
- [ ] Add package-specific tests if needed

### Documentation
- [ ] Add README.md to the new project
- [ ] Update main documentation to reference the new package

## Notes

### Files to Extract from `source/timewarp-nuru-core/io/`

**Core abstractions:**
- `iconsole.cs` - IConsole interface
- `iterminal.cs` - ITerminal interface  
- `nuru-console.cs` - Default console implementation
- `nuru-terminal.cs` - Default terminal implementation

**ANSI support:**
- `ansi-colors.cs` - ANSI color definitions
- `ansi-color-extensions.cs` - Color extension methods
- `ansi-hyperlink-extensions.cs` - Hyperlink ANSI codes
- `terminal-hyperlink-extensions.cs` - Terminal hyperlink helpers

**Widget system (`io/widgets/`):**
- `alignment.cs`, `border-style.cs`, `box-chars.cs`, `line-style.cs` - Styling enums/types
- `ansi-string-utils.cs` - ANSI string manipulation
- `panel-widget.cs`, `rule-widget.cs`, `table-widget.cs` - Widget implementations
- `table-builder.cs`, `table-column.cs` - Table building infrastructure
- `terminal-*-extensions.cs` - Extension methods for widgets

**Test infrastructure:**
- `test-console.cs`, `test-terminal.cs` - Test doubles
- `test-terminal-context.cs`, `nuru-test-context.cs` - Test context helpers

### Consideration

`response-display.cs` may need to remain in core as it likely depends on CLI-specific types, or it could be refactored to use abstractions from the terminal package.
