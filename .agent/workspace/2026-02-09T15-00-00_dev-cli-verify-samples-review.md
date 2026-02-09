# Dev-CLI verify-samples Review: Adding --category Filter

## Executive Summary

The `verify-samples` command currently verifies **all** samples in the repository. This review analyzes the implementation and proposes adding a `--category` option to filter verification by sample type (fluent, endpoints, hybrid, legacy), enabling targeted validation during development workflows.

## Current Implementation Analysis

### File Location
`tools/dev-cli/commands/verify-samples-command.cs`

### How It Works

1. **Sample Discovery**: Scans the `samples/` directory recursively
2. **Runfile Samples**: Finds `*.cs` files with shebang containing "dotnet"
3. **Project Samples**: Finds `*.csproj` files
4. **Exclusions**: Skips directories starting with underscore (`_`)
5. **Build**: Uses `DotNet.Build()` with Release configuration

### Current Sample Categories

| Category | Path | Count | Description |
|----------|------|-------|-------------|
| **fluent** | `samples/fluent/` | 22 | Fluent API pattern samples |
| **endpoints** | `samples/endpoints/` | 30+ | Endpoint-based pattern samples |
| **hybrid** | `samples/hybrid/` | 3 | Mixed endpoint + fluent patterns |
| **legacy** | `samples/01-hello-world/` etc | 40+ | Original numbered samples |

### Sample Structure
```
samples/
├── fluent/                    # Fluent API samples
│   ├── 01-hello-world/
│   ├── 02-calculator/
│   ├── 03-syntax/
│   ├── ...
│   └── 12-runtime-di/
├── endpoints/                 # Endpoint-based samples  
│   ├── 01-hello-world/
│   ├── 02-calculator/
│   ├── 03-syntax/
│   ├── ...
│   └── 13-runtime-di/
├── hybrid/                    # Hybrid pattern samples
│   └── 01-migration/
└── 01-hello-world/           # Legacy samples (numbered)
    ├── 01-hello-world-lambda.cs
    ├── 02-hello-world-method.cs
    └── 03-hello-world-endpoint.cs
```

## Proposed Enhancement

### Option A: `--category` Filter (Recommended)

Add a `--category` option to filter samples by type:

```bash
dev verify-samples                    # All samples (default)
dev verify-samples --category fluent  # Only fluent samples
dev verify-samples --category endpoints  # Only endpoint samples
dev verify-samples --category hybrid     # Only hybrid samples
dev verify-samples --category legacy     # Only legacy numbered samples
```

### Option B: `--path` Parameter

Allow specifying a custom path:

```bash
dev verify-samples --path samples/fluent/01-hello-world
dev verify-samples --path ./my-custom-samples
```

## Implementation Approach

### Code Changes Required

```csharp
// tools/dev-cli/commands/verify-samples-command.cs

[NuruRoute("verify-samples", Description = "Verify sample compilation")]
internal sealed class VerifySamplesCommand : ICommand<Unit>
{
  [Option("category", "c", Description = "Filter by category: fluent, endpoints, hybrid, or legacy")]
  public string? Category { get; set; }

  internal sealed class Handler : ICommandHandler<VerifySamplesCommand, Unit>
  {
    // ... existing constructor

    public async ValueTask<Unit> Handle(VerifySamplesCommand command, CancellationToken ct)
    {
      // ... existing setup code

      // Apply category filter
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
          "legacy" => char.IsDigit(parts[0][0]), // Starts with number
          _ => true
        };
      }).ToList();
    }
  }
}
```

### Pattern Reference

The `[Option]` attribute pattern is already established in the codebase:

**From `format-command.cs`**:
```csharp
[Option("fix", "f", Description = "Fix formatting issues instead of just checking")]
public bool Fix { get; set; }
```

**From `ci-command.cs`**:
```csharp
[Option("mode", "m", Description = "CI mode: pr, merge, or release")]
public string? Mode { get; set; }
```

## Benefits

1. **Faster Development Cycles**: Verify only the category you're working on
2. **CI Efficiency**: Targeted sample verification in specific workflows
3. **Parallel Testing**: Different categories can be verified in parallel jobs
4. **Clearer Output**: Filtered results show only relevant samples

## Usage Examples

### During Development
```bash
# Working on fluent API samples? Verify only those:
dev verify-samples --category fluent

# Test endpoint pattern changes:
dev verify-samples --category endpoints

# Quick check before commit:
dev verify-samples --category hybrid
```

### In CI/CD Workflows
```yaml
# .github/workflows/ci.yml
- name: Verify Fluent Samples
  run: ./bin/dev verify-samples --category fluent

- name: Verify Endpoint Samples  
  run: ./bin/dev verify-samples --category endpoints

- name: Verify All Samples
  if: github.event_name == 'release'
  run: ./bin/dev verify-samples
```

## Considerations

### Alternative: Multiple Categories
Support multiple categories with repeated options:
```bash
dev verify-samples --category fluent --category hybrid
```

Implementation would use `[Option("category", ..., AllowMultiple = true)]` if supported.

### Path-Based Filtering
Could also support glob patterns:
```bash
dev verify-samples samples/fluent/*
dev verify-samples samples/endpoints/01-hello-world/
```

## References

- Current implementation: `tools/dev-cli/commands/verify-samples-command.cs`
- Option pattern: `tools/dev-cli/commands/ci-command.cs`, `tools/dev-cli/commands/format-command.cs`
- Route syntax: [TimeWarp.Nuru Options Pattern](https://github.com/TimeWarpEngineering/timewarp-nuru)
- Sample structure: `samples/` directory
