# Add --search and --group-filter options to --capabilities

## Purpose

Extend the `--capabilities` output to support filtering and searching capabilities, making it easier for AI agents and users to discover relevant CLI commands.

## Checklist

- [x] Add TimeWarp.Amuru dependency and migrate clipboard code (#445-001)
- [x] Implement --group-filter option (#445-002)
- [x] Create timewarp-nuru-search companion tool with on-demand indexing (#445-003)
- [x] Implement --search option for --capabilities (#445-004)

## Notes

### What Was Actually Done (2026-03-16)

**Previously claimed done but was broken:**
- On-demand indexing was designed but never implemented
- Search just queried empty database and returned nothing
- Test file `capabilities-search.cs` was claimed to exist but didn't

**Now actually implemented:**
- Added `GetCliVersionAsync()` to `SearchIndex` to check if CLI is indexed
- Added `EnsureCliIndexedAsync()` to `SearchQuery.Handler` for on-demand indexing
- Added `FindCliInPath()` helper to locate CLI executables
- Search now auto-indexes CLIs when `--cli` is specified

### Verified Working

```bash
# On-demand indexing
$ nuru search --cli ganda --query "kanban"
Auto-indexed ganda v1.0.0-beta.21 (75 endpoints)
Found 13 result(s):

# End-to-end via ganda
$ ganda --capabilities --search "kanban"
Found 13 result(s):

# Combined with group filter
$ ganda --capabilities --search "create" --group-filter "kanban"
Found 2 result(s):

# Index status
$ nuru index list
Indexed CLIs (1):
  ganda v1.0.0-beta.21
    Endpoints: 75
```

### Test Results

- CI tests: 1103 passed, 7 skipped, 0 failed
- Build: 0 warnings, 0 errors

### Files Changed

- `source/timewarp-nuru-search/services/search-index.cs` - Added `GetCliVersionAsync()`
- `source/timewarp-nuru-search/endpoints/search-query.cs` - Added on-demand indexing logic

### Remaining Work

- [ ] Publish TimeWarp.Nuru.Search to NuGet (optional, works locally)
- [ ] Add unit tests for on-demand indexing (optional, verified manually)
