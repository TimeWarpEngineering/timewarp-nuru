# Manual Shell Completion Testing

This document provides specific test cases with expected outcomes for verifying shell completion works correctly.

## Setup

```bash
# From repository root (run all commands from there)

# Build and publish to ~/.timewarp/bin (already in PATH)
dotnet publish samples/15-completion/completion-example.cs -c Release -o ~/.timewarp/bin

# Verify it runs
completion-example --help
```

Expected `--help` output:
```
Usage: app [command] [options]

Commands:
  deploy {env} --version {tag} Deploy a version to an environment
  list-environments         List all available environments
  list-tags                 List all available tags
  status                    Check system status

Options:
  --help, -h             Show this help message
  --version              Show version information
  --capabilities         Show capabilities for AI tools
```

## Test Cases

### Test 1: __complete callback returns commands

```bash
completion-example __complete 1 completion-example
```

**Expected stdout:**
```
deploy
list-environments
list-tags
status
--help  Show help for this command
:4
```

**Expected stderr:**
```
Completion ended with directive: NoFileComp
```

### Test 2: __complete with partial command

```bash
completion-example __complete 1 completion-example dep
```

**Expected stdout** (source generator filters to matching commands):
```
deploy
:4
```

### Test 3: __complete for parameter position

```bash
completion-example __complete 2 completion-example deploy
```

**Expected stdout** (custom EnvironmentCompletionSource):
```
production	Production environment
staging	Staging environment
development	Development environment
qa	QA environment
demo	Demo environment
:4
```

### Test 4: --generate-completion bash

```bash
completion-example --generate-completion bash | head -5
```

**Expected output:**
```
# Dynamic Bash completion for completion-example
# This completion script calls back to the application at Tab-press time
# to get context-aware completion suggestions.

_completion-example_completions()
```

### Test 5: --generate-completion zsh

```bash
completion-example --generate-completion zsh | head -5
```

**Expected output:**
```
#compdef completion-example

# Dynamic Zsh completion for completion-example
# This completion script calls back to the application at Tab-press time
# to get context-aware completion suggestions.
```

### Test 6: --generate-completion fish

```bash
completion-example --generate-completion fish | head -5
```

**Expected output:**
```
# Dynamic Fish completion for completion-example
# This completion script calls back to the application at Tab-press time
# to get context-aware completion suggestions.

function __fish_completion-example_complete
```

### Test 7: --generate-completion pwsh

```bash
completion-example --generate-completion pwsh | head -5
```

**Expected output:**
```
# Dynamic PowerShell completion for completion-example
# This completion script calls back to the application at Tab-press time
# to get context-aware completion suggestions.

$completionExampleCompleter = {
```

### Test 8: --install-completion --dry-run

```bash
completion-example --install-completion --dry-run
```

**Expected output:**
```
Installing completions for all shells...

🔍 Dry run for Bash:
   Would write to: /home/<user>/.local/share/bash-completion/completions/completion-example
   Script size: XXXX bytes

🔍 Dry run for Zsh:
   Would write to: /home/<user>/.local/share/zsh/site-functions/_completion-example
   Script size: XXXX bytes

🔍 Dry run for Fish:
   Would write to: /home/<user>/.config/fish/completions/completion-example.fish
   Script size: XXXX bytes

🔍 Dry run for PowerShell:
   Would write to: /home/<user>/.local/share/nuru/completions/completion-example.ps1
   Script size: XXXX bytes

✅ All shell completions installed successfully!
```

### Test 9: --install-completion --dry-run bash

```bash
completion-example --install-completion --dry-run bash
```

**Expected output:**
```
🔍 Dry run for Bash:
   Would write to: /home/<user>/.local/share/bash-completion/completions/completion-example
   Script size: XXXX bytes

✅ Bash completion installed successfully!
```

## Interactive Shell Testing

After running `--install-completion` for your shell:

### Bash

```bash
# Install
completion-example --install-completion bash
source ~/.local/share/bash-completion/completions/completion-example

# Test 1: Command completion
completion-example <TAB><TAB>
# Expected: deploy  list-environments  list-tags  status

# Test 2: Parameter completion  
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
# Install
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

- [ ] Test 1: __complete returns commands
- [ ] Test 2: __complete with partial input
- [ ] Test 3: __complete for parameter (custom source)
- [ ] Test 4: --generate-completion bash
- [ ] Test 5: --generate-completion zsh
- [ ] Test 6: --generate-completion fish
- [ ] Test 7: --generate-completion pwsh
- [ ] Test 8: --install-completion --dry-run (all shells)
- [ ] Test 9: --install-completion --dry-run bash (single shell)
- [ ] Interactive: Bash TAB completion
- [ ] Interactive: Zsh TAB completion
- [ ] Interactive: Fish TAB completion
- [ ] Interactive: PowerShell TAB completion

## Cleanup

```bash
rm -f ~/.timewarp/bin/completion-example
rm -f ~/.local/share/bash-completion/completions/completion-example
rm -f ~/.local/share/zsh/site-functions/_completion-example
rm -f ~/.config/fish/completions/completion-example.fish
rm -f ~/.local/share/nuru/completions/completion-example.ps1
```
