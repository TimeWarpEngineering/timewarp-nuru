# Bug: Generator does not include 'using static' directives in generated code

## Description

When user code has `using static System.Console;` and handler code uses `WriteLine(...)`, the generated interceptor code fails to compile because the generated file doesn't include the `using static` directive.

**User code:**
```csharp
using static System.Console;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("add {x:double} {y:double}")
    .WithHandler((double x, double y) => WriteLine($"{x} + {y} = {x + y}"))
    .Done()
  .Build();
```

**Generated code (broken):**
```csharp
namespace TimeWarp.Nuru.Generated
{
  // Missing: using static System.Console;
  
  file static class GeneratedInterceptor
  {
    // ...
    void __handler_0() => WriteLine($"{x} + {y} = {x + y}");  // CS0103: 'WriteLine' does not exist
  }
}
```

**Error:**
```
error CS0103: The name 'WriteLine' does not exist in the current context
```

## Test Case

**File:** `samples/02-calculator/01-calc-delegate.cs`

## Reproduction

1. Run `./samples/02-calculator/01-calc-delegate.cs`
2. Observe compilation errors for `WriteLine`

## Analysis

The generator copies handler lambda bodies verbatim into the generated code, but doesn't preserve/include the `using static` directives from the original file that the handler code depends on.

**Options to fix:**
1. Scan source file for `using static` directives and include them in generated code
2. Fully qualify all method calls in handler bodies during code generation
3. Use `global::System.Console.WriteLine(...)` in generated code

## Relevant Source Files

- `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/handler-emitter.cs` (if exists)

## Checklist

- [ ] Add test case for `using static` pattern
- [ ] Identify where handler code is emitted
- [ ] Include `using static` directives in generated code (or fully qualify calls)
- [ ] Verify sample compiles after fix

## Notes

This blocks using `using static System.Console;` pattern which is common in simple CLI samples.
