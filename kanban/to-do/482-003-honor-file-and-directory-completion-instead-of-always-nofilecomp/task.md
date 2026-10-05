# Honor file and directory completion instead of always NoFileComp

## Parent

482 — Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

Finding R-4 from task 482. Fix it before the `3.0.0` tag. The parent review record is the source of the disposition. Do not reopen the whole review on this id.

Shell completion never delegates to the filesystem.

`DynamicCompletionHandler.HandleCompletion` always sets `CompletionDirective.NoFileComp` and never reads `CompletionCandidate.Type`. `File` and `Directory` candidates therefore never run. Public `CompletionDirective` flags (`NoSpace`, `KeepOrder`, file fallback) are not applied. Zsh completion splits unquoted suggestion lines, so values that contain spaces break. Bash completion is already quoted.

Evidence: `source/timewarp-nuru/completion/completion/dynamic-completion-handler.cs`, `source/timewarp-nuru/completion/scripts/bash-completion-dynamic.sh`, `fish-completion-dynamic.fish`, `zsh-completion-dynamic.zsh`. Parent record: `review/runtime-core.md` R-4.

## Checklist

- [ ] A `File` or `Directory` candidate does not force `NoFileComp` unless the candidate asked for it
- [ ] `NoSpace` and `KeepOrder` are copied onto the directive the shell script reads
- [ ] Zsh suggestions that contain spaces stay one candidate
- [ ] Add a test or a script fixture for a path candidate and a spaced suggestion

## How to validate

Smoke:

```bash
dotnet test tests/timewarp-nuru-tests/timewarp-nuru-tests.csproj --filter FullyQualifiedName~Completion
```

Expect: A file candidate leaves file completion enabled in the directive. A suggestion with a space is one zsh word, matching the bash script.

## Session

- Created: 527561 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
