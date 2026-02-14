# Investigate issue 175 - parameterized service constructor regression

## Description

Claim: After updating to beta.48, services with parameterized constructors cause compilation errors. The source generator creates references to non-existent static fields.

### Reported Issue Details

**Error Message:** (Not specified in issue, but mentions CS0103 errors)

**Reproduction Steps:**
1. Service with parameterized constructor (implementation not shown)
2. Registered in ConfigureServices (implementation not shown)
3. Handler uses the service (implementation not shown)
4. Build fails with CS0103 errors

**Root Cause Claim:**
Generated code (NuruGenerated.g.cs) has:
- Missing lazy field
- Broken reference

**Expected Behavior:**
Option 1: Generate the static lazy field for parameterized services (if they have parameterless constructors for the Lazy init)
Option 2: Generate code that uses `GetRequiredService<T>()` from the service provider
Option 3: Fail with a clear error explaining the limitation

**Related:**
- Issue #172 (parameterized constructor support)
- PR that fixed #172 appears incomplete

### Current Investigation Status

**Test Results:**
- `generator-20-parameterized-service-constructor.cs` - ALL 4 TESTS PASS ✓
  - Should_resolve_service_with_configuration_dependency ✓
  - Should_resolve_service_with_registered_service_dependency ✓
  - Should_resolve_mixed_parameterless_and_parameterized_services ✓
  - Should_resolve_transitive_dependencies ✓

**Code Review Findings:**

The implementation in beta.48 correctly handles parameterized service constructors:

1. **ServiceExtractor** (`service-extractor.cs:274-285`):
   - Extracts constructor dependencies from implementation type
   - Uses `SymbolDisplayFormat.FullyQualifiedFormat` for type names

2. **InterceptorEmitter** (`interceptor-emitter.cs:324-349`):
   - `EmitServiceFields()` only emits Lazy<T> fields for Singleton/Scoped services WITHOUT constructor dependencies
   - This is CORRECT - parameterized constructors are resolved inline, not via lazy
   - Line 332: `Where(s => (s.Lifetime is ServiceLifetime.Singleton or ServiceLifetime.Scoped) && !s.HasConstructorDependencies)`

3. **ServiceResolverEmitter** (`service-resolver-emitter.cs:86-114`):
   - Checks `service.HasConstructorDependencies`
   - If true, emits `new {ImplementationTypeName}({resolvedArgs})` with inline dependency resolution
   - **No Lazy<T> field is used for parameterized constructors** - this is by design

**Analysis:**
The current implementation is **correct** - parameterized service constructors are resolved inline by inlining `new Type(dep1, dep2...)`. This is the design goal: compile-time resolution without runtime DI.

### Possible Explanations

Since the tests all pass and the code is correct, the issue might be:

1. **User's scenario not covered by tests** - Perhaps they're using a different pattern
   - Example: Singleton service with parameterized constructor (should use runtime DI)
   - Example: Complex dependency chain we haven't tested
   - Example: Using UseMicrosoftDependencyInjection() with mixed services

2. **Misunderstanding of feature behavior** - The issue says "generate references to non-existent static fields" but we don't generate lazy fields for parameterized services by design

3. **Edge case not tested** - Need to understand the exact service/registration pattern from the user

### Next Steps

- [ ] Clarify the reproduction case with the user - get their exact code
- [ ] Ask for the generated `NuruGenerated.g.cs` code showing the broken reference
- [ ] Ask for the build error message (CS0103 details)
- [ ] Create a test case that reproduces the reported issue if we can understand the pattern

## Notes

Issue #425 (fix source generator parameterized service constructors) was marked as done in beta.47/beta.48. The tests confirm the feature works for all tested scenarios:

1. Service with IConfiguration dependency
2. Service with other registered service dependency
3. Mixed mode: parameterless + parameterized services
4. Transitive dependencies (service depending on service)

Need more information from the user to reproduce the reported regression.

## Checklist

- [ ] Get reproduction code from the user
- [ ] Get generated NuruGenerated.g.cs code
- [ ] Get full build error message
- [ ] Create test case matching their scenario
- [ ] Fix if genuine issue, or clarify misinterpretation
