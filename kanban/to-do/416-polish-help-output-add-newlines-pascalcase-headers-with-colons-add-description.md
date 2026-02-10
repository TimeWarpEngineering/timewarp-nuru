# Polish help output: add newlines, PascalCase headers with colons, add Description

## Description

Refine help output format based on review of dev --help output:
1. Add newline between OPTIONS and COMMANDS tables for better visual separation
2. Change headers from UPPERCASE ("OPTIONS", "COMMANDS") to PascalCase with colon ("Options:", "Commands:")
3. Add missing "Description:" text to app description line

## Current Behavior

```
  dev v1.0.0...
Development CLI for TimeWarp.Nuru

USAGE: dev [command] [options]

OPTIONS
┌────────────────┬────────────────────────────────┐
│ --help, -h     │ Show this help message         │
│ --version      │ Show version information       │
└────────────────┴────────────────────────────────┘
COMMANDS
┌────────────────┬──────────────────────────────────────────────────┐
│ analyze        │ Run Roslynator analysis and fixes                │
│ build          │ Build all TimeWarp.Nuru projects                 │
└────────────────┴──────────────────────────────────────────────────┘
```

**Issues:**
- No blank line between OPTIONS and COMMANDS tables (cramped)
- Headers are "OPTIONS", "COMMANDS" (UPPERCASE)
- Missing "Description:" before app description

## Expected Behavior

```
  dev v1.0.0...
Description: Development CLI for TimeWarp.Nuru    ← Add "Description:"

USAGE: dev [command] [options]

Options:                                          ← PascalCase with colon
┌────────────────┬────────────────────────────────┐
│ --help, -h     │ Show this help message         │
│ --version      │ Show version information       │
└────────────────┴────────────────────────────────┘

Commands:                                         ← PascalCase with colon, newline before
┌────────────────┬──────────────────────────────────────────────────┐
│ analyze        │ Run Roslynator analysis and fixes                │
│ build          │ Build all TimeWarp.Nuru projects                 │
└────────────────┴──────────────────────────────────────────────────┘
```

## Changes Required

### Update help-emitter.cs

1. **Add "Description:" prefix to description line** in EmitHeader():

```csharp
// App description with "Description:" prefix
if (model.Description is not null)
{
  sb.AppendLine($"    terminal.WriteLine($\"Description: {EscapeString(model.Description)}\".Gray());");
}
```

2. **Change header format** in EmitOptions() and EmitCommands():

```csharp
// EmitOptions()
sb.AppendLine("    terminal.WriteLine(\"Options:\".Cyan().Bold());");  // PascalCase with colon

// EmitCommands()
sb.AppendLine($"    terminal.WriteLine(\"{EscapeString(categoryName)}:\".Cyan().Bold());");  // "Commands:" or "Docker:"
```

3. **Add blank line between tables**:

```csharp
// In EmitCommands(), before each group except the first
// (already has logic for this, but ensure there's always a blank line before "Commands:")
// The first group (Options) already has terminal.WriteLine(); before it
```

Wait - looking at current code, EmitOptions() already adds `terminal.WriteLine();` at the start, so the blank line before OPTIONS is there. We need to ensure blank line between OPTIONS and COMMANDS.

Current EmitOptions() starts with:
```csharp
sb.AppendLine("    terminal.WriteLine();");
sb.AppendLine("    terminal.WriteLine(\"OPTIONS\".Cyan().Bold());");
```

So there's already a blank line before OPTIONS (after USAGE). We need one after OPTIONS table too.

Add in EmitCommands() - but wait, the first group check already adds a blank line if not first group. But OPTIONS is emitted before COMMANDS, so COMMANDS is "firstGroup" and won't get the blank line.

We need to always add a blank line before COMMANDS (after OPTIONS).

## Implementation

### Change 1: EmitHeader() - Add "Description:" prefix

```csharp
private static void EmitHeader(StringBuilder sb, AppModel model)
{
  // App name with version in cyan bold
  string version = model.Version ?? "1.0.0";
  sb.AppendLine($"    terminal.WriteLine($\"  {{__appName}} v{version}\".BrightCyan().Bold());");

  // App description with "Description:" prefix
  if (model.Description is not null)
  {
    sb.AppendLine($"    terminal.WriteLine($\"Description: {EscapeString(model.Description)}\".Gray());");
  }

  sb.AppendLine("    terminal.WriteLine();");
}
```

### Change 2: EmitOptions() - PascalCase header

```csharp
private static void EmitOptions(StringBuilder sb)
{
  sb.AppendLine("    terminal.WriteLine();");
  sb.AppendLine("    terminal.WriteLine(\"Options:\".Cyan().Bold());");  // Changed from "OPTIONS"
  sb.AppendLine("    terminal.WriteTable(table => table");
  sb.AppendLine("      .AddColumn(\"Option\")");
  sb.AppendLine("      .AddColumn(\"Description\")");
  sb.AppendLine("      .AddRow(\"--help, -h\", \"Show this help message\")");
  sb.AppendLine("      .AddRow(\"--version\", \"Show version information\")");
  sb.AppendLine("      .AddRow(\"--capabilities\", \"Show capabilities for AI tools\")");
  sb.AppendLine("      .HideHeaders()");
  sb.AppendLine("    );");
  sb.AppendLine("    terminal.WriteLine();");  // NEW: Blank line after OPTIONS table
}
```

### Change 3: EmitCommands() - PascalCase headers, remove firstGroup logic

```csharp
private static void EmitCommands(StringBuilder sb, AppModel model)
{
  if (!model.HasRoutes)
  {
    return;
  }

  // Group routes by GroupPrefix
  IEnumerable<IGrouping<string, RouteDefinition>> groups = model.Routes
    .GroupBy(r => r.GroupPrefix ?? "")
    .OrderBy(g => g.Key);

  foreach (IGrouping<string, RouteDefinition> group in groups)
  {
    string categoryName = string.IsNullOrEmpty(group.Key)
      ? "Commands"           // Changed from "COMMANDS"
      : group.Key;            // Keep original case, not ToUpperInvariant()

    // Category header in cyan bold with colon
    sb.AppendLine($"    terminal.WriteLine(\"{EscapeString(categoryName)}:\".Cyan().Bold());");

    // Table with command names only (not full patterns)
    sb.AppendLine("    terminal.WriteTable(table => table");
    sb.AppendLine("      .AddColumn(\"Command\")");
    sb.AppendLine("      .AddColumn(\"Description\")");

    foreach (RouteDefinition route in group)
    {
      string commandName = GetCommandName(route);
      string description = route.Description ?? "";
      sb.AppendLine($"      .AddRow(\"{EscapeString(commandName)}\", \"{EscapeString(description)}\")");
    }

    sb.AppendLine("      .HideHeaders()");
    sb.AppendLine("    );");
  }
}
```

## Files to Modify

| File | Changes |
|------|---------|
| `help-emitter.cs` | 1. Add "Description:" prefix in EmitHeader(), 2. Change "OPTIONS" to "Options:" in EmitOptions(), 3. Add blank line after OPTIONS table, 4. Change "COMMANDS" to "Commands:" in EmitCommands(), 5. Remove firstGroup logic and always emit headers |

## Checklist

- [ ] Add "Description:" prefix to app description line in EmitHeader()
- [ ] Change "OPTIONS" to "Options:" (PascalCase with colon)
- [ ] Add blank line after OPTIONS table (before COMMANDS)
- [ ] Change "COMMANDS" to "Commands:" (PascalCase with colon)
- [ ] Remove firstGroup logic and always emit category headers
- [ ] Run existing tests to verify no regressions
- [ ] Verify output visually with `dev --help`

## Testing

Test with:
```bash
dev --help
```

Expected output:
```
  dev v1.0.0...
Description: Development CLI for TimeWarp.Nuru

USAGE: dev [command] [options]

Options:
┌────────────────┬────────────────────────────────┐
│ --help, -h     │ Show this help message         │
...
└────────────────┴────────────────────────────────┘

Commands:
┌────────────────┬──────────────────────────────────────────────────┐
│ analyze        │ Run Roslynator analysis and fixes                │
...
└────────────────┴──────────────────────────────────────────────────┘
```

## Related Tasks

- #415 - Reorder help output (OPTIONS first, completed)
- #414 - Fix help output to use assembly name (completed)
- #413 - Polish help output with colors (completed)
