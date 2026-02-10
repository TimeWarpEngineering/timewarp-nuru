# Use terminal.WriteTable for generated --help output

**Supersedes**: Task #144 (archived - different approach)

## Description

Replace the current manual padding approach in the source generator's help emitter with TimeWarp.Terminal's `WriteTable()` method for improved formatting, alignment, and visual presentation. The source generator currently emits code that uses `.PadRight(25)` for alignment, which doesn't adapt to long command names and lacks visual separation between columns.

The help output looks like shit with manual padding. Tables will make it readable and professional.

## Configuration Decisions

1. **Table Style**: Simple ASCII borders (`+`, `-`, `|`)
2. **Migration Strategy**: Direct switch - no fallback or configuration options
3. **Scope**: Full implementation including per-route help tables (parameters and options)
4. **Columns**: Two columns only - Command/Description (no message type, tags, etc.)

## Checklist

### Phase 1: Update EmitCommands() for Commands Table
- [ ] Modify `EmitCommands()` method in `help-emitter.cs` (lines 63-81)
- [ ] Replace per-route `WriteLine()` loop with table builder emission
- [ ] Emit code: `terminal.WriteTable(table => table.AddColumn(...).AddRow(...))`
- [ ] Remove `EmitRouteHelp()` helper method (lines 83-98) - no longer needed
- [ ] Keep `BuildPatternDisplay()` helper (lines 100-172) - still needed for pattern formatting
- [ ] Test: Verify commands appear in table format

### Phase 2: Update EmitOptions() for Options Table
- [ ] Modify `EmitOptions()` method in `help-emitter.cs` (lines 174-183)
- [ ] Replace three `terminal.WriteLine()` calls with table builder
- [ ] Emit code for global options table (--help, --version, --capabilities)
- [ ] Test: Verify options appear in table format

### Phase 3: Update Per-Route Help Tables
- [ ] Find per-route help emission code in `route-matcher-emitter.cs` (around lines 50-65)
- [ ] Update Parameters section to emit table with columns: Name, Required, Type
- [ ] Update Options section (if route has options) to emit table with columns: Option, Description
- [ ] Test: Verify per-route help shows tables for `command --help`

### Phase 4: Create Comprehensive Tests
- [ ] Create new test file: `tests/timewarp-nuru-tests/help/help-02-table-formatting.cs`
- [ ] Test: Should_display_commands_in_table_format
- [ ] Test: Should_display_options_in_table_format
- [ ] Test: Should_handle_long_command_names
- [ ] Test: Should_display_per_route_parameters_table
- [ ] Test: Should_display_per_route_options_table
- [ ] Test: Should_preserve_command_descriptions
- [ ] Test: Should_handle_empty_descriptions

### Phase 5: Verify Existing Tests
- [ ] Run `tests/timewarp-nuru-tests/help/help-01-per-route-help.cs`
- [ ] Verify all 8 tests still pass with new table formatting
- [ ] Content should be preserved (`.OutputContains()` assertions)
- [ ] Update any tests that check exact string formatting

### Phase 6: Full Test Suite
- [ ] Run full CI test suite: `dotnet run tests/ci-tests/run-ci-tests.cs`
- [ ] Verify no regressions across ~500 tests
- [ ] Fix any broken tests related to help output formatting

## Expected Output Comparison

### Before (Current - Manual Padding):
```
Commands:
  deploy {env}              Deploy to an environment
  build {project}           Build a project

Options:
  --help, -h             Show this help message
  --version              Show version information
```

### After (With Tables - Simple ASCII):
```
Commands:
+------------------------+------------------------------+
| Command                | Description                  |
+------------------------+------------------------------+
| deploy {env}           | Deploy to an environment     |
| build {project}        | Build a project              |
+------------------------+------------------------------+

Options:
+-----------------+--------------------------------+
| Option          | Description                    |
+-----------------+--------------------------------+
| --help, -h      | Show this help message         |
| --version       | Show version information       |
| --capabilities  | Show capabilities for AI tools |
+-----------------+--------------------------------+
```

### Per-Route Help Example:
```
deploy {env}

  Deploy to an environment

Parameters:
+-------+----------+--------+
| Name  | Required | Type   |
+-------+----------+--------+
| env   | Yes      | string |
+-------+----------+--------+
```

## Technical Details

### Files to Modify

| File | Lines | Changes |
|------|-------|---------|
| `source/timewarp-nuru-analyzers/generators/emitters/help-emitter.cs` | 63-81 | Update `EmitCommands()` to emit table code |
| `source/timewarp-nuru-analyzers/generators/emitters/help-emitter.cs` | 174-183 | Update `EmitOptions()` to emit table code |
| `source/timewarp-nuru-analyzers/generators/emitters/help-emitter.cs` | 83-98 | Remove `EmitRouteHelp()` method |
| `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs` | ~50-65 | Update per-route help emission |
| `tests/timewarp-nuru-tests/help/help-02-table-formatting.cs` | New | Create comprehensive table tests |

### Code Generation Example

**Current generated code** (from `help-emitter.cs`):
```csharp
terminal.WriteLine("Commands:");
terminal.WriteLine("  deploy {env}              Deploy to an environment");
terminal.WriteLine("  build {project}           Build a project");
```

**New generated code** (with tables):
```csharp
terminal.WriteLine("Commands:");
terminal.WriteTable(table => table
  .AddColumn("Command")
  .AddColumn("Description")
  .AddRow("deploy {env}", "Deploy to an environment")
  .AddRow("build {project}", "Build a project")
);
```

### TimeWarp.Terminal Table API

Based on TimeWarp.Terminal README:
```csharp
terminal.WriteTable(table => table
    .AddColumn("Name")
    .AddColumn("Value")
    .AddRow("CPU", "45%")
    .AddRow("Memory", "2.1 GB"));
```

- Fluent API with method chaining
- Auto-calculates column widths
- Simple ASCII borders (`+`, `-`, `|`)
- No border style configuration needed for simple style

## Notes

### Implementation Plan (Updated 2026-02-10)

---

## Implementation Plan

### Phase 1: Create HelpPatternHelper
Create new file: `source/timewarp-nuru-analyzers/generators/emitters/help-pattern-helper.cs` with shared `BuildPatternDisplay()` method.

### Phase 2: Update help-emitter.cs
- Remove `BuildPatternDisplay()` method (move to helper)
- Modify `EmitCommands()` to emit WriteTable code
- Modify `EmitOptions()` to emit WriteTable code  
- Remove `EmitRouteHelp()` method

### Phase 3: Update route-help-emitter.cs
- Modify `EmitRouteHelpContent()` to emit WriteTable for parameters (3 columns: Name, Required, Type)
- Modify `EmitRouteHelpContent()` to emit WriteTable for options (2 columns: Option, Description)
- Remove `BuildParameterHelpLine()` and `BuildOptionHelpLine()` methods
- Update `EmitGroupHelpContent()` to use WriteTable

### Phase 4: Create Tests
Create new test file: `tests/timewarp-nuru-tests/help/help-02-table-formatting.cs`

### Phase 5: Verify Existing Tests
Run existing help tests to ensure no regressions

### Phase 6: Full Test Suite
Run `dotnet run tests/ci-tests/run-ci-tests.cs`

## Key Code Changes

**EmitCommands() will generate:**
```csharp
terminal.WriteLine("Commands:");
terminal.WriteTable(table => table
  .AddColumn("Command")
  .AddColumn("Description")
  .AddRow("deploy {env}", "Deploy to an environment")
);
```

**EmitOptions() will generate:**
```csharp
terminal.WriteLine("Options:");
terminal.WriteTable(table => table
  .AddColumn("Option")
  .AddColumn("Description")
  .AddRow("--help, -h", "Show this help message")
  .AddRow("--version", "Show version information")
  .AddRow("--capabilities", "Show capabilities for AI tools")
);
```

## Files to Modify

| File | Action |
|------|--------|
| `help-pattern-helper.cs` | Create |
| `help-emitter.cs` | Modify |
| `route-help-emitter.cs` | Modify |
| `help-02-table-formatting.cs` | Create |

---

### Why Source Source Generator Changes?

The source generator emits the `PrintHelp()` method at compile time. This task modifies the **emitter** (`help-emitter.cs`) to generate code that calls `terminal.WriteTable()` instead of `terminal.WriteLine()` with manual padding.

### Why This Supersedes #144

Task #144 was focused on a runtime HelpProvider approach with separate files and complex filtering logic. After analysis, we determined the **source generator emitter** is the correct place to fix help formatting - it's simpler, generates the code at compile time, and avoids runtime complexity.

### Risk Assessment

**Low Risk**:
- ✅ Generated code change only (no runtime API changes)
- ✅ Content preserved (only formatting changes)
- ✅ Existing `.OutputContains()` tests should still pass

**Medium Risk**:
- ⚠️ Tests that check exact string format may break
- ⚠️ Terminal width issues if WriteTable doesn't handle narrow terminals well

**Mitigation**:
- Run full test suite before/after
- Add tests for edge cases (very long command names)
- Simple ASCII borders avoid Unicode rendering issues

### Implementation Order

1. Phase 1 (Commands table) - Core functionality
2. Phase 2 (Options table) - Consistent with Phase 1
3. Phase 4 (New tests) - Validate changes work
4. Phase 5 (Existing tests) - Ensure no regressions
5. Phase 3 (Per-route help) - More complex, build on foundation
6. Phase 6 (Full suite) - Final validation

## Related Tasks

- #144 - Improve help output formatting (runtime HelpProvider)
- #356 - Implement per-route help support (done)
- #370 - Help behavior for routes with same prefix (open)
