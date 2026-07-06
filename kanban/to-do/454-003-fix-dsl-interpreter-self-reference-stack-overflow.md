# Fix DSL Interpreter Self Reference Stack Overflow

Parent: 454 (2026-07-06 full code review). Severity: HIGH.

## Description

`source/timewarp-nuru-analyzers/interpreter/dsl-interpreter.cs:440-454` —
`ResolveIdentifier` checks its cache first but only writes the cache *after* evaluating
the initializer. Self-referential or mutually-referential locals — `var x = x;` or
`var a = b; var b = a;` — are ordinary mid-typing states in an IDE, and cause infinite
recursion → `StackOverflowException`, which is uncatchable and kills the analyzer /
compiler / IDE host process. The existing `catch (InvalidOperationException)` cannot help.

## Requirements

- Write a sentinel (e.g. "resolving" marker or null placeholder) into the cache BEFORE
  recursing into the initializer; on re-entry, bail out gracefully (treat as unresolved).
- Ensure the bail-out path produces no crash and at most a benign diagnostic.

## Checklist

- [ ] Add cycle guard in ResolveIdentifier
- [ ] Unit test: `var x = x;` and mutual `var a = b; var b = a;` inside a Nuru builder block
- [ ] Verify no StackOverflow and no AD0001 in test
- [ ] `ganda runfile cache --clear` + run CI tests
