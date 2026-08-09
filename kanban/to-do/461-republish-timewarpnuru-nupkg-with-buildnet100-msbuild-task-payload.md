# Republish TimeWarp.Nuru nupkg with build/net10.0 MSBuild task payload

## Description

**Bug:** nuget.org `TimeWarp.Nuru` **3.0.0-beta.72** is an **incomplete nupkg**.  
`build/TimeWarp.Nuru.targets` expects `build/net10.0/TimeWarp.Nuru.Build.dll`, but that
directory (and the Build task assembly) is **not in the published package**.

Consumers that restore a **clean** cache (GitHub Actions, new machines) fail MSBuild when
the GenerateNuruJsonContext task loads:

```text
error MSB4062: The "TimeWarp.Nuru.Build.GenerateNuruJsonContextTask" task could not be
loaded from the assembly
…/timewarp.nuru/3.0.0-beta.72/build/net10.0/TimeWarp.Nuru.Build.dll
The system cannot find the file specified.
```

Local builds that “work” are often on a **hand-patched** global-packages cache (e.g.
operator extracted a complete local pack into `~/.nuget/packages/timewarp.nuru/3.0.0-beta.72/`
with a `.manual-extract` marker). That does **not** fix CI or clean restores.

**Note:** `TimeWarp.Nuru.DevCli` 3.0.0-beta.72 on nuget.org is fine. The hole is **Nuru**
(the app package that ships the MSBuild task), not DevCli.

### Evidence (2026-08-09)

Fresh nupkg from nuget.org contains only:

| Present | Missing |
|---------|---------|
| `build/TimeWarp.Nuru.targets` | **`build/net10.0/*`** including `TimeWarp.Nuru.Build.dll` |
| `lib/net10.0/TimeWarp.Nuru.dll` | |
| `analyzers/dotnet/cs/*` | |

Targets (correct path, package incomplete):

```xml
<_NuruBuildTaskDir Condition="'$(_NuruBuildTaskDir)' == ''">
  $(MSBuildThisFileDirectory)net10.0/
</_NuruBuildTaskDir>
<UsingTask
  TaskName="TimeWarp.Nuru.Build.GenerateNuruJsonContextTask"
  AssemblyFile="$(_NuruBuildTaskDir)TimeWarp.Nuru.Build.dll" />
```

**Downstream impact:**

- timewarp-ganda PR #66 / `dev` CI: `dotnet run tools/dev-cli/dev.cs -- workflow` dies in
  ~7s on self-install/build with MSB4062 (same root cause).
- Documented earlier on ganda task **202** (machine-local cache override; not committed).

**Not the bug:** truncated log text like `…/build/net1` (path is `net10.0`, not a bad TFM).
Clearing NuGet cache does not fix; it removes any local workaround.

Possible timing: beta.72 release packaging / CI promote may have dropped the Build task
payload; re-check pack layout for the next ship even if “we just fixed CI.”

## Requirements

- [ ] Confirm pack inputs: `TimeWarp.Nuru.Build` (or equivalent) is included under
      `build/net10.0/` in the **nupkg that gets published** (not only in local artifacts)
- [ ] Fix pack/csproj/targets packing so nuget.org nupkgs include:
      - `build/TimeWarp.Nuru.targets`
      - `build/net10.0/TimeWarp.Nuru.Build.dll` (+ any required task dependencies you ship)
- [ ] Publish a fixed package: prefer **3.0.0-beta.73** (or re-push beta.72 only if policy
      allows overwriting — usually new version is safer)
- [ ] Verify on a **clean** machine/cache:
      ```bash
      # empty temp cache
      export NUGET_PACKAGES=$(mktemp -d)
      # restore/build a consumer that uses TimeWarp.Nuru (e.g. ganda tools/dev-cli)
      # expect no MSB4062
      ```
- [ ] Smoke: GHA on a consumer (ganda `workflow`) green after pin bump
- [ ] Audit why beta.72 nupkg lost `build/net10.0` (pack project, CI promote, path filter)
      so it does not recur

## Checklist

- [ ] Repro: download nuget.org beta.72 nupkg; `unzip -l` shows no `build/net10.0/`
- [ ] Locate pack definition for TimeWarp.Nuru + Build task (csproj / nuspec / targets)
- [ ] Fix pack so Build task payload is included
- [ ] Local pack → inspect nupkg listing includes `build/net10.0/TimeWarp.Nuru.Build.dll`
- [ ] Cut release / publish fixed version
- [ ] Clean-cache restore smoke (temp `NUGET_PACKAGES`)
- [ ] Notify consumers (ganda, etc.) to bump pin when ready
- [ ] Optional: yank or deprecate incomplete beta.72 if policy allows

## Notes

### Consumer workaround (do not treat as fix)

Install a **complete** local nupkg into the machine cache (as done for ganda operators):

```text
~/.nuget/packages/timewarp.nuru/3.0.0-beta.72/build/net10.0/TimeWarp.Nuru.Build.dll
```

CI cannot use this; republish is required.

### Related

- ganda kanban **202** (adopt beta.72; documented incomplete nupkg)
- ganda PR **#66** / CI run failing MSB4062 on ubuntu-latest
- Nuru program / release pipeline that produced v3.0.0-beta.72 (2026-08-08)

## Session

- Created: Grok Build (2026-08-09) from ganda CI investigation + clean nupkg listing
