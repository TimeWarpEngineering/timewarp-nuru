# Add --category filter to verify-samples command

## Description

Add a `--category` option to the `dev verify-samples` command to allow filtering samples by type (fluent, endpoints, hybrid). This enables targeted sample verification during development and CI workflows.

## Current Behavior

```bash
$ dev verify-samples --help
verify-samples

  Verify all samples compile
```

Currently verifies **all** samples without filtering.

## Proposed Behavior

```bash
$ dev verify-samples --help
verify-samples

  Verify sample compilation

Options:
  -c, --category <category>  Filter by category: fluent, endpoints, hybrid
```

## Implementation

### Files to Modify

- `tools/dev-cli/commands/verify-samples-command.cs`

### Code Changes

```csharp
[NuruRoute("verify-samples", Description = "Verify sample compilation")]
internal sealed class VerifySamplesCommand : ICommand<Unit>
{
  [Option("category", "c", Description = "Filter by category: fluent, endpoints, hybrid")]
  public string? Category { get; set; }

  internal sealed class Handler : ICommandHandler<VerifySamplesCommand, Unit>
  {
    // ... existing code ...

    public async ValueTask<Unit> Handle(VerifySamplesCommand command, CancellationToken ct)
    {
      string samplesDir = Path.Combine(repoRoot, "samples");

      // Apply category filter if specified
      string? category = command.Category?.ToLowerInvariant();
      List<string> filteredRunfiles = FilterByCategory(runfileSamples, category, samplesDir);
      List<string> filteredProjects = FilterByCategory(projectSamples, category, samplesDir);

      // ... rest of existing logic
    }

    private static List<string> FilterByCategory(List<string> samples, string? category, string samplesDir)
    {
      if (string.IsNullOrEmpty(category))
        return samples;

      return samples.Where(s =>
      {
        string relativePath = Path.GetRelativePath(samplesDir, s);
        string[] parts = relativePath.Split(Path.DirectorySeparatorChar);

        return category switch
        {
          "fluent" => parts[0] == "fluent",
          "endpoints" => parts[0] == "endpoints",
          "hybrid" => parts[0] == "hybrid",
          _ => throw new ArgumentException($"Unknown category: {category}. Valid options: fluent, endpoints, hybrid")
        };
      }).ToList();
    }
  }
}
```

## Checklist

- [x] Add `[Option("category", "c", ...)]` property to `VerifySamplesCommand`
- [x] Implement `FilterByCategory()` helper method
- [x] Filter runfile samples by category
- [x] Filter project samples by category
- [x] Add proper error handling for invalid category
- [x] Test with each category:
  - [x] `dev verify-samples --category fluent`
  - [x] `dev verify-samples --category endpoints`
  - [x] `dev verify-samples --category hybrid`
  - [x] `dev verify-samples` (all - default behavior)
- [x] Test invalid category shows error message
- [x] Update help text for verify-samples command

## Test Cases

| Command | Expected |
|---------|----------|
| `dev verify-samples` | All samples verified |
| `dev verify-samples -c fluent` | Only `samples/fluent/*` |
| `dev verify-samples --category endpoints` | Only `samples/endpoints/*` |
| `dev verify-samples --category hybrid` | Only `samples/hybrid/*` |
| `dev verify-samples --category invalid` | Error: "Unknown category: invalid" |

## Notes

- Default behavior (no flag) must remain unchanged
- Categories map directly to directory structure under `samples/`
- Implementation follows existing `[Option]` pattern used in `ci-command.cs` and `format-command.cs`
- Error messages should be user-friendly and list valid options

## References

- Current implementation: `tools/dev-cli/commands/verify-samples-command.cs`
- Sample structure: `samples/` directory
- Reference commands: `tools/dev-cli/commands/format-command.cs` (has `[Option]` pattern)

## Results

Successfully implemented `--category` filter for `dev verify-samples` command.

### What Was Implemented
- Added `[Option("category", "c", Description = "Filter by category: fluent, endpoints, hybrid")]` property to `VerifySamplesCommand` class
- Added `FilterByCategory()` helper method that filters samples by directory name matching the category
- Applied filter to both runfile samples and project samples
- Added output line showing active category filter when specified
- Implemented case-insensitive category matching
- Added descriptive error for invalid category values

### Files Changed
- `tools/dev-cli/commands/verify-samples-command.cs` - 33 lines added (Option property + FilterByCategory method + filter application)

### Key Decisions
- Default behavior unchanged: no flag = verify all samples
- Categories map directly to directory names under `samples/`
- Error messages clearly list valid options
- Filter applied consistently to both runfile and project samples

### Test Results
| Command | Result |
|---------|--------|
| `verify-samples --category fluent` | ✅ 23 samples |
| `verify-samples --category endpoints` | ✅ 29 samples |
| `verify-samples --category hybrid` | ✅ 4 samples (all passed) |
| `verify-samples --category invalid` | ✅ Error: "Unknown category: invalid. Valid options: fluent, endpoints, hybrid" |
| `verify-samples` (no flag) | ✅ 98 samples (all categories, default behavior preserved) |

All tests passed. Implementation complete.
