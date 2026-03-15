# Add --search and --group-filter options to --capabilities

## Purpose

Extend the `--capabilities` output to support filtering and searching capabilities, making it easier for AI agents and users to discover relevant CLI commands.

## Checklist

- [x] Add TimeWarp.Amuru dependency and migrate clipboard code (#445-001)
- [x] Implement --group-filter option (#445-002)
- [x] Create timewarp-nuru-search companion tool (#445-003)
- [ ] Implement --search option for --capabilities (#445-004)

## Notes

### Overview

This epic adds two new options to the `--capabilities` built-in route:

1. **--group-filter** - Filter capabilities by group name
2. **--search** - Search capabilities using the timewarp-nuru-search tool

### Subtasks

- **#445-001**: Add TimeWarp.Amuru dependency (DONE)
- **#445-002**: Implement --group-filter option (DONE)
- **#445-003**: Create timewarp-nuru-search companion tool (DONE)
- **#445-004**: Implement --search option (TODO)
