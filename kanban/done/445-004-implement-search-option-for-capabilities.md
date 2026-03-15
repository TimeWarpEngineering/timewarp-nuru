# Implement --search option for --capabilities

## Description

Extend the generated `PrintCapabilities` method to support `--search` option that calls the nuru-search subprocess. Requires nuru-search tool to be installed (#445-003).

## Checklist

- [x] Add --search option handling (nuru-search subprocess)
- [x] Handle combined --group-filter + --search (pass to nuru-search)
- [x] Add error message when nuru-search not installed
- [x] Update help output to show new option
- [x] Add tests for search error handling (nuru-search not installed)

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

## Implementation Plan

### Changes Summary

| File | Lines Changed | Description |
|------|---------------|-------------|
| `built-in-flags.cs` | +5 | Add `SearchForms` constant |
| `interceptor-emitter.cs` | +30 | Add `--search` pattern matching in `EmitBuiltInFlags` |
| `capabilities-emitter.cs` | +35 | Add `EmitSearchCapabilitiesAsync` method |
| `help-emitter.cs` | +2 | Add `--search` to help output |
| `capabilities-search.cs` | +80 | New test file |

### Key Design Decisions

1. **Search scope:** Pass `--cli <name>` to nuru-search to limit results to current CLI
2. **Group filter:** Pass `--group <filter>` to nuru-search for server-side filtering
3. **Flag order:** Support both `--search <q> --group-filter <g>` and `--group-filter <g> --search <q>`
4. **Error handling:** Show install instructions when nuru-search is not found

### Execution Order

1. Add `SearchForms` to `built-in-flags.cs`
2. Add `EmitSearchCapabilitiesAsync` to `capabilities-emitter.cs`
3. Update `Emit` in `capabilities-emitter.cs` to call the new method
4. Add pattern matching in `interceptor-emitter.cs` (BEFORE existing `--group-filter` block)
5. Update help output in `help-emitter.cs`
6. Create test file `capabilities-search.cs`
7. Run `ganda runfile cache --clear`
8. Run CI tests to verify

## Results

### What Was Implemented
- Added `--search` option to `--capabilities` that calls nuru-search subprocess
- Support for combined `--search` and `--group-filter` (both flag orders)
- Added `--group` option to nuru-search for server-side filtering
- Error handling with install instructions when nuru-search not found

### Files Changed
- `source/timewarp-nuru-analyzers/generators/models/built-in-flags.cs` - Added `SearchForms` constant
- `source/timewarp-nuru-analyzers/generators/emitters/capabilities-emitter.cs` - Added `EmitSearchCapabilities`
- `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` - Added `--search` pattern matching
- `source/timewarp-nuru-analyzers/generators/emitters/help-emitter.cs` - Added `--search` to help output
- `source/timewarp-nuru-search/endpoints/search-query.cs` - Added `--group` option
- `source/timewarp-nuru-search/services/search-index.cs` - Added group filtering
- `tests/timewarp-nuru-tests/capabilities/capabilities-search.cs` - New test file (6 tests)

### Key Decisions
- **Search scope:** Pass `--cli <name>` to nuru-search to limit results to current CLI
- **Group filter:** Pass `--group <filter>` to nuru-search for server-side filtering
- **Flag order:** Support both `--search <q> --group-filter <g>` and `--group-filter <g> --search <q>`
- **Error handling:** Wrapped nuru-search call in try-catch for `Win32Exception` to handle missing tool gracefully

### Test Outcomes
- CI tests: 1109 passed, 7 skipped, 0 failed
- Build: 0 warnings, 0 errors
