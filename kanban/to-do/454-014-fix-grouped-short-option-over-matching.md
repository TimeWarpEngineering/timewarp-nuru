# Fix Grouped Short Option Over Matching

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M11).

## Description

`source/timewarp-nuru-parsing/runtime/matchers/option-matcher.cs:79-85` — for an option
with a short alias (e.g. `--edit,-e`), TryMatch matches any `-xyz` argument where
`arg.Contains(shortChar)` ANYWHERE in the string. So `-e` matches `-help` (the 'e' in
"help"); a route with `-s`, `-e`, or `-t` matches `-set`. The intent is grouped flags
(`-la` → `-l` + `-a`), but it never verifies that every character in the group is itself
a defined flag.

Impact: runtime matcher mis-routes real input.

Also (same lines): `arg.Contains(shortChar.ToString(), ...)` allocates a one-char string
per call — `arg.IndexOf(shortChar) >= 0` (or better, the corrected logic) avoids it.

## Requirements

- Grouped-flag matching must only apply when the argument consists entirely of known
  short-flag characters for the route (standard getopt semantics). Otherwise no match.

## Checklist

- [ ] Correct grouped-flag semantics in OptionMatcher.TryMatch
- [ ] Remove per-call string allocation
- [ ] Tests: `-e` does NOT match `-help`; `-la` matches `-l`+`-a` when both defined;
      `-lx` with unknown `x` does not match
- [ ] `ganda runfile cache --clear` + run CI tests
