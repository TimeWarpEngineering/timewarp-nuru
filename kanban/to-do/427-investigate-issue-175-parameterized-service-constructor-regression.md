# Fix issue 175 - parameterized service constructor regression

## Description

After updating to beta.48, Singleton/Scoped services with parameterized constructors cause compilation errors (CS0103). The generator references non-existent Lazy<T> fields.

### Actual Bug/Error

**Error:**
```
error CS0103: The name '__svc_TimeWarp_Zana_Kanban_KanbanService' does not exist in the current context
```

**Service Registration Pattern:**
- Service: `KanbanService` (from timewarp-ganda)
- Likely: Singleton or Scoped with parameterized constructor
- Constructor parameter: `IWorkspaceService` or similar

**Root Cause:**
In `service-resolver-emitter.cs:86-114`:

```csharp
if (service.HasConstructorDependencies)
{
  // Emit inline: new Type(args)
  string args = ResolveConstructorArguments(service, services);
  sb.AppendLine($"{indent}{typeName} {varName} = new {service.ImplementationTypeName}({args});");
}
else if (service.Lifetime == ServiceLifetime.Transient)
{
  sb.AppendLine($"{indent}{typeName} {varName} = new {service.ImplementationTypeName}();");
}
else  // Singleton/Scoped without deps - use Lazy<T>
{
  string fieldName = InterceptorEmitter.GetServiceFieldName(service.ImplementationTypeName);
  sb.AppendLine($"{indent}{typeName} {varName} = {fieldName}.Value;");
}
```

**The bug:** When the code reaches the `else` block (path 3), it references a Lazy<T> field.
But `InterceptorEmitter.EmitServiceFields()` only emits Lazy<T> fields for:
- Singleton/Scoped services AND
- **NOT** HasConstructorDependencies (line 332)

So if:
- `HasConstructorDependencies` is incorrectly `false`, OR
- There's a code path that doesn't check `HasConstructorDependencies` properly

Then the code will reference a non-existent Lazy field.

### Analysis of the Fix in Beta.48

The previous fix (#425) added `HasConstructorDependencies` handling, but it appears incomplete.

Looking at line 100-112, the logic is:
1. Check `HasConstructorDependencies` first → emit inline
2. Else check `Transient` → emit new()
3. Else (Singleton/Scoped) → use Lazy field

This IS correct logic. So the question is: **Why doesn't it work?**

### Possible Root Causes

1. **`HasConstructorDependencies` is false when it should be true**
   - `ServiceExtractor.ExtractConstructorDependencies()` might not be finding the constructor
   - Constructor might be non-public (private/protected)
   - Constructor parameters might not be resolvable

2. **Code path bypasses the check**
   - There might be another code path that doesn't check `HasConstructorDependencies`

3. **Service lookup returns wrong service**
   - `FindService(typeName, services)` might return a service definition with wrong state

### Investigation Steps

- [ ] Check `ServiceExtractor.ExtractConstructorDependencies()` logic
- [ ] Verify it finds constructors for `KanbanService`
- [ ] Check if constructor visibility matters (public vs non-public)
- [ ] Look for other code paths in ServiceResolverEmitter that might reference Lazy fields

## Notes

**Issue #425 Results said:**
"Successfully fixed issue #172 - source generator now handles services with parameterized constructors."

This fix worked for **Transient** services with parameterized constructors (see test generator-20).

But it seems **Singleton/Scoped** services with parameterized constructors were not tested.

**Test Coverage Gap:**
- `generator-20-parameterized-service-constructor.cs` only tests Transient with parameterized constructors
- No tests for Singleton with parameterized constructors
- No tests for Scoped with parameterized constructors

## Checklist

- [ ] Check ServiceExtractor constructor extraction for non-public constructors
- [ ] Create test case: Singleton service with parameterized constructor
- [ ] Create test case: Scoped service with parameterized constructor
- [ ] Identify the exact code path causing the bug
- [ ] Fix the root cause
- [ ] Add test coverage for Singleton/Scoped with parameterized constructors
- [ ] Verify fix works in timewarp-ganda context