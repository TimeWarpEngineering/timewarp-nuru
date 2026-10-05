# Remove or internalize NuruAppHolder and ResponseDisplay before 3.0

## Parent

482 — Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

Finding R-9 from task 482. Fix it before the `3.0.0` tag. The parent review record is the source of the disposition. Do not reopen the whole review on this id.

`NuruAppHolder` and `ResponseDisplay` are public and unused.

`NuruAppHolder.SetApp` is internal and has no callers. `Build()` does not set it. `App` throws `NuruApp has not been set. This is a framework bug`. `ResponseDisplay.Write` is unreferenced and is marked `RequiresUnreferencedCode` / `RequiresDynamicCode`. The generator writes handler output itself.

Evidence: `source/timewarp-nuru/services/nuru-app-holder.cs`, `source/timewarp-nuru/io/response-display.cs`. Parent record: `review/runtime-core.md` R-9.

Make them internal or delete them before the stable surface freezes. Do not add a caller just to keep them public.

## Checklist

- [x] `NuruAppHolder` and `ResponseDisplay` are no longer public, or they are deleted
- [x] No remaining reference in `source/` or `tests/` fails to compile
- [x] Public API surface (ShipPublicApi or the assembly's public types) does not list them
- [x] AOT publish of the delegates test app still reports zero IL2026/IL3050

## How to validate

Smoke:

```bash
rg -n "NuruAppHolder|ResponseDisplay" source tests
dotnet publish tests/test-apps/timewarp-nuru-testapp-delegates/timewarp-nuru-testapp-delegates.csproj -c Release -r linux-x64 -p:PublishAot=true
```

Expect: `rg` shows no public declaration of either type. AOT publish exits 0 with no IL2026 or IL3050.

## Session

- Created: 538291 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)
- Implemented: claude implement oracle (2026-10-05) — deleted both types
- Review: grok 01a10b8e-35a2-71f1-b60b-4ccbf82609a0 (2026-10-05)

## Results

Deleted both types rather than internalizing them. Neither had a caller in `source/`, `tests/`,
`samples/`, or docs, and the generator already writes handler output itself.

- Removed `source/timewarp-nuru/services/nuru-app-holder.cs` (`NuruAppHolder`; `SetApp` had no callers).
  The `services/` folder is now empty and gone.
- Removed `source/timewarp-nuru/io/response-display.cs` (`ResponseDisplay.Write`, the reflection-based
  `RequiresUnreferencedCode` / `RequiresDynamicCode` JSON fallback).
- The repo has no PublicAPI.*.txt baseline files. Deleting the types takes them out of the assembly's
  public surface.
- `dotnet build source/timewarp-nuru` succeeded with 0 warnings and 0 errors. The delegates AOT publish
  exited 0 with 0 IL2026/IL3050. The CI tests passed (3774 tests, exit 0) after `ganda runfile cache --clear`. `ganda repo audit` passes.

## Review

- Rounds: 1. Roster: general. Effort: 1.
- Counts (round 1): bug 0 open / 0 fixed / 0 wontfix. Suggestion 0. Nit 0.
- Disposition: clean. No wontfix and no escalation.
- Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.

### How to validate

Smoke:

```bash
rg -n "NuruAppHolder|ResponseDisplay" source tests samples
dotnet build source/timewarp-nuru/timewarp-nuru.csproj -c Release
dotnet publish tests/test-apps/timewarp-nuru-testapp-delegates/timewarp-nuru-testapp-delegates.csproj -c Release -r linux-x64 -p:PublishAot=true 2>&1 | grep -cE "IL2026|IL3050"
```

Expect: `rg` prints nothing. The build succeeds with 0 errors. The publish exits 0 and the grep count is `0`.

## Notes

- Parent review: `kanban/done/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/runtime-core.md` R-9. Findings index: `review/findings.md` on that task.
- Implementation review: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`. Outcome clean (no findings).
