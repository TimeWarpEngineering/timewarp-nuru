# Add HelpOptions Configuration

## Description

Add `HelpOptions` class to allow configuration of help output filtering and display. This addresses cluttered help output that shows per-command help routes, REPL commands in CLI mode, and completion infrastructure routes.

## Requirements

### HelpOptions Class (in `timewarp-nuru-core`)

```csharp
public sealed class HelpOptions
{
  /// <summary>
  /// Whether to show auto-generated per-command help routes in help output.
  /// When false, routes like "blog --help?" are hidden from listings.
  /// The routes still work, they're just not displayed.
  /// Default: false
  /// </summary>
  public bool ShowPerCommandHelpRoutes { get; set; } = false;

  /// <summary>
  /// Whether to show REPL-specific commands in CLI help output.
  /// REPL commands: exit, quit, q, clear, cls, clear-history, history, help (literal)
  /// When false, these are hidden from CLI --help but shown in REPL's help command.
  /// Default: false
  /// </summary>
  public bool ShowReplCommandsInCli { get; set; } = false;

  /// <summary>
  /// Whether to show shell completion infrastructure routes in help output.
  /// Completion routes: __complete, --generate-completion, --install-completion
  /// Default: false
  /// </summary>
  public bool ShowCompletionRoutes { get; set; } = false;

  /// <summary>
  /// Additional route patterns to exclude from help output.
  /// Supports wildcards: * matches any characters.
  /// Example: ["*-debug", "*-internal"]
  /// </summary>
  public IList<string>? ExcludePatterns { get; set; }
}
```

### HelpContext Enum

```csharp
public enum HelpContext { Cli, Repl }
```

### Alias Grouping

Group endpoints by description before filtering and display:
- Same description = alias group (e.g., `exit`, `quit`, `q` all have "Exit the REPL")
- Filtering applies to entire group, not individual patterns
- Display consolidated: `exit, quit, q              Exit the REPL`

### Expected Help Output (with defaults)

```
Description:
  Ardalis CLI - Tools and links from Steve 'Ardalis' Smith

Usage:
  ardalis [command] [options]

Commands:
  blog                          Open Ardalis's blog
  books --no-paging? --page-size? <size:int?>  Display published books
  card                          Display Ardalis's business card
  ...

Options:
  -i, --interactive             Enter interactive REPL mode
  --help                        Show available commands
```

## Checklist

### Design
- [x] Create `HelpOptions` class in `timewarp-nuru-core`
- [x] Create `HelpContext` enum in `timewarp-nuru-core`

### Implementation
- [x] Add `Action<HelpOptions>? ConfigureHelp` to `NuruAppOptions`
- [x] Update `HelpProvider.GetHelpText()` signature to accept `HelpOptions` and `HelpContext`
- [x] Implement endpoint grouping by description
- [x] Implement filtering logic:
  - Filter `* --help?` patterns when `ShowPerCommandHelpRoutes = false`
  - Filter REPL commands when `ShowReplCommandsInCli = false && context == Cli`
  - Filter `__complete*`, `--generate-completion*`, `--install-completion*` when `ShowCompletionRoutes = false`
  - Filter by `ExcludePatterns` wildcard matching
- [x] Update `HelpRouteGenerator` to pass options to help handlers
- [x] Update REPL's help command to use `HelpContext.Repl`
- [x] Wire `HelpOptions` through `NuruCoreAppBuilder`

### Testing
- [x] Add tests for filtering behavior
- [x] Add tests for alias grouping display
- [x] Verify REPL commands visible in REPL help, hidden in CLI help

## Notes

- REPL commands identified by: `exit`, `quit`, `q`, `clear`, `cls`, `clear-history`, `history`, `help` (literal without dash)
- Completion routes identified by pattern: `__complete*`, `--generate-completion*`, `--install-completion*`
- Per-command help routes identified by: `* --help?` suffix
- Grouping by description works because `MapMultiple` guarantees same description for all patterns

## Related Tasks

- 118: Fix HelpProvider single-dash option detection (bug fix, independent)
- 119: Fix AddInteractiveRoute alias syntax (improves `-i, --interactive` display)

## Implementation Summary

### Files Created
- `source/timewarp-nuru-core/help/help-context.cs` - HelpContext enum (Cli, Repl)
- `source/timewarp-nuru-core/help/help-options.cs` - HelpOptions configuration class
- `tests/timewarp-nuru-core-tests/help-provider-02-filtering.cs` - 10 new tests for filtering behavior

### Files Modified
- `source/timewarp-nuru-core/help/help-provider.cs` - Updated GetHelpText to accept options/context, implemented filtering and alias grouping
- `source/timewarp-nuru-core/help/help-route-generator.cs` - Pass HelpOptions and context to help routes
- `source/timewarp-nuru-core/nuru-core-app-builder.cs` - Store HelpOptions, add ConfigureHelp() method
- `source/timewarp-nuru-core/nuru-core-app.cs` - Add HelpOptions property, update constructors and ShowAvailableCommands
- `source/timewarp-nuru/nuru-app-options.cs` - Add ConfigureHelp action
- `source/timewarp-nuru/nuru-app-builder-extensions.cs` - Wire ConfigureHelp in UseAllExtensions
