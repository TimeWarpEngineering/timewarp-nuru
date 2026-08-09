# Root cause analysis: incomplete TimeWarp.Nuru 3.0.0-beta.72 nupkg

**Task:** 461  
**Date:** 2026-08-09  
**Status:** Root cause determined (implementation / republish still open on task)

## Symptom

nuget.org `TimeWarp.Nuru` **3.0.0-beta.72** lacks `build/net10.0/` (including
`TimeWarp.Nuru.Build.dll`). `build/TimeWarp.Nuru.targets` still points at that path →
consumers with a clean NuGet cache fail with **MSB4062**.

`TimeWarp.Nuru.DevCli` 3.0.0-beta.72 is fine. Hand-patched global-packages caches (with
`.manual-extract`) mask the bug locally only.

## Executive conclusion

Three layers:

1. **Latent pack bug** — evaluation-time `*.dll` wildcards + `GeneratePackageOnBuild`
   silently omit task DLLs when the first pack of Nuru runs without those files visible
   at MSBuild evaluation.
2. **Merge CI has long produced incomplete Nuru nupkgs** in `Packages-*` artifacts
   (proven for beta.71 *and* beta.72).
3. **beta.71 release masked it** with post-build `dotnet pack --no-build`.  
   **beta.72 promote pipeline (458-002) removed that heal** and published the incomplete
   merge artifact as-is.

Pack-relevant csproj globs and `build-command` project order did **not** change between
beta.71 and beta.72. Attestation/probe work did not break packaging; **promote-without-repack
exposed the latent incomplete merge package**.

---

## Mechanism (MSBuild)

In `source/timewarp-nuru/timewarp-nuru.csproj`:

```xml
<!-- Concrete path: item always present; file resolved later when it exists -->
<None Include="../timewarp-nuru-analyzers/bin/$(Configuration)/$(TargetFramework)/TimeWarp.Nuru.Analyzers.dll"
      Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />

<!-- Wildcard: expanded at project EVALUATION; empty set if DLLs missing then -->
<None Include="../timewarp-nuru-build/bin/$(Configuration)/net10.0/*.dll"
      Pack="true" PackagePath="build/net10.0" />
```

Also: `source/Directory.Build.props` has `GeneratePackageOnBuild=true` for packable
source projects, so building **MCP** (which ProjectReferences Nuru) packs Nuru as a
side effect.

`build-command.cs` order:

```text
analyzers → timewarp-nuru-build → mcp → timewarp-nuru → search → devcli
```

First and only `Successfully created package .../TimeWarp.Nuru.*.nupkg` in CI logs is
during the **MCP** build. The later explicit `timewarp-nuru` build does not repack
(incremental / up-to-date), so a bad first pack sticks.

Same class of bug as kanban **389** (beta.28/29 missing task DLL), different pipeline
symptom surface.

---

## Local reproduction (2026-08-09)

| Scenario | Result |
|----------|--------|
| Clean tree → `dotnet build` **MCP only** (`GeneratePackageOnBuild` packs Nuru as ref) | **Incomplete** ~602 KB — targets + lib/analyzers, **no `build/net10.0/`** |
| Then `dotnet pack source/timewarp-nuru/timewarp-nuru.csproj -c Release --no-build` | **Healed** ~5.0 MB — full `build/net10.0/*` |
| Pre-build `timewarp-nuru-build`, then build MCP | Often **complete** locally (globs see files at eval) |
| Incremental rebuild of Nuru after incomplete pack | Stays incomplete (no repack) |

Commands (minimal):

```bash
# incomplete
rm -rf source/timewarp-nuru{,-build,-mcp,-analyzers}/{bin,obj}
rm -f artifacts/packages/TimeWarp.Nuru.*.nupkg
dotnet build source/timewarp-nuru-mcp/timewarp-nuru-mcp.csproj -c Release
unzip -l artifacts/packages/TimeWarp.Nuru.*.nupkg | grep build/

# heal (old release path)
dotnet pack source/timewarp-nuru/timewarp-nuru.csproj -c Release --no-build \
  -o artifacts/packages
unzip -l artifacts/packages/TimeWarp.Nuru.*.nupkg | grep build/net10
```

---

## CI / release history evidence

### Artifacts (still downloadable as of investigation)

| Run | Role | Nuru nupkg size | `build/net10.0` |
|-----|------|-----------------|-----------------|
| beta.71 **master** CI `Packages-42` (run 27553073284) | merge only | **645,787 B** | **missing** |
| beta.71 **release** `Packages-43` (run 27553776617) | clean→build→**pack**→push | **4,940,853 B** | **present** |
| beta.72 **PR** `Packages-48` (run 31265195436) | merge-style | **658,752 B** | **missing** |
| beta.72 **master** `Packages-49` (run 31265573461, commit `3a5e0026`) | merge only, **promoted** | **658,848 B** | **missing** |
| nuget.org beta.71 | from release pack | ~4.95 MB | present |
| nuget.org beta.72 | from promote | ~672 KB signed | **missing** |

### Pipelines

**beta.71 release:**

```text
check-version → clean → build → pack → push
```

Pack implementation (`PackProjectsAsync`):

```text
dotnet pack <csproj> --configuration Release --output artifacts/packages --no-build
```

After the ordered build, task DLLs exist; pack re-evaluates globs and overwrites the
incomplete GeneratePackageOnBuild nupkg. That is what nuget.org received for beta.71.

**beta.72 release (458-002 promote):**

```text
tag-gate → check-version → locate-run → download-artifact → verify → push
```

- No local pack.
- Verify checks package **set / IDs / version**, not nupkg **contents**.
- Pushes exact merge CI bytes → incomplete beta.72 on nuget.org.

### Diff surface (v3.0.0-beta.71 → 3a5e0026) relevant to pack

| Area | Change? |
|------|---------|
| Wildcard pack lines in `timewarp-nuru.csproj` | Unchanged (only analyzer logging TFM → `AnalyzerDependencyTfm`) |
| `build-command.cs` `projectsToBuild` order | Unchanged |
| `workflow-command.cs` release path | **Promote; `PackProjectsAsync` removed** |
| `workflow.yml` | Release no longer builds/packs; uploads merge packages only |

Attestation / TP probe / `dev release` machinery are adjacent 458 work; they did not alter
the Nuru pack globs.

---

## Would pack before upload fix CI artifacts?

**Yes.** Explicit `dotnet pack … -c Release --no-build` (or equivalent) **after** the
ordered Release build and **before** `actions/upload-artifact` would rewrite
`artifacts/packages/TimeWarp.Nuru.*.nupkg` with `build/net10.0` present — same heal as
beta.71 release, applied on the merge path that promote now trusts.

Limits:

- Heals the uploaded artifact; does not remove the fragile evaluation-time glob.
- Without a content check, a future pack path regression can still ship incomplete
  packages silently.

Recommended defense in depth:

1. Pack-time collection of task DLLs (target before `GenerateNuspec` / `_GetPackageFiles`), not static `*.dll` globs.
2. Fail pack if `TimeWarp.Nuru.Build.dll` is missing.
3. Optional: nupkg layout assertion before upload / before promote push.
4. Explicit pack after build before upload **or** content-aware promote gate.

---

## What is not the root cause

- Missing `timewarp-nuru-build` from solution / build list (fixed in task 389; still present).
- DevCli package incompleteness.
- Attestation verifier logic incorrectly rewriting nupkgs.
- Path typo (`net1` vs `net10.0`).
- Truncated log path text.
- Consumer NuGet cache alone (clean restore reproduces from nuget.org).

---

## Related

- Nuru kanban **389** — same missing-DLL class of bug (beta.28/29); CI build-list fix.
- Nuru program **458** / **458-002** — build-once promote; removed release-local pack.
- Ganda kanban **202** / PR **#66** — consumer MSB4062 on clean CI.

## Recommended task outcomes (implementation still open)

- [ ] Fix pack definition (pack-time items + fail-loud).
- [ ] Ensure merge CI artifacts are complete (post-build pack and/or layout gate).
- [ ] Publish **3.0.0-beta.73** (or newer) complete nupkg.
- [ ] Clean-cache smoke; notify consumers (ganda, etc.) to bump pin.
- [ ] Optional: deprecate/yank incomplete beta.72 if policy allows.

---

## Independent verification (Claude session, 2026-08-09)

Every load-bearing claim re-verified from primary sources:

| Claim | Evidence | Verdict |
|---|---|---|
| nuget beta.72 incomplete | Downloaded from flatcontainer: 671,946 B, `build/` contains ONLY the .targets — zero `build/net10.0` entries | **CONFIRMED** |
| nuget beta.71 complete | 4,953,951 B, full `build/net10.0/*` payload incl. task deps | **CONFIRMED** |
| Merge artifact incomplete for beta.72 | `Packages-49` (run 31265573461, the promote source): Nuru nupkg 658,848 B, zero `build/net10.0` | **CONFIRMED** |
| Merge artifact incomplete for beta.71 TOO | `Packages-42` (run 27553073284): Nuru nupkg 645,787 B, zero `build/net10.0` — while nuget's beta.71 is the complete 4.95 MB package | **CONFIRMED — the decisive comparison** |
| Wildcard include is the vector | `timewarp-nuru.csproj:112` `<None Include="../timewarp-nuru-build/bin/$(Configuration)/net10.0/*.dll" Pack="true" .../>` (evaluation-time glob); analyzers use a concrete-path include and their package IS complete in the same artifact | **CONFIRMED** |
| Build order enables side-effect pack | build-command.cs: analyzers → nuru-build → **mcp** → nuru → search → devcli; MCP ProjectReferences Nuru + GeneratePackageOnBuild ⇒ Nuru packs during the MCP step | **CONFIRMED** |

**Residual nondeterminism honestly noted:** the microscopic reason the glob
reliably evaluates empty in CI (despite nuru-build building one step earlier)
is not 100% pinned — grok's own repro table says pre-building nuru-build makes
local packs "often" complete, implying timing/incremental sensitivity. The
class-fix (pack-time item collection + fail-loud + explicit post-build repack +
nupkg layout gate) is correct REGARDLESS of that microscopic detail, so
resolving it is not a blocker.

**Systemic lesson for the 458 promote pipeline:** "publish only tested bytes"
held — but the bytes were never tested AS A PACKAGE. Tests exercise project
references; nothing anywhere validated nupkg CONTENTS, and the promote
pipeline's VerifyPackageSet checks package names/versions, not payload layout.
The old rebuild-at-release was silently HEALING this latent bug (fresh pack
evaluation after full build → globs resolved); 458-002 removed the heal and
faithfully promoted what CI had always been producing. The durable fix is a
**package layout gate** (assert critical payload files inside each nupkg) in
merge CI before artifact upload — extending exactly the fail-loud family the
458 program established.

**Root cause: DETERMINED and verified. Grok's three-layer analysis stands in
full.**
