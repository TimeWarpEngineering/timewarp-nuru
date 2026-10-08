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

- [ ] Bash template no longer requires bash-completion
- [ ] Zsh template reviewed
- [ ] Test under a bare bash
- [ ] `changelog.md` Unreleased: Fixed entry
- [ ] PR merged

## Session

- Created: claude 2412bd45 (2026-10-09)
