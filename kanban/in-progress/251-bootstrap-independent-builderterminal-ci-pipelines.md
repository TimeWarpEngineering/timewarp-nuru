# Bootstrap independent Builder/Terminal CI pipelines

## Description

Establish independent CI/CD pipelines for `TimeWarp.Builder` and `TimeWarp.Terminal` packages, publish them to NuGet, then update Jaribu to unblock Nuru CI.

**Why this is needed:**
- Builder and Terminal are independent packages that other projects (Jaribu) depend on
- They need to be on NuGet before Jaribu can reference them
- Jaribu needs to be on NuGet before Nuru tests can run in CI
- PR #126 is blocked because tests reference local Jaribu path which doesn't exist on CI runner

**Key Decisions:**
- Builder and Terminal version independently (not tied to Nuru versioning)
- Each has its own CI workflow triggered by changes to its folder
- Workflows are manually triggerable (`workflow_dispatch`) for bootstrap

## Checklist

### Phase 1: Add Independent Versioning
- [x] Add `<Version>1.0.0-beta.1</Version>` to `source/timewarp-builder/timewarp-builder.csproj`
- [x] Add `<Version>1.0.0-beta.1</Version>` to `source/timewarp-terminal/timewarp-terminal.csproj`

### Phase 2: Create Builder Workflow
- [x] Create `.github/workflows/builder-publish.yml`
- [x] Trigger on: `push` to `source/timewarp-builder/**`, `release` with `builder-v*` tag, `workflow_dispatch`
- [x] Build step (always runs)
- [x] Publish step (only on release or workflow_dispatch)

### Phase 3: Create Terminal Workflow
- [x] Create `.github/workflows/terminal-publish.yml`
- [x] Trigger on: `push` to `source/timewarp-terminal/**`, `release` with `terminal-v*` tag, `workflow_dispatch`
- [x] Build step (always runs)
- [x] Publish step (only on release or workflow_dispatch)

### Phase 4: Update Main CI Workflow
- [x] Remove `TimeWarp.Builder` and `TimeWarp.Terminal` from Nuru publish list in `ci-cd.yml`
- [x] Skip tests temporarily (add TODO comment linking to this task)

### Phase 5: Merge PR
- [x] Commit and push changes
- [x] Verify CI passes (build succeeds, tests skipped)
- [x] Merge PR #126

### Phase 6: Bootstrap - Publish to NuGet
- [x] Manually trigger `builder-publish.yml` workflow via GitHub Actions UI
- [x] Verify `TimeWarp.Builder 1.0.0-beta.1` appears on NuGet
- [x] Convert Terminal to use Builder NuGet package (PR #127)
- [x] Manually trigger `terminal-publish.yml` workflow
- [x] Verify `TimeWarp.Terminal 1.0.0-beta.1` appears on NuGet

### Phase 7: Update Jaribu (Separate Repo)
- [x] Jaribu `1.0.0-beta.6` already references `TimeWarp.Terminal` from NuGet
- [x] No new Jaribu version needed

### Phase 8: Re-enable Nuru Tests
- [x] `Directory.Packages.props` already has Jaribu `1.0.0-beta.6`
- [x] Remove test skip from `ci-cd.yml`
- [ ] Commit, push, verify CI passes

## Notes

### Dependency Chain
```
TimeWarp.Builder (no dependencies)
    ^
    |
TimeWarp.Terminal
    ^
    |
TimeWarp.Jaribu (separate repo)
    ^
    |
TimeWarp.Nuru tests
```

### Bootstrap Sequence Critical
Builder MUST be published to NuGet before Terminal can be usable by consumers. The Terminal package declares a NuGet dependency on Builder, so:
1. Trigger Builder workflow first
2. Wait for NuGet propagation (~15 min)
3. Then trigger Terminal workflow

### Future Work (Out of Scope)
- Convert Nuru's ProjectReferences to Builder/Terminal into PackageReferences for full independence
- Add local development story (conditional swap to ProjectReference when developing across packages)
