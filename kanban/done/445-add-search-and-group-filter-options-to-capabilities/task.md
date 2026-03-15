# Add --search and --group-filter options to --capabilities

## Purpose

Extend the `--capabilities` output to support filtering and searching capabilities, making it easier for AI agents and users to discover relevant CLI commands.

## Checklist

- [x] Add TimeWarp.Amuru dependency and migrate clipboard code (#445-001)
- [x] Implement --group-filter option (#445-002)
- [x] Create timewarp-nuru-search companion tool (#445-003)
- [x] Implement --search option for --capabilities (#445-004)

## Notes

### Overview

This epic adds two new options to the `--capabilities` built-in route:

1. **--group-filter** - Filter capabilities by group name
2. **--search** - Search capabilities using the timewarp-nuru-search tool

### Subtasks

- **#445-001**: Add TimeWarp.Amuru dependency (DONE)
- **#445-002**: Implement --group-filter option (DONE)
- **#445-003**: Create timewarp-nuru-search companion tool (DONE)
- **#445-004**: Implement --search option (DONE)

## Results

All four child tasks completed successfully:

1. **#445-001**: TimeWarp.Amuru dependency added, clipboard code migrated
2. **#445-002**: --group-filter option implemented for filtering by group name
3. **#445-003**: timewarp-nuru-search companion tool created
4. **#445-004**: --search option implemented for --capabilities

The `--capabilities` output now supports both filtering by group and searching capabilities.
