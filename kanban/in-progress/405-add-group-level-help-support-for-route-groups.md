# Add Group-Level Help Support for Route Groups

## Description

Currently, Nuru auto-generates help for individual routes (e.g., `worktree list --help`) and global app help (e.g., `ganda --help`), but **does not support group-level help** (e.g., `ganda worktree --help`).

### Current Behavior

```bash
$ ganda worktree list --help    # ✅ Shows help for "worktree list" command
$ ganda --help                  # ✅ Shows global help for all commands
$ ganda worktree --help         # ❌ "Unknown command" - NO group-level help!
```

### Expected Behavior

```bash
$ ganda worktree --help
# Should show:
#   worktree add {branch}     Add a new worktree
#   worktree list             List worktrees
#   worktree remove [branch]  Remove an worktree
#   worktree wip              Show work-in-progress worktrees
```

### Technical Analysis

**Root Cause:** The `RouteHelpEmitter.GetLiteralPrefix()` method (line 68-92 in `route-help-emitter.cs`) generates help check patterns that include the full route path (group prefix + command literals), but there's no code to handle the intermediate "group only" case.

**Current Generated Code Pattern:**
```csharp
// For route: worktree list
if (routeArgs is ["worktree", "list", "--help" or "-h"])
{
  // Show help for "worktree list"
}
```

**Missing Generated Code:**
```csharp
// For group: worktree (with routes: add, list, remove, wip)
if (routeArgs is ["worktree", "--help" or "-h"])
{
  // Show group-level help listing all worktree subcommands
}
```

### Files to Modify

| File | Change |
|------|--------|
| `source/timewarp-nuru-analyzers/generators/emitters/route-help-emitter.cs` | Add `EmitGroupHelpCheck()` method |
| `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs` | Emit group help checks before route matching |
| `source/timewarp-nuru-analyzers/generators/models/` | May need `GroupDefinition` model to track groups |

### Implementation Approach

1. **Collect Groups:** During route discovery, identify unique `GroupPrefix` values
2. **Emit Group Help:** For each group, emit a pattern match for `["{group}", "--help" or "-h"]`
3. **Generate Content:** List all routes within that group with descriptions
4. **Priority:** Group help checks should come AFTER per-route help but BEFORE the "unknown command" handler

### Test Requirements

New test file: `tests/timewarp-nuru-tests/help/help-04-group-level-help.cs`

```csharp
public static async Task Should_show_group_level_help()
{
  using TestTerminal terminal = new();
  NuruApp app = NuruApp.CreateBuilder()
    .UseTerminal(terminal)
    .WithGroupPrefix("worktree")
      .Map("add {branch}").WithHandler((string branch) => "added").WithDescription("Add worktree").Done()
      .Map("list").WithHandler(() => "listed").WithDescription("List worktrees").Done()
    .Done()
    .Build();

  int exitCode = await app.RunAsync(["worktree", "--help"]);

  exitCode.ShouldBe(0);
  terminal.OutputContains("worktree add").ShouldBeTrue();
  terminal.OutputContains("worktree list").ShouldBeTrue();
  terminal.OutputContains("Add worktree").ShouldBeTrue();
  terminal.OutputContains("List worktrees").ShouldBeTrue();
}
```

### Benefits

1. **Better UX** - Users can discover subcommands naturally
2. **Consistency** - Matches behavior of other CLI tools (git, gh, kubectl)
3. **No Breaking Changes** - Purely additive feature

### Related

- Task #356 (Per-route help - already implemented)
- timewarp-ganda worktree commands
- GitHub CLI pattern: `gh repo --help` shows repo subcommands

## Checklist

- [ ] `ganda worktree --help` shows all worktree subcommands
- [ ] Works for all route groups (workspace, repo, kanban, etc.)
- [ ] Includes group description if available
- [ ] Shows subcommand descriptions
- [ ] Tests cover nested groups (e.g., `workspace repo --help`)
- [ ] No regression in existing help functionality
- [ ] Add `EmitGroupHelpCheck()` method to `route-help-emitter.cs`
- [ ] Update `route-matcher-emitter.cs` to emit group help checks
- [ ] Create test file: `tests/timewarp-nuru-tests/help/help-04-group-level-help.cs`

## Notes

## Implementation Plan

### Step 1: Add Group-Level Help Emitter (`route-help-emitter.cs`)

**New Method:**

```csharp
/// <summary>
/// Emits group-level help checks for each unique group prefix.
/// This enables "group --help" to show all subcommands in that group.
/// </summary>
public static void EmitGroupHelpChecks(
  StringBuilder sb,
  IEnumerable<RouteDefinition> routes,
  int indent = 4)
{
  string indentStr = new(' ', indent);

  // Group routes by their GroupPrefix
  Dictionary<string, List<RouteDefinition>> groups = new(StringComparer.OrdinalIgnoreCase);
  foreach (RouteDefinition route in routes)
  {
    if (!string.IsNullOrEmpty(route.GroupPrefix))
    {
      if (!groups.TryGetValue(route.GroupPrefix, out var list))
      {
        list = [];
        groups[route.GroupPrefix] = list;
      }
      list.Add(route);
    }
  }

  // Emit help check for each group
  foreach (var kvp in groups)
  {
    string groupPrefix = kvp.Key;
    List<RouteDefinition> groupRoutes = kvp.Value;

    // Split prefix into words (e.g., "admin config" -> ["admin", "config"])
    string[] prefixWords = groupPrefix.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    // Build pattern: ["word1", "word2", ..., "--help" or "-h"]
    StringBuilder patternBuilder = new();
    patternBuilder.Append('[');
    for (int i = 0; i < prefixWords.Length; i++)
    {
      if (i > 0) patternBuilder.Append(", ");
      patternBuilder.Append($"\"{EscapeString(prefixWords[i])}\"");
    }
    string helpFormsPattern = string.Join(" or ", BuiltInFlags.HelpForms.Select(f => $"\"{f}\""));
    patternBuilder.Append($", {helpFormsPattern}]");

    sb.AppendLine();
    sb.AppendLine($"{indentStr}// Group-level help: {groupPrefix} --help");
    sb.AppendLine($"{indentStr}if (routeArgs is {patternBuilder})");
    sb.AppendLine($"{indentStr}{{");

    // Emit help content for all routes in this group
    EmitGroupHelpContent(sb, groupPrefix, groupRoutes, indent + 2);

    sb.AppendLine($"{indentStr}  return 0;");
    sb.AppendLine($"{indentStr}}}");
  }
}
```

**New Method for Content Generation:**

```csharp
private static void EmitGroupHelpContent(
  StringBuilder sb,
  string groupPrefix,
  List<RouteDefinition> routes,
  int indent)
{
  string indentStr = new(' ', indent);

  sb.AppendLine($"{indentStr}app.Terminal.WriteLine(\"{EscapeString(groupPrefix)} commands:\");");
  sb.AppendLine($"{indentStr}app.Terminal.WriteLine();");

  foreach (RouteDefinition route in routes)
  {
    string pattern = BuildPatternDisplay(route);
    string paddedPattern = pattern.PadRight(25);
    string description = route.Description ?? string.Empty;
    sb.AppendLine($"{indentStr}app.Terminal.WriteLine(\"  {EscapeString(paddedPattern)} {EscapeString(description)}\");");
  }
}
```

### Step 2: Modify Interceptor Emitter (`interceptor-emitter.cs`)

**In `EmitMethodBody()` around line 588, add:**

```csharp
// Group-level help checks (before user routes so groups get priority over routes)
// Example: "worktree --help" shows all worktree subcommands
RouteHelpEmitter.EmitGroupHelpChecks(sb, allRoutesOrdered, 4);
sb.AppendLine();
```

### Step 3: Test Requirements

**File:** `tests/timewarp-nuru-tests/help/help-04-group-level-help.cs`

Create tests for:
- Basic group help (`worktree --help`)
- Short form (`config -h`)
- Non-group command (single route --help)
- Nested groups (`admin config --help`)
- Per-route help still works (`worktree add --help`)

### Design Decisions

1. **Show all commands under group prefix** (recursive, all nesting levels)
2. **Use full path in header** (e.g., "admin config commands:")
3. **Group help takes precedence** over route matching for exact match patterns

### Files Modified

| File | Change |
|------|--------|
| `route-help-emitter.cs` | Add `EmitGroupHelpChecks()` and `EmitGroupHelpContent()` methods |
| `interceptor-emitter.cs` | Add call to `EmitGroupHelpChecks()` |
| New test file | `help-04-group-level-help.cs` |