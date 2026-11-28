# Round 2 — general
**Date:** 2026-11-28
**Scope reviewed:** fix commit `2eb06261` against round-1 M1 and M2, plus the surrounding `changes`, `green-key`, `stage`, and `ci` jobs in `.github/workflows/workflow.yml`

## Summary

The fix takes `matrix` out of the stage job `if`. Pack-only now picks the legs in
`strategy.matrix` through `fromJSON(... == 'pack-only' && '["build"]' || '[four legs]')`.
`strategy` may read `needs`. The `!= 'skip'` gate stays in the job `if`. The precedence is
correct: `==` binds before `&&`, and `&&` before `||`. The two outputs are grouped in
parentheses. On a push the `changes` outputs are empty by design, so the `green-key` output
decides. An empty value keeps the four legs. `name: ${{ matrix.stage }}`, `runs-on`, and the step
`if`s may all read `matrix`. The `ci` job reads only `needs.stage.result`, and with one pack-only
leg that is success or failure. It does not need artifacts from the test or samples legs.
The fix record shows actionlint 1.7.12 exits 0 on the file. It also exits 1 with
`context "matrix" is not allowed here` once the old `if` is put back. That covers M1's
parse-check request. The test now checks that the stage job `if` text, up to `runs-on:`,
does not contain `matrix`.

M2: the `green-key` job now notes UTC ticks in its first step. Its last step runs under
`always() && !cancelled()` and logs `green-key-v1 green-key-seconds=`, rounded up the same
way as `ci-finish-seconds`. The workflow default shell is `pwsh`, so `$env:GITHUB_ENV` and
`[DateTimeOffset]` work. The tally rule in Results now subtracts `green-key-seconds` on every
push, and it says when `ci-finish-seconds` is subtracted again.

The goal restatement matches the task. The fix pass also added the `green-key-seconds`
subtraction to `## Requirements`. That makes the requirement stricter, not weaker. It is
how M2 was adopted, so it is not a misread.

No new defects found. No targeted command was run.

## Issues

<!-- None. -->
