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

- [ ] `build/TimeWarp.Nuru.props` + `buildTransitive/TimeWarp.Nuru.props` packed with explicit `<None Pack>` includes
- [ ] Out-of-repo console app and runfile consumers build with no extra properties (document the exact commands in Results)
- [ ] `kanban/done/487-*/demo/app.cs.txt` and its readme: drop the workaround line and the note
- [ ] `changelog.md` Unreleased: Fixed entry
- [ ] PR merged

## Notes

- Found by task 487 while verifying launch copy (2026-10-09). The 3.0 announcement says "nothing extra to install"; this is what makes that true.
- Related: `documentation/user/guides/migrating-to-3.0.md` should not have to mention the property at all after this.

## Session

- Created: claude 2412bd45 (2026-10-09)
