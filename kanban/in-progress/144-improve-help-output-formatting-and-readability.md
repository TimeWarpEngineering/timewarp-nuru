# Improve help output formatting and readability

## Description

The `--help` output for Nuru CLI applications is plain, hard to scan, and visually cluttered. Parameter descriptions are inlined with commands making it difficult to read. The help output needs visual improvements including color, better layout, and possibly table formatting.

## Checklist

- [x] Audit current HelpProvider implementation
- [x] Add color highlighting for commands, parameters, and options
  - Commands/literals in cyan
  - Parameters in yellow
  - Options in green
  - Descriptions in gray
  - Section headers in bold yellow
- [x] Separate command names from their descriptions with better visual hierarchy
  - Use ANSI-aware padding for proper column alignment
- [ ] Consider table layout for commands listing (deferred - current formatting sufficient)
- [ ] Group related commands visually (deferred - would require hierarchical route parsing)
- [x] Simplify parameter display in command signatures
  - Show only parameter names (e.g., `<name>`) instead of `<name|Description>`
  - Optional parameters shown with brackets: `[name]`
- [ ] Add examples section to help output (future enhancement)
- [x] Test with various terminal color support levels
  - Added `useColor` parameter to GetHelpText
  - SessionContext.SupportsColor tracks terminal capability
- [x] Ensure graceful fallback for non-color terminals
  - When useColor=false, produces plain text output
  - Tests verify both colored and non-colored output

## Notes

**Current issues with `dev-setup --help`:**
```
  repo add <path|Repository path> <title|Display title> --config,-c? <config?|Target configuration (uses default if not specified)>
```
- Parameter descriptions inline with syntax (`<path|Repository path>`) is cluttered
- No color differentiation between commands, parameters, options
- No visual grouping of related commands
- Long lines are hard to scan
- Default route description appears empty/minimal

**Inspiration/Examples:**
- `dotnet --help` uses color and clear sections
- `gh --help` has clean grouping and color
- `cargo --help` uses indentation and color effectively

**Possible improvements:**
1. Use Spectre.Console markup for colors:
   - Commands in cyan/blue
   - Parameters in yellow
   - Options in green
   - Descriptions in default/gray
2. Table layout:
   ```
   Command              Description
   ─────────────────────────────────────
   list                 List available configurations
   repo add             Add a repository to a configuration
   ```
3. Separate detailed parameter info:
   ```
   repo add <path> <title> [--config <name>]
     
     Arguments:
       path     Repository path
       title    Display title
     
     Options:
       --config, -c    Target configuration (default if not specified)
   ```

**Related code:**
- `source/timewarp-nuru-core/` - likely contains HelpProvider
- Look for help generation/formatting code
