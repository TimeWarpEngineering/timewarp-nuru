# Fix 6 failing sample tests

## Description

Six sample files fail to compile due to missing using directives and API changes. These need to be updated to match the current codebase.

**Note:** The 7th failing sample (`aot-example`) is a generator bug tracked in task 233.

## Checklist

### Testing samples (add using TimeWarp.Terminal, fix factory method)
- [ ] `samples/testing/debug-test.cs` - Add using, fix NuruAppBuilder() -> CreateSlimBuilder()
- [ ] `samples/testing/runfile-test-harness/real-app.cs` - Add using TimeWarp.Terminal
- [ ] `samples/testing/test-colored-output.cs` - Add using, fix NuruAppBuilder() -> CreateSlimBuilder()
- [ ] `samples/testing/test-output-capture.cs` - Same as above
- [ ] `samples/testing/test-terminal-injection.cs` - Same as above

### API mismatch samples
- [ ] `samples/unified-middleware/unified-middleware.cs` - Convert to fluent Map() API

## Notes

### Fix Pattern for Testing Samples

**Before (broken):**
```csharp
using TimeWarp.Nuru;

using TestTerminal terminal = new();  // ❌ TestTerminal not found
NuruCoreApp app = new NuruAppBuilder()  // ❌ constructor is internal
  .UseTerminal(terminal)
  ...
```

**After (fixed):**
```csharp
using TimeWarp.Nuru;
using TimeWarp.Terminal;  // ✅ Add this

using TestTerminal terminal = new();
NuruCoreApp app = NuruCoreApp.CreateSlimBuilder()  // ✅ Use factory method
  .UseTerminal(terminal)
  ...
```

### Fix Pattern for unified-middleware.cs

**Before (broken):**
```csharp
.Map(pattern: "add {x:int} {y:int}", handler: (...) => {...}, description: "...")
```

**After (fixed):**
```csharp
.Map("add {x:int} {y:int}")
  .WithHandler((...) => {...})
  .WithDescription("...")
  .AsQuery()
  .Done()
```
