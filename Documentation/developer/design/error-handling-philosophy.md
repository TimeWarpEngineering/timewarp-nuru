# Error Handling Philosophy

Design principles and approach for error handling in TimeWarp.Nuru.

## Core Philosophy

Nuru follows a **"fail fast with clear messages"** approach to error handling.

## Design Principles

### 1. Simplicity First
Avoid complex error recovery mechanisms in favor of clear, predictable failures. The framework should not try to guess user intent or automatically recover from errors.

### 2. Clear Communication
Provide specific, actionable error messages that tell users:
- What went wrong
- Where it went wrong (parameter names, values)
- How to fix it (when possible)

### 3. Graceful Degradation
When commands are invalid or ambiguous:
- Show available commands via automatic help generation
- Suggest similar commands when appropriate
- Provide usage examples

### 4. Stream Separation
Maintain strict separation between output streams:
- **stdout**: Normal command output and results only
- **stderr**: All error messages and diagnostic information

This enables proper piping and scripting workflows where errors don't contaminate data streams.

### 5. Standard Exit Codes
Use conventional exit codes for compatibility with shell scripts and CI/CD:
- `0` = Success
- `1` = General error (default for all failures)
- Future: Consider specific exit codes for different error types

## Error Categories

### Parse-Time Errors
Detected during route pattern parsing. Should fail immediately with clear syntax error messages.

### Bind-Time Errors
Detected during parameter binding. Should indicate which parameter failed and why.

### Runtime Errors
Exceptions during handler execution. Should preserve original error information while adding context.

## Non-Goals

- **Error Recovery**: Nuru does not attempt to recover from errors or guess corrections
- **Retry Logic**: Failed commands should be re-run by the user, not retried automatically
- **Complex Error Codes**: Keep it simple with 0/1 rather than elaborate error code schemes