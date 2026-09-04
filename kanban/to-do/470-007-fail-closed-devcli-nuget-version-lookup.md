# Fail-closed DevCli NuGet version lookup

Parent: 470 (2026-09-04 full-repo review). Severity: bug (M9, M10). Suggestion folded: M31.

## Description

`GetPackageVersionsAsync` (`nuget-version-service.cs:43-46`) returns `[]` for every non-success HTTP status. NuGet 404 for a never-published id is correctly empty, but 429/5xx/auth are indistinguishable. Callers treat empty as “not published”: check-version `continue`s (`check-version-command.cs:117-120`) so `alreadyPublished` stays empty → `PublishState.None` → “safe to release”. A transient NuGet outage can clear the already-released gate.

M10: `GetVersionFromSource` (`check-version-command.cs:202-203`) does not trim `<Version>`. `release-command.cs:464-465` and workflow `ReadPropsVersion` do. Whitespace in the element makes check-version miss a published version.

M31: package id is interpolated unescaped (`nuget-version-service.cs:40`); `../evil` normalizes off `v3-flatcontainer`. Host stays `api.nuget.org`. Reachable from `--package`.

## Requirements

- Treat only 404 (maybe 400) as empty; other statuses fail-closed. Dispose HttpResponseMessage.
- Trim Version in check-version (or share one props-version reader).
- Validate NuGet id grammar and/or Uri.EscapeDataString the path segment (M31).
- Tests for non-404 HTTP and whitespace Version.

## Checklist

- [ ] Fail-closed HTTP (M9)
- [ ] Trim Version (M10)
- [ ] Package id validation (M31)
- [ ] Tests

## Notes

Evidence: parent 470 `review/round-1/merged.md` M9, M10, M31. 458 policy is out of scope; this is code correctness of the gate.
