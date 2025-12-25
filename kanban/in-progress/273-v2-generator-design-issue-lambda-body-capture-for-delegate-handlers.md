# V2 Generator Design Issue: Lambda Body Capture for Delegate Handlers

## Description

The V2 generator has a **fundamental design issue**: the current implementation attempts to invoke handlers using a `Handler(args)` pattern, but the generated code doesn't have access to the actual lambda/delegate code at runtime.

**The Problem:**
When a user writes:
```csharp
.Map("ping")
  .WithHandler(() => "pong")
  .AsQuery()
  .Done()
```

The generator sees the lambda `() => "pong"` in the source code during compilation. However, the current `HandlerInvokerEmitter` generates code like:
```csharp
string result = Handler(args);  // ERROR: "Handler" is not defined
```

There is no `Handler` symbol available in the generated code - the lambda exists only in the original source.

## Parent

#265 Epic: V2 Source Generator Implementation

## Blocked

#272 V2 Generator Phase 6: Testing - Cannot complete testing until this is resolved

## Decision: Option A - Capture Lambda Body Source

**Chosen approach:** Capture the lambda body as source text during extraction and transform it into a local function in the generated code.

**Why this works:**
1. **No closures allowed** - The existing analyzer `NURU_H002` enforces that lambda handlers cannot capture variables from enclosing scope
2. **Self-contained handlers** - All inputs come from route parameters or DI injection
3. **Architecture doc already specified this** - The `HandlerModel` in the design doc includes `LambdaSource` property (line 481)

**Key insight:** Every lambda can be transformed into a local function:

| Lambda Form                        | Generated Local Function       |
| ---------------------------------- | ------------------------------ |
| `() => "pong"`                     | `string __handler_0() => "pong";` |
| `(string name) => $"Hello {name}"` | `string __handler_0() => $"Hello {name}";` |
| `() => { DoWork(); return "done"; }` | `string __handler_0() { DoWork(); return "done"; }` |
| `async () => await FetchAsync()`   | `async Task<string> __handler_0() => await FetchAsync();` |

## Implementation Plan

### Step 1: Update `HandlerDefinition` Model

**File:** `generators/models/handler-definition.cs`

Add two new properties to the record:

```csharp
internal sealed record HandlerDefinition(
  HandlerKind HandlerKind,
  string? FullTypeName,
  string? MethodName,
  string? LambdaBodySource,  // NEW: Lambda body text (expression or block with braces)
  bool IsExpressionBody,     // NEW: true for expression body, false for block body
  ImmutableArray<ParameterBinding> Parameters,
  HandlerReturnType ReturnType,
  bool IsAsync,
  bool RequiresCancellationToken,
  bool RequiresServiceProvider)
```

Update `ForDelegate` factory method to accept these parameters.

### Step 2: Fix `InferReturnType` for Block Bodies

**File:** `generators/extractors/handler-extractor.cs`

**Issue discovered:** Current `InferReturnType` only handles expression bodies. For block bodies like `() => { return "hello"; }`, it incorrectly returns `Void`.

**Fix:** Add block body handling:

```csharp
// Block body - find return statements and infer type
if (body is BlockSyntax block)
{
  ReturnStatementSyntax? returnStatement = block
    .DescendantNodes()
    .OfType<ReturnStatementSyntax>()
    .FirstOrDefault(r => r.Expression is not null);
  
  if (returnStatement?.Expression is not null)
  {
    TypeInfo typeInfo = semanticModel.GetTypeInfo(returnStatement.Expression, cancellationToken);
    // ... create HandlerReturnType from typeInfo
  }
}
```

### Step 3: Capture Lambda Body in `HandlerExtractor`

**File:** `generators/extractors/handler-extractor.cs`

In `ExtractFromLambda` and `ExtractFromSimpleLambda`:

```csharp
string? lambdaBodySource = null;
bool isExpressionBody = false;

if (lambda.Body is ExpressionSyntax expr)
{
  lambdaBodySource = expr.ToFullString();
  isExpressionBody = true;
}
else if (lambda.Body is BlockSyntax block)
{
  lambdaBodySource = block.ToFullString();
  isExpressionBody = false;
}
```

### Step 4: Update `HandlerInvokerEmitter`

**File:** `generators/emitters/handler-invoker-emitter.cs`

Transform lambdas into local functions. Use route index for unique naming (`__handler_0`, `__handler_1`, etc.).

**Expression body (has value):**
```csharp
// Input: () => "pong"
// Output:
string __handler_0() => "pong";
string result = __handler_0();
if (result is not null) app.Terminal.WriteLine(result.ToString());
```

**Block body (has value):**
```csharp
// Input: () => { DoWork(); return "done"; }
// Output:
string __handler_0()
{ DoWork(); return "done"; }
string result = __handler_0();
if (result is not null) app.Terminal.WriteLine(result.ToString());
```

**Async block body:**
```csharp
// Input: async () => { await Task.Delay(1); return "done"; }
// Output:
async Task<string> __handler_0()
{ await Task.Delay(1); return "done"; }
string result = await __handler_0();
if (result is not null) app.Terminal.WriteLine(result.ToString());
```

**Void handlers:**
```csharp
// Input: () => { Console.WriteLine("hi"); }
// Output:
void __handler_0()
{ Console.WriteLine("hi"); }
__handler_0();
```

### Step 5: Handle Method Groups

For handlers like `.WithHandler(MyClass.DoWork)`:

```csharp
// Generate direct call using FullTypeName and MethodName
string result = MyClass.DoWork(name);
if (result is not null) app.Terminal.WriteLine(result.ToString());
```

### Step 6: Add `NURU_H005` Parameter Name Mismatch Analyzer

**File:** `analyzers/diagnostics/diagnostic-descriptors.handler.cs`

```csharp
public static readonly DiagnosticDescriptor ParameterNameMismatch = new(
  id: "NURU_H005",
  title: "Handler parameter name doesn't match route segment",
  messageFormat: "Handler parameter '{0}' doesn't match any route segment. Available segments: {1}",
  category: HandlerCategory,
  defaultSeverity: DiagnosticSeverity.Error,  // ERROR - blocks compilation
  isEnabledByDefault: true,
  description: "Handler parameter names must match route segment names for code generation.");
```

**File:** `analyzers/nuru-handler-analyzer.cs`

Add validation that handler parameter names match route segment names. This ensures generated code compiles.

### Step 7: Update Route Matcher Variable Names

**File:** `generators/emitters/route-matcher-emitter.cs`

Ensure pattern variables match handler parameter names:

```csharp
// Route: "greet {name}" with handler (string name) => ...
if (args is ["greet", var name])  // 'name' matches handler parameter
{
  // handler body uses 'name' directly
}
```

### Step 8: Update `AnalyzerReleases.Unshipped.md`

```
NURU_H005 | Handler.Validation | Error | Handler parameter name doesn't match route segment
```

## Checklist

- [x] Update `HandlerDefinition` model with `LambdaBodySource` and `IsExpressionBody`
- [x] Fix `InferReturnType` to handle block bodies with return statements
- [x] Capture lambda body source in `HandlerExtractor`
- [x] Update `HandlerInvokerEmitter` to emit local functions
- [ ] Handle method group handlers with qualified name calls
- [x] Add `NURU_H005` diagnostic descriptor
- [ ] Add parameter name validation to `NuruHandlerAnalyzer`
- [x] Update `AnalyzerReleases.Unshipped.md`
- [x] Ensure route matcher variable names align with handler parameters
- [ ] Test with minimal test case from task #272
- [x] **Fix .NET 10 / C# 14 interceptor compatibility** (added during implementation)

## Files to Modify

| File | Changes |
| ---- | ------- |
| `generators/models/handler-definition.cs` | Add `LambdaBodySource`, `IsExpressionBody` properties |
| `generators/extractors/handler-extractor.cs` | Capture lambda body, fix `InferReturnType` for blocks |
| `generators/emitters/handler-invoker-emitter.cs` | Emit local functions for all lambda types |
| `generators/emitters/route-matcher-emitter.cs` | Ensure variable names match handler parameters |
| `analyzers/diagnostics/diagnostic-descriptors.handler.cs` | Add `NURU_H005` |
| `analyzers/nuru-handler-analyzer.cs` | Add parameter name validation |
| `AnalyzerReleases.Unshipped.md` | Document `NURU_H005` |

## Design Constraints

### No Closures Allowed

The existing analyzer `NURU_H002` enforces this:

> "Lambda handlers cannot capture variables from the enclosing scope. The Mediator handler will not be generated for this route. Pass dependencies as handler parameters and they will be injected from the DI container."

This constraint makes Option A viable - since handlers are self-contained, the lambda body can be safely duplicated in generated code.

### Parameter Names Must Match

Handler parameter names must exactly match route segment names. The generator relies on this to wire up captured route values to the handler body. Example:

```csharp
// Route: "greet {name}"
// Handler: (string name) => $"Hello {name}!"  ✓ OK - 'name' matches
// Handler: (string n) => $"Hello {n}!"        ✗ ERROR - 'n' doesn't match segment 'name'
```

## Risks & Considerations

1. **Whitespace/formatting:** `ToFullString()` preserves trivia. May need `NormalizeWhitespace()` for cleaner output.

2. **Nested lambdas:** If handler body contains nested lambdas, they're captured as-is. Should work but worth testing.

3. **Service parameters:** Handlers with DI-injected parameters need service resolution emitted before handler call. `ServiceResolverEmitter` handles this.

4. **Async expression bodies:** `async () => await FetchAsync()` - ensure proper handling of await in expression position.

## Notes

### Discovery Context

This issue was discovered while implementing Phase 6 Testing (Task #272). The minimal test case:

```csharp
NuruCoreApp app = NuruApp.CreateBuilder([])
  .UseTerminal(terminal)
  .Map("ping")
    .WithHandler(() => "pong")
    .AsQuery()
    .Done()
  .Build();

int exitCode = await app.RunAsync(["ping"]);
```

### Architecture Doc Reference

The design document `.agent/workspace/2024-12-25T14-00-00_v2-source-generator-architecture.md` at line 481 specifies:

```csharp
public sealed class HandlerModel
{
    public string? LambdaSource { get; init; }  // For inline lambdas
    // ...
}
```

This was specified in the design but never implemented in Phase 1 (models) or Phase 3 (extractors).

### Related Diagnostics

| Diagnostic | Severity | Purpose |
| ---------- | -------- | ------- |
| `NURU_H001` | Error | Instance method handlers not supported |
| `NURU_H002` | Warning | Closure detected in handler (blocks generation) |
| `NURU_H003` | Error | Unsupported handler expression type |
| `NURU_H004` | Warning | Private method handler not accessible |
| `NURU_H005` | Error | Handler parameter name doesn't match route segment (NEW) |

### Session 2024-12-26: .NET 10 / C# 14 Interceptor Compatibility

**Problem Discovered:** The original interceptor implementation used the deprecated `InterceptsLocationAttribute(string filePath, int line, int character)` constructor. In .NET 10 / C# 14, this was replaced with a new versioned encoding.

**Changes Made:**

1. **New Roslyn API:** Used `SemanticModel.GetInterceptableLocation()` which returns an `InterceptableLocation` object with:
   - `Version` (int) - encoding version
   - `Data` (string) - opaque base64-encoded location data
   - `GetInterceptsLocationAttributeSyntax()` - generates the full attribute

2. **Updated `InterceptSiteModel`** - Now wraps `InterceptableLocation` instead of raw file/line/column. Keeps file/line/column for diagnostics only.

3. **Updated `InterceptSiteExtractor`** - Uses the new API, requires `SemanticModel` parameter.

4. **Updated `InterceptorEmitter`** - Emits the new attribute format:
   ```csharp
   [global::System.Runtime.CompilerServices.InterceptsLocationAttribute(1, "base64data==")]
   ```

5. **MSBuild Property Change:** `InterceptorsPreviewNamespaces` → `InterceptorsNamespaces`

6. **Additional Fixes:**
   - Changed `WriteError` to `WriteErrorLineAsync` (correct ITerminal method)
   - Removed `CancellationToken` from interceptor signature (doesn't match `RunAsync`)
   - Added `System.Reflection` using for `GetCustomAttribute<T>()`
   - Fixed namespace structure (block-scoped for both namespaces in same file)

**Files Modified:**
- `Directory.Build.props` - Added `InterceptorsNamespaces`
- `tests/test-apps/Directory.Build.props` - Added `InterceptorsNamespaces`
- `generators/models/intercept-site-model.cs` - Wraps `InterceptableLocation`
- `generators/extractors/intercept-site-extractor.cs` - Uses new Roslyn API
- `generators/extractors/builders/app-model-builder.cs` - Updated signature
- `generators/locators/run-async-locator.cs` - Uses new extractor
- `generators/emitters/interceptor-emitter.cs` - Emits new attribute format
- `generators/nuru-generator.cs` - Removed separate polyfill file emission

**Build Status:** Solution builds successfully with 0 errors.

**Remaining Work:**
- Route matching not yet emitting (only built-in flags work)
- Need to complete lambda body emission in `HandlerInvokerEmitter`
- Test with actual route handlers
