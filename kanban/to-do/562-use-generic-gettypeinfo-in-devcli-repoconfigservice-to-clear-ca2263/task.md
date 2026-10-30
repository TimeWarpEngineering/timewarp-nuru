# Use generic GetTypeInfo in DevCli RepoConfigService to clear CA2263

## Description

TimeWarp.Nuru.DevCli ships its services as content files, and consumers compile them under their
own analyzer settings. In 4.0.0-beta.9, `RepoConfigService.Parse` in
`source/timewarp-nuru-devcli/content/any/services/repo-config-service.cs` (line 60) does this:

```csharp
JsonTypeInfo<RepoConfig> typeInfo = (JsonTypeInfo<RepoConfig>)JsonOptions.GetTypeInfo(typeof(RepoConfig));
```

That trips **CA2263** ("prefer the generic overload") in consuming repos. timewarp-architecture
treats warnings as errors, so its dev CLI does not build on beta.9 (architecture task 457).
Architecture will not suppress the warning or keep a patched copy of the file. It is waiting for
a Nuru release with the fix.

Fix: use the generic overload, which stays on the source-generated resolver (no IL2026/IL3050):

```csharp
JsonTypeInfo<RepoConfig> typeInfo = JsonOptions.GetTypeInfo<RepoConfig>();
```

Architecture's worker made this exact change in a local copy, and its dev CLI built clean with
`dotnet build tools/dev-cli/dev.cs --no-incremental` (0 warnings, 0 errors).

## Requirements

- Change the call to `JsonOptions.GetTypeInfo<RepoConfig>()` and update the comment above it so
  it still explains why the generic `Deserialize<T>(string, options)` overload is avoided.
- Check the other DevCli content files for the same `GetTypeInfo(typeof(...))` cast pattern and
  fix them the same way.
- This is the second consumer-only analyzer break in DevCli content in a row: beta.8 tripped
  RCS1194 on `PackageFeedUnreadableException`, and beta.9 fixed that. Find out why Nuru's own
  build does not flag these. If DevCli content can be compiled under the analyzer set consumers
  use (CA and Roslynator at warning-as-error), add that check so the next one fails in Nuru CI.
  If it can't, record why in Results.
- Ship it in the next 4.0.0 beta. Nuru releases itself; architecture takes the pin afterwards.

## Checklist

- [ ] `GetTypeInfo<RepoConfig>()` in RepoConfigService; comment updated
- [ ] Other DevCli content checked for the same pattern
- [ ] Consumer-analyzer check added, or the reason it can't be recorded
- [ ] Released in the next 4.0.0 beta

## Session

- Created: 8d5a7906-5253-4dc5-b6d4-729819f6f6b0 (2026-10-30), filed from timewarp-architecture
  task 457 at Steve's request ("file the bug with Nuru").

## Notes

- Consumer: timewarp-architecture task 457 (take Nuru 4.0.0-beta.x), parked waiting for this fix.

## Results
