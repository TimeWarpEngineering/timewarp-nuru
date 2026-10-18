# Round 1 — general
**Date:** 2026-10-19
**Scope reviewed:** branch vs master: the zsh template, `completion-31-zsh-autoload.cs`, the `workflow.yml` zsh step, and the changelog

## Summary

The template now branches on `zsh_eval_context[-1]`. When the context is `loadautofunc` it calls `_app "$@"`, which is the path `compinit` uses for the first Tab. Otherwise it calls `compdef`. The `compinit` guard is still in place. This matches the task's suggested fix and the implementer restatement. Risk is low, and nothing else in `source/` uses `compdef`.

Checks I ran:

- `completion-31-zsh-autoload.cs` passes 4 of 4 locally on zsh 5.9.
- I ran the autoload driver from a scratch dir against both templates. The master template gives `HITS:0`, so the bug reproduces. The branch template gives `HITS:1`, so the test catches a regression.
- `source` gives the context `file` and `eval` gives `eval`, so both take the `compdef` branch as intended.
- A `zcompile`d `_app.zwc` on `fpath` also completes on the first call (`HITS:1`).

The install handler (`install-completion-handler.cs`) writes the file to `~/.local/share/zsh/site-functions` and adds that path to `fpath`. That is the autoload path the fix covers.

## Issues

### Issue 1 — Severity: nit
- File: .github/workflows/workflow.yml:81
- Description: The new `Install zsh` step has no `if:`. It runs in `mode=probe`, where `Run CI Pipeline` is skipped and zsh is never used. In release mode it also runs `apt-get update` between `nuget/login` and the pipeline, so an apt mirror outage could now fail a release run.
- Suggestion: Give it the same guard as `Run CI Pipeline`: `if: github.event_name != 'workflow_dispatch' || inputs.mode != 'probe'`. Moving it above the NuGet login step would also keep apt time out of the OIDC token window.
- Status: open
