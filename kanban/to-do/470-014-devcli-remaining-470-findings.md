# DevCli remaining 470 findings

Parent: 470 (2026-09-04 full-repo review). Suggestions/nits: M23, M24, M41.

## Description

M23: `packable-project-service.cs:126-140` — MSBuild exit 0 with unparseable JSON silently omits the project from the packable set used by check-version, release, and promote.

M24: `GenerateNuruJsonContextTask.cs:58-70` — any exception in ExecuteCore is a warning + return true with empty GeneratedFiles (intentional fail-soft). Unexpected bugs become silent ToString fallback.

M41: Windows successful self-install leaves `dev.exe.old`.

NuGet fail-open / Version trim / package-id path are **470-007**, not this task.

## Requirements

- If MSBuild exit is 0 but Properties cannot be parsed, throw naming the project (M23).
- Keep fail-soft for “no DSL”; fail or emit a highly visible diagnostic for unexpected JSON-context exceptions (M24).
- Best-effort delete `oldExe` after successful Windows self-install (M41).

## Checklist

- [ ] M23 packable parse fail-loud
- [ ] M24 JSON-context unexpected errors
- [ ] M41 self-install .old cleanup
- [ ] Tests where practical

## Notes

Evidence: parent 470 `review/round-1/merged.md` M23, M24, M41. 458 versioning policy is out of scope.
