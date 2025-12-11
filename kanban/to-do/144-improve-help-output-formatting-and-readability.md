# Improve help output formatting and readability

## Description

The `--help` output for Nuru CLI applications is plain, hard to scan, and visually cluttered. Parameter descriptions are inlined with commands making it difficult to read. The help output needs visual improvements including color, better layout, and possibly table formatting.

## Checklist

- [ ] Audit current HelpProvider implementation
- [ ] Add color highlighting for commands, parameters, and options
- [ ] Separate command names from their descriptions with better visual hierarchy
- [ ] Consider table layout for commands listing
- [ ] Group related commands visually (e.g., all `repo` subcommands together)
- [ ] Move parameter descriptions below or separate from command signatures
- [ ] Add examples section to help output
- [ ] Test with various terminal color support levels
- [ ] Ensure graceful fallback for non-color terminals

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
