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
- [x] Create `source/timewarp-terminal/timewarp-terminal.csproj`
- [x] Configure NuGet package metadata (TimeWarp.Terminal)
- [x] Add to solution file `timewarp-nuru.slnx`
- [x] Create `GlobalUsings.cs` with standard usings
- [x] Add reference to `TimeWarp.Builder`

#### Move Core Terminal Abstractions
- [x] Move `io/iconsole.cs`
- [x] Move `io/iterminal.cs`
- [x] Move `io/nuru-console.cs`
- [x] Move `io/nuru-terminal.cs`

#### Move ANSI Support
- [x] Move `io/ansi-colors.cs`
- [x] Move `io/ansi-color-extensions.cs`
- [x] Move `io/ansi-hyperlink-extensions.cs`
- [x] Move `io/terminal-hyperlink-extensions.cs`

#### Move Widget System
- [x] Move `io/widgets/` directory (entire widget system)
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
- [x] Move `io/test-console.cs`
- [x] Move `io/test-terminal.cs`
- [x] Move `io/test-terminal-context.cs`

#### Update Dependencies
- [x] Add `timewarp-terminal` as dependency to `timewarp-nuru-core`
- [x] Update any internal references in timewarp-nuru-core

#### Testing
- [ ] Create `tests/timewarp-terminal-tests/` project with `GlobalUsings.cs`
- [ ] Move relevant tests from `timewarp-nuru-core-tests`
- [x] Verify all existing tests still pass

### Phase 2: Rename (separate commits after Phase 1 passes)
- [x] Update namespace from `TimeWarp.Nuru` to `TimeWarp.Terminal`
- [ ] Rename `NuruConsole` → `TimeWarpConsole`
- [ ] Rename `NuruTerminal` → `TimeWarpTerminal`
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

## Results

### Phase 1 & 2 Completed (2025-12-20)

Successfully extracted terminal/console functionality into `TimeWarp.Terminal` NuGet package:

- Created new `source/timewarp-terminal/` project with all terminal abstractions
- Updated namespace to `TimeWarp.Terminal`
- Temporarily switched TimeWarp.Jaribu from NuGet to ProjectReference to resolve chicken-and-egg dependency issue
- All 1694 tests pass (1678 passed, 16 skipped)

**Remaining work:**
- Rename types (`NuruConsole` → `TimeWarpConsole`, etc.)
- Create dedicated test project for TimeWarp.Terminal
- Switch Jaribu back to NuGet after both packages are published
