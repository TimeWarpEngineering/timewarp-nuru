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

## Goal (implementer restatement)

Make DevCli content compile clean in a consumer that builds it with CA and Roslynator at
warnings-as-errors. Today `RepoConfigService.Parse` trips CA2263 in timewarp-architecture.

Acceptance:
- `RepoConfigService.Parse` no longer raises CA2263 for a consumer. It stays on the source-generated
  resolver (no IL2026/IL3050) and keeps the `.jsonc` options (comments, trailing commas,
  case-insensitive). The comment says why the reflection `Deserialize<T>(string, options)` overload
  is avoided.
- No other DevCli content file uses the `GetTypeInfo(typeof(...))` cast pattern.
- Results say why Nuru's own build missed CA2263 (and RCS1194 before it). If the content can be
  compiled the way consumers compile it, Nuru CI does that so the next break fails here.
- Release happens in the next 4.0.0 beta. By repo convention the release is its own task
  (`release-400-betaN`), so this task does not bump the version.

## Checklist

- [x] CA2263 cleared in RepoConfigService; comment updated. This uses a context instance rather
  than `GetTypeInfo<RepoConfig>()`, which is net11-only (see Results)
- [x] Other DevCli content checked for the same pattern
- [x] Consumer-analyzer check added (`tests/devcli-consumer-check`, CI samples stage)
- [ ] Released in the next 4.0.0 beta (separate `release-400-beta10` task by convention)

## Session

- Created: 8d5a7906-5253-4dc5-b6d4-729819f6f6b0 (2026-10-30), filed from timewarp-architecture
  task 457 at Steve's request ("file the bug with Nuru").
- 2026-10-31 implement (ganda task work, implementer-claude): context-instance fix,
  `tests/devcli-consumer-check` + CI step, boy-scout IDE1006 in check-version-02.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 0cfdedef-883a-4b18-b818-2be5cd1e031f, max-turns 500, Done — 2026-10-30T19:37:34Z

## Notes

- Consumer: timewarp-architecture task 457 (take Nuru 4.0.0-beta.x), parked waiting for this fix.

## Results

### Fix: no `GetTypeInfo<RepoConfig>()`, because it is net11-only

The requested `JsonOptions.GetTypeInfo<RepoConfig>()` does not exist on net10.0.
`JsonSerializerOptions` on 10.0.14 has only `GetTypeInfo(Type)`. 11.0.0-rc.3 adds `GetTypeInfo<T>()`
and `TryGetTypeInfo<T>`. I checked this by reflection on both runtimes. Nuru, `tools/dev-cli` and
every net10 consumer would fail to compile with the generic call. Architecture's local patch built
only because architecture targets net11.0.

`RepoConfigService` now keeps the `.jsonc` options on a `DevCliJsonContext` instance
(`new DevCliJsonContext(options)`). It deserializes with the generated `JsonContext.RepoConfig`
(`JsonTypeInfo<RepoConfig>`). That works on every TFM, stays on the source-generated resolver (no
IL2026/IL3050) and has no `GetTypeInfo` call for CA2263 to flag. The other DevCli callers already
use the same `DevCliJsonContext.Default.X` style. The comment above the call says why neither the
reflection `Deserialize<T>(string, options)` overload nor `GetTypeInfo(typeof(T))` is used. The
`System.Text.Json.Serialization.Metadata` using went away with the cast.

### Same pattern elsewhere in DevCli content

None. `GetTypeInfo` appeared only in `repo-config-service.cs`. The only other `typeof(` uses in the
content are the `[JsonSerializable]` attributes in `dev-cli-json-context.cs`.

### Why Nuru's own build missed CA2263 and RCS1194

Nuru compiles DevCli content in `tools/dev-cli`, `tests/ci-tests` and `tests/timewarp-nuru-tests/devcli`.
It differs from a consumer in two ways:

1. **TFM.** Nuru builds net10.0 only. CA2263 fires only when a generic overload exists, and
   `GetTypeInfo<T>()` exists only on net11, so the rule had nothing to report here. The analyzer
   package version is not the cause. Nuru pins NetAnalyzers 10.0.401 and architecture uses
   11.0.100-rc.3. I swapped Nuru to 11.0.100-rc.3 and the net10 dev-cli build was still clean. I
   kept 10.0.401 and built `tools/dev-cli` for net11.0 under SDK 11 rc.3, and it reported exactly
   `repo-config-service.cs(60,67): error CA2263`.
2. **.editorconfig.** The content sits under Nuru's root `.editorconfig`, so a rule set to `none`
   there is invisible to Nuru (RCS1194 on beta.8). A consumer compiles the content from the NuGet
   cache, where no `.editorconfig` applies, so it gets analyzer defaults plus `AnalysisMode=All`.

### Consumer check added: `tests/devcli-consumer-check`

This is a net11.0 exe that compiles DevCli content the way a consumer dev CLI does:
- It imports the shipped `build/TimeWarp.Nuru.DevCli.props` (so the package props are exercised too).
- `TreatWarningsAsErrors`, root analyzer set (AnalysisMode=All, Roslynator), and the same `NoWarn`,
  `PublishAot` and `InvariantGlobalization` as `tools/dev-cli`. Architecture's dev CLI sets the same
  two flags. `InvariantGlobalization` turns off the CA1307 culture rules. Without it the check
  reports 6 CA1307 sites (`IndexOf(char)`) that no real dev CLI sees.
- `DiscoverEditorConfigFiles=false`, so the repo `.editorconfig` cannot hide a rule. This is set in
  the csproj, not with `-p:`, because a global property would also reach the referenced Nuru
  projects. With `-p:` it hit CA1308 in the analyzers project.
- Its own `global.json` selects SDK `11.0.100-rc.3.26522.148` (rollForward latestFeature,
  allowPrerelease). Its own `Directory.Build.props/.targets` import the repo root and skip the
  Jaribu/Shouldly test props.
- It is not in `timewarp-nuru.slnx`, which builds on the repo's .NET 10 SDK. Build it from its
  folder. `dotnet build tests/devcli-consumer-check` from the repo root uses SDK 10 and fails with
  the net11 targeting error.

I confirmed the check catches both breaks:
- With the pre-fix `repo-config-service.cs` it fails with `CA2263` at `(60,67)`.
- With `package-feed-lookup.cs` from before 558 (missing the `HttpRequestError` constructor) it
  fails with `RCS1194` at `(121,21)`.
- With this branch it reports 0 warnings and 0 errors.

CI: `workflow.yml` `stage` job, samples matrix leg. After `dev workflow --stage samples` it installs
.NET 11 (`dotnet-version: 11.0.x`, `dotnet-quality: preview`) and runs `dotnet build` in
`tests/devcli-consumer-check`. The samples leg is the existing "compile like a consumer" stage and
stays off the build leg's packaging critical path.

### Boy-scout

`tests/timewarp-nuru-tests/devcli/check-version-02-legacy-strategy-config.cs` (the `Parse` test)
failed standalone with IDE1006 on `const string json`. 558 re-enabled IDE1006. I renamed the
const to `Json`. **Follow-up, not done here:** the same camelCase local consts remain in
`attestation-01-verifier.cs` (1) and `packable-projects-01-parse.cs` (17). The CI multi-mode
assembly does not report them, but a standalone `dotnet run` of those files would.

### Gates

- `tests/devcli-consumer-check`: `dotnet build --no-incremental` reports 0 warnings and 0 errors (SDK 11.0.100-rc.3).
- `dotnet build tools/dev-cli/dev.cs --no-incremental` reports 0 warnings and 0 errors.
- `dotnet build timewarp-nuru.slnx` reports 0 warnings and 0 errors.
- `dotnet run tests/timewarp-nuru-tests/devcli/check-version-02-legacy-strategy-config.cs`: 3/3 passed.
  This covers `.jsonc` comments, trailing commas and the ignored legacy key, all through the new
  context-bound options.
- `ganda runfile cache --clear` then `dotnet run tests/ci-tests/run-ci-tests.cs` exits 0. Multi-mode
  finished in 118 s, and standalone reported 0 failed.
- `ganda repo audit`: passes all checks.

### Release

There is no version bump here. By convention a `release-400-betaN` task (559 for beta.9) bumps
`source/Directory.Build.props` and runs `dev release`. Ship this as 4.0.0-beta.10. Release-note
callout: DevCli `RepoConfigService` no longer trips CA2263 for net11 consumers, and Nuru CI now
compiles DevCli content as a net11 consumer (562). After that, architecture task 457 can take the pin.

### How to validate

**Smoke:**
```bash
grep -n "GetTypeInfo" source/timewarp-nuru-devcli/content -r
(cd tests/devcli-consumer-check && dotnet build --no-incremental)
dotnet build tools/dev-cli/dev.cs --no-incremental
dotnet run tests/timewarp-nuru-tests/devcli/check-version-02-legacy-strategy-config.cs
ganda runfile cache --clear && dotnet run tests/ci-tests/run-ci-tests.cs
ganda repo audit
```

**Expect:** The grep finds only the comment in `repo-config-service.cs`, with no `GetTypeInfo(`
call. The consumer check needs SDK 11.0.100-rc.3 or later and reports 0 warnings, 0 errors. If you
restore the old line (`git show 0ab1c314:source/timewarp-nuru-devcli/content/any/services/repo-config-service.cs`)
it fails with CA2263. The dev-cli build reports 0 warnings, 0 errors. The Parse test passes 3/3,
CI exits 0 with 0 failed, and the audit passes. On the PR, the `samples` CI job shows a passing
"DevCli consumer check" step.
