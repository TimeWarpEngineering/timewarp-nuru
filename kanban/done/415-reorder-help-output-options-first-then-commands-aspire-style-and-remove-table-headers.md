# Reorder help output: OPTIONS first, then COMMANDS (Aspire style) and remove table headers

## Description

Current help output puts COMMANDS first, then OPTIONS at bottom. This task changes to Aspire-style: OPTIONS first (reference-able at bottom of screen), then COMMANDS. Also removes redundant "Command"/"Description" table headers.

## Current Behavior

```
COMMANDS
┌────────────────┬──────────────────────────────────────────────────┐
│ Command        │ Description                                      │  ← Redundant
│ analyze        │ Run Roslynator analysis and fixes                 │
│ build          │ Build all TimeWarp.Nuru projects                 │
└────────────────┴──────────────────────────────────────────────────┘

OPTIONS                                                          ← At bottom
┌────────────────┬────────────────────────────────┐
│ Option         │ Description                    │  ← Redundant
│ --help, -h     │ Show this help message         │
└────────────────┴────────────────────────────────┘
```

## Expected Behavior

```
OPTIONS                                                          ← At TOP (Aspire style)
┌────────────────┬────────────────────────────────┐
│ --help, -h     │ Show this help message         │  ← No headers
│ --version      │ Show version information       │
│ --capabilities │ Show capabilities for AI tools │
└────────────────┴────────────────────────────────┘

COMMANDS                                                         ← After OPTIONS
┌────────────────┬──────────────────────────────────────────────────┐
│ analyze        │ Run Roslynator analysis and fixes                 │  ← No headers
│ build          │ Build all TimeWarp.Nuru projects                  │
│ clean          │ Clean solution and build artifacts                │
│ format         │ Check or fix code formatting                       │
│ test           │ Run the CI test suite                             │
│ verify-samples │ Verify all samples compile                        │
└────────────────┴──────────────────────────────────────────────────┘
```

## Changes Required

### Update help-emitter.cs

1. **Reorder Emit() method**: Call `EmitOptions()` before `EmitCommands()`

```csharp
public static void Emit(StringBuilder sb, AppModel model, string methodSuffix = "")
{
  sb.AppendLine($"  private static void PrintHelp{methodSuffix}(ITerminal terminal)");
  sb.AppendLine("  {");

  EmitHeader(sb, model);
  EmitUsage(sb);
  EmitOptions(sb);      // ← OPTIONS first (Aspire style)
  EmitCommands(sb, model);  // ← Then COMMANDS

  sb.AppendLine("  }");
}
```

2. **Add `.HideHeaders()` to command table** in `EmitCommands()`:
```csharp
sb.AppendLine("    terminal.WriteTable(table => table");
sb.AppendLine("      .AddColumn(\"Command\")");
sb.AppendLine("      .AddColumn(\"Description\")");
// ... rows ...
sb.AppendLine("      .HideHeaders()");  // ← Remove headers
sb.AppendLine("    );");
```

3. **Add `.HideHeaders()` to options table** in `EmitOptions()`:
```csharp
sb.AppendLine("    terminal.WriteTable(table => table");
sb.AppendLine("      .AddColumn(\"Option\")");
sb.AppendLine("      .AddColumn(\"Description\")");
// ... rows ...
sb.AppendLine("      .HideHeaders()");  // ← Remove headers
sb.AppendLine("    );");
```

## Files to Modify

| File | Changes |
|------|---------|
| `help-emitter.cs` | Reorder Emit() to call EmitOptions() before EmitCommands(), add `.HideHeaders()` to both tables |

## Checklist

- [ ] Reorder Emit() to call EmitOptions() before EmitCommands()
- [ ] Add `.HideHeaders()` to command table
- [ ] Add `.HideHeaders()` to options table
- [ ] Run existing tests to verify no regressions
- [ ] Verify output visually with `dev --help`

## Testing

Test with:
```bash
dev --help
```

Expected output:
1. OPTIONS table first (no "Option"/"Description" headers)
2. COMMANDS table second (no "Command"/"Description" headers)

## Reference

**Aspire CLI help output (OPTIONS first):**
```
Options:
  -d, --debug          Enable debug logging to the console.
  --non-interactive    Run the command in non-interactive mode
  -?, -h, --help       Show help and usage information
  --version            Show version information

Commands:
  new                Create a new Aspire project.
  init               Initialize Aspire support
  run                Run an Aspire apphost
```

## Related Tasks

- #413 - Polish help output with colors, spacing, command-only names
- #414 - Fix help output to use assembly name when WithName() not called
