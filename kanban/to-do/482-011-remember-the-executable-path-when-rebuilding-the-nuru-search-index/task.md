# Remember the executable path when rebuilding the nuru search index

## Parent

482 — Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

Finding S-3 from task 482. Fix it before the `3.0.0` tag. The parent review record is the source of the disposition. Do not reopen the whole review on this id.

`index rebuild --all` does not remember the executable path.

`--all` calls `TryIndexCliAsync(cli.Name)`. The index stores `capabilities.Name`, which the emitter sets from `WithName` or `Assembly.GetEntryAssembly().GetName().Name`. `--cli` rejects anything `File.Exists` does not see, so a PATH name fails, while a full path is stored under the assembly name. The next rebuild starts that assembly name as a process.

Evidence: `source/timewarp-nuru-search/endpoints/index-rebuild-command.cs`, `services/search-index.cs`, `generators/emitters/capabilities-emitter.cs` (capabilities `Name`). Parent record: `review/supporting.md` S-3.

Store the path that was actually executed. `--all` must invoke that path. `--cli` on search should still filter by the capabilities name.

## Checklist

- [x] Indexing a CLI by full path stores that path separately from `capabilities.Name`
- [x] `index rebuild --all` executes the stored path, not the assembly name
- [x] `search --cli` still filters on the capabilities name
- [x] Add a test with a fake executable path whose file name differs from the capabilities name

## How to validate

Smoke:

```bash
dotnet test tests/timewarp-nuru-tests/timewarp-nuru-tests.csproj --filter FullyQualifiedName~IndexRebuild
```

Expect: Rebuild invokes the stored path. The capabilities name used for `--cli` is unchanged.

## Session

- Created: 535767 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)
- Implemented: claude (ganda task work implement oracle, 2026-10-05)

## Results

- `clis` gets a nullable `cli_path` column. `SearchIndex.InitializeAsync` adds it with
  `ALTER TABLE` to an index that predates it. Older rows keep a null path.
- `SearchIndex.IndexCliAsync` takes `string? cliPath`. `CliInfo.CliPath` exposes it.
- `index rebuild --cli` resolves its argument to an absolute path. That is either an existing
  file or a name found on PATH, which used to be rejected. The command stores that path beside
  `capabilities.Name`.
- `index rebuild --all` runs `CliPath`. It falls back to the name only for rows that have no
  stored path.
- Auto-indexing for `search --cli` also stores the resolved PATH executable. Search filtering
  stays on `cli_name`, which is the capabilities name.
- New test `tests/timewarp-nuru-search-tests/search-06-index-rebuild-path.cs` covers 3 cases:
  - A fake `fake-tool-482-011` script that reports the name `mycli`: its full path is stored,
    and `--all` re-runs that path and picks up the new version.
  - `--cli mycli` matches and `--cli fake-tool-482-011` does not.
  - Indexing without a path stores a null path.
  - A legacy index has the column added.
- The Smoke in the task description names a `timewarp-nuru-tests` csproj filter that does not
  exist. Search tests are runfiles, so the command below replaces it.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-search-tests/search-06-index-rebuild-path.cs
dotnet run tests/ci-tests/run-ci-tests.cs
```

Expect: `IndexRebuildPath` 3/3 pass. In those tests, `rebuild --all` re-runs the stored path,
which updates the version to 2.0.0 with "1 succeeded, 0 failed". `search --cli mycli` (the
capabilities name) still matches. The CI runner exits 0.

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
