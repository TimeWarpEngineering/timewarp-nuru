# Fix verify-samples warnings and 7 failing sample tests

## Description

The `./runfiles/verify-samples.cs` script currently treats warnings as passing. Additionally, there are 7 sample tests that fail to compile. Both issues need to be addressed.

## Checklist

### Part 1: Make verify-samples treat warnings as failures
- [ ] Update `verify-samples.cs` to detect and report warnings
- [ ] Warnings should cause the sample to be marked as failed (or warn prominently)

### Part 2: Fix failing sample tests (7 total)

#### Testing samples (missing ITerminal, TestTerminal, TestTerminalContext)
- [ ] `samples/testing/debug-test.cs` - Missing ITerminal, TestTerminal, TestTerminalContext
- [ ] `samples/testing/runfile-test-harness/real-app.cs` - Missing ITerminal
- [ ] `samples/testing/test-colored-output.cs` - Missing TestTerminal, ITerminal, NuruAppBuilder ctor issue, .Green() extension
- [ ] `samples/testing/test-output-capture.cs` - Same issues as above
- [ ] `samples/testing/test-terminal-injection.cs` - Same issues as above

#### API mismatch samples
- [ ] `samples/unified-middleware/unified-middleware.cs` - Map() method signature changed (no `handler:` or `description:` named params)
- [ ] `samples/aot-example/aot-example.csproj` - Generated code references `WriteLine` without context

### Part 3: Address warnings (optional but recommended)
- [ ] Investigate `MSG0005: MediatorGenerator found message without any registered handler` warnings
- [ ] Investigate `CS0436` type conflict warnings in aot-example
- [ ] Investigate `CS8785: Generator 'NuruRouteAnalyzer' failed` warning

## Notes

### Failing samples detail

**Testing samples** - These appear to reference types (`ITerminal`, `TestTerminal`, `TestTerminalContext`) and APIs (`NuruAppBuilder` parameterless constructor, `.Green()` string extension) that either:
- Were removed/renamed in a recent refactoring
- Need a different using directive or package reference

**unified-middleware.cs** - The `Map()` method no longer accepts `handler:` or `description:` named parameters. The sample needs updating to match current API.

**aot-example** - The source generator is producing code that calls `WriteLine` without qualification. The generator needs to emit `Console.WriteLine` or the sample needs a `using static System.Console;`.

### Warnings observed

Many samples emit `MSG0005: MediatorGenerator found message without any registered handler: TimeWarp.Nuru.Generated.Default_Generated_Query` - this may indicate a configuration issue or an expected warning that should be suppressed.
