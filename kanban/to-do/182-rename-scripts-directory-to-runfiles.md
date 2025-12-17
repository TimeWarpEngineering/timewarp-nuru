# Rename scripts directory to runfiles

## Description

Rename the `scripts/` directory to `runfiles/` to better reflect that these are C# runfile applications, not shell scripts. The term "scripts" tends to confuse AI models about what's inside. Update all references in CI workflows and documentation.

## Checklist

- [ ] `git mv scripts/ runfiles/`
- [ ] Update `.github/workflows/ci-cd.yml` references (`scripts/build.cs` -> `runfiles/build.cs`)
- [ ] Update `agents.md` references
- [ ] Update `claude.md` references
- [ ] Verify `dotnet runfiles/build.cs` works
- [ ] Commit changes

## Notes

Files to update:
- `.github/workflows/ci-cd.yml` (lines 55, 69)
- `agents.md` (lines 5-6, 11)
- `claude.md` (lines 19, 22, 25)

This is part of a larger effort to improve the developer test experience and establish `runfiles/` as the convention for developer tooling across TimeWarp repositories.
