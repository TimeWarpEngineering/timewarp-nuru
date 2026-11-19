# Round 5 — general
**Date:** 2026-11-20
**Scope reviewed:** fix pass `61bb293f..59102fad` (commit `59102fad`, the M7 and M8 fix). Re-verifies M7 and M8
from round 4 and scans the delta for new defects. Effort 2, roster general.

## Summary

The fix adds a give-back step after option parsing. The rightmost greedy repeated-option run returns trailing
values to required non-catch-all positionals that are still unbound, and it keeps one value when the element is
required. A typed element stops at the first token that does not convert when a required positional or a
catch-all follows. The positional count in `EmitGreedyPositionalGiveBack` uses the same filters as
`EmitPositionalArrayConstruction` (`--` skip, consumed indices, unknown option-like tokens). `minPositionalArgs`
is the right count on both the main and alias paths. `RemoveRange` is offset from the run start, so a later
`--opt=value` entry on the same list is not touched. `TryParseCondition` with `out _` compiles for every
built-in type, and char has its own length check. routing-09 uses `--tag A run.sh` again. The runfile is
`routing-45`. The CI multi assembly picks it up by glob, so no list needed an edit.

M7 is fixed for the cases round 4 named (`{args}* {path}`, routing-09, the endpoint collection). One shape is
still wrong, and the new behaviour makes it worse. A *string* repeated option followed by a required positional
and a catch-all now exits 0 with the wrong values, where round 4 gave `Unknown command`.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.unknown-options.cs:179
  (give-back), source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.option-parsing.cs:234
  (`accepts` is null for a string element)
- Description: The in-repo sample route `fluent-docker-run --env {e}* -p {port:int}* {image} {*cmd}`
  (`samples/fluent/03-syntax/fluent-syntax-examples.cs:147`), called as `fluent-docker-run --env A=1 nginx echo hi`,
  binds `e=[A=1,nginx,echo]`, `image=hi`, `cmd=[]`, and exits 0. `--env` is a string element, so `accepts`
  is null and the run takes every token up to the end. The required count is 2 and the positionals present
  are 1, so the deficit is 1. The give-back returns only the last token (`hi`), and the catch-all gets nothing.
  Before this branch the result was `e=[A=1]`, `image=nginx`, `cmd=[echo,hi]`. Round 4 gave `Unknown command`.
  This pass turns that into a wrong binding with exit 0. The same applies to `docker --env? {e}* {*cmd}`
  (`lexer-15b`): with no required positional, `--env A echo hi` gives the catch-all nothing. The new test
  `Should_leave_non_integers_for_the_image_and_command` covers only `-p 80 nginx echo hi`, with no `--env`,
  so it misses this. The migration guide (line 226) and the migrate skill (line 322) describe optional and
  required positionals. Neither says what happens to a catch-all.
  Found by reading the emitted code. Command not run: a scratch runfile would have to be written outside
  the kitchen.
- Suggestion: When the route has a catch-all and no `--` separates it from the options, do not let a
  repeated option run greedily into the positionals. Take one value per flag occurrence, which was the
  pre-branch behaviour, or give back everything past `keep`. Add a routing-45 case for
  `fluent-docker-run --env A=1 nginx echo hi` (expect `image:nginx`, `cmd:[echo,hi]`) and one for
  `docker --env? {e}* {*cmd}`. The operator may instead keep the current rule. In that case, document in
  the migration guide and the skill that a repeated option starves a catch-all, and change the sample to
  `-- {*cmd}`.
- Status: open

### Issue 2 — Severity: suggestion
- File: kanban/.../task.md `### How to validate` (Gates, implement-fix-2, 2026-11-20)
- Description: This pass changed the generator for every route with a repeated value option. The record runs
  routing-45, routing-09, and routing-06 one at a time. It does not record `ganda runfile cache --clear`.
  `.agent/local/nuru-specific.md` requires that step after a generator change, and without it the unedited
  routing-06 may have used stale generated code. The record also has no CI multi or standalone run, so the
  other repeated-option routes in the suite (routing-09's `docker run ... --env {e}* -- {*cmd}`, routing-06,
  the endpoint collection tests) were not run against the new give-back. The PR's CI will run them. The
  next fix pass changes the same emitter, so its record should cover this.
- Suggestion: In the next fix pass, clear the runfile cache and run
  `env -u NURU_CI_PHASE dotnet run tests/ci-tests/run-ci-tests.cs`. Record the multi and standalone counts.
- Status: open

## Resolved prior

- M7 (bug): fixed for `{args}* {path}`, `--tag A run.sh` (routing-09 restored), docker build args, and the
  endpoint collection. The catch-all case is carried as Issue 1 (merged M9).
- M8 (nit): fixed. The file is `routing-45-binding-strictness.cs` and nothing else references the old name.
