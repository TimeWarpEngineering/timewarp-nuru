# Package must enable InterceptorsNamespaces so new consumers do not hit CS9137

## Description

**3.0.0 launch gate #1 (blocks the "one package" promise).** On `3.0.0-beta.79`, a fresh
`dotnet new console` + `dotnet add package TimeWarp.Nuru`, and a `#:package TimeWarp.Nuru` runfile,
both fail to build:

```
NuruGenerated.g.cs: error CS9137: The 'interceptors' feature is not enabled in this namespace.
Add '<InterceptorsNamespaces>$(InterceptorsNamespaces);TimeWarp.Nuru.Generated</InterceptorsNamespaces>' to your project.
```

Every in-repo consumer works only because `Directory.Build.props:40` and `samples/Directory.Build.props:25`
set the property. The package ships `build/TimeWarp.Nuru.targets` (from `source/timewarp-nuru-build/build/`)
but nothing sets `InterceptorsNamespaces` for the consumer. The launch demo (`kanban/done/487-*/demo/app.cs.txt`)
carries a `#:property InterceptorsNamespaces=...` workaround line that must disappear.

## Requirements

- Ship the property in the package so it applies to every consumer project and runfile:
  a `build/TimeWarp.Nuru.props` (and `buildTransitive/`) that appends `;TimeWarp.Nuru.Generated` to
  `InterceptorsNamespaces`. Follow the explicit-path packing rule in `timewarp-nuru.csproj` (no wildcards; see
  the kanban 461 / 389 note there).
- Verify from **outside** the repo, with the packed `.nupkg` in a local feed: `dotnet new console` + add package
  + a `Map(...)` route builds with zero properties set; a runfile with only `#:package TimeWarp.Nuru@<ver>` builds.
- Keep in-repo `Directory.Build.props` lines as they are (harmless duplicate append) or remove them if the
  packed props now cover tests and samples; either way CI stays green.

## Checklist

- [x] `build/TimeWarp.Nuru.props` + `buildTransitive/TimeWarp.Nuru.props` packed with explicit `<None Pack>` includes
- [x] Out-of-repo console app and runfile consumers build with no extra properties (document the exact commands in Results)
- [x] `kanban/done/487-*/demo/app.cs.txt` and its readme: drop the workaround line and the note
- [x] `changelog.md` Unreleased: Fixed entry
- [ ] PR merged

## Notes

- Found by task 487 while verifying launch copy (2026-10-09). The 3.0 announcement says "nothing extra to install"; this is what makes that true.
- Related: `documentation/user/guides/migrating-to-3.0.md` should not have to mention the property at all after this.

## Session

- Created: claude 2412bd45 (2026-10-09)
- Implemented: claude sub-agent impl-488 (2026-10-11)

## Results

Version bumped to `3.0.0-beta.80` (`source/Directory.Build.props`); no other package-version pins found
(remaining beta.79 hits are changelog history, kanban/done, a release-guard unit test, and
`documentation/user/guides/migrating-to-3.0.md:4`, which task 490 already covers).

Pack (`dotnet build source/timewarp-nuru/timewarp-nuru.csproj -c Release`, then
`dotnet pack ... -c Release -o <scratchpad>/feed`) -> `TimeWarp.Nuru.3.0.0-beta.80.nupkg`:

```
unzip -l TimeWarp.Nuru.3.0.0-beta.80.nupkg | grep props
      384  build/TimeWarp.Nuru.props
      384  buildTransitive/TimeWarp.Nuru.props
```

Console app, outside any repo (`dotnet new console -n Probe`, `dotnet new nugetconfig`,
`dotnet nuget add source <feed> -n local488 --configfile nuget.config`,
`dotnet add Probe package TimeWarp.Nuru --version 3.0.0-beta.80 --source <feed>`, Program.cs from task brief):

```
dotnet msbuild Probe -getProperty:InterceptorsNamespaces   -> ;TimeWarp.Nuru.Generated
dotnet run --project Probe -- greet World                  -> Hello, World!
```

Runfile, outside any repo (demo `app.cs.txt` with the workaround removed and pin at beta.80, same nuget.config):

```
dotnet run app.cs -- greet World   -> Hello, World!
```

No CS9137 in either run. The only warnings were TimeWarp.Build.Tasks "not a git repository" noise.

Tests:

```
dotnet run tests/timewarp-nuru-tests/devcli/nupkg-layout-02-csproj-gate-parity.cs  -> Passed: 3 of 3
dotnet run tests/timewarp-nuru-tests/devcli/nupkg-layout-01-check.cs               -> Passed: 4 of 4
ganda runfile cache --clear; dotnet run tests/ci-tests/run-ci-tests.cs             -> exit 0, no FAILED lines
```

CI per-file tables summed: Total 3854, Passed 3842, Failed 0, Skipped 12.
Note: the parity test only compares `build/` and `analyzers/` entries, so the `buildTransitive/` entry in the gate list is not diffed by it.
