# Implement REPL functionality with source generator support

## Description

Implement REPL (Read-Eval-Print-Loop) functionality directly in `timewarp-nuru` with full source generator support. This replaces the old `timewarp-nuru-repl` package which relied on runtime endpoint discovery.

The new implementation should:
- Work with the V2 source generator architecture
- Generate REPL loop code at compile time where possible
- Support interactive command entry and execution
- Maintain feature parity with the reference implementation

## Reference Implementation

The old REPL code is preserved for reference:
- `source/timewarp-nuru-repl-reference-only/`
- `tests/timewarp-nuru-repl-tests-reference-only/`

## Key Features to Implement

1. **Interactive mode** (`--interactive` or `-i` flag)
2. **Command history** (up/down arrow navigation)
3. **Tab completion** for commands and parameters
4. **Custom prompt** configuration
5. **Exit commands** (`exit`, `quit`, Ctrl+C handling)
6. **Help integration** (show available commands in REPL)

## Checklist

### Analysis
- [ ] Review `source/timewarp-nuru-repl-reference-only/` for key patterns
- [ ] Identify what can be source-generated vs runtime
- [ ] Design REPL architecture compatible with V2 generator

### Core Implementation
- [ ] Add `AddRepl()` DSL method to builder
- [ ] Implement REPL loop in `timewarp-nuru`
- [ ] Generate REPL entry point via source generator
- [ ] Handle `--interactive` / `-i` flag

### Features
- [ ] Command history (readline-style)
- [ ] Tab completion integration
- [ ] Custom prompt support
- [ ] Graceful exit handling

### Testing
- [ ] Create REPL tests using TestTerminal
- [ ] Test interactive mode entry/exit
- [ ] Test command execution in REPL
- [ ] Test history navigation

### Samples
- [ ] Update `samples/_repl-demo/` to use new implementation
- [ ] Rename folder to numbered convention after migration

## Architecture Considerations

### What can be source-generated:
- Route matching logic (already done)
- Help text generation
- Completion candidates

### What must be runtime:
- REPL loop itself (read input → match → execute → repeat)
- History management
- Terminal I/O

### Potential approach:
The source generator could emit a `RunRepl()` method alongside `RunAsync()` that:
1. Uses the same generated route matching
2. Wraps it in a read-eval-print loop
3. Integrates with generated help/completion

## Notes

- Old REPL used `EndpointCollection` for runtime route discovery - this no longer exists
- New REPL must work with compile-time generated route matching
- Consider whether REPL needs any runtime route registration (probably not for v1)
