# Create Step-By-Step Agent

## Description

Create a new agent file at `.opencode/agent/step-by-step.md` that implements the step-by-step workflow rules defined in `.agent/step-by-step-workflow.md`. This agent is designed for careful, methodical implementation tasks where the user wants full control and visibility over every action.

## Requirements

- Follow the markdown frontmatter format used by existing agents in `.opencode/agent/`
- Include appropriate tool permissions for implementation work (bash, read, write, edit, list, glob, grep)
- Implement the workflow rules from `.agent/step-by-step-workflow.md`:
  - STOP AND COMMUNICATE at every step
  - Tell user what was just done (if anything)
  - Tell user what will be done next
  - Get confirmation before proceeding
  - Handle unexpected results by presenting options and asking what to do
- Agent personality should be methodical, communicative, and never race ahead

## Checklist

### Implementation
- [ ] Create `.opencode/agent/step-by-step.md` with proper frontmatter
- [ ] Include description field for agent selection
- [ ] Set mode to `subagent`
- [ ] Enable all implementation tools (bash, read, write, edit, list, glob, grep)
- [ ] Document the step-by-step workflow rules clearly
- [ ] Reference `.agent/step-by-step-workflow.md` for detailed process

## Notes

Existing agents for reference:
- `docs.md` - Simple agent with just description
- `kanban.md` - Agent with tools and detailed instructions
- `git-committer.md` - Agent with mode and external reference

The step-by-step workflow core rules:
1. Before starting: Explain understanding, state first action, ask for confirmation
2. After each action: State what was done, show result, state next action, ask to proceed
3. On unexpected results: Describe what happened, analyze, present options, ask for direction
