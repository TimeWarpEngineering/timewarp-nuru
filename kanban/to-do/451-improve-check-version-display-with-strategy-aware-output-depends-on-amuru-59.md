# Improve check-version display with strategy-aware output (depends on Amuru #59)

## Description

Update `check-version-command.cs` to display rich, strategy-aware output using enriched `CheckVersionResult` from Amuru.

**Blocked on:** [TimeWarp.Amuru #59](https://github.com/TimeWarpEngineering/timewarp-amuru/issues/59)

## Checklist

- [ ] Wait for Amuru #59 to ship (enriched `CheckVersionResult`)
- [ ] Update Amuru package reference
- [ ] Rewrite display logic in `check-version-command.cs` for git-tag strategy
- [ ] Rewrite display logic in `check-version-command.cs` for nuget-search strategy
- [ ] Remove service terminal writes (Amuru side)
- [ ] Test both strategies locally
- [ ] Verify CI pipeline output

## Expected Output

### git-tag strategy

New version:
```
Strategy: git-tag (GitHub releases)

Version in source: 3.0.0-beta.68
Latest release tag on GitHub: v3.0.0-beta.67

✓ Version in source is new — safe to release.
```

Already released:
```
Strategy: git-tag (GitHub releases)

Version in source: 3.0.0-beta.67
Latest release tag on GitHub: v3.0.0-beta.67

✗ Version 3.0.0-beta.67 was already released.
  Bump the version before releasing.
```

### nuget-search strategy

New version:
```
Strategy: nuget-search (NuGet packages)

Version in source: 3.0.0-beta.68
Latest NuGet version: 3.0.0-beta.67
Packages checked: TimeWarp.Nuru, TimeWarp.Nuru.Analyzers

✓ Version in source is new — safe to release.
```

Already published:
```
Strategy: nuget-search (NuGet packages)

Version in source: 3.0.0-beta.67
Latest NuGet version: 3.0.0-beta.67
Packages checked: TimeWarp.Nuru, TimeWarp.Nuru.Analyzers
Already published: TimeWarp.Nuru, TimeWarp.Nuru.Analyzers

✗ Version 3.0.0-beta.67 was already published on NuGet.
  Bump the version before releasing.
```

## Files to Edit

- `source/timewarp-nuru-devcli/content/any/endpoints/check-version-command.cs`

## Notes

- Service returns data only, command handles all display
- Amuru service should stop writing to terminal
- Display should state strategy upfront so developer knows which check ran
