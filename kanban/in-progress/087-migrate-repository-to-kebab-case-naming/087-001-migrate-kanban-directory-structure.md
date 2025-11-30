# Migrate Kanban Directory Structure

## Description

Rename the Kanban directory and all contents to kebab-case naming convention.

## Parent

087-migrate-repository-to-kebab-case-naming

## Requirements

- Rename `Kanban/` → `kanban/`
- Rename subdirectories: `ToDo/` → `to-do/`, `InProgress/` → `in-progress/`, `Done/` → `done/`, `Backlog/` → `backlog/`
- Rename all task files from underscore format (`001_Task-Name.md`) to hyphen format (`001-task-name.md`)
- Rename `Task-Template.md` → `task-template.md`
- Rename `Workflow.md` → `workflow.md`
- Rename `Overview.md` → `overview.md` in all subdirectories

## Checklist

- [x] Rename Kanban directory to lowercase
- [x] Rename subdirectories to kebab-case
- [x] Rename all task files to hyphen format
- [x] Rename metadata files (template, workflow, overview)
- [x] Update CLAUDE.md Kanban references if needed
- [x] Verify no broken references

## Notes

- ~140+ task files to rename
- Use script to generate `git mv` commands for bulk rename
- Commit after completing all renames
