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

- [ ] Replace static Dictionary with ConcurrentDictionary
- [ ] Disambiguate disk-cache file names (path hash)
- [ ] Check other statics in the MCP server for the same pattern
- [ ] Run MCP tests (note: currently excluded from CI — see task 454-001)
