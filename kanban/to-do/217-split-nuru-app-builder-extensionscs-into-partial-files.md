# Split nuru-app-builder-extensions.cs into partial files

## Description

The `nuru-app-builder-extensions.cs` file (509 lines) is already a partial class containing version display, update checking, and SemVer comparison logic. These distinct features should be extracted into separate partial files.

**Location:** `source/timewarp-nuru/nuru-app-builder-extensions.cs`

## Parent

204-review-large-files-for-refactoring-opportunities

## Checklist

### File Creation
- [ ] Create `nuru-app-builder-extensions.version.cs` - Version route handler
- [ ] Create `nuru-app-builder-extensions.updates.cs` - Update checking and SemVer comparison
- [ ] Extract SemVer comparison to internal `sem-ver-comparer.cs` utility class

### Documentation
- [ ] Add `<remarks>` to main file listing all partial files
- [ ] Add XML summary to each new partial file

### Verification
- [ ] All tests pass
- [ ] Build succeeds
- [ ] Version and check-updates commands work correctly

## Notes

### Proposed Split

| New File | ~Lines | Content |
|----------|--------|---------|
| `.version.cs` | ~60 | `AddVersionRoute()`, `DisplayVersion()` |
| `.updates.cs` | ~160 | `AddCheckUpdatesRoute()`, `CheckForUpdatesAsync()`, `FetchGitHubReleasesAsync()`, `FindLatestRelease()` |
| `sem-ver-comparer.cs` | ~145 | `CompareVersions()`, `SplitVersion()`, `CompareBaseVersions()`, `ComparePrereleaseLabels()` - internal utility class |
| Main file | ~145 | `UseAllExtensions()`, core extension wiring |

### SemVer Comparison

The version comparison logic (~145 lines) is general-purpose and self-contained. Extract to an internal utility class:
```csharp
/// <summary>
/// Internal utility for semantic version comparison.
/// </summary>
internal static class SemVerComparer
{
    public static int Compare(string version1, string version2) { ... }
}
```

Keep internal since Nuru isn't a utility library.

### GitHub Integration

The update checking logic includes:
- HTTP client usage for GitHub API
- `[GeneratedRegex]` for GitHub URL parsing
- Release JSON parsing

This could benefit from better testability via dependency injection of HttpClient.

### Reference Pattern

Follow established partial class conventions with XML documentation.
