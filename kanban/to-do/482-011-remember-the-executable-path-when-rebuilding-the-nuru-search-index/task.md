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

- [ ] Indexing a CLI by full path stores that path separately from `capabilities.Name`
- [ ] `index rebuild --all` executes the stored path, not the assembly name
- [ ] `search --cli` still filters on the capabilities name
- [ ] Add a test with a fake executable path whose file name differs from the capabilities name

## How to validate

Smoke:

```bash
dotnet test tests/timewarp-nuru-tests/timewarp-nuru-tests.csproj --filter FullyQualifiedName~IndexRebuild
```

Expect: Rebuild invokes the stored path. The capabilities name used for `--cli` is unchanged.

## Session

- Created: 535767 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
