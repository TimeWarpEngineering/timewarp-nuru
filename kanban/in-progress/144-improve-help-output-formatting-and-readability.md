# Improve help output formatting and readability

## Description

The `--help` output for Nuru CLI applications is plain, hard to scan, and visually cluttered. Parameter descriptions are inlined with commands making it difficult to read. The help output needs visual improvements including color, better layout, and possibly table formatting.

## Checklist

### Phase 1: Color and Basic Formatting (COMPLETE)
- [x] Audit current HelpProvider implementation
- [x] Add color highlighting for commands, parameters, and options
  - Commands/literals in cyan
  - Parameters in yellow
  - Options in green
  - Descriptions in gray
  - Section headers in bold yellow
- [x] Separate command names from their descriptions with better visual hierarchy
  - Use ANSI-aware padding for proper column alignment
- [x] Simplify parameter display in command signatures
  - Show only parameter names (e.g., `<name>`) instead of `<name|Description>`
  - Optional parameters shown with brackets: `[name]`
- [x] Test with various terminal color support levels
  - Added `useColor` parameter to GetHelpText
  - SessionContext.SupportsColor tracks terminal capability
- [x] Ensure graceful fallback for non-color terminals
  - When useColor=false, produces plain text output
  - Tests verify both colored and non-colored output

### Phase 2: Table Layout and Hierarchical Help (TODO)
- [ ] Use Table widget for commands/options listing
  - No borders (cleaner look)
  - Auto-shrink to terminal width (uses new `Table.Shrink` feature)
  - Two columns: Command | Description
- [ ] Implement hierarchical help (collapse groups at top level)
  - Top-level help shows collapsed groups: `docker (4 subcommands)`
  - Group help (`docker --help`) shows commands within that group
  - Only collapse if 2+ commands share a prefix
- [ ] Add footer: `Run 'appname COMMAND --help' for more information`
- [ ] Update group help output (`GetCommandGroupHelpText`) to use Table widget
- [ ] Add/update tests for new table-based output

### Phase 3: Future Enhancements (DEFERRED)
- [ ] Add examples section to help output

## Notes

### Current State (Phase 1 Complete)

Color formatting is implemented and working:
- Commands in cyan, parameters in yellow, options in green
- Descriptions in gray, section headers in bold yellow
- ANSI-aware padding for alignment
- Graceful fallback for non-color terminals

### Phase 2 Analysis

**Key Discovery: Group prefix extraction already exists**

`HelpRouteGenerator.GetCommandPrefix()` extracts leading literal segments from compiled routes:
- Works for both endpoints (`[NuruRouteGroup]`) AND delegate routes (`app.Map()`)
- Consistent behavior regardless of how route was defined
- Already used for generating per-command help routes

**What `NuruRouteGroupAttribute` generates:**
- At code generation: Prepends `GroupPrefix` to route pattern
- At runtime: `Endpoint` only has the final combined pattern (e.g., `"docker build <path>"`)
- Group metadata is NOT preserved at runtime - only the full route pattern exists

**Implication:** Use `GetCommandPrefix()` approach for grouping (extract leading literals from compiled route) rather than tracking attribute metadata. This ensures consistent grouping for all routes.

### Phase 2 Implementation Plan

**1. Add group prefix extraction to HelpProvider**
- Reuse/share logic from `HelpRouteGenerator.GetCommandPrefix()`

**2. Modify `GetHelpText()` to collapse groups**
- Group commands by their first literal segment (first word)
- If prefix has 2+ commands → collapse: `docker (4 subcommands)`
- If prefix has 1 command → show full command directly
- Standalone commands shown directly

**3. Use Table widget for output**
- No borders (per preference)
- Auto-shrink to terminal width
- `TruncateMode.End` for descriptions

**4. Example output (proposed):**

Top-level help (`app --help`):
```
Description:
  Sample demonstrating endpoints

Usage:
  endpoints [command] [options]

Commands:
  config                      Configuration commands (2 subcommands)
  docker                      Docker commands (4 subcommands)
  bye, cya, goodbye       (C) Say goodbye and exit
  deploy <env>            (C) Deploy to an environment
  greet <name>            (Q) Greet someone by name

Options:
  --version, -v           (C) Display version information
  --interactive, -i       (C) Enter interactive REPL mode

Run 'endpoints COMMAND --help' for more information on a command.
```

Group help (`app docker --help`):
```
Usage patterns for 'docker':

  docker build <path>     (C) Build an image from a Dockerfile
  docker ps               (Q) List containers
  docker run <image>      (C) Run a container from an image
  docker tag <src> <dst>  (I) Create a tag TARGET_IMAGE...

Arguments:
  path                    (Required) Type: string
  ...

Options:
  --tag, -t               Name and optionally a tag
  ...
```

### Files to Modify (Phase 2)

| File                          | Changes                                            |
| ----------------------------- | -------------------------------------------------- |
| `help-provider.cs`            | Use Table widget, add group collapsing logic       |
| `help-provider.filtering.cs`  | Add `GroupByPrefix()` method, modify `EndpointGroup` |
| `help-provider.formatting.cs` | Update to build Table instead of StringBuilder     |
| `help-route-generator.cs`     | Update `GetCommandGroupHelpText` to use Table      |
| `tests/help-provider-*.cs`    | Update tests for new output format                 |

### Open Questions (Phase 2)

1. **Group description source**: When collapsing a group like `docker`, where should the description come from?
   - A) Auto-generate: `"Docker commands (4 subcommands)"`
   - B) Use description from `NuruRouteGroupAttribute.Description` if available, else auto-generate
   - C) Just show: `"4 subcommands"` without prefix

2. **Shared prefix depth**: How deep should prefix detection go?
   - Currently `GetCommandPrefix()` extracts ALL leading literals (`"docker build"`)
   - Should we collapse at first level only (`docker`) or deepest common prefix?
   - Recommendation: First level only for top-level help, to avoid over-nesting

## Related Code

- `source/timewarp-nuru-core/help/help-provider.cs` - Main help orchestration
- `source/timewarp-nuru-core/help/help-provider.filtering.cs` - Route filtering and grouping
- `source/timewarp-nuru-core/help/help-provider.formatting.cs` - Pattern formatting
- `source/timewarp-nuru-core/help/help-provider.ansi.cs` - ANSI color helpers
- `source/timewarp-nuru-core/help/help-route-generator.cs` - Per-command help route generation
- `source/timewarp-terminal/widgets/table-widget.cs` - Table widget with Shrink support
