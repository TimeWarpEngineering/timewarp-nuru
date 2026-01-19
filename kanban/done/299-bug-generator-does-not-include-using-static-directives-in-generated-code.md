# Bug: Generator does not include user's using directives in generated code

## Description

When user code has `using` directives (both regular and `static`), the generated interceptor code fails to compile if handler lambdas reference types/methods from those usings.

**Examples:**
- `using static System.Console;` → `WriteLine(...)` fails
- `using MyApp.Helpers;` → `MyHelper.DoSomething()` fails

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

**New test to add:** `tests/timewarp-nuru-core-tests/generator/generator-06-user-usings.cs`

## Solution Architecture

Extract both regular and `static using` directives from the source file and include them (in global form) in the generated code.

### Data Flow

```
CompilationUnit.Usings
    ↓ ExtractUserUsings()
ImmutableArray<string> (global form)
    ↓ added to AppModel after interpretation
AppModel.UserUsings
    ↓ CombineModels() merges & deduplicates
Combined AppModel
    ↓ InterceptorEmitter.EmitNamespaceAndUsings()
Generated code with user's usings
```

## Files to Modify

### 1. `source/timewarp-nuru-analyzers/generators/models/app-model.cs`

Add new property to store user usings:

```csharp
/// <param name="UserUsings">User's using directives to include in generated code</param>
public sealed record AppModel(
  // ... existing params ...
  ImmutableArray<string> UserUsings  // NEW
)
```

Update `Empty()` factory methods to include empty array.

### 2. `source/timewarp-nuru-analyzers/generators/ir-builders/ir-app-builder.cs`

Add field and method to collect user usings:

```csharp
private readonly List<string> UserUsings = [];

public TSelf AddUserUsings(IEnumerable<string> usings)
{
  UserUsings.AddRange(usings);
  return (TSelf)this;
}
```

Update `FinalizeModel()` to include `UserUsings` in the returned AppModel.

### 3. `source/timewarp-nuru-analyzers/generators/extractors/app-extractor.cs`

Extract user usings from CompilationUnit and add to models:

```csharp
/// <summary>
/// Extracts user's using directives and converts to global form.
/// </summary>
private static ImmutableArray<string> ExtractUserUsings(CompilationUnitSyntax compilationUnit)
{
  ImmutableArray<string>.Builder usings = ImmutableArray.CreateBuilder<string>();
  
  foreach (UsingDirectiveSyntax usingDirective in compilationUnit.Usings)
  {
    // Skip if it's an alias (using Foo = Bar;)
    if (usingDirective.Alias is not null)
      continue;
    
    string namespaceName = usingDirective.Name?.ToString() ?? "";
    if (string.IsNullOrEmpty(namespaceName))
      continue;
    
    // Convert to global form for safety
    string globalUsing = usingDirective.StaticKeyword != default
      ? $"using static global::{namespaceName};"
      : $"using global::{namespaceName};";
    
    usings.Add(globalUsing);
  }
  
  return usings.ToImmutable();
}
```

Modify AppModel after `interpreter.Interpret*()` returns to add usings.

### 4. `source/timewarp-nuru-analyzers/generators/nuru-generator.cs`

In `CombineModels()`, merge `UserUsings` from all models:

```csharp
ImmutableArray<string>.Builder allUserUsings = ImmutableArray.CreateBuilder<string>();

// In the foreach loop:
allUserUsings.AddRange(model.UserUsings);

// In the final AppModel construction:
UserUsings: allUserUsings.Distinct().ToImmutableArray()
```

### 5. `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs`

Modify `EmitNamespaceAndUsings()` to accept `AppModel` and emit user's usings:

```csharp
private static void EmitNamespaceAndUsings(StringBuilder sb, AppModel model)
{
  sb.AppendLine("namespace TimeWarp.Nuru.Generated");
  sb.AppendLine("{");
  sb.AppendLine();
  
  // Standard usings (existing)
  sb.AppendLine("using global::System.Linq;");
  // ... etc ...
  
  // User's usings (NEW)
  if (model.UserUsings.Length > 0)
  {
    sb.AppendLine();
    sb.AppendLine("// User-defined usings");
    foreach (string userUsing in model.UserUsings)
    {
      sb.AppendLine(userUsing);
    }
  }
  
  sb.AppendLine();
}
```

Update `Emit()` to pass model to `EmitNamespaceAndUsings()`.

Remove any hardcoded hacks.

## Filtering Considerations

Exclude usings already in the generated code's default set to avoid duplicates:

- `System.Linq`
- `System.Net.Http`
- `System.Reflection`
- `System.Runtime.CompilerServices`
- `System.Text.Json`
- `System.Text.Json.Serialization`
- `System.Text.RegularExpressions`
- `System.Threading.Tasks`
- `Microsoft.Extensions.Configuration`
- `Microsoft.Extensions.Configuration.Json`
- `Microsoft.Extensions.Configuration.EnvironmentVariables`
- `Microsoft.Extensions.Configuration.UserSecrets` (DEBUG only)
- `TimeWarp.Nuru`
- `TimeWarp.Terminal`

Filter these out in `ExtractUserUsings()` or in the emitter.

## Test Cases for `generator-06-user-usings.cs`

1. `using static System.Console;` - `WriteLine()` works
2. `using static System.Math;` - `Abs()`, `Round()` work
3. Multiple static usings
4. Regular usings (e.g., `using System.IO;`)
5. No custom usings (should still work with defaults)
6. Duplicate usings (should be deduplicated)

## Checklist

- [x] Revert any hardcoded hacks in `interceptor-emitter.cs`
- [x] Add `UserUsings` property to `AppModel`
- [x] Update `AppModel.Empty()` factory methods
- [x] ~~Add `UserUsings` field and method to `IrAppBuilder`~~ (Not needed - used Approach 2)
- [x] ~~Update `IIrAppBuilder` interface~~ (Not needed - used Approach 2)
- [x] Add `ExtractUserUsings()` helper to `AppExtractor`
- [x] Modify `AppExtractor.Extract()` to add usings to returned models
- [x] Update `CombineModels()` in `nuru-generator.cs` to merge usings
- [x] Update `EmitNamespaceAndUsings()` to accept model and emit user usings
- [x] Filter out default usings to avoid duplicates
- [x] Create test file `generator-06-user-usings.cs`
- [x] Verify all existing generator tests still pass
- [ ] Verify `samples/02-calculator/01-calc-delegate.cs` compiles (after #300 is also fixed)

## Notes

This blocks using `using static System.Console;` pattern which is common in simple CLI samples.

Must be fixed before Bug #300 (typed parameters with options).
