# Polish help output: colon format with newlines and indentation

## Description

Refine help output format to use colon-style headers where the value appears on a new line with indentation:
```
Description:
  Development CLI for TimeWarp.Nuru

Usage:
  dev [command] [options]
```

Also change table headers from UPPERCASE to PascalCase with colons.

## Current Behavior

```
  dev v1.0.0...
Development CLI for TimeWarp.Nuru

USAGE: dev [command] [options]

OPTIONS
┌────────────────┬────────────────────────────────┐
│ --help, -h     │ Show this help message         │
└────────────────┴────────────────────────────────┘
COMMANDS
┌────────────────┬──────────────────────────────────────────────────┐
│ analyze        │ Run Roslynator analysis and fixes                │
└────────────────┴──────────────────────────────────────────────────┘
```

**Issues:**
- Description inline with header (not colon format)
- USAGE inline with header (not colon format)  
- Headers are "OPTIONS", "COMMANDS" (UPPERCASE)
- No blank line between OPTIONS and COMMANDS tables

## Expected Behavior (Colon Format)

```
  dev v1.0.0...
Description:
  Development CLI for TimeWarp.Nuru

Usage:
  dev [command] [options]

Options:
┌────────────────┬────────────────────────────────┐
│ --help, -h     │ Show this help message         │
└────────────────┴────────────────────────────────┘

Commands:
┌────────────────┬──────────────────────────────────────────────────┐
│ analyze        │ Run Roslynator analysis and fixes                │
└────────────────┴──────────────────────────────────────────────────┘
```

## Changes Required

### Update help-emitter.cs

1. **Change EmitHeader() to colon format with newline + indent:**

```csharp
private static void EmitHeader(StringBuilder sb, AppModel model)
{
  // App name with version in cyan bold
  string version = model.Version ?? "1.0.0";
  sb.AppendLine($"    terminal.WriteLine($\"  {{__appName}} v{version}\".BrightCyan().Bold());");

  // Description in colon format with newline + indent
  if (model.Description is not null)
  {
    sb.AppendLine("    terminal.WriteLine(\"Description:\".Gray());");
    sb.AppendLine($"    terminal.WriteLine($\"  {EscapeString(model.Description)}\".Gray());");
  }

  sb.AppendLine("    terminal.WriteLine();");
}
```

2. **Change EmitUsage() to colon format:**

```csharp
private static void EmitUsage(StringBuilder sb)
{
  sb.AppendLine("    terminal.WriteLine(\"Usage:\".Yellow());");  // Colon format
  sb.AppendLine("    terminal.WriteLine(\"  {__appName} [command] [options]\".Yellow());");  // Indented
  sb.AppendLine("    terminal.WriteLine();");
}
```

3. **Change EmitOptions() to PascalCase with colon:**

```csharp
private static void EmitOptions(StringBuilder sb)
{
  sb.AppendLine("    terminal.WriteLine(\"Options:\".Cyan().Bold());");  // PascalCase with colon
  sb.AppendLine("    terminal.WriteTable(table => table");
  sb.AppendLine("      .AddColumn(\"Option\")");
  sb.AppendLine("      .AddColumn(\"Description\")");
  sb.AppendLine("      .AddRow(\"--help, -h\", \"Show this help message\")");
  sb.AppendLine("      .AddRow(\"--version\", \"Show version information\")");
  sb.AppendLine("      .AddRow(\"--capabilities\", \"Show capabilities for AI tools\")");
  sb.AppendLine("      .HideHeaders()");
  sb.AppendLine("    );");
  sb.AppendLine("    terminal.WriteLine();");  // Blank line after OPTIONS
}
```

4. **Change EmitCommands() to PascalCase with colon:**

```csharp
private static void EmitCommands(StringBuilder sb, AppModel model)
{
  if (!model.HasRoutes)
  {
    return;
  }

  IEnumerable<IGrouping<string, RouteDefinition>> groups = model.Routes
    .GroupBy(r => r.GroupPrefix ?? "")
    .OrderBy(g => g.Key);

  foreach (IGrouping<string, RouteDefinition> group in groups)
  {
    string categoryName = string.IsNullOrEmpty(group.Key)
      ? "Commands"
      : group.Key;  // Keep original case

    // PascalCase header with colon
    sb.AppendLine($"    terminal.WriteLine(\"{EscapeString(categoryName)}:\".Cyan().Bold());");

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
| `help-emitter.cs` | 1. Colon format for Description (newline + indent), 2. Colon format for Usage (newline + indent), 3. PascalCase headers with colons for Options/Commands |

## Checklist

- [ ] Change Description to colon format ("Description:" + newline + "  value")
- [ ] Change Usage to colon format ("Usage:" + newline + "  value")
- [ ] Change "OPTIONS" to "Options:" (PascalCase with colon)
- [ ] Change "COMMANDS" to "Commands:" (PascalCase with colon)
- [ ] Add blank line after OPTIONS table
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
Description:
  Development CLI for TimeWarp.Nuru

Usage:
  dev [command] [options]

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

## Reference

Many CLIs use this colon + newline + indent pattern:
```
Description:
  This is a long description that spans
  multiple lines with proper indentation.

Usage:
  myapp <command> [options]
```

## Related Tasks

- #415 - Reorder help output (OPTIONS first, completed)
- #414 - Fix help output to use assembly name (completed)
- #413 - Polish help output with colors (completed)
