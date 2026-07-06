# Fix DSL Interpreter Self Reference Stack Overflow

Parent: 454 (2026-07-06 full code review). Severity: HIGH.

## Description

`source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs`
(`ResolveIdentifier`, ~line 440) checked its cache first but only wrote the cache *after*
evaluating the initializer. Cyclic references therefore recursed forever →
`StackOverflowException`, which is uncatchable and kills the analyzer / compiler / IDE
host process. The existing `catch (InvalidOperationException)` could not help.

Worse than originally reported: the review assumed mid-typing states (`var x = x;`), but
the FIELD path cycles on **cleanly-compiling code** — `FindFieldAssignmentInContainingType`
evaluates the RHS of every assignment to a field, so a plain `Pattern = Pattern;` or a
mutual pair (`PatternA = PatternB; PatternB = PatternA;`) anywhere in the containing type
crashed the generator on valid user code.

(The local-variable repros turned out NOT to crash: `var x = x;` has a binding error, so
`GetSymbolInfo(...).Symbol` is null and resolution bails before recursing.)

## Checklist

- [x] Add cycle guard in ResolveIdentifier (null sentinel written into VariableState
      BEFORE evaluating; re-entrant resolution hits the cache and terminates)
- [x] Guard covers both the local-initializer and field-assignment paths (sentinel is
      written before either branch runs)
- [x] Regression test: `var x = x;`, mutual `var a = b; var b = a;`, field
      self-assignment, and mutual field assignment inside Nuru builder code
- [x] Verify no StackOverflow: reproduced the crash against the UNFIXED interpreter
      (process died with a stack-overflow trace on the field repros), then 4/4 pass
      with the fix
- [x] `ganda runfile cache --clear` + run CI tests

## Results

- Fix: `dsl-interpreter.cs` ResolveIdentifier pre-caches `VariableState[symbol] = null`
  before evaluating; success paths overwrite with the real value. Cycles now resolve to
  null (treated as unresolved) instead of recursing.
- Regression test: `tests/timewarp-nuru-tests/generator/generator-28-interpreter-cycle-guard.cs`
  hosts NuruGenerator in a CSharpGeneratorDriver over in-memory source — first
  Roslyn-hosted generator test in the repo.
- CI integration: the test references timewarp-nuru-analyzers as a LIBRARY, whose shared
  parsing types (Lexer/Token) collide with timewarp-nuru's in the single multi-mode
  compilation (CS0433). It is excluded from the multi assembly (CiTestExcludes with
  reason) and `tests/ci-tests/run-ci-tests.cs` now runs a second standalone phase (via
  TimeWarp.Amuru) so CI still covers it through the same entry point; the runner's exit
  code reflects both phases.
- Full CI: multi-mode 1274 tests (1267 passed, 7 pre-existing skips) + standalone 4/4,
  exit 0.

## Session

- Created: 2026-07-06 (full-repo review session)
- Implementation: 2026-07-06 (same session)
