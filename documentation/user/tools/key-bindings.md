# Key bindings

Lists the built-in REPL key bindings, grouped by category, in the same layout as the REPL command `key-bindings`.

## Run

From the repository root:

```bash
dotnet run tools/nuru-key-bindings/nuru-key-bindings.cs -- key-bindings
dotnet run tools/nuru-key-bindings/nuru-key-bindings.cs -- key-bindings --profile Emacs --function BackwardChar
dotnet run tools/nuru-key-bindings/nuru-key-bindings.cs -- key-bindings --key Ctrl+a --detailed
```

A bare `dotnet run tools/nuru-key-bindings/nuru-key-bindings.cs` lists the Default profile. The entry point forwards any arguments that do not start with `key-bindings` to that command, so `-- Emacs --detailed` works too.

## Options

| Flag | Short | Meaning |
|------|-------|---------|
| `--profile` | `-p` | `Default`, `Emacs`, `Vi`, or `VSCode`. Defaults to `Default`. A positional name (`key-bindings Emacs`) does the same when `--profile` is absent. |
| `--key` | `-k` | Exact chord when the parser accepts it, otherwise a case-insensitive substring of the displayed chord. |
| `--function` | `-f` | Case-insensitive substring of the function name. |
| `--detailed` | `-d` | One block per binding instead of the table. |

An unknown profile prints the error and exits with code 1. The listing covers the built-in catalogs. It does not expand a custom profile or a JSON file from `~/.nuru/keybindings.json`.

Inside a REPL, the same flags are the `key-bindings` command. That command does not set the process exit code. See [REPL Key Bindings](../features/repl-key-bindings.md).
