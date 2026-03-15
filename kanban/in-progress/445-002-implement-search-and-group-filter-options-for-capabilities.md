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

## Implementation Plan

### Key Decisions
- **Case sensitivity:** Case-insensitive matching
- **Filter output:** Original string format: `{"group": "kanban"}`

### Step 1: Extend CapabilitiesResponse DTO

**File:** `source/timewarp-nuru-analyzers/generators/emitters/capabilities-emitter.cs`

Add filter metadata class:
```csharp
public sealed class CapabilitiesFilter
{
  public string? Group { get; init; }  // Original string, not array
}

public sealed class CapabilitiesResponse
{
  public string Name { get; init; } = "";
  public string Version { get; init; } = "";
  public string? Description { get; init; }
  public CapabilitiesFilter? Filter { get; init; }  // NEW
  public IReadOnlyList<EndpointInfo> Endpoints { get; init; } = [];
}
```

### Step 2: Update PrintCapabilities Generation

**File:** `source/timewarp-nuru-analyzers/generators/emitters/capabilities-emitter.cs`

Add overload with groupFilter parameter:
```csharp
private static void PrintCapabilities(ITerminal terminal, string? groupFilter = null)
{
  // Filter endpoints if groupFilter provided
  var endpoints = groupFilter is null 
    ? Endpoints 
    : FilterByGroup(Endpoints, groupFilter);
  
  // Build response with filter metadata
  var response = new CapabilitiesResponse
  {
    Name = AppName,
    Version = Version,
    Filter = groupFilter is null ? null : new CapabilitiesFilter { Group = groupFilter },
    Endpoints = endpoints
  };
  
  terminal.WriteLine(JsonSerializer.Serialize(response));
}

private static IReadOnlyList<EndpointInfo> FilterByGroup(
  IReadOnlyList<EndpointInfo> endpoints, 
  string groupFilter)
{
  // Filter endpoints by GroupPath prefix match (case-insensitive)
  string[] groupParts = groupFilter.Split('.');
  return endpoints.Where(e => 
    e.GroupPath.Length >= groupParts.Length &&
    groupParts.Zip(e.GroupPath).All(p => 
      string.Equals(p.First, p.Second, StringComparison.OrdinalIgnoreCase)))
    .ToList();
}
```

### Step 3: Update Interceptor Emitter

**File:** `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs`

Add pattern matching for --group-filter:
```csharp
// With group filter (local)
if (routeArgs is ["--capabilities", "--group-filter", var group] or ["--capabilities", "-g", var group])
{
  PrintCapabilities(app.Terminal, groupFilter: group);
  return 0;
}
// No options
else if (routeArgs is ["--capabilities"])
{
  PrintCapabilities(app.Terminal);
  return 0;
}
```

### Step 4: Update Help Emitter

**File:** `source/timewarp-nuru-analyzers/generators/emitters/help-emitter.cs`

Add --group-filter to capabilities help text.

### Step 5: Add Tests

**File:** `tests/timewarp-nuru-tests/capabilities/capabilities-group-filter.cs`

Test cases:
1. `Should_filter_endpoints_by_single_segment_group_prefix`
2. `Should_filter_endpoints_by_multi_segment_group_prefix`
3. `Should_return_empty_endpoints_when_no_match`
4. `Should_include_filter_metadata_in_output`
5. `Should_support_short_form_flag`
6. `Should_match_exact_group_path`
7. `Should_match_partial_group_path_prefix`
8. `Should_match_case_insensitively`

### Step 6: Run Tests

```bash
ganda runfile cache --clear
dotnet run tests/ci-tests/run-ci-tests.cs
```
