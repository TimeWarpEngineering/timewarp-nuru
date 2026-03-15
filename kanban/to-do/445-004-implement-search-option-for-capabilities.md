# Implement --search option for --capabilities

## Description

Extend the generated `PrintCapabilities` method to support `--search` option that calls the nuru-search subprocess. Requires nuru-search tool to be installed (#445-003).

## Checklist

- [ ] Add --search option handling (nuru-search subprocess)
- [ ] Handle combined --group-filter + --search (pass to nuru-search)
- [ ] Add error message when nuru-search not installed
- [ ] Update help output to show new option
- [ ] Add tests for search error handling (nuru-search not installed)

## Notes

### Generated Code Pattern

Extended pattern with search:
```csharp
// With search (requires nuru-search)
if (routeArgs is ["--capabilities", "--search", var query] or ["--capabilities", "-s", var query])
{
  await SearchCapabilitiesAsync(app.Terminal, query: query);
}
// Both combined (pass to nuru-search)
else if (routeArgs has --group-filter and --search)
{
  await SearchCapabilitiesAsync(app.Terminal, query: query, groupFilter: group);
}
```

### Search Subprocess (Amuru)

```csharp
CommandOutput output = await Shell.Builder("nuru-search")
  .WithArguments("search", "--cli", cliName, "--version", version, "--query", query)
  .When(groupFilter is not null, b => b.WithArguments("--group", groupFilter))
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

### CLI Surface

```bash
ganda --capabilities --search "commit"                  # nuru-search
ganda --capabilities -s "push"                          # Short form
ganda --capabilities --group-filter "git" --search "push"  # Both → nuru-search
```

### Dependencies

- Requires #445-003 (nuru-search tool) to be complete
- Uses TimeWarp.Amuru for subprocess execution (added in #445-001)

### Parent Task

#445 - Add --search and --group-filter options to --capabilities
