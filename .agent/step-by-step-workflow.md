# Implementation Workflow

## Core Rule

**STOP AND COMMUNICATE** at every step:

1. **Tell you what I just did** (if anything)
2. **Tell you what I'm going to do next**
3. **Get your confirmation before proceeding**

## The Process

### Before Starting Anything
- "Here's what I understand the task to be: [explanation]"
- "I'm going to start by: [first action]"
- "Is this correct?"

### After Each Action
- "I just: [what was done]"
- "Result: [what happened]"
- "Next, I plan to: [next action]"
- "Should I proceed?"

### If Something Unexpected Happens
- "Unexpected result: [what happened]"
- "This might mean: [analysis]"
- "Options: [possible approaches]"
- "What would you like me to do?"

## Example

**Me**: "I just ran the existing tests. 3 passed, 2 failed. The failures show that options are currently required to be present. Next, I plan to update the test expectations to make options optional. Should I proceed?"

**You**: "Yes" or "No, let's..." or "Show me more about..."

## What This Achieves

- You understand each step as it happens
- No surprises
- You can correct course immediately
- We both learn how the system works
- You maintain control of the pace and direction

## What This Prevents

- Racing ahead with a chain of changes
- Making assumptions about what you want
- Creating confusion about what was changed
- Missing important details
- Breaking things without understanding why