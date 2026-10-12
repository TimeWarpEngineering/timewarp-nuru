# Round 1 — general
**Date:** 2026-10-12
**Scope reviewed:** `task/492-generated-bash-completion-must-not-hard-depend-on` vs `origin/master` (bash and zsh dynamic completion templates, bare-bash test, changelog)

## Summary

The bash template calls `_init_completion` and `_filedir` only when `declare -F` finds them. Otherwise it copies `COMP_WORDS` / `COMP_CWORD` into the same `words` / `cword` / `cur` locals the callback already uses, and appends `compgen -f` or `compgen -d` matches one quoted element at a time. `compopt -o filenames` is guarded the same way the existing `nospace` / `nosort` calls are. The zsh template still uses `_describe` and `_files`; it runs `compinit` only when `compdef` is not defined yet, which is the gap on a zshrc that sources the script before `compinit`. The install path still registers `#compdef` files from fpath after the user's own `compinit`, so that new block does not run there.

Checked and not filed: a suggestion that is also a filename is listed twice when file completion is on, which is the same append `_filedir` already does. `compopt` returns non-zero when the function is called outside a completion context; stderr is redirected and a real Tab invocation returns 0. zsh is not installed here, so `compinit` was not executed. Spaced filenames stay one `COMPREPLY` element (`my notes.txt`). Directive 4 returns only the app suggestion; directive 16 returns only the matching directory; `set -u` with `COMP_CWORD=0` does not abort; a defined `_init_completion` is the path that runs.

`completion-30-bare-bash` 2/2, `completion-20` 15/15, and `completion-29` 11/11 passed. A pty Tab on `app dep` under `bash --norc --noprofile` listed `deploy` and `deploy-notes.txt` and completed the common prefix, with no `command not found`.

## Issues

No issues.
