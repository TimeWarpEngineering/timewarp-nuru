# Achieve Full AOT Compatibility

## Goal
Enable TimeWarp.Nuru to support full AOT (Ahead-Of-Time) compilation without warnings, providing optimal performance and reduced binary size for command-line applications.

## Current State Analysis

### What's Working
- ✅ JsonSerializerContext exists for AOT-compatible JSON serialization (`NuruJsonSerializerContext.cs`)
- ✅ Test apps compile with AOT (using TrimMode=partial)
- ✅ Roslyn analyzer implemented as IIncrementalGenerator
- ✅ Core routing logic doesn't require runtime code generation

### AOT Incompatibilities Identified

#### 1. DelegateParameterBinder.cs
- Line 24: `handler.Method.GetParameters()` - Runtime reflection on method parameters
- Line 28: `handler.DynamicInvoke()` - Dynamic invocation incompatible with AOT
- Line 43: `param.ParameterType.GetElementType()` - Runtime type inspection

#### 2. CommandExecutor.cs (Mediator Pattern)
- Line 28: `Activator.CreateInstance(commandType)` - Runtime type instantiation
- Line 44: `commandType.GetProperties()` - Runtime property discovery
- Lines 57, 62, 66, 74: `property.SetValue()` - Runtime property manipulation
- Line 106: `responseType.GetMethod("ToString")` - Runtime method discovery

#### 3. NuruApp.cs
- Line 148: `taskType.GetProperty("Result")` - Runtime Task.Result access
- Line 151: `resultProperty.GetValue(task)` - Runtime property value extraction

#### 4. Configuration Issues
- Directory.Build.props line 43: All AOT warnings suppressed (IL2026, IL2067, IL2070, IL2075, IL3050, IL2104, IL3053)
- Missing `IsAotCompatible` property in library project
- No trim/AOT analyzers enabled

## Implementation Plan

### Phase 1: Enable AOT Analyzers and Annotations
**Priority: High | Effort: Small**

1. **Update Directory.Build.props:**
   ```xml
   <!-- Remove from NoWarn -->
   <NoWarn>$(NoWarn);CA1812;CA1014</NoWarn>

   <!-- Add analyzers -->
   <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
   <EnableAotAnalyzer>true</EnableAotAnalyzer>
   ```

2. **Update TimeWarp.Nuru.csproj:**
   ```xml
   <PropertyGroup>
     <IsAotCompatible>true</IsAotCompatible>
     <IsTrimmable>true</IsTrimmable>
   </PropertyGroup>
   ```

### Phase 2: Annotate Current Reflection Usage
**Priority: High | Effort: Small**

Add proper attributes to methods using reflection:

```csharp
// DelegateParameterBinder.cs
[RequiresDynamicCode("Uses DynamicInvoke for delegate invocation")]
[RequiresUnreferencedCode("Uses reflection to inspect method parameters")]
public static object? InvokeWithParameters(...)

// CommandExecutor.cs
[RequiresDynamicCode("Creates command instances at runtime")]
[RequiresUnreferencedCode("Uses reflection for property population")]
public Task<object?> ExecuteCommandAsync(...)

// NuruApp.cs
[RequiresDynamicCode("Accesses Task.Result via reflection")]
private async Task<int> ExecuteRouteAsync(...)
```

### Phase 3: Implement Source Generators
**Priority: Medium | Effort: Large**
**Note: Task 008 already exists for this**

Create compile-time code generation to replace runtime reflection:

1. **New Project: TimeWarp.Nuru.SourceGenerators**
   - Analyze AddRoute calls at compile time
   - Generate strongly-typed invoker methods
   - Create route registry mapping

2. **Generated Code Example:**
   ```csharp
   // Input: app.AddRoute("add {x:int} {y:int}", (int x, int y) => x + y)

   // Generated:
   private static object? InvokeRoute_Add_Int_Int(Delegate handler, object?[] args)
   {
       var typedHandler = (Func<int, int, int>)handler;
       return typedHandler((int)args[0], (int)args[1]);
   }
   ```

### Phase 4: Handle Task Results Without Reflection
**Priority: High | Effort: Medium**

Replace reflection-based Task.Result access:

```csharp
// Current (uses reflection):
PropertyInfo? resultProperty = taskType.GetProperty("Result");
object? result = resultProperty.GetValue(task);

// AOT-compatible alternative:
private static async ValueTask<object?> UnwrapTaskResult(Task task)
{
    await task.ConfigureAwait(false);

    return task switch
    {
        Task<int> t => t.Result,
        Task<string> t => t.Result,
        Task<bool> t => t.Result,
        // ... other common types
        _ => HandleGenericTask(task) // Marked with [RequiresDynamicCode]
    };
}
```

### Phase 5: Mediator Pattern Strategy
**Priority: Medium | Effort: Large**

Two options:

**Option A: Source Generator for Mediator**
- Generate command property population code
- Generate handler invocation code
- Maintain full AOT compatibility

**Option B: Document Limitations**
- Mark Mediator approach as requiring `TrimMode=partial`
- Provide clear guidance on when to use Delegate vs Mediator
- Update README with AOT compatibility matrix

### Phase 6: Testing and Validation
**Priority: High | Effort: Medium**

1. **Create AOT test project:**
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <PublishAot>true</PublishAot>
       <TrimMode>full</TrimMode>
       <TrimmerSingleWarn>false</TrimmerSingleWarn>
     </PropertyGroup>
   </Project>
   ```

2. **Validate zero warnings during publish:**
   ```bash
   dotnet publish -c Release -r linux-x64 --no-self-contained
   ```

3. **Automated CI/CD checks for AOT warnings**

## Success Criteria

- [x] Zero AOT/trim warnings for delegate-based routing
- [x] All reflection usage properly annotated
- [x] Source generators eliminate DynamicInvoke
- [x] Clear documentation on AOT limitations
- [x] Test suite validates AOT functionality
- [x] Performance benchmarks show improvement

## Benefits

1. **Performance:**
   - Faster startup time
   - Predictable performance (no JIT)
   - Reduced memory footprint

2. **Size:**
   - Smaller binary size (trimmed unused code)
   - No runtime compilation overhead

3. **Security:**
   - No runtime code generation
   - Reduced attack surface

## Dependencies

- Coordinate with Task 008 (Source Generators for Reflection-Free Routing)
- Consider impact on martinothamar/Mediator integration
- Update documentation and samples

## References

- [Microsoft: Creating AOT-compatible Libraries](https://devblogs.microsoft.com/dotnet/creating-aot-compatible-libraries/)
- [.NET AOT Deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [Trimming Documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/)

## Notes

The goal is to make the direct delegate approach fully AOT-compatible while providing clear guidance for users who need the Mediator pattern's DI capabilities. This aligns with Nuru's philosophy of offering both high-performance and enterprise-friendly options.

## Results

**Status:** Complete

### What Was Implemented

1. **AOT Analyzers Enabled** - `IsAotCompatible=true`, `EnableTrimAnalyzer=true`, `EnableAotAnalyzer=true` in timewarp-nuru-core.csproj

2. **Reflection Usage Annotated** - All reflection-using methods properly annotated with:
   - `[RequiresDynamicCode]`
   - `[RequiresUnreferencedCode]`
   - `[DynamicallyAccessedMembers]` for type parameters

3. **Source Generators Implemented** - `NuruInvokerGenerator` generates typed invokers at compile time:
   - Analyzes `Map*` calls and extracts delegate signatures
   - Generates strongly-typed invoker methods
   - `InvokerRegistry` provides runtime lookup by signature key
   - Eliminates `DynamicInvoke` for registered routes

4. **Mediator AOT Support** - Switched to `martinothamar/Mediator` which uses source generation for full AOT compatibility

5. **Fallback Path Preserved** - `DynamicInvoke` fallback remains as safety net for edge cases (properly annotated)

6. **Testing Validated** - AOT compilation tested and verified working