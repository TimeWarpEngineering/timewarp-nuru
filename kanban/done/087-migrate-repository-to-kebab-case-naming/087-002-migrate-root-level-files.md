# Migrate Root Level Files

## Description

Rename root-level files to lowercase naming convention.

## Parent

087_Migrate-Repository-To-Kebab-Case-Naming

## Requirements

- Rename `CLAUDE.md` → `claude.md`
- Rename `Agents.md` → `agents.md`
- Rename `OPTIMIZATION-RESULTS.md` → `optimization-results.md`
- Rename `LICENSE` → `license`
- Keep `readme.md` as-is (already lowercase)
- Keep `changelog.md` as-is (already lowercase)

## Checklist

- [x] Rename CLAUDE.md to lowercase
- [x] Rename Agents.md to lowercase
- [x] Rename OPTIMIZATION-RESULTS.md to lowercase
- [x] Rename LICENSE to lowercase
- [x] Update any internal references to these files

## Notes

- Low risk - no project dependencies
- Verify git properly tracks case-sensitive renames
