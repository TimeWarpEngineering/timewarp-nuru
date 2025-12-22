# Split TimeWarp.Terminal and TimeWarp.Builder to separate repositories

## Description

Move TimeWarp.Terminal and TimeWarp.Builder source code from timewarp-nuru repository to their own dedicated repositories. Both repositories already exist with worktrees created at:
- `/home/steventcramer/worktrees/github.com/TimeWarpEngineering/timewarp-builder/Cramer-2025-12-22-dev`
- `/home/steventcramer/worktrees/github.com/TimeWarpEngineering/timewarp-terminal/Cramer-2025-12-22-dev`

## Checklist

- [ ] Move TimeWarp.Terminal source code to separate repository
- [ ] Move TimeWarp.Builder source code to separate repository
- [ ] Update timewarp-nuru-core to use PackageReference instead of ProjectReference for both packages
- [ ] Update Directory.Packages.props to reference correct NuGet package versions
- [ ] Remove source directories from timewarp-nuru repository
- [ ] Update CI/CD workflows (builder-publish.yml, terminal-publish.yml) if needed
- [ ] Update samples and tests to use new package references
- [ ] Verify builds work after migration
- [ ] Update documentation references

## Notes

**Current State:**
- TimeWarp.Terminal and TimeWarp.Builder currently use ProjectReference in timewarp-nuru-core
- Both packages have independent versioning and CI publishing workflows
- Splitting will simplify repository management and allow independent development

**Target Worktrees:**
- timewarp-builder: `/home/steventcramer/worktrees/github.com/TimeWarpEngineering/timewarp-builder/Cramer-2025-12-22-dev`
- timewarp-terminal: `/home/steventcramer/worktrees/github.com/TimeWarpEngineering/timewarp-terminal/Cramer-2025-12-22-dev`

**Files to Move:**
- `source/timewarp-terminal/` → timewarp-terminal repository
- `source/timewarp-builder/` → timewarp-builder repository

**Dependencies:**
- This will complete the migration to independent package management
- TimeWarp.Terminal 1.0.0-beta.2 needs to be published first
