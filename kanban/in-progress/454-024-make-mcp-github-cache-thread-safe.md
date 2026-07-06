# Make MCP GitHub Cache Thread Safe

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M24 + related LOW).

## Description

`source/timewarp-nuru-mcp/services/github-cache-service.cs:9,39,50,59` — `MemoryCache` is
a `static Dictionary<string, CachedContent>` read and mutated from `FetchAsync` with no
lock. MCP servers dispatch tool calls concurrently, so overlapping invocations (e.g.
GetExample + GetBehavior) can interleave writes with reads → InvalidOperationException or
corrupted buckets. Use `ConcurrentDictionary` (or lock).

Related LOW (same file, :132-142): `GetSafeCacheFileName` reduces a relative path to
`Path.GetFileNameWithoutExtension`, so two docs sharing a filename within one
cacheCategory (e.g. `a/overview.md` and `b/overview.md`) collide on `overview.cache` and
serve each other's content on the disk tier (memory tier keys on full path). Include a
hash of the full relative path in the cache file name.

## Checklist

- [x] Replace static Dictionary with ConcurrentDictionary
- [x] Disambiguate disk-cache file names (path hash)
- [x] Check other statics in the MCP server for the same pattern
- [x] Run MCP tests (note: currently excluded from CI — see task 454-001)

## Notes

### Implementation Plan (2026-07-06)

#### Decisions

| # | Question | Decision |
|---|----------|----------|
| 1 | Memory cache | `Dictionary` → `ConcurrentDictionary` (drop-in swap, keep explicit read-check-write flow — no AddOrUpdate/GetOrAdd) |
| 2 | Filename collision | Full relativePath with `/`/`\` → `-` (preserves readability, eliminates collision) |
| 3 | Test approach | Unit test `GetSafeCacheFileName` (no network needed); promote from `private` to `internal` for test access |
| 4 | MCP tests in CI | CI glob already covers `mcp-07-*.cs`, no Directory.Build.props change needed |

#### Step 1: Swap to ConcurrentDictionary
File: `source/timewarp-nuru-mcp/services/github-cache-service.cs`
- Add `using System.Collections.Concurrent;` if not present
- Line 9: `Dictionary<string, CachedContent>` → `ConcurrentDictionary<string, CachedContent>`
- Lines 39, 50, 59: no change — TryGetValue + indexer semantics identical
- Keep explicit read-check-write flow (no AddOrUpdate/GetOrAdd — would call value factory on cache hits)

#### Step 2: Fix GetSafeCacheFileName collision (lines 132-142)
File: `source/timewarp-nuru-mcp/services/github-cache-service.cs`
- Promote from `private` to `internal` (for test access)
- Replace body: use `Path.GetDirectoryName(path)` + `Path.GetFileNameWithoutExtension(path)`, join with `-` after replacing `/`/`\` with `-`
- `documentation/reference/foo.md` → `"documentation-reference-foo"` (was `"foo"` — collided with any other `foo.md`)
- Top-level `foo.md` → `"foo"` (unchanged, backwards compatible)

#### Step 3: Create test file
New file: `tests/timewarp-nuru-mcp-tests/mcp-07-cache-filename.cs`
- 3 unit tests for `GetSafeCacheFileName` (no network):
  1. `Should_not_collide_for_same_filename_in_different_directories` — `"examples/routing/foo.md"` vs `"examples/parser/foo.md"` → different results
  2. `Should_replace_path_separators_with_dashes` — `"documentation/reference/foo.md"` → `"documentation-reference-foo"`, no `/` or `\`
  3. `Should_handle_top_level_filename_without_directory` — `"foo.md"` → `"foo"`
- Uses `#:project $(SourceDirectory)timewarp-nuru-mcp/timewarp-nuru-mcp.csproj` header + `using TimeWarp.Nuru.Mcp.Services;`

#### Step 4: Verify
1. `ganda runfile cache --clear`
2. `dotnet run tests/timewarp-nuru-mcp-tests/mcp-07-cache-filename.cs` (standalone)
3. `dotnet run tests/ci-tests/run-ci-tests.cs` (full CI)

#### Files touched
- Edit: `source/timewarp-nuru-mcp/services/github-cache-service.cs` (ConcurrentDictionary + GetSafeCacheFileName fix + internal promotion)
- Create: `tests/timewarp-nuru-mcp-tests/mcp-07-cache-filename.cs` (3 tests)

#### Risk assessment
- ConcurrentDictionary swap is idempotent (same content from GitHub on concurrent fetches, last-writer-wins is fine)
- Disk cache files for nested paths get new names — one-time orphan, regenerated on demand (CacheManagementTool.ClearCache cleans up)
- No AddOrUpdate/GetOrAdd temptation — would defeat caching
- No trailing whitespace (RCS1037)

## Results

### What was implemented

Made the MCP GitHub cache thread-safe and fixed the disk-cache filename collision.

- **M24 (thread safety)**: Swapped `Dictionary<string, CachedContent>` to `ConcurrentDictionary<string, CachedContent>` at `github-cache-service.cs:11`. `TryGetValue` and indexer semantics are identical — no changes to the `FetchAsync` read-check-write flow. No `AddOrUpdate`/`GetOrAdd` (would call value factory on cache hits, defeating the cache).
- **Filename collision (LOW)**: Rewrote `GetSafeCacheFileName` to use the full relative path (with `/`/`\` replaced by `-`) instead of just `Path.GetFileNameWithoutExtension`. `examples/routing/foo.md` → `"examples-routing-foo"` (was `"foo"` — collided with any other `foo.md` in the same cache category). Promoted from `private` to `internal` for test access. Top-level `foo.md` → `"foo"` (backwards compatible).
- **Bonus: Fixed broken `generate-internals-visible-to.cs`**: The generator was crashing because it referenced non-existent project directories (`source/timewarp-nuru-core`, `source/timewarp-nuru-completion`, `source/timewarp-nuru-repl`). Added a `Directory.Exists` guard to skip non-existent directories. Regenerated all `internals-visible-to.g.cs` files (added `mcp-07-cache-filename`, removed stale entries for deleted test files).

### Files changed

- `source/timewarp-nuru-mcp/services/github-cache-service.cs` — ConcurrentDictionary swap + GetSafeCacheFileName fix + internal promotion + `using System.Collections.Concurrent`
- `tests/timewarp-nuru-mcp-tests/mcp-07-cache-filename.cs` (new) — 3 unit tests for GetSafeCacheFileName
- `runfiles/generate-internals-visible-to.cs` — fixed crash on non-existent project directories (Directory.Exists guard)
- `source/timewarp-nuru-mcp/internals-visible-to.g.cs` — regenerated (added mcp-07-cache-filename, removed stale entries)
- `source/timewarp-nuru-parsing/internals-visible-to.g.cs` — regenerated
- `source/timewarp-nuru/internals-visible-to.g.cs` — regenerated

### Key decisions made

- **No AddOrUpdate/GetOrGet**: Kept the explicit read-check-write flow. `AddOrUpdate`/`GetOrAdd` would call the value factory even on cache hits, defeating the cache. `ConcurrentDictionary` indexer write is fine (last-writer-wins, both concurrent fetches produce identical content).
- **Full-path-with-dashes over hash**: Preserves readability in the cache directory (`documentation-reference-foo` is human-readable, a SHA256 hash is not). The existing fallback code already used this pattern — just promoted it to the primary path.
- **Fixed the generator rather than hand-editing `.g.cs`**: The `InternalsVisibleTo` entry for `mcp-07-cache-filename` needed to be added. The generator was the intended mechanism but was broken. Fixing it was cleaner than manually editing the generated file.
- **`using` inside namespace**: Put `using System.Collections.Concurrent;` inside the file-scoped namespace (line 3) to avoid IDE0065 warning (warnings are errors).

### Test outcomes

- **Standalone** (`dotnet run tests/timewarp-nuru-mcp-tests/mcp-07-cache-filename.cs`): 3 passed, 0 failed
- **Full CI** (`dotnet run tests/ci-tests/run-ci-tests.cs`): 1340 passed, 7 skipped, 0 failed. No regressions.
