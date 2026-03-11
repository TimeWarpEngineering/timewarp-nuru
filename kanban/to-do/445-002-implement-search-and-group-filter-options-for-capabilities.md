# Implement --search and --group-filter options for --capabilities

## Description

Extend the generated `PrintCapabilities` method to support `--group-filter` (local) and `--search` (nuru-search subprocess) options.

## Checklist

- [ ] Extend CapabilitiesResponse DTO with filter metadata
- [ ] Add --group-filter option handling (local filtering)
- [ ] Add --search option handling (nuru-search subprocess)
- [ ] Handle combined --group-filter + --search (pass to nuru-search)
- [ ] Add error message when nuru-search not installed
- [ ] Update help output to show new options
- [ ] Add tests for group-filter functionality
- [ ] Add tests for search error handling (nuru-search not installed)

## Notes

### Generated Code Pattern

Current built-in flag handling:
```csharp
if (routeArgs is ["--capabilities"])
{
  PrintCapabilities(app.Terminal);
}
```

Extended pattern with options:
```csharp
// With group filter only (local)
if (routeArgs is ["--capabilities", "--group-filter", var group] or ["--capabilities", "-g", var group])
{
  PrintCapabilities(app.Terminal, groupFilter: group);
}
// With search (requires nuru-search)
else if (routeArgs is ["--capabilities", "--search", var query] or ["--capabilities", "-s", var query])
{
  await SearchCapabilitiesAsync(app.Terminal, query: query);
}
// Both combined (pass to nuru-search)
else if (routeArgs has --group-filter and --search)
{
  await SearchCapabilitiesAsync(app.Terminal, query: query, groupFilter: group);
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
var filtered = endpoints.Where(e => 
  e.GroupPath.Length >= groupParts.Length &&
  groupParts.Zip(e.GroupPath).All(p => p.First == p.Second));
```

### Search Subprocess (Amuru)

```csharp
CommandOutput output = await Shell.Builder("nuru-search")
  .WithArguments("search", "--cli", cliName, "--version", version, "--query", query)
  .WithNoValidation()
  .CaptureAsync();

if (!output.Success)
{
  terminal.WriteLine("Search requires timewarp-nuru-search to be installed.");
  terminal.WriteLine("Install with: dotnet tool install --global TimeWarp.Nuru.Search");
}
else
{
  terminal.WriteLine(output.Stdout);
}
```

### Error Message

```
Search requires timewarp-nuru-search to be installed.
Install with: dotnet tool install --global TimeWarp.Nuru.Search
```

### Parent Task

#445 - Add --search and --group-filter options to --capabilities
