# Kanban Board

This Kanban board manages and tracks epics and tasks for the project using a simple folder structure.
Each item is represented by a Markdown file, and the status of the item is indicated by the folder it is in.

## Folders

1. **Backlog**: Contains tasks that are not yet ready to be worked on. These tasks have a temporary backlog scoped unique identifier.
   a. **Scratch** - contains epics or tasks that are works in progress or ideas.  They can be stored in folders under users names if needed.
2. **ToDo**: Contains tasks that are ready to be worked on. When a task from the Backlog becomes ready, it is assigned a unique identifier and moved to this folder.
3. **InProgress**: Contains tasks that are currently being worked on.
4. **Done**: Contains tasks that have been completed.

## File Naming Convention

### Tasks
- For tasks in the Backlog folder, use a short description with a 'B' prefix followed by a three-digit identifier,
such as `B001_research-authentication-methods.md` or `B002_design-game-rules.md`.
- When a task becomes "Ready," assign it a unique identifier (without the 'B' prefix) and move it to the ToDo folder, e.g.,
`001_implement-user-registration.md` or `002_create-game-logic.md`.
- <3 digit Id>_<short-description-separated-by-hyphens>

### Depth
001_top-level.md
001_001_second-level.md
001_001_001_third-level.md
001_002_second-level.md
002_top-level.md

## Definition of Ready

Before moving a task from Backlog to ToDo, ensure it meets these criteria:

### API Feature Endpoint
- [ ] Data required by client has been defined
- [ ] Endpoint requirements are clear

### Client Feature  
- [ ] Figma designs complete (if UI work)
- [ ] Requirements and acceptance criteria defined
- [ ] Dependencies identified and available

## Definition of Done

Tasks are considered complete when they meet the appropriate criteria:

### API Endpoint

**Implementation:**
- [ ] Server
  - [ ] *Endpoint (required)
  - [ ] Server side only Validator  
  - [ ] Mapper
  - [ ] *Handler (required)
- [ ] Api
  - [ ] *Request (required)
  - [ ] *Response (required) 
  - [ ] *RequestValidator (required)

**Integration Tests (Fixie):**
- [ ] *Handler Tests (required)
  - [ ] *Returns a valid Response given a valid Request via Handler
- [ ] *Endpoint Tests (required)
  - [ ] *Returns valid http Response given valid http Request via Endpoint
  - [ ] *Should throw a validation error given invalid Request (only need to test one validation rule)
- [ ] *RequestValidator Tests (required - test all validation rules)

**Documentation:**
- [ ] *Request class and properties (required)
- [ ] *Response class and properties (required)

### Client Feature

**Implementation:**
- [ ] *State (required)
- [ ] Actions
- [ ] Pipeline
- [ ] Notification
- [ ] Components
- [ ] Pages

**Integration Tests:**
- [ ] State
  - [ ] ShouldClone
  - [ ] ShouldSerialize (To support Redux DevTools)
- [ ] Every Action should have at least a positive test

**End-to-end Tests:**
- [ ] Test each Page can at least render without error given valid states
- [ ] Test happy paths for each primary use case

*Items marked with `*` are required. Others are optional based on feature needs.*

## Workflow

1. Create an item in the Backlog folder with a short description as the filename
2. When an item meets Definition of Ready criteria, assign it a unique identifier and move it to the ToDo folder
3. As you work on items, move them to the InProgress folder
4. When an item meets Definition of Done criteria, move it to the Done folder
5. Update Implementation notes as work is being done with pertinent information or references

## Task Examples

See `Kanban/Task-Examples/` for detailed examples of well-structured task specifications using narrative format with acceptance criteria.
