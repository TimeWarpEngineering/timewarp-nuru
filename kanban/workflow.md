# Kanban Workflow for Cocona to Nuru Migration Tasks

## Overview

This document describes the workflow for managing the migration of Cocona samples to Nuru using the kanban board.

## Task Structure

- **Parent Task**: `001-recreate-all-cocona-samples-using-nuru.md` (remains in in-progress throughout the project)
- **Child Tasks**: 29 individual sample recreation tasks (001_001 through 001_029)

## Workflow States

### to-do
- Initial state for all child tasks
- Tasks waiting to be started

### in-progress
- Parent task location during active development
- Child tasks move here when work begins
- Only work on one child task at a time

### done
- Completed child tasks
- Parent task moves here only after all children complete

## Git Commands for Task Movement

### Starting a Child Task
```bash
# Move child task from to-do to in-progress
git mv kanban/to-do/001-xxx-task-name.md kanban/in-progress/
```

### Completing a Child Task
```bash
# Move child task from in-progress to done
git mv kanban/in-progress/001-xxx-task-name.md kanban/done/
```

### Example Workflow

1. Start work on MinimalApp sample:
   ```bash
   git mv kanban/to-do/001-001-recreate-gettingstarted-minimalapp-with-nuru.md kanban/in-progress/
   ```

2. Complete implementation and documentation

3. Move to Done:
   ```bash
   git mv kanban/in-progress/001-001-recreate-gettingstarted-minimalapp-with-nuru.md kanban/done/
   ```

4. Select next task and repeat

## Progress Tracking

- Parent task checklist in `001-recreate-all-cocona-samples-using-nuru.md` tracks overall progress
- Individual task files track detailed implementation steps
- Each completed child task should have:
  - Working Nuru implementation
  - Overview.md with Cocona vs Nuru comparison
  - All checklist items completed