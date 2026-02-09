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

- [ ] Add `[Option("category", "c", ...)]` property to `VerifySamplesCommand`
- [ ] Implement `FilterByCategory()` helper method
- [ ] Filter runfile samples by category
- [ ] Filter project samples by category
- [ ] Add proper error handling for invalid category
- [ ] Test with each category:
  - [ ] `dev verify-samples --category fluent`
  - [ ] `dev verify-samples --category endpoints`
  - [ ] `dev verify-samples --category hybrid`
  - [ ] `dev verify-samples` (all - default behavior)
- [ ] Test invalid category shows error message
- [ ] Update help text for verify-samples command

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
