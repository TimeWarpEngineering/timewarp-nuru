# Update CI CD Pipeline

## Description

Update the CI/CD pipeline configuration for the new directory and file structure.

## Parent

087_Migrate-Repository-To-Kebab-Case-Naming

## Requirements

- Update `.github/workflows/ci-cd.yml` with new paths
- Update any hardcoded directory references
- Update solution file reference
- Verify pipeline runs successfully

## Checklist

- [x] Audit ci-cd.yml for hardcoded paths (already uses lowercase paths)
- [x] Update solution file reference (no solution file reference in ci-cd.yml)
- [x] Update any project-specific paths (already correct in ci-cd.yml)
- [x] Update any script references (fixed PascalCase paths in check-version.cs, format.cs, generate-internals-visible-to.cs)
- [x] Removed leftover PascalCase directories (Source/, Tests/, Benchmarks/) containing only build artifacts
- [x] YAML syntax validated successfully
- [ ] Push and verify CI passes (requires push to remote)
- [ ] Verify all workflow jobs succeed (requires push to remote)

## Changes Made

1. **check-version.cs**: Fixed `Source/Directory.Build.props` → `source/Directory.Build.props`
2. **format.cs**: Removed obsolete parsing project reference, fixed `**/Benchmarks/**` → `**/benchmarks/**`
3. **generate-internals-visible-to.cs**: Fixed all `Source/` → `source/` and `Tests` → `tests` paths
4. **Cleanup**: Removed leftover PascalCase directories that contained only build artifacts

## Notes

- CI/CD pipeline (.github/workflows/ci-cd.yml) was already using correct lowercase paths
- Final CI validation requires pushing to remote
