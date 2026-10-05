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

- [ ] `NuruAppHolder` and `ResponseDisplay` are no longer public, or they are deleted
- [ ] No remaining reference in `source/` or `tests/` fails to compile
- [ ] Public API surface (ShipPublicApi or the assembly's public types) does not list them
- [ ] AOT publish of the delegates test app still reports zero IL2026/IL3050

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

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
