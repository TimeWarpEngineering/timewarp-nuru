# CsCheck pilot: fuzz route parsing with random argv

## Description

Add a CsCheck property test that feeds random argv into Nuru route parsing/dispatch and asserts it never throws an unhandled exception and always yields a meaningful exit code (success, or a defined usage/error code with a message).

Sibling pilot: Amuru task 128 (quote-then-parse round-trip), https://github.com/TimeWarpEngineering/timewarp-amuru/blob/master/kanban/to-do/128-cscheck-pilot-quote-then-parse-round-trip-property-for-argument-escaping/task.md

## Context

- Busara (Wisdom) verdict 2026-10-13: GREEN for CsCheck as a **test-only** dependency, pilots first. We have no property-based testing today.
- Akili (Intelligence) evaluation: `/workspace/ideas/2026-10-13-cscheck-eval.md` (box). Verified CsCheck works in a Jaribu runfile.
- CsCheck: https://github.com/AnthonyLloyd/CsCheck (Apache-2.0, forkable if needed).

## Rules

- Pin **CsCheck 4.9.1** (no need to wait for 5.0; tests do not need AOT).
- Test-only. CsCheck must never be referenced by product/package code.
- Pin `CsCheck_Seed` in CI; print the seed on every failure so it can be replayed.
- Do not use `Faster` in build-blocking tests (CI timing is flaky).

## Kill criterion

If both pilots (this one and the sibling) find nothing and the generators are costly to maintain, stop at these two pilots.

## Deferred (until pilots show value)

- State reducer model-based tests
- Parse/format round-trip
- Terminal / Flexbox invariants

## Goal (implementer restatement)

Pilot CsCheck (test-only, pinned 4.9.1) on Nuru argv handling. Add a property test that feeds generated
argv into a Nuru app with a broad route table and shows that `RunAsync` never throws and always ends with a
defined exit code (0, or 1 with a message). CI must stay deterministic: pin the seed, print a replayable seed
on every failure, no `Faster`. Record what the pilot found so the kill-criterion review can decide whether
property testing continues past the two pilots.

Acceptance:

- CsCheck 4.9.1 is referenced only by test code (the runfile and `tests/ci-tests`), never by product projects.
- Generators cover known options, flags, values, unknown tokens, empty strings, and unicode.
- The property runs in `./bin/dev test` (CI multi-mode and standalone) and passes.
- The seed is pinned for CI and every failure prints a `CsCheck_Seed` that replays the case.
- Findings are recorded in this kitchen.

## Checklist

- [x] Add CsCheck 4.9.1 to the test project/runfile only
- [x] Generators for random argv (options, flags, values, unknown tokens, empty, unicode)
- [x] Property: no unhandled exceptions; exit code is in the defined set
- [x] Pin CsCheck_Seed in CI; print seed on failure
- [x] Record findings for the kill-criterion review

## Review

- Round 1: Done

## Notes

Not launched. Filed 2026-10-13 at Steven's request ("sure lets try").

## Session

- 2026-11-15 (implementer-claude): merged origin/master (kanban 520 landing) into the branch before the
  first commit. Built the pilot in `tests/timewarp-nuru-tests/routing/routing-41-argv-fuzz-property.cs`.
  Ran the explore mode for about 30M random cases in total (CsCheck_Iter 20k/50k, CsCheck_Time 30/60/90 s).
  The first `./bin/dev test` failed in the CI multi-mode assembly only (NU1504 duplicate PackageReference,
  then finding 2 below); both are handled, and the second run passed.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 43078b26-6d48-413d-82e2-c757977a61e5, max-turns 500, Done — 2026-11-15T02:57:12Z
- Review oracle round 1: Grok 4.7 (grok, vendor xai), body tw-implementation-review, effort 2, roster general, turn cap 120 (cursor session uncapped; no --max-turns flag), session not reported, disposition clean — 2026-11-15. No findings. The restatement matches the task; no misread. See `review/round-1/merged.md` and `review/disposition.md`.
- Review oracle: review by reviewer-cursor (cursor, vendor xai, model grok-4.7), session not reported, max-turns uncapped (no --max-turns flag; bound 120), artifacts general.md, merged.md, disposition.md — 2026-11-15
- Review oracle: review by reviewer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 120), artifacts general.md, merged.md, disposition.md — 2026-11-15T03:05:34Z

## Results

### What changed

- `Directory.Packages.props`: `CsCheck` 4.9.1 under the Testing group, commented test-only.
- `tests/timewarp-nuru-tests/routing/routing-41-argv-fuzz-property.cs` (new, `#:package CsCheck`). The CI
  multi-mode assembly gets the package through that directive (transitive directives are on), so
  `tests/ci-tests/Directory.Build.props` only carries a comment; a second PackageReference fails with NU1504.
  No product project references CsCheck.
- Generators: `Token`/`Argv` (route words, declared options and aliases, built-in flags, `--`, `-`,
  `--key=value` forms, unknown `-x`/`--xyz` options, numbers including overflow and negatives, empty and
  whitespace strings, unicode including emoji, ZWJ, zero-width, NBSP, NUL, newlines, and RTL override,
  random strings) and `WellFormed` (a valid argv per route, with option order and alias choice drawn).
- Properties, each a deterministic 1000-case walk from `PinnedSeed` (`0N0XIzNsQ0O2`) or `CsCheck_Seed`:
  - `Random_argv_never_throws_and_exits_with_defined_code`: no exception out of `RunAsync`; exit is 0, or
    1 with a message on stdout or stderr; at least one case reaches a handler.
  - `Well_formed_argv_reaches_its_handler`: every case exits 0 and writes the handler's expected text.
  - `Explore_random_argv_with_shrinking`: CsCheck `SampleAsync` with fresh seeds and shrinking for both
    generators. It runs only with `NURU_ARGV_FUZZ_EXPLORE=1`, so it never blocks CI. `Faster` is not used.
- Seed contract: every failing case prints `CsCheck_Seed=<seed> argv=[...]: <why>`. Setting that variable
  makes the case the first one of the walk, and a seed printed by the explore test replays the same way.
- Two `[Skip]` regression tests pin findings 1 and 2 below.

### Findings for the kill-criterion review

The pilot found real defects, so the kill criterion ("both pilots find nothing") is not met from this side.
Generator cost: about 150 lines of generators for a 14-route app. Walk cost in CI: 0.06 s plus 0.04 s.

1. **Bug: option conversion runs before literal matching.** `route-matcher-emitter.cs` `EmitMatch` appends
   the option conversions before the literal and positional-count checks. A route whose literal does not
   match still fails on its typed option and exits 1. With `scale {replicas:int} --ratio? {ratio:double?}`
   and `zoom --ratio {level}`, the valid `zoom --ratio fast` exits 1 with "Invalid value 'fast' for option
   '--ratio'. Expected: double". It also blames routes the user never typed: `--stage -226` reports the
   `release` route's Stage values. Pinned by the skipped
   `Option_value_error_on_another_route_does_not_block_a_valid_command`.
2. **Bug (crash): `AddCommandLine` throws out of `RunAsync`.** With configuration on, argv goes to
   `ConfigurationBuilder.AddCommandLine(args)`, and a single-dash token with `=` (`-v=true`, `-c=Release`,
   `-=x`) throws `FormatException: The short switch '-v=true' is not defined in the switch mappings`.
   Tokens after `--` are passed too. Configuration detection is compilation-wide (`nuru-generator.cs` 6c), so
   one `IConfiguration`/`IOptions<T>` use anywhere turns it on for every app. That is why only the CI
   multi-mode assembly hit it at first. Pinned by the skipped
   `Short_option_with_equals_does_not_crash_a_configured_app`.
3. **Bug: positional values that look like `/name=...` or `/name:...` disappear.** The generated
   `IsConfigArg` peels them as configuration overrides, so `greet /path=with/equals` (or `/opt/a:b`) exits 1
   with "Unknown command". This is the `/` variant of the known `--option=value` filter bug (the skipped test
   in routing-24).
4. **Design question: `--json-args -` reads the process stdin through `System.Console`, not the app's
   `ITerminal`.** `TestTerminal` cannot supply it, and with an open stdin pipe `RunAsync` waits for EOF. The
   explore run hung on it once.
5. **Inconsistency: error streams.** Argv value conversion errors go to stdout (`WriteArgumentError`,
   documented "as it always was"). Unknown options and unknown commands go to stderr. The property
   accepts a message on either stream.
6. Not a Nuru defect: explore found `sum 2147483647 1` overflowing the test handler's `int` `Sum()`.
   Handler exceptions propagate by design (`documentation/user/reference/builder-api.md`), so the
   handler now sums as `long`.

The walk keeps findings 2 and 4 out of the random generator (`WithoutKnownFindings`). The well-formed
generator prefixes values that start with `-` or `/` (finding 3). Remove those rewrites when the bugs are
fixed.

Follow-ups to file (not filed from this walk): findings 1, 2, and 3 as bugs; findings 4 and 5 as design
questions.

### How to validate

Smoke:

```bash
dotnet run tests/timewarp-nuru-tests/routing/routing-41-argv-fuzz-property.cs < /dev/null
NURU_ARGV_FUZZ_EXPLORE=1 CsCheck_Time=10 dotnet run tests/timewarp-nuru-tests/routing/routing-41-argv-fuzz-property.cs < /dev/null
CsCheck_Seed=3w0Nccc8ovC2 dotnet run tests/timewarp-nuru-tests/routing/routing-41-argv-fuzz-property.cs < /dev/null
```

Expect:

- First command: `walk from seed 0N0XIzNsQ0O2: 1000 cases, 16 exit 0, 0 failed`, then
  `walk from seed 0N0XIzNsQ0O2: 1000 cases, 1000 exit 0, 0 failed`; 3 passed, 2 skipped (findings 1 and 2).
- Second command: the explore test prints `Passed N iterations.` twice.
- Third command: both walks start from the given seed and pass; that seed was the explore failure behind
  finding 3, before the well-formed generator stopped producing `/`-prefixed values.
- `grep -rn CsCheck source/ --include=*.cs --include=*.csproj --include=*.props` prints nothing. The only
  hits under `source/*/obj` are the restore's `centralPackageVersions` list, not a reference.

### Gates

- `./bin/dev build`: passed (0 warnings, 0 errors).
- `./bin/dev test`: passed. Multi-mode: 2238 total, 2230 passed, 8 skipped, 0 failed. Standalone: 48
  files, 0 failed. The first run failed (NU1504 duplicate CsCheck PackageReference, then finding 2 in the
  multi-mode assembly); both fixed before this run.
- `ganda repo audit`: passed with one advisory warning (`vscode-window-icon`: the gitignored local
  `.vscode/settings.json` was missing in this worktree). `ganda repo audit --fix` wrote that local file
  (untracked, nothing to commit), and its re-audit reports "Repository passes all audit checks."
  The commit hook's audit then failed `runfile-executable` on the new runfile, which the working-tree audit
  had not flagged while the file was untracked. Fixed with `chmod +x` (commit 25aa7885), and that commit attested.

### Review

- Round 1, roster general, effort 2. Counts: bug 0, suggestion 0, nit 0 (all open 0). Disposition: clean. No wontfix, no escalation.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
- The restatement matches the task. No misread.
