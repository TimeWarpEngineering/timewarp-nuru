## Kanban Task Guidelines

### Folder Structure = Status

Tasks live in folders that indicate their status:
- `kanban/to-do/` - Tasks waiting to be started
- `kanban/in-progress/` - Tasks being worked on
- `kanban/done/` - Completed tasks
- `kanban/backlog/` - Future/deferred tasks

Move tasks between folders using `git mv`:
```bash
git mv kanban/to-do/123-task-name.md kanban/in-progress/
git mv kanban/in-progress/123-task-name.md kanban/done/
```

### NEVER Add These Fields

- **Status** - folder location IS the status
- **Priority** - not used
- **Category** - unnecessary
- **Priority Justification** - not needed
- **Implementation Status** - no temporal indicators

**WHY:** Status is determined by folder location. Adding status fields creates redundancy.

### Allowed Fields

- **Description** - Brief description of task purpose and goals
- **Parent** (optional) - Reference to parent epic/task
- **Requirements** (optional) - Criteria for completion
- **Checklist** (optional) - Subtasks to complete
- **Notes** (optional) - Additional context, resources
- **Implementation Notes** (optional) - Notes added during work

### Task Naming

Format: `{number}-{brief-description}.md`

Examples:
- `359-implement-shared-agent-instructions.md`
- `001-001-recreate-minimalapp-with-nuru.md` (child of 001)

### Workflow

1. **Starting work**: Move task from `to-do/` to `in-progress/`
2. **Completing work**: Move task from `in-progress/` to `done/`
3. **Deferring**: Move to `backlog/`
4. **CRITICAL**: Never start a new task without explicit user approval
