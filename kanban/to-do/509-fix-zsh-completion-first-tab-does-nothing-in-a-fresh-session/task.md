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

- [x] First Tab completes in a fresh zsh session (autoloaded from `fpath`).
- [x] A test covers it: a zsh pty-driven (`zpty`) test, or at minimum a template-content test plus a scripted zsh check.
- [x] The existing source/eval path (`source <(app --generate-completion zsh)`) keeps working.

## Goal (implementer restatement)

The generated zsh script is one file with two entry paths. `compinit` autoloads it from `fpath` on the first Tab. Zsh runs that file as the completion function and, because the file also defines the function, does not run the new body. A trailing `compdef` only registers the command, so the first Tab shows nothing and the second Tab works. When zsh is loading the file (`zsh_eval_context` ends in `loadautofunc`), the script has to call the function it just defined. When the user sources or evals the script, it has to `compdef` instead, and it still runs `compinit` when `compdef` is missing.

Acceptance:

- A fresh `zsh -f` with the script on `fpath` lists candidates on the first Tab.
- Tests lock the template text, the first autoloaded call, a `zpty` Tab, and both `source <(…)` and `eval`.
- Sourcing or eval'ing the script still registers the completer and completes.

## Review

- Round 1: Done

## Notes

- Context: the known-issue mention in Masoko's blog draft `/workspace/masoko/same-cli-every-shell/blog-post.md` (on the Grok Bot box).
- Docs: `documentation/user/features/shell-completion.md` (Zsh install section).

## Session

- Created: 3431885 (2026-10-18)
- Implementation: task-work (2026-10-19)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-18T18:40:39Z
- Review oracle: tw-implementation-review, effort 2, roster general (claude-opus-5-5, headless, session id not exposed), round 1, disposition accepted-exceptions — 2026-10-19
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-18T18:43:30Z

## Results

The generated zsh script calls `_{{APP_NAME}}` when zsh is autoloading the file, and still calls `compdef` when the file is sourced or eval'd. The `compinit` guard above that branch is unchanged.

- Template: `source/timewarp-nuru/completion/completion/templates/zsh-completion-dynamic.zsh`. On `loadautofunc` (the context `compinit` uses for the first Tab) the script runs the function the file just defined. `source` and `eval` see a different `zsh_eval_context` and register with `compdef`.
- Test: `tests/timewarp-nuru-tests/completion/completion-31-zsh-autoload.cs`. It checks the template order, drives the first autoloaded `_app` call, drives `source <(cat _app)` and `eval`, and drives one Tab through `zpty` in `zsh -f -i`. The callback stub records one hit. A regression to a bare trailing `compdef` leaves that hit at 0 and the Tab times out.
- CI: `.github/workflows/workflow.yml` installs zsh when the runner image does not have it, so the zsh checks run in GitHub Actions.
- Changelog: Unreleased Fixed entry (509).
- Docs: `documentation/user/features/shell-completion.md` already describes both the `source <(…)` path and the `fpath` + `compinit` path. The install steps did not change.
- `ganda repo audit`: 30 passed, 0 failed.
- Related script tests: `completion-20-dynamic-script-gen.cs` 15 passed, `completion-29-directive.cs` 11 passed.
- CI runner (`dotnet run tests/ci-tests/run-ci-tests.cs`): multi-mode 1943 total, 1937 passed, 6 skipped, 0 failed. `ZshAutoload` passed 4 inside that run. Standalone: 0 failed.

### How to validate

Smoke:

```bash
dotnet run tests/timewarp-nuru-tests/completion/completion-31-zsh-autoload.cs
```

Expect:

- Total 4, Passed 4.
- `Should_complete_on_the_first_autoloaded_call` prints `CANDIDATES`, `nuru509candidate:Unique desc`, `status`, and `HITS:1` on that first call.
- `Should_complete_when_sourced_and_when_evald` prints `COMP:_app`, `HITS:SOURCE:1`, and `HITS:EVAL:1`.
- `Should_list_the_candidate_on_the_first_tab` prints `TAB_HITS:1` and `TAB_OK`. The pty listing contains `nuru509candidate`.
- None of the runs print `FILES_CALLED` or `command not found`.

### Review

- Rounds: 1. Effort 2, roster general (review oracle).
- Final counts: bug 0, suggestion 0, nit 1 (wontfix). Open: 0.
- Disposition: **accepted-exceptions**. M1 (nit): the `Install zsh` step in `workflow.yml` has no probe-mode `if:`. It is harmless because of the `command -v` guard. Tidy it the next time `workflow.yml` changes.
- Verified by the reviewer: completion-31 passes 4 of 4 locally on zsh 5.9. The master template fails the same autoload driver (`HITS:0`) and the branch template passes it (`HITS:1`). A `zcompile`d `.zwc` also completes on the first call.
- Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
