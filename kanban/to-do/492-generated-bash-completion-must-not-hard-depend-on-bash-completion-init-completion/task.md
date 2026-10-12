# Generated bash completion must not hard-depend on bash-completion _init_completion

## Description

**3.0.0 launch gate #5.** `app --generate-completion bash` emits
`source/timewarp-nuru/completion/completion/templates/bash-completion-dynamic.sh`, whose function starts with
`_init_completion || return`. `_init_completion` comes from the separate `bash-completion` package
(`/usr/share/bash-completion/bash_completion`). On any bash where that is not sourced (minimal containers,
VHS/ttyd recordings, CI shells, macOS default bash), pressing Tab prints
`bash: _init_completion: command not found` and nothing completes. Seen while recording the 3.0 launch video
(task 487); the fix for the recording was to source bash-completion first, which users will not know to do.

## Requirements

- The generated script works with plain bash. Standard pattern (used by cobra, gh, dotnet): define a fallback
  when `_init_completion` is missing, e.g.
  ```bash
  if ! type _init_completion >/dev/null 2>&1; then
    _init_completion() { COMPREPLY=(); _get_comp_words_by_ref() { :; }; cur="${COMP_WORDS[COMP_CWORD]}"; prev="${COMP_WORDS[COMP_CWORD-1]}"; words=("${COMP_WORDS[@]}"); cword=$COMP_CWORD; }
  fi
  ```
  or avoid `_init_completion` entirely and read `COMP_WORDS`/`COMP_CWORD` directly.
- Zsh template checked for the equivalent assumption.
- Test: run the generated script under `bash --norc --noprofile` with `COMP_WORDS`/`COMP_CWORD` set and
  assert `COMPREPLY` contains `deploy` for `app dep`.

## Checklist

- [x] Bash template no longer requires bash-completion
- [x] Zsh template reviewed
- [x] Test under a bare bash
- [x] `changelog.md` Unreleased: Fixed entry
- [ ] PR merged

## Session

- Created: claude 2412bd45 (2026-10-09)
- Implement: claude (ganda task work, 2026-10-12)

## Results

- `bash-completion-dynamic.sh`: uses `_init_completion` / `_filedir` only when `declare -F` finds them;
  otherwise fills `words`/`cword`/`cur`/`prev` from `COMP_WORDS`/`COMP_CWORD` and adds path candidates
  with `compgen -f` / `compgen -d` + `compopt -o filenames`. `_filedir` was a second hard dependency
  (any directive without NoFileComp hit it) and is covered by the same fallback.
- `zsh-completion-dynamic.zsh`: `_describe` / `_files` are part of zsh's own completion system, but
  `compdef` exists only after `compinit`. The script now runs `autoload -Uz compinit && compinit` when
  `compdef` is undefined. zsh is not installed on this machine, so this path was not run.
- Test `tests/timewarp-nuru-tests/completion/completion-30-bare-bash.cs`: sources the generated script in
  `bash --norc --noprofile` with a stub `app` callback, sets `COMP_WORDS=(app dep)` `COMP_CWORD=1`, and
  asserts `COMPREPLY` contains `deploy` (directive 4) and also `deploy-notes.txt` from the compgen file
  fallback (directive 0), with no `command not found` on stderr. Against the old template the same
  driver prints `_init_completion: command not found` and an empty `COMPREPLY`.
- `dotnet run tests/ci-tests/run-ci-tests.cs`: exit 0, 1975 passed, 0 failed.

### How to validate

Smoke:
```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/completion/completion-30-bare-bash.cs
```

Expect: both `BareBash` tests pass (`Total: 2`, `Passed: 2`). Manually: in a shell without bash-completion
(`bash --norc --noprofile`), `source <(app --generate-completion bash)` then `app dep<Tab>` completes to
`deploy` with no `_init_completion: command not found`.
