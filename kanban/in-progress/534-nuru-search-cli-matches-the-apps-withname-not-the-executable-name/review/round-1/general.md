# Round 1 — general
**Date:** 2026-11-14
**Scope reviewed:** product commits `3c4f8f6a` and `64255366` (search `--cli` key). Kitchen `## Goal (implementer restatement)` checked against the task description. `## Results`, `### Gates`, and `### How to validate` read and not re-run.

## Summary

`nuru search --cli` and `nuru index clear --cli` resolve the app name (capabilities name: `WithName`, otherwise the assembly name) or the indexed executable's file name. An exact capabilities name wins; otherwise every row whose stored `cli_path` file name matches, with or without a single extension, case-insensitive on Windows only. Auto-index still resolves a PATH executable, and it skips that step when either name is already indexed, which closes the `core-routing` / `probe` re-index loop.

The restatement matches the task. The description asks the source to decide the key and for the code, `--help`, and docs to agree. Notes record that decision from the 482-011 storage (`clis.name` plus `clis.cli_path`), and the Design regions state it. There is no misread.

`## Results` covers the change: build, `./bin/dev test` (2205 total, 2199 passed, 6 skipped, 0 failed; `CliKey` 8/8), audit, the executable-bit follow-up, and a smoke probe with expected output. Those gates were not re-run.

Checked and not filed:

- `ResolveCliNamesAsync` parameterizes `e.cli_name IN (...)`. An empty resolution returns no rows before the query. `GetCliVersionAsync` has no remaining callers.
- `search-06` now expects the executable file name to match. Task Notes state that this reverses the 482-011 filter note on purpose.
- `search-07-cli-key.cs` covers both names, a `.exe` stem, exact-name precedence, two CLIs sharing a file name, a null `cli_path`, clear by executable name, an unknown clear, and an end-to-end auto-index through `TimeWarp.Nuru.Search.dll` under a temp `HOME` / `USERPROFILE`. `DatabasePath` reads `SpecialFolder.UserProfile`, which is that home on Unix.
- Help text, `source/timewarp-nuru-search/readme.md`, and `skills/tw-cli-search/SKILL.md` name both keys. `index rebuild --cli` stays a path or PATH name, which is a different option.
- A shared executable name selects every matching row, while the one-line help says "one CLI". The Design region and `Should_match_every_cli_sharing_an_executable_name` record the collision. The task's acceptance is the either-name key for one CLI.
- `changelog.md` has no Unreleased line for this fix. The task's user docs are the search readme and `tw-cli-search`, and both name the key.
- The skill "How It Works" line about re-indexing when the version changes was already ahead of the code. `GetCliVersionAsync` returned when any row existed; it did not compare versions.
- The end-to-end test returns immediately on Windows, same as `search-06`, because the fake CLI is a shell script. The in-memory resolver tests still run there.

## Issues
