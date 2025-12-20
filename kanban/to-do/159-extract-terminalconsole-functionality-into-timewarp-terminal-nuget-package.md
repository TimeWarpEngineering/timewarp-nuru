# Extract Terminal/Console functionality into TimeWarp.Terminal NuGet package

## Description

Extract the terminal and console functionality from `timewarp-nuru-core` into a new standalone NuGet package `TimeWarp.Terminal` (`timewarp-terminal.csproj`). This will allow consumers to use the terminal/console abstractions and widgets independently without requiring the full Nuru CLI framework.

**Prerequisite:** Task 206 (Extract TimeWarp.Builder) must be completed first.

**Naming Decision:** Using `TimeWarp.Terminal` (not `TimeWarp.Nuru.Terminal`) because:
- These abstractions are general-purpose, not Nuru-specific
- Can be reused by other TimeWarp projects (Amuru, Ganda, etc.)
- Useful for any .NET app needing formatted console output (web apps, background services, etc.)
- Follows the pattern of other standalone TimeWarp packages (TimeWarp.Fixie, TimeWarp.Jaribu, etc.)

## Checklist

### Phase 1: Extract (no renames)

#### Project Setup
- [ ] Create `source/timewarp-terminal/timewarp-terminal.csproj`
- [ ] Configure NuGet package metadata (TimeWarp.Terminal)
- [ ] Add to solution file `timewarp-nuru.slnx`
- [ ] Create `GlobalUsings.cs` with standard usings
- [ ] Add reference to `TimeWarp.Builder`

#### Move Core Terminal Abstractions
- [ ] Move `io/iconsole.cs`
- [ ] Move `io/iterminal.cs`
- [ ] Move `io/nuru-console.cs`
- [ ] Move `io/nuru-terminal.cs`

#### Move ANSI Support
- [ ] Move `io/ansi-colors.cs`
- [ ] Move `io/ansi-color-extensions.cs`
- [ ] Move `io/ansi-hyperlink-extensions.cs`
- [ ] Move `io/terminal-hyperlink-extensions.cs`

#### Move Widget System
- [ ] Move `io/widgets/` directory (entire widget system)
  - alignment.cs
  - ansi-string-utils.cs
  - border-style.cs
  - box-chars.cs
  - line-style.cs
  - nested-panel-builder.cs
  - nested-rule-builder.cs
  - nested-table-builder.cs
  - panel-widget.cs
  - rule-widget.cs
  - table-builder.cs
  - table-column.cs
  - table-widget.cs
  - terminal-panel-extensions.cs
  - terminal-rule-extensions.cs
  - terminal-table-extensions.cs

#### Move Test Infrastructure
- [ ] Move `io/test-console.cs`
- [ ] Move `io/test-terminal.cs`
- [ ] Move `io/test-terminal-context.cs`

#### Update Dependencies
- [ ] Add `timewarp-terminal` as dependency to `timewarp-nuru-core`
- [ ] Update any internal references in timewarp-nuru-core

#### Testing
- [ ] Create `tests/timewarp-terminal-tests/` project with `GlobalUsings.cs`
- [ ] Move relevant tests from `timewarp-nuru-core-tests`:
  - ansi-string-utils-01-basic.cs
  - ansi-string-utils-02-wrap-text.cs
  - panel-widget-01-basic.cs
  - panel-widget-02-terminal-extensions.cs
  - panel-widget-03-word-wrap.cs
  - rule-widget-01-basic.cs
  - rule-widget-02-terminal-extensions.cs
  - table-widget-01-basic.cs
  - table-widget-02-borders.cs
  - table-widget-03-styling.cs
  - table-widget-04-expand.cs
  - test-terminal-context-01-basic.cs
- [ ] Verify all existing tests still pass

### Phase 2: Rename (separate commits after Phase 1 passes)
- [ ] Rename `NuruConsole` → `TerminalConsole`
- [ ] Rename `NuruTerminal` → `Terminal`
- [ ] Update namespace from `TimeWarp.Nuru` to `TimeWarp.Terminal`
- [ ] Update file names to match new type names

## Notes

### Files to STAY in `timewarp-nuru-core`

- `io/response-display.cs` - depends on `NuruJsonSerializerContext`
- `io/nuru-test-context.cs` - depends on `NuruCoreApp`

### Dependency Graph After Extraction

```
TimeWarp.Builder
    ^
    |
TimeWarp.Terminal (new) ----+
    ^                       |
    |                       |
TimeWarp.Nuru.Core ---------+
```

### Namespace Strategy

**Phase 1:** Keep `TimeWarp.Nuru` namespace to minimize extraction diff.  
**Phase 2:** Refactor to `TimeWarp.Terminal` namespace after build passes.

Final namespace structure:
```
TimeWarp.Terminal
TimeWarp.Terminal.Widgets
TimeWarp.Terminal.Testing
```
