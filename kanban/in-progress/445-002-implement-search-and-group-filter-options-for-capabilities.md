# Implement --group-filter option for --capabilities

## Description

Extend the generated `PrintCapabilities` method to support `--group-filter` option for local filtering by GroupPath prefix. This is a standalone feature with no external dependencies.

## Checklist

- [ ] Extend CapabilitiesResponse DTO with filter metadata
- [ ] Add --group-filter option handling (local filtering)
- [ ] Update help output to show new option
- [ ] Add tests for group-filter functionality

## Notes

### Generated Code Pattern

Current built-in flag handling:
```csharp
if (routeArgs is ["--capabilities"])
{
  PrintCapabilities(app.Terminal);
}
```

Extended pattern with group-filter:
```csharp
// With group filter (local)
if (routeArgs is ["--capabilities", "--group-filter", var group] or ["--capabilities", "-g", var group])
{
  PrintCapabilities(app.Terminal, groupFilter: group);
}
// No options
else if (routeArgs is ["--capabilities"])
{
  PrintCapabilities(app.Terminal);
}
```

### Local Group Filter Logic

```csharp
// Filter endpoints by GroupPath prefix match
string[] groupParts = group.Split('.');
var filtered = endpoints.Where(e => 
  e.GroupPath.Length >= groupParts.Length &&
  groupParts.Zip(e.GroupPath).All(p => p.First == p.Second));
```

### Output Format (when filtered)

```json
{
  "name": "ganda",
  "version": "1.2.3",
  "filter": {
    "group": "kanban"
  },
  "endpoints": [...]
}
```

### CLI Surface

```bash
ganda --capabilities                         # Full
ganda --capabilities --group-filter "kanban" # Local filter
ganda --capabilities -g "git"                # Short form
```

### Parent Task

#445 - Add --search and --group-filter options to --capabilities
