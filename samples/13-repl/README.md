# REPL

Demonstrates the interactive Read-Eval-Print Loop with tab completion and customization.

## Run It

```bash
# Enter interactive mode
dotnet run samples/13-repl/01-repl-cli-dual-mode.cs -- -i
dotnet run samples/13-repl/01-repl-cli-dual-mode.cs -- --interactive

# CLI mode (single command)
dotnet run samples/13-repl/01-repl-cli-dual-mode.cs -- greet Alice
```

## What's Demonstrated

- **01-cli-dual-mode**: CLI and REPL in the same app via `-i`/`--interactive`
- **02-custom-keys**: Custom key bindings for the REPL
- **03-options**: REPL configuration (prompt, welcome/goodbye messages)
- **04-complete**: Tab completion with custom completion sources

## Related Documentation

- [Built-in Routes](../../documentation/user/features/built-in-routes.md)
- [REPL Key Bindings](../../documentation/user/features/repl-key-bindings.md)
