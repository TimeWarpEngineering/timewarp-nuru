# Nuru consumer check: TimeWarp.Nuru 4.0.0-beta.5

- Source: `https://api.nuget.org/v3/index.json` (private cache, `<clear/>` nuget.config)
- Docs: `v4.0.0-beta.5` (all)
- Run: 20261023T140329Z, dotnet 10.0.402
- Result: **FAIL**: 1 passed, 1 failed, 0 skipped

| Step | Check | Result | Evidence | Detail |
|------|-------|--------|----------|--------|
| launch | restore resolves TimeWarp.Nuru 4.0.0-beta.5 from the intended source | PASS | `launch/timewarp.nuru.nupkg.metadata` | https://api.nuget.org/v3/index.json |
| cache | global cache timewarp.nuru 4.0.0-beta.5 matches the published package | FAIL | `cache/global-cache-audit.tsv` | global entry came from ~/.cache/nuru-verify/repro-647/feed; delete ~/.cache/nuru-verify/repro-647/fake-global-cache/timewarp.nuru/4.0.0-beta.5 |
