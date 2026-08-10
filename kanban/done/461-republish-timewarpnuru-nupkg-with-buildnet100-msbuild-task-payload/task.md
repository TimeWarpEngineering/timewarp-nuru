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
- [x] Audit why beta.72 nupkg lost `build/net10.0` (pack project, CI promote, path filter)
      so it does not recur — see `kitchen/root-cause-analysis-2026-08-09.md`

## Checklist

- [x] Repro: download nuget.org beta.72 nupkg; `unzip -l` shows no `build/net10.0/`
- [x] Locate pack definition for TimeWarp.Nuru + Build task (csproj / nuspec / targets)
- [x] Audit why beta.72 nupkg lost `build/net10.0` (see kitchen root-cause analysis)
- [x] Fix pack so Build task payload is included: replaced the eval-time `*.dll` glob in
      `source/timewarp-nuru/timewarp-nuru.csproj` with 10 **explicit** `None Include` entries
      (from beta.71's complete manifest) — concrete paths create pack items regardless of
      evaluation timing and fail loudly at pack if a file is missing
- [x] Reorder `dev build` curated list (nuru before mcp) so Nuru's first
      `GeneratePackageOnBuild` pack runs in its own step with payload present (belt-and-braces)
- [x] Ensure merge CI artifacts are complete before upload: package **layout gate** in
      `dev workflow` (PR + merge) — `NupkgLayoutCheck.FindMissing` verifies the Nuru nupkg
      contains `build/TimeWarp.Nuru.targets`, all 10 `build/net10.0/*.dll`, and
      `lib/net10.0/TimeWarp.Nuru.dll`; aborts the run on any missing entry
- [x] Tests: `tests/timewarp-nuru-tests/devcli/nupkg-layout-01-check.cs` (4 cases: pass,
      missing-reported-by-name, ordinal case sensitivity, empty-required) wired into both
      test props files
- [x] Local pack → inspect nupkg listing includes `build/net10.0/TimeWarp.Nuru.Build.dll`
      (clean `dev build`: nupkg 5,067,541 B with all 10 build/net10.0 entries vs hollow
      658,848 B before the fix)
- [x] Cut release / publish fixed version: **3.0.0-beta.73** shipped 2026-08-10 (PR #221 →
      merge c9a0f035, tag v3.0.0-beta.73, CI run 31353054071 promoted by release run 31353385089)
- [x] Clean-cache restore smoke (temp `NUGET_PACKAGES`): fresh cache, nuget.org-only source,
      minimal consumer restored + built (no MSB4062; generator + interceptors ran) + app executed
- [x] Notify consumers (ganda, etc.) to bump pin to beta.73 when ready —
      **ganda** `Directory.Packages.props`: TimeWarp.Nuru + TimeWarp.Nuru.DevCli →
      **3.0.0-beta.73** (commit `7321fea` on `dev`); clean-cache self-install/build/test
      465/465 + audit 23/23; PR **#66** re-ran CI green and **merged** to master
      (`454de807`, 2026-08-10). Other consumers still on beta.72 should bump similarly.
- [ ] Optional: deprecate incomplete beta.72 on nuget.org — operator (NuGet UI)

## Notes

### Root cause (summary)

Folderized kitchen write-up:

- `kitchen/root-cause-analysis-2026-08-09.md`

**Layers:** (1) evaluation-time `*.dll` wildcards + `GeneratePackageOnBuild` silently omit
`build/net10.0` when the first pack of Nuru runs without those files at eval;
(2) merge CI `Packages-*` artifacts have been incomplete for beta.71 **and** beta.72;
(3) beta.71 release healed via `dotnet pack --no-build` after build; beta.72 promote
(458-002) ships merge artifacts with no repack → nuget.org incomplete.

**Yes:** an explicit pack after build, before upload-artifact, would fix promoted bytes
(same heal as beta.71). Prefer also fixing the glob fragility and fail-loud layout check.

### Consumer workaround (do not treat as fix)

Install a **complete** local nupkg into the machine cache (as done for ganda operators):

```text
~/.nuget/packages/timewarp.nuru/3.0.0-beta.72/build/net10.0/TimeWarp.Nuru.Build.dll
```

CI cannot use this; republish is required.

### Review (2026-08-10)

Commit `6f8c932d` reviewed (single reviewer, sonnet): **VERDICT: clean**. Verified: include
list ↔ gate list consistency (all 12 entries), gate runs in PR *and* merge modes, gate failure
blocks promotion (release only promotes `--status success` runs, so the `if: always()` debug
artifact upload is not reachable by promote), all fail-closed paths, package naming/paths, build
reorder, test coverage. One non-blocking suggestion carried as follow-up: the required-payload
list is hand-duplicated in `timewarp-nuru.csproj` and `NuruRequiredPackageEntries`
(workflow-command.cs) — if a future timewarp-nuru-build dependency is added to one list but not
the other, the gate silently stops covering the new file. Follow-up idea: derive the gate list
from the csproj items or add a test that diffs them.

### Related

- Nuru kanban **389** (same missing-DLL class; build-list fix)
- Nuru **458-002** (build-once promote; removed release-local pack)
- ganda kanban **202** (adopt beta.72; documented incomplete nupkg)
- ganda PR **#66** / CI run failing MSB4062 on ubuntu-latest
- Nuru release that produced v3.0.0-beta.72 (2026-08-08, run 31266668921; promoted master Packages-49)

## Results

**Shipped: TimeWarp.Nuru 3.0.0-beta.73 on nuget.org, complete** (5,172,550 B; hollow beta.72
was 671,946 B). Verified by downloading the published nupkg from nuget.org: contains
`build/TimeWarp.Nuru.targets`, all 10 `build/net10.0/*.dll` (incl. `TimeWarp.Nuru.Build.dll`),
`lib/net10.0/TimeWarp.Nuru.dll`, and `analyzers/dotnet/cs/*`.

**Root cause** (3 layers, all verified — see `kitchen/root-cause-analysis-2026-08-09.md`):
evaluation-time `*.dll` wildcard in `timewarp-nuru.csproj` expanded empty when
`GeneratePackageOnBuild` packed Nuru as a side effect of the MCP build; merge CI artifacts were
hollow for beta.71 **and** beta.72; the old release-local repack silently healed beta.71, and
the 458-002 build-once/promote pipeline faithfully shipped the hollow beta.72 bytes.

**Fix** (commit `6f8c932d` on dev, merged to master via PR #221 as `c9a0f035`):

1. `source/timewarp-nuru/timewarp-nuru.csproj` — wildcard replaced with 10 explicit
   `None Include` entries (beta.71's complete manifest); missing files now fail pack loudly.
2. `tools/dev-cli/endpoints/build-command.cs` — nuru packs in its own step before mcp.
3. `tools/dev-cli/endpoints/workflow-command.cs` + new
   `source/timewarp-nuru-devcli/content/any/services/nupkg-layout-check.cs` — package layout
   gate after build in PR **and** merge pipelines: verifies all 12 required payload entries in
   the Nuru nupkg, fail-closed (missing entry, missing nupkg, or unreadable version all abort
   with exit 1). Release only promotes `--status success` runs, so gated-failed artifacts can
   never ship.
4. Tests: `tests/timewarp-nuru-tests/devcli/nupkg-layout-01-check.cs` (4/4) wired into both
   test props files.

**Review:** 1 round, single reviewer (sonnet), verdict **clean**; one non-blocking follow-up
noted (payload list duplicated between csproj and gate — derive or diff-test later).

**Release evidence:** guards 7/7 green; tag `v3.0.0-beta.73` at `c9a0f035`; CI run
31353054071 (layout gate printed `Package layout verified: … all 12 required payload entries`);
release run 31353385089 succeeded (tag-pin, attestation, promote, OIDC push).

**Left for operator:** notify consumers to bump pins to beta.73; deprecate beta.72 in the
NuGet UI.

### How to validate

Smoke (any machine, no repo needed):

```bash
d=$(mktemp -d) && cd $d
curl -sfL -o n.nupkg https://api.nuget.org/v3-flatcontainer/timewarp.nuru/3.0.0-beta.73/timewarp.nuru.3.0.0-beta.73.nupkg
unzip -l n.nupkg | grep build/net10.0
```

**Expect:** 10 DLL entries under `build/net10.0/` including `TimeWarp.Nuru.Build.dll`.

Clean-cache consumer (proves no MSB4062):

```bash
export NUGET_PACKAGES=$(mktemp -d)
# minimal console app with <PackageReference Include="TimeWarp.Nuru" Version="3.0.0-beta.73" />
# and <InterceptorsNamespaces>$(InterceptorsNamespaces);TimeWarp.Nuru.Generated</InterceptorsNamespaces>
dotnet build -c Release
```

**Expect:** build succeeds, no `MSB4062`.

Automated gate (repo, guards regression):

```bash
dotnet run tests/timewarp-nuru-tests/devcli/nupkg-layout-01-check.cs   # expect 4/4 passed
dotnet run --file tools/dev-cli/dev.cs -- workflow --mode pr           # expect "Package layout verified: … all 12 required payload entries" and Pipeline SUCCEEDED
```

## Session

- Created: Grok Build (2026-08-09) from ganda CI investigation + clean nupkg listing
- Root-cause analysis: Grok Build (2026-08-09) — folderized task; kitchen write-up; local repro + beta.71/72 artifact comparison
- Implementation: Claude (2026-08-10) — explicit includes + build reorder + layout gate + tests; targeting 3.0.0-beta.73
