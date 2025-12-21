# Fix 7 failing sample tests

## Description

Seven sample files fail to compile due to missing types and API changes. These need to be updated to match the current codebase.

## Checklist

### Testing samples (missing ITerminal, TestTerminal, TestTerminalContext)
- [ ] `samples/testing/debug-test.cs` - Missing ITerminal, TestTerminal, TestTerminalContext
- [ ] `samples/testing/runfile-test-harness/real-app.cs` - Missing ITerminal
- [ ] `samples/testing/test-colored-output.cs` - Missing TestTerminal, ITerminal, NuruAppBuilder ctor issue, .Green() extension
- [ ] `samples/testing/test-output-capture.cs` - Same issues as above
- [ ] `samples/testing/test-terminal-injection.cs` - Same issues as above

### API mismatch samples
- [ ] `samples/unified-middleware/unified-middleware.cs` - Map() method signature changed (no `handler:` or `description:` named params)
- [ ] `samples/aot-example/aot-example.csproj` - Generated code references `WriteLine` without context

## Notes

### Testing samples
These reference types (`ITerminal`, `TestTerminal`, `TestTerminalContext`) and APIs (`NuruAppBuilder` parameterless constructor, `.Green()` string extension) that either:
- Were removed/renamed in a recent refactoring
- Need a different using directive or package reference

### unified-middleware.cs
The `Map()` method no longer accepts `handler:` or `description:` named parameters. The sample needs updating to match current API.

### aot-example
The source generator is producing code that calls `WriteLine` without qualification. Either:
- The generator needs to emit `Console.WriteLine`
- Or the sample needs a `using static System.Console;`
