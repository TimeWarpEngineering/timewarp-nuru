# Kanban Workflow for Cocona to Nuru Migration Tasks

## Overview

This document describes the workflow for managing the migration of Cocona samples to Nuru using the kanban board.

## Task Structure

- **Parent Task**: `001_Recreate-All-Cocona-Samples-Using-Nuru.md` (remains in InProgress throughout the project)
- **Child Tasks**: 29 individual sample recreation tasks (001_001 through 001_029)

## Workflow States

### ToDo
- Initial state for all child tasks
- Tasks waiting to be started

### InProgress
- Parent task location during active development
- Child tasks move here when work begins
- Only work on one child task at a time

### Done
- Completed child tasks
- Parent task moves here only after all children complete

## Git Commands for Task Movement

### Starting a Child Task
```bash
# Move child task from ToDo to InProgress
git mv Kanban/ToDo/001_XXX_TaskName.md Kanban/InProgress/
```

### Completing a Child Task
```bash
# Move child task from InProgress to Done
git mv Kanban/InProgress/001_XXX_TaskName.md Kanban/Done/
```

### Example Workflow

1. Start work on MinimalApp sample:
   ```bash
   git mv Kanban/ToDo/001_001_Recreate-GettingStarted-MinimalApp-with-Nuru.md Kanban/InProgress/
   ```

2. Complete implementation and documentation

3. Move to Done:
   ```bash
   git mv Kanban/InProgress/001_001_Recreate-GettingStarted-MinimalApp-with-Nuru.md Kanban/Done/
   ```

4. Select next task and repeat

## Progress Tracking

- Parent task checklist in `001_Recreate-All-Cocona-Samples-Using-Nuru.md` tracks overall progress
- Individual task files track detailed implementation steps
- Each completed child task should have:
  - Working Nuru implementation
  - Overview.md with Cocona vs Nuru comparison
  - All checklist items completed