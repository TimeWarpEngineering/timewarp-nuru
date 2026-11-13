# nuru search --cli matches the app's WithName, not the executable name

## Description

`nuru search --cli <name>` matches the app's `WithName(...)` value, not the executable name. In the task 523 consumer check, the probe `core-routing` (app named `probe` via `WithName`) was indexed, and `nuru search --cli core-routing` found nothing. Found against TimeWarp.Nuru 4.0.0-beta.5 from nuget.org, clean cache (evidence in `kanban/done/523-*/evidence/4.0.0-beta.5/` on branch `task/523-nuru-verification-skill-launch-and-drive-the-real`).

Which name is the intended key is a source question: decide from the TimeWarp.Nuru.Search source (and its Purpose/Design regions, adding them if absent) and make the code and the `--help` text agree. Docs follow the source.

## Checklist

- [ ] Read the TimeWarp.Nuru.Search indexing and query code to establish the intended key (app name vs executable name vs both); record in Notes (ask the human if unclear)
- [ ] Fix the code (for example match either, or key on the executable name), or fix the `--cli` help text to say app name
- [ ] Add `#region Purpose` / `#region Design` stating the key
- [ ] Regression test: index an app whose `WithName` differs from its executable name, then search by each
- [ ] Update any user doc that names the key
- [ ] `ganda runfile cache --clear`, then `dotnet run tests/ci-tests/run-ci-tests.cs` green
- [ ] Verify with the `tw-nuru-verify` search probe

## Notes

- Use `.Map<T>()` in tests, not `DiscoverEndpoints()`.

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
