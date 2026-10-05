# Round 1 — general
**Date:** 2026-10-05
**Scope reviewed:** commit `f814c86d` (R-4 file and directory completion)

## Summary

`ResolveDirective` ORs each candidate's `Directive`, adds `NoFileComp` only when no `File` or `Directory` candidate is present, and adds `FilterDirs` (16) when the path request is directory-only. Empty `File` / `Directory` values stay delegation markers and are not printed. Bash, zsh, and fish read those bits: bash calls `_filedir` / `_filedir -d` and `compopt` for `NoSpace` / `KeepOrder`; zsh splits on quoted `"${(@f)output}"`, escapes `:` in the value, and calls `_files` / `_files -/`; fish prints candidates then `__fish_complete_path` / `__fish_complete_directories`. Flag 16 does not collide with `NoFileComp` (4), `NoSpace` (8), or `KeepOrder` (64). The optional `CompletionCandidate.Directive` parameter is source-compatible with existing positional constructors, including the generator's `new CompletionCandidate(...)` calls.

Checked and not filed: bash-completion 2.16 still provides `_filedir` as `_comp_compgen -a filedir`, so suggestions already in `COMPREPLY` are kept. A local simulation with directive 72 kept `my notes.txt` as one entry and appended files; directive 4 returned only the suggestion; directive 16 returned only `onlydir`; directive 20 (`NoFileComp|FilterDirs`) returned nothing. PowerShell still skips `^:` lines. An empty candidate list therefore still falls through to the host's path fallback, and `FilterDirs` / `NoSpace` / `KeepOrder` are not applied there. Parent R-4 named bash, fish, and zsh; Cobra's PowerShell script also does not implement `FilterDirs`. Fish has no per-call `NoSpace` / `KeepOrder`, which the template states. zsh and fish were not executed here (neither shell is installed). `completion-29` (11), `completion-20` (15), and `repl-41` (5) passed.

## Issues

No issues.
