# Review framework — task 529

**Date:** 2026-11-14
**Host task:** kanban/in-progress/529-find-generated-code-that-depends-on-internal-types-and-add-a-consumer-compile-test-lane-without-internalsvisibleto/
**Diff scope:** task commits `502772ce` and `7c74f2a7` (`36bec28f..HEAD`). Product paths: `tests/timewarp-nuru-tests/generator/generator-75-consumer-surface-no-ivt.cs`, `tests/ci-standalone/run-standalone-tests.cs`, `tests/ci-tests/Directory.Build.props`, `source/timewarp-nuru-analyzers/generators/nuru-generator.cs`, `runfiles/generate-internals-visible-to.cs`, and the three `internals-visible-to.g.cs` files (nuru, parsing, mcp). Other tasks on this branch arrived through the origin/master merge and are out of scope.
**Plan / brief:** Audit emitter references to TimeWarp.Nuru* internals, add a consumer-compile lane whose generated code is compiled under an ungranted assembly name, decide the blanket InternalsVisibleTo grant, and record the consumer-surface rule in the generator Design region.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** reviewer session not reported; implementer session not reported (implement oracle 2026-11-13T22:05:35Z)

## Budget (by-diff)

- Lines changed: 694
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
