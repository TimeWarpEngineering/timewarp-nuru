# Fix sample using AnsiColors in repl-prompt-fix-demo

## Description

The `repl-prompt-fix-demo.cs` sample references `AnsiColors` which doesn't exist in the current context. Need to add correct using statement or project reference.

Error: `CS0103: The name 'AnsiColors' does not exist in the current context`

## Checklist

- [x] samples/repl-demo/repl-prompt-fix-demo.cs

## Notes

- Discovered by `runfiles/verify-samples.cs` (task 221)
- `AnsiColors` may be in `TimeWarp.Terminal` namespace
- May need `#:project ../../source/timewarp-terminal/timewarp-terminal.csproj`
