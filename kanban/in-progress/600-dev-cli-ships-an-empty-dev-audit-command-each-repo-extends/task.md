# DevCli ships an empty dev audit command each repo extends

## Description

`ganda repo audit` is the gate every walk, merge and manual audit runs. It has no way to run
checks that belong to one repo. timewarp-ganda task 700 adds the hook: when a repo's dev CLI
has `dev audit`, repo audit runs `./bin/dev audit` and fails on a non-zero exit. The
first user is ganda's `dev deps check`, which today runs only in CI, so the walk misses it.

This task gives every repo that dev CLI command through TimeWarp.Nuru.DevCli, so a repo
adds its checks without writing the command itself.

## Wanted

- **DevCli ships `dev audit`** (`content/any/endpoints/`). With no checks registered it
  prints that there is nothing to run and exits 0.
- **A repo adds checks without redefining the route.** Two endpoints cannot both own
  `audit`, so the shell needs an extension point, for example an `IDevAuditCheck`
  interface (name, run, pass or fail with a message) that the repo implements in its own
  `tools/dev-cli`. `dev audit` runs every registered check in order, prints one line each,
  and exits non-zero when any fails. How checks are found (DI registration or source
  generation) is the implementer's call; it must work in the AOT dev CLI. Say which in
  Results.
- **Output.** One line per check, with its wall time. A failing check's message is printed
  in full. The last line says how many passed and failed.
- **Readme.** Add `audit` to the endpoint table and document the extension point with a
  short example.

## Rules

- `dev audit` never calls `ganda repo audit`. ganda repo audit calls `dev audit`, so the
  other direction would loop.
- It is for pre-test checks: fast, no full test suite. Say so in the readme.

## Out of scope

- The ganda side (repo audit calling `dev audit`, and ganda's own checks) is
  timewarp-ganda task 700. It lands without waiting for this task.
- Adding checks for this repo. If Nuru has a repo-specific pre-test check in its CI
  pipeline today, list it in Results as a follow-up, do not move it here.

## Checklist

- [ ] `dev audit` endpoint in DevCli; with no checks it exits 0
- [ ] Extension point for repo checks, working under AOT
- [ ] A failing check makes `dev audit` exit non-zero and prints its message (test)
- [ ] Per-check line with wall time, and a summary line
- [ ] Readme endpoint table and extension example
- [ ] Results: how ganda 700's own `dev audit` endpoint moves onto this extension point
      once ganda takes the new DevCli version (a route clash otherwise)

## Session

- Created: 64677b22-d705-4b25-b537-4b53732bcb92 (2026-11-21)

## Notes

- Steven 2026-11-21: "maybe even just a dev audit so the main audit just calls dev audit if
  dev audit exists run it and we put a shell inside of Nuru for dev audit". Then "yes file
  both".
- Filed from the timewarp-ganda cockpit session. The launch belongs to a timewarp-nuru
  session (`ganda herdr work 600 --repo timewarp-nuru`).
