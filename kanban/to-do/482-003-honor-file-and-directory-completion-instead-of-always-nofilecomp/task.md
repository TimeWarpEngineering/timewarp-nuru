# Honor file and directory completion instead of always NoFileComp

## Parent

482 — Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

Finding R-4 from task 482. Fix it before the `3.0.0` tag. The parent review record is the source of the disposition. Do not reopen the whole review on this id.

Shell completion never delegates to the filesystem.

`DynamicCompletionHandler.HandleCompletion` always sets `CompletionDirective.NoFileComp` and never reads `CompletionCandidate.Type`. `File` and `Directory` candidates therefore never run. Public `CompletionDirective` flags (`NoSpace`, `KeepOrder`, file fallback) are not applied. Zsh completion splits unquoted suggestion lines, so values that contain spaces break. Bash completion is already quoted.

Evidence: `source/timewarp-nuru/completion/completion/dynamic-completion-handler.cs`, `source/timewarp-nuru/completion/scripts/bash-completion-dynamic.sh`, `fish-completion-dynamic.fish`, `zsh-completion-dynamic.zsh`. Parent record: `review/runtime-core.md` R-4.

## Checklist

- [x] A `File` or `Directory` candidate does not force `NoFileComp` unless the candidate asked for it
- [x] `NoSpace` and `KeepOrder` are copied onto the directive the shell script reads
- [x] Zsh suggestions that contain spaces stay one candidate
- [x] Add a test or a script fixture for a path candidate and a spaced suggestion

## Session

- Created: 527561 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)
- Implemented: claude implementer (2026-10-05)
- Review: grok 01a10bc9-6abd-7f80-b644-2541da60e83c (2026-10-05)

## Results

- `CompletionCandidate` gains an optional `Directive` (`CompletionDirective`); `CompletionDirective` gains `FilterDirs = 16`.
- `DynamicCompletionHandler.ResolveDirective` ORs candidate directives; adds `NoFileComp` only when no `File`/`Directory` candidate is present (a candidate can still request it); directory-only adds `FilterDirs`. Empty-value `File`/`Directory` candidates are delegation markers and are not printed.
- Bash: `_filedir` / `_filedir -d` when file completion allowed, `compopt -o nospace` / `-o nosort` for NoSpace / KeepOrder.
- Zsh: `completions=("${(@f)output}")` keeps spaced suggestions as one word; `:` in values escaped for `_describe`; `-S ''` (NoSpace), `-V` (KeepOrder), `_files` / `_files -/` fallback.
- Fish: directive parsed; `__fish_complete_path` / `__fish_complete_directories` fallback. Fish has no per-call NoSpace/KeepOrder.
- PowerShell template unchanged (empty result already falls back to path completion).
- Tests: new `tests/timewarp-nuru-tests/completion/completion-29-directive.cs` (11 tests); `repl-41-lowsev-sweep.cs` fish assertion updated to the new directive-capture form.
- CI: `dotnet run tests/ci-tests/run-ci-tests.cs` exit 0, 0 failures.

### Review

- Rounds: 1. Roster: general. Effort: 2 (395-line diff, commit `f814c86d`).
- Counts: bug 0; suggestion 0; nit 0; open 0.
- Disposition: clean. No wontfix. No escalation.
- Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.
- Re-checked: `completion-29` 11/11, `completion-20` 15/15, `repl-41` 5/5. Bash simulation of directives 72, 4, 16, and 20 matched the script (spaced suggestion kept, `_filedir -d` directories only).

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/completion/completion-29-directive.cs
dotnet run tests/timewarp-nuru-tests/completion/completion-20-dynamic-script-gen.cs
dotnet run tests/ci-tests/run-ci-tests.cs
```

Expect: all tests pass. `Complete_with_path_source_emits_file_directive_and_spaced_value` shows a `File` candidate yields directive `:72` (NoSpace|KeepOrder, no NoFileComp) and `my notes.txt` as one line; `Zsh_template_keeps_spaced_suggestion_as_one_word` asserts the quoted `(@f)` split. The original kitchen smoke used a non-existent `.csproj`; tests are runfiles.

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
- Implementation review: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`. Outcome clean.
