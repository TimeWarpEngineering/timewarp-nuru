# nuru search --cli matches the app's WithName, not the executable name

## Description

`nuru search --cli <name>` matches the app's `WithName(...)` value, not the executable name. In the task 523 consumer check, the probe `core-routing` (app named `probe` via `WithName`) was indexed, and `nuru search --cli core-routing` found nothing. Found against TimeWarp.Nuru 4.0.0-beta.5 from nuget.org, clean cache (evidence in `kanban/done/523-*/evidence/4.0.0-beta.5/` on branch `task/523-nuru-verification-skill-launch-and-drive-the-real`).

Which name is the intended key is a source question: decide from the TimeWarp.Nuru.Search source (and its Purpose/Design regions, adding them if absent) and make the code and the `--help` text agree. Docs follow the source.

## Goal (implementer restatement)

`nuru search --cli X` must find an indexed CLI whether X is its app name (`WithName`, stored as
the capabilities name) or the file name of the executable that was indexed. Today auto-index
resolves X on PATH (executable name) but the filter compares X to the capabilities name, so an app
whose `WithName` differs from its file name is indexed and then never found (and re-indexed on every
search).

Acceptance:
- The intended key is decided from the source and written in Purpose/Design regions.
- `search --cli` (and the auto-index check, and `index clear --cli`) accept the app name or the
  executable name; the `--help` text says so.
- Regression test indexes an app whose `WithName` differs from its executable name and searches by
  each name.
- User docs (search readme, `tw-cli-search` skill) name the key.
- Build, full test, audit green; local search probe shows `--cli <exe-name>` finds the routes.

## Checklist

- [x] Read the TimeWarp.Nuru.Search indexing and query code to establish the intended key (app name vs executable name vs both); record in Notes (ask the human if unclear)
- [x] Fix the code (for example match either, or key on the executable name), or fix the `--cli` help text to say app name
- [x] Add `#region Purpose` / `#region Design` stating the key
- [x] Regression test: index an app whose `WithName` differs from its executable name, then search by each
- [x] Update any user doc that names the key
- [x] `ganda runfile cache --clear`, then `dotnet run tests/ci-tests/run-ci-tests.cs` green
- [x] Verify with the `tw-nuru-verify` search probe

## Notes

- Use `.Map<T>()` in tests, not `DiscoverEndpoints()`.
- **Key decision (from source):** rows are stored under the capabilities name (`WithName`, else the
  assembly name) and `clis.cli_path` holds the executable that was run (482-011). `search --cli`
  auto-index already treated the value as an executable name (`PathResolver.ResolveExecutable`), while
  the filter compared it to the capabilities name only. That mismatch is the bug: the probe was
  indexed as `probe`, `--cli core-routing` filtered on `core-routing`, found nothing, and re-indexed on
  every search. Both names are legitimate user input (the app name is what results print in brackets;
  the executable name is what the user runs), so `--cli` matches **either**: an exact capabilities
  name wins; otherwise every row whose stored executable file name matches (with or without its
  extension; case-insensitive on Windows only). Rows indexed before `cli_path` existed match by app
  name only. This reverses the 482-011 note "search --cli still filters on the name"; its test now
  expects the file name to match too.
- `index clear --cli` takes the same key, so the two `--cli` options agree.
- `SearchIndex.GetCliVersionAsync` had one caller (the auto-index check) and was removed in favour of
  `ResolveCliNamesAsync`.
- Not changed: `skills/tw-nuru-verify/scripts/nuru-consumer-check.sh` still searches without `--cli`.
  Adding a `--cli core-routing` row there would fail every published version up to the one that ships
  this fix; left for a follow-up if wanted.

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
- 2026-11-14 implement oracle (ganda task work, implementer-claude): merged origin/master (kanban 531
  land), implemented the either-name key, tests, docs; gates green.

## Results

`nuru search --cli` and `nuru index clear --cli` now accept the app name (`WithName`) or the
executable name of an indexed CLI.

- `source/timewarp-nuru-search/services/search-index.query.cs`: new `ResolveCliNamesAsync` and
  `IsExecutableName`; `SearchAsync` filters `e.cli_name IN (resolved names)`; Design region states
  the key.
- `source/timewarp-nuru-search/endpoints/search-query.cs`: auto-index check uses the resolver, so a
  CLI already indexed under another app name is not re-indexed; Design region; `--cli` description
  says "by app name or executable name".
- `source/timewarp-nuru-search/endpoints/index-clear-command.cs`: resolves `--cli` the same way and
  reports `No indexed CLI matches: X` when nothing matches.
- `source/timewarp-nuru-search/services/search-index.cs`: removed dead `GetCliVersionAsync`;
  `IndexCliAsync` doc points at the resolver.
- Tests: new `tests/timewarp-nuru-search-tests/search-07-cli-key.cs` (8 tests, incl. an end-to-end
  run of the real tool with a fake `core-routing-534` executable named `probe` on a child-process
  PATH); `search-06` now expects the executable file name to match.
- Docs: `source/timewarp-nuru-search/readme.md`, `skills/tw-cli-search/SKILL.md` (also dropped the
  non-existent `-c` alias on the line it touched).
- Audit boy-scout: `ganda repo audit --fix` wrote the gitignored `.vscode/settings.json` window title
  (advisory `vscode-window-icon`); nothing committed from it.

### Gates

| Gate | Command | Result |
|------|---------|--------|
| Build | `./bin/dev build` | PASS (first run failed CA1305 on an interpolated `StringBuilder.AppendLine`; fixed with `AppendJoin`, rerun green) |
| Test | `ganda runfile cache --clear` then `./bin/dev test` | PASS — multi-mode 2205 total, 2199 passed, 6 skipped, 0 failed (`CliKey` 8/8, `IndexRebuildPath` 3/3); standalone 0 failed |
| Audit | `ganda repo audit` | PASS with 1 advisory (`vscode-window-icon`); after `ganda repo audit --fix`: passes all checks |

### How to validate

**Smoke** (local search probe: the `tw-nuru-verify` core-routing probe, which sets `WithName("probe")`,
built against this worktree and exposed on PATH as `core-routing`; temp `HOME`):

```bash
nuru search --cli core-routing integers   # first run
nuru search --cli core-routing integers   # second run
nuru search --cli probe integers
nuru index list
```

**Expect:**

- First run: `Auto-indexed probe v1.0.0 (5 endpoints)`, then `Found 1 result(s):` and
  `  add {x:int} {y:int} [probe]`.
- Second run: the same result with no `Auto-indexed` line (before the fix: `No results found.` and a
  re-index on every run).
- `--cli probe`: the same result.
- `index list`: one CLI, `probe v1.0.0`, 5 endpoints.

Observed exactly that on 2026-11-14. Automated equivalent:
`Should_auto_index_by_executable_name_then_search_by_either_name` in `search-07-cli-key.cs`.
