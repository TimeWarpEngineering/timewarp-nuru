# Add --check-updates command

## Description

Add a `--check-updates` command that checks if a newer version is available on GitHub by comparing the current version against the latest release. This provides users with a simple way to know when updates are available without requiring network calls on `--version`.

## Requirements

### Core Functionality

1. **New route**: `--check-updates` (no alias)
2. **Reads from assembly metadata**:
   - `RepositoryUrl` - GitHub repo URL (from `<RepositoryUrl>` in csproj)
   - `InformationalVersion` - Current version
3. **Calls GitHub API**: `GET https://api.github.com/repos/{owner}/{repo}/releases`
4. **Pre-release handling**:
   - If current version is **stable** (no `-`): only compare against stable releases (`"prerelease": false`)
   - If current version is **pre-release** (contains `-`): compare against all releases
5. **Version comparison**: SemVer comparison with `published_at` date fallback
6. **User-Agent header**: Use the user's app name from entry assembly

### Output Format

**Up to date (green):**
```
✓ You are on the latest version
```

**Update available (yellow):**
```
⚠ A newer version is available: 1.2.0
  Released: 2025-12-01
  https://github.com/org/repo/releases/tag/v1.2.0
```

**Error cases:**
```
Unable to check for updates: <reason>
```
- Network error
- RepositoryUrl not configured
- No releases found
- Non-GitHub repository

### AOT Compatibility

- Use `System.Text.Json` with source generators for JSON parsing
- Create minimal `GitHubRelease` record with only needed fields:
  - `tag_name`
  - `prerelease`
  - `published_at`
  - `html_url`

### Configuration

- Add `DisableCheckUpdatesRoute` to `NuruAppOptions` (separate from `DisableVersionRoute`)
- Register route in `UseAllExtensions()` when not disabled

## Checklist

### Implementation
- [ ] Add `DisableCheckUpdatesRoute` property to `NuruAppOptions`
- [ ] Create `GitHubRelease` record for JSON deserialization
- [ ] Create `JsonSerializerContext` for AOT-compatible JSON parsing
- [ ] Implement `CheckForUpdates()` async handler
- [ ] Parse GitHub owner/repo from `RepositoryUrl`
- [ ] Implement SemVer parsing and comparison
- [ ] Implement pre-release filtering logic
- [ ] Add fallback to `published_at` date comparison
- [ ] Register `--check-updates` route in `UseAllExtensions()`
- [ ] Add ANSI color output (green ✓, yellow ⚠)

### Error Handling
- [ ] Handle missing `RepositoryUrl` gracefully
- [ ] Handle network errors gracefully
- [ ] Handle non-GitHub URLs gracefully
- [ ] Handle empty releases list
- [ ] Handle rate limiting (optional: inform user)

### Testing
- [ ] Test with stable version against stable releases
- [ ] Test with pre-release version against all releases
- [ ] Test version comparison logic
- [ ] Test error handling scenarios

## Notes

### GitHub API Response (relevant fields)

```json
{
  "tag_name": "v3.0.0-beta.18",
  "prerelease": true,
  "published_at": "2025-12-09T13:15:28Z",
  "html_url": "https://github.com/org/repo/releases/tag/v3.0.0-beta.18"
}
```

### File locations

- Main implementation: `source/timewarp-nuru/nuru-app-builder-extensions.cs`
- Options: `source/timewarp-nuru/nuru-app-options.cs`
- JSON types: Consider new file `source/timewarp-nuru/github-release.cs` or inline

### Version Comparison Strategy

1. Strip `v` prefix from `tag_name`
2. Parse as SemVer: `Major.Minor.Patch[-prerelease]`
3. Compare: Major → Minor → Patch → prerelease (none > any)
4. Fallback to `published_at` if parsing fails

### Pre-release Detection

- Current version contains `-` → pre-release (e.g., `1.0.0-beta.5`)
- Current version has no `-` → stable (e.g., `1.0.0`)
