# Manual Shell Completion Testing

This document covers **shell TAB completion testing only** - the only completion feature that requires manual verification.

All other completion features are fully automated:
- Endpoint protocol tests: `tests/timewarp-nuru-tests/completion/completion-27-endpoint-protocol.cs`
- REPL completion: `tests/timewarp-nuru-tests/repl/` (fully automated)

> **Demo tip:** Use `completion-example -i` for REPL mode to demonstrate interactive completion in presentations.

## Setup

```bash
# From repository root

# Build and publish to ~/.timewarp/bin (already in PATH)
dotnet publish samples/15-completion/completion-example.cs -c Release -o ~/.timewarp/bin

# Verify it runs
completion-example --help
```

## Shell Completion Testing

These tests require actual shell interaction and cannot be automated.

### Bash

```bash
# Install completion
completion-example --install-completion bash
source ~/.local/share/bash-completion/completions/completion-example

# Test 1: Command completion
completion-example <TAB><TAB>
# Expected: deploy  list-environments  list-tags  status

# Test 2: Parameter completion (custom source)
completion-example deploy <TAB><TAB>
# Expected: demo  development  production  qa  staging

# Test 3: Partial match
completion-example dep<TAB>
# Expected: completes to "deploy"

# Test 4: Option completion after command
completion-example deploy production --<TAB><TAB>
# Expected: --version
```

### Zsh

```zsh
# Install completion
completion-example --install-completion zsh
# Add to ~/.zshrc if not already: fpath=(~/.local/share/zsh/site-functions $fpath)
autoload -Uz compinit && compinit

# Same tests as Bash above
```

### Fish

```fish
# Install (auto-loads, no source needed)
completion-example --install-completion fish

# Test - Fish shows completions differently (grey autosuggestion)
completion-example <TAB>
# Expected: Menu with deploy, list-environments, list-tags, status
```

### PowerShell

```powershell
# Install
completion-example --install-completion pwsh
. $HOME/.local/share/nuru/completions/completion-example.ps1

# Test
completion-example <TAB>
# Expected: Cycles through deploy, list-environments, list-tags, status
```

## Verification Checklist

- [ ] Bash: TAB completion shows commands
- [ ] Bash: TAB completion shows custom parameter values
- [ ] Bash: Partial input completes correctly
- [ ] Zsh: TAB completion works
- [ ] Fish: TAB completion works
- [ ] PowerShell: TAB completion works

## Cleanup

```bash
rm -f ~/.timewarp/bin/completion-example
rm -f ~/.local/share/bash-completion/completions/completion-example
rm -f ~/.local/share/zsh/site-functions/_completion-example
rm -f ~/.config/fish/completions/completion-example.fish
rm -f ~/.local/share/nuru/completions/completion-example.ps1
```
