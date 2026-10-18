# Fix zsh completion: first Tab does nothing in a fresh session

## Description

In a fresh zsh session the first Tab after the app name does nothing; only the second Tab completes. The generated `_app` script ends with `compdef _app app`. When zsh autoloads the file from `fpath` (the `#compdef` path that `compinit` sets up), the first Tab runs the file body as the autoloaded function: it registers `compdef` again but never calls `_app`, so that first completion request produces nothing. The next Tab calls the now-defined `_app` and works.

Found by Masoko (Nuru 3.0.0) while building the "Same CLI, every shell" demo.

## Template

`source/timewarp-nuru/completion/completion/templates/zsh-completion-dynamic.zsh`. The current tail:

```zsh
# _describe/_files ship with zsh's completion system; only compdef needs compinit.
# Load it when the user's zshrc has not, instead of failing with "command not found: compdef".
if (( ! $+functions[compdef] )); then
    autoload -Uz compinit && compinit
fi
compdef _{{APP_NAME}} {{APP_NAME}}
```

## Repro

1. Generate or install zsh completion for a sample app (`app --generate-completion zsh > ~/.zfunc/_app`, or `app --install-completion zsh`).
2. Start a fresh `zsh -f`, then `fpath=(~/.zfunc $fpath); autoload -Uz compinit && compinit`.
3. Type `app <Tab>`: nothing happens.
4. Press Tab again: candidates appear.

## Suggested fix

Masoko's tested change: replace the final `compdef` line with an autoload check, using the template placeholder:

```zsh
if [[ "${zsh_eval_context[-1]}" == loadautofunc ]]; then
    _{{APP_NAME}} "$@"
else
    compdef _{{APP_NAME}} {{APP_NAME}}
fi
```

Keep the existing `compinit` guard above it for the source/eval path.

## Acceptance criteria

- [ ] First Tab completes in a fresh zsh session (autoloaded from `fpath`).
- [ ] A test covers it: a zsh pty-driven (`zpty`) test, or at minimum a template-content test plus a scripted zsh check.
- [ ] The existing source/eval path (`source <(app --generate-completion zsh)`) keeps working.

## Notes

- Context: the known-issue mention in Masoko's blog draft `/workspace/masoko/same-cli-every-shell/blog-post.md` (on the Grok Bot box).
- Docs: `documentation/user/features/shell-completion.md` (Zsh install section).

## Session

- Created: 3431885 (2026-10-18)
