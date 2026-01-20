# Shell Completion

Demonstrates shell and REPL tab completion with custom completion sources.

## Run It

```bash
# Interactive REPL with tab completion
dotnet run samples/15-completion/completion-example.cs -- -i

# Generate shell completion script
dotnet run samples/15-completion/completion-example.cs -- --generate-completion bash
dotnet run samples/15-completion/completion-example.cs -- --generate-completion zsh
dotnet run samples/15-completion/completion-example.cs -- --generate-completion fish
dotnet run samples/15-completion/completion-example.cs -- --generate-completion pwsh

# Test __complete callback
dotnet run samples/15-completion/completion-example.cs -- __complete 1 deploy
```

## What's Demonstrated

- `.EnableCompletion()` for shell completion support
- Custom `ICompletionSource` for dynamic completions
- `CompletionContext` for context-aware suggestions
- Shell script generation for bash/zsh/fish/PowerShell

## Related Documentation

- [Shell Completion](../../documentation/user/features/shell-completion.md)
