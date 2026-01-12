# Fix tests using closures/discards that fail with generator

## Description

8 test files use closure patterns (capturing external variables in lambda handlers) that are 
incompatible with the Nuru source generator. The generator inlines lambda handler bodies into 
generated interceptor code, so variables captured from the enclosing scope don't exist in the 
generated context, causing `CS0103` errors.

## Affected Files (Full Audit Complete)

| File | Closure Refs | Tests | Status |
|------|-------------|-------|--------|
| `routing/routing-13-negative-numbers.cs` | 42 | ~9 | Fails to build |
| `routing/routing-12-colon-filtering.cs` | 40 | ~10 | Fails to build |
| `options/options-01-mixed-required-optional.cs` | 27 | 4 | Fails to build |
| `routing/routing-23-multiple-map-same-handler.cs` | 22 | 5 | Fails to build (also #342) |
| `routing/routing-18-option-alias-with-description.cs` | 15 | 5 | Fails to build |
| `options/options-03-nuru-context.cs` | 14 | ? | Fails to build |
| `options/options-02-optional-flag-optional-value.cs` | 9 | 3 | Fails to build |
| `help-provider-03-session-context.cs` | 8 | ? | Fails to build |

**Total:** ~177 closure references across ~50 test methods in 8 files.

**Note:** `test-terminal-context-01-basic.cs` has "captured" references in test assertions,
not handler closures - no changes needed.

## Errors

```
error NURU_H006: Handler lambda uses discard parameter '_'. Discards are not supported 
because the lambda body is inlined into generated code. Use named parameters instead.

error CS0103: The name 'capturedEnv' does not exist in the current context
```

## Solution: TestTerminal Pattern

Replace closure-based assertions with the `TestTerminal` pattern (see #332 for examples).

### Before (Closure - NOT SUPPORTED)
```csharp
string? capturedName = null;
NuruCoreApp app = NuruApp.CreateBuilder([])
  .Map("greet {name}")
  .WithHandler((string name) => { capturedName = name; })
  .AsCommand().Done()
  .Build();

await app.RunAsync(["greet", "Alice"]);
capturedName.ShouldBe("Alice");
```

### After (TestTerminal - SUPPORTED)
```csharp
using TestTerminal terminal = new();
NuruCoreApp app = NuruApp.CreateBuilder([])
  .UseTerminal(terminal)
  .Map("greet {name}")
  .WithHandler((string name) => name)  // Return value goes to terminal
  .AsCommand().Done()
  .Build();

await app.RunAsync(["greet", "Alice"]);
terminal.OutputContains("Alice").ShouldBeTrue();
```

## Implementation Plan

### Phase 1: High-Volume Files (largest first)
- [ ] `routing/routing-13-negative-numbers.cs` (42 refs, ~9 tests)
- [ ] `routing/routing-12-colon-filtering.cs` (40 refs, ~10 tests)
- [ ] `options/options-01-mixed-required-optional.cs` (27 refs, 4 tests)

### Phase 2: Medium Files
- [ ] `routing/routing-23-multiple-map-same-handler.cs` (22 refs, 5 tests)
  - **Blocked by #342** - Also has `HelpProvider.GetHelpText` issue
- [ ] `routing/routing-18-option-alias-with-description.cs` (15 refs, 5 tests)
- [ ] `options/options-03-nuru-context.cs` (14 refs)

### Phase 3: Small Files
- [ ] `options/options-02-optional-flag-optional-value.cs` (9 refs, 3 tests)
- [ ] `help-provider-03-session-context.cs` (8 refs)

### Final Verification
- [ ] All 8 test files compile
- [ ] Run tests to verify behavior preserved

## Special Cases

### Discard Parameters
`options-01-mixed-required-optional.cs` line 86 uses discards:
```csharp
.WithHandler((string _, string? _, bool _) => 0)
```
Replace with named parameters:
```csharp
.WithHandler((string env, string? ver, bool dryRun) => 0)
```

### Multiple Values Assertion
For tests verifying multiple captured values, format output:
```csharp
.WithHandler((int x, int y) => $"x:{x}|y:{y}")
// Then assert:
terminal.OutputContains("x:5|y:-3").ShouldBeTrue();
```

### Execution Count Tracking
`routing-23-multiple-map-same-handler.cs` tracks execution count:
```csharp
int executionCount = 0;
Func<int> handler = () => { executionCount++; };
```
Replace with counter output pattern:
```csharp
.WithHandler(() => "executed")
terminal.Output.Split('\n').Where(l => l.Contains("executed")).Count().ShouldBe(1);
```

## Dependencies

- **#342** - `HelpProvider.GetHelpText` API blocks 2 tests in `routing-23-multiple-map-same-handler.cs`
- **#332** - Reference for TestTerminal refactoring pattern (completed)
- **#307** - NURU_H002 false positive on object initializers (completed)

## Related

- #332 - Promote NURU_H002 to Error (completed, has refactoring examples)
- #342 - Fix HelpProvider.GetHelpText API (to-do)
