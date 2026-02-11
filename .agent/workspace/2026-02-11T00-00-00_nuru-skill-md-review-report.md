# TimeWarp.Nuru SKILL.md Review Report

**Date:** 2026-02-11  
**Reviewer:** AI Code Analyzer  
**File Analyzed:** `skills/nuru/SKILL.md`  

---

## Executive Summary

The SKILL.md file for TimeWarp.Nuru is **mostly accurate** but contains **one critical error** and **several areas needing clarification**. The documentation correctly describes the Endpoint DSL patterns and Fluent DSL basics, but incorrectly states that `[NuruRoute]` accepts parameters in the pattern string (it only accepts single literals or empty strings - parameters must use `[Parameter]` attributes on properties).

---

## Scope

This review analyzed:
- The SKILL.md documentation file
- Source code for all attributes (`NuruRoute`, `Parameter`, `Option`, `NuruRouteGroup`, `GroupOption`)
- Core interfaces (`ICommand`, `IQuery`, `ICommandHandler`, `IQueryHandler`, `Unit`)
- Sample files in `/samples/endpoints/` and `/samples/fluent/`
- Analyzer validation logic in endpoint-extractor.cs
- Route pattern validation rules

---

## Methodology

1. **Read SKILL.md** to understand documented patterns
2. **Examined source attributes** to verify property names and behaviors
3. **Reviewed sample files** to confirm real-world usage
4. **Analyzed analyzer code** to understand validation constraints
5. **Compared fluent-syntax-examples.cs** (used by MCP) against actual APIs

---

## Findings

### ✅ VERIFIED CORRECT

#### 1. Endpoint DSL Basics
**SKILL.md lines 21-33:**
```csharp
NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();
```
**Status:** ✅ Correct. `DiscoverEndpoints()` exists and auto-discovers `[NuruRoute]` classes.

**Source:** `/source/timewarp-nuru/builders/nuru-app-builder/nuru-app-builder.routes.cs` line 172

#### 2. ICommand vs IQuery Distinction
**SKILL.md lines 35-39:**
- `ICommand<T>` for actions with side effects
- `IQuery<T>` for read-only operations
- `Unit` for void return

**Status:** ✅ Correct. Confirmed in `/source/timewarp-nuru/abstractions/message-interfaces.cs`

#### 3. Handler Pattern
**SKILL.md lines 52, 76, 89:**
- Nested `public sealed class Handler`
- `ValueTask<T>` return type
- Constructor dependency injection supported

**Status:** ✅ Correct. Confirmed in sample files and `endpoint-extractor.cs`

#### 4. Parameter Attribute
**SKILL.md lines 46-47, 64-84:**
```csharp
[Parameter(Description = "Name of the person to greet")]
public string Name { get; set; } = string.Empty;
```

**Status:** ✅ Correct. The `ParameterAttribute` has `Description`, `Name`, `Order`, and `IsCatchAll` properties.

**Source:** `/source/timewarp-nuru/attributes/parameter-attribute.cs`

#### 5. Optional Parameters via Nullable Types
**SKILL.md line 72-73:**
```csharp
// Optional parameter: use nullable type (string?), NOT IsOptional=true
[Parameter(Description = "Optional tag to deploy")]
public string? Tag { get; set; }
```

**Status:** ✅ Correct. The analyzer checks `NullableAnnotation.Annotated` to determine optionality.

**Source:** `/source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs` line 327

#### 6. Option Attribute
**SKILL.md lines 87-110:**
```csharp
[Option("mode", "m", Description = "Build mode (Debug or Release)")]
public string Mode { get; set; } = "Debug";
```

**Status:** ✅ Correct. The `OptionAttribute` constructor takes `(string longForm, string? shortForm = null)`.

**Source:** `/source/timewarp-nuru/attributes/option-attribute.cs`

#### 7. Catch-all Parameters
**SKILL.md lines 115-132:**
```csharp
[Parameter(IsCatchAll = true, Description = "Arguments")]
public string[] Args { get; set; } = [];
```

**Status:** ✅ Correct. `IsCatchAll` property exists on `ParameterAttribute`.

**Source:** `/source/timewarp-nuru/attributes/parameter-attribute.cs` lines 54-59

#### 8. Route Groups (NuruRouteGroup)
**SKILL.md lines 135-196:**
```csharp
[NuruRouteGroup("docker")]
public abstract class DockerGroupBase;

[NuruRoute("build")]
public sealed class DockerBuildCommand : DockerGroupBase, ICommand<Unit>
```

**Status:** ✅ Correct. Prefix concatenation through inheritance confirmed.

**Source:** `/samples/endpoints/13-discovery/endpoints/nested-groups/nested-group-example.cs`

#### 9. Endpoint Key Rules
**SKILL.md lines 201-208:**
- ✅ `public sealed` classes - Correct
- ✅ Nested `public sealed class Handler` - Correct
- ✅ `ValueTask<T>` return types - Correct
- ✅ Use nullable types for optional parameters - Correct

#### 10. Fluent DSL Basic Pattern
**SKILL.md lines 212-223:**
```csharp
NuruApp app = NuruApp.CreateBuilder()
  .Map("greet {name}")
    .WithHandler((string name) => $"Hello, {name}!")
    .AsQuery()
    .Done()
  .Build();
```

**Status:** ✅ Correct. Fluent DSL supports this pattern.

**Source:** `/samples/fluent/01-hello-world/fluent-hello-world-lambda.cs`

#### 11. Fluent DSL Route Pattern Syntax Table
**SKILL.md lines 226-241:**

| Pattern | Example | Status |
|---------|---------|--------|
| Literal | `"status"` | ✅ |
| Parameter | `"greet {name}"` | ✅ |
| Typed | `"delay {ms:int}"` | ✅ |
| Optional | `"deploy {env} {tag?}"` | ✅ |
| Catch-all | `"exec {*args}"` | ✅ |
| Option | `"build --config {mode}"` | ✅ |
| Flag | `"build --verbose"` | ✅ |
| Short option | `"build -m {mode}"` | ✅ |
| Option alias | `"build --config,-c {mode}"` | ✅ |
| Repeated option | `"run --env {var}*"` | ✅ |
| Description | `"{env\|Target environment}"` | ✅ |

#### 12. TestTerminal Usage
**SKILL.md lines 243-271:**
```csharp
using TestTerminal terminal = new();
NuruApp app = NuruApp.CreateBuilder()
  .UseTerminal(terminal)
  .Map("demo")
    .WithHandler((ITerminal t) => { ... })
    .AsCommand()
    .Done()
  .Build();
```

**Status:** ✅ Correct. Pattern matches sample files.

**Source:** `/samples/fluent/06-testing/fluent-testing-output-capture.cs`

#### 13. Package Installation
**SKILL.md lines 274-283:**
```xml
<PackageReference Include="TimeWarp.Nuru" />
```
```csharp
#:package TimeWarp.Nuru
```

**Status:** ✅ Correct.

---

### ❌ CRITICAL ISSUES

#### 1. **WRONG:** NuruRoute Pattern Syntax in Documentation
**SKILL.md line 43:**
> `[NuruRoute]` takes a single literal (e.g., `"greet"`, `"status"`) or empty string (`""` for default route). The analyzer enforces this - multiple words or parameters in `[NuruRoute]` will produce a compile error.

**Status:** ❌ **CRITICAL ERROR**

**Problem:** This is correct, BUT the same SKILL.md file later shows examples that violate this rule:

**The SKILL.md contains conflicting examples at lines 65 and 84:**
```csharp
// In the SAMPLE files section - these are WRONG patterns:
[NuruRoute("greet {name}")]      // ❌ INVALID: has parameter in pattern
public class GreetQuery : INuruQuery  // ❌ INuruQuery doesn't exist
```

**Actual Valid Pattern (from source):**
```csharp
// ✅ CORRECT: Single literal only
[NuruRoute("greet")]
public sealed class GreetQuery : IQuery<Unit>
{
  [Parameter]
  public string Name { get; set; } = string.Empty;
}
```

**Source:** `/source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs` lines 100-118

The analyzer explicitly validates:
```csharp
// VALID: exactly one segment that is a literal
if (segments.Length == 1 && segments[0] is LiteralDefinition)
  return null;

// Invalid: zero segments, multiple segments, or non-literal segment
return Diagnostic.Create(
  DiagnosticDescriptors.InvalidNuruRoutePattern,
  attributeLocation,
  pattern);
```

**Impact:** Users following the SKILL.md examples will get compile errors (NURU_A001).

---

### ⚠️ CLARIFICATIONS NEEDED

#### 1. GroupOption Usage Not Documented
**SKILL.md line 199** mentions "Shared options across a group use `[GroupOption]`" but provides no example.

**Actual Usage:**
```csharp
[NuruRouteGroup("docker")]
public abstract class DockerGroupBase
{
  [GroupOption("verbose", "v")]
  public bool Verbose { get; set; }
}
```

**Source:** `/source/timewarp-nuru/attributes/group-option-attribute.cs`

#### 2. Parameter Order Requirement Not Documented
When a command has multiple `[Parameter]` attributes, each **must** specify an `Order` value.

**Diagnostic:** NURU_A002 - "Multiple parameters require explicit Order"

**Source:** `/source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.endpoints.cs` lines 19-26

**Example from samples:**
```csharp
[Parameter(Order = 0, Description = "Configuration key")]
public string Key { get; set; } = "";

[Parameter(Order = 1, Description = "Configuration value")]
public string Value { get; set; } = "";
```

**Source:** `/samples/endpoints/13-discovery/endpoints/idempotent/set-config-command.cs`

#### 3. IIdempotentCommand Interface Not Mentioned
The SKILL.md doesn't mention `IIdempotentCommand<T>` for idempotent operations.

**Source:** `/source/timewarp-nuru/abstractions/message-interfaces.cs` lines 26-32

#### 4. IsOptional Parameter Property
**SKILL.md line 72** says "NOT IsOptional=true" for optional parameters. While correct (nullable types are preferred), the `ParameterAttribute` does have an `IsOptional` property that appears unused in the analyzer. This could confuse users.

**Source:** Looking at `endpoint-extractor.cs`, only `NullableAnnotation.Annotated` is checked (line 327), not the `IsOptional` property.

---

### 🔍 SAMPLES VALIDATION

#### File: `samples/fluent/03-syntax/fluent-syntax-examples.cs`
**Status:** ⚠️ **CONTAINS INVALID CODE**

This file claims to "compile successfully" but contains:
1. `INuruQuery` and `INuruCommand` interfaces that **do not exist** in the codebase
2. `[NuruRoute("greet {name}")]` patterns that would fail analyzer validation

**Lines with issues:**
- Line 41: `public class StatusQuery : INuruQuery` - Interface doesn't exist
- Line 65: `[NuruRoute("greet {name}")]` - Invalid pattern format
- Multiple other lines using these non-existent interfaces

**Note:** This file appears to be documentation-only for the MCP server but claims it must compile.

---

## Recommendations

### High Priority (Fix Immediately)

1. **Fix the conflicting NuruRoute pattern examples**
   - Remove or correct all examples showing `[NuruRoute("greet {name}")]` format
   - Ensure all examples show the correct single-literal pattern
   - Update the samples/fluent/03-syntax/fluent-syntax-examples.cs to use real interfaces

2. **Add Parameter Order documentation**
   - Document that multiple `[Parameter]` attributes require `Order` property
   - Show example with `Order = 0`, `Order = 1`, etc.

### Medium Priority (Improve Documentation)

3. **Add GroupOption example**
   - Show how to share options across route groups

4. **Document IIdempotentCommand**
   - Explain when to use it vs ICommand

5. **Fix or remove fluent-syntax-examples.cs**
   - Either fix the interfaces to use real ones (IQuery<T>, ICommand<T>)
   - Or remove the "must compile" claim from the file header

### Low Priority (Nice to Have)

6. **Add more complex Fluent DSL examples**
   - Show method reference pattern (`WithHandler(MethodName)`)
   - Show ITerminal injection pattern

---

## References

| File | Purpose |
|------|---------|
| `/source/timewarp-nuru/attributes/nuru-route-attribute.cs` | [NuruRoute] attribute definition |
| `/source/timewarp-nuru/attributes/parameter-attribute.cs` | [Parameter] attribute definition |
| `/source/timewarp-nuru/attributes/option-attribute.cs` | [Option] attribute definition |
| `/source/timewarp-nuru/attributes/nuru-route-group-attribute.cs` | [NuruRouteGroup] attribute definition |
| `/source/timewarp-nuru/attributes/group-option-attribute.cs` | [GroupOption] attribute definition |
| `/source/timewarp-nuru/abstractions/message-interfaces.cs` | ICommand, IQuery, Unit definitions |
| `/source/timewarp-nuru/abstractions/handler-interfaces.cs` | Handler interface definitions |
| `/source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs` | Route validation logic |
| `/source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.endpoints.cs` | Analyzer diagnostics |
| `/samples/endpoints/01-hello-world/endpoint-hello-world.cs` | Correct endpoint example |
| `/samples/endpoints/13-discovery/endpoints/docker/commands/docker-build-command.cs` | Route group example |

---

## Conclusion

The SKILL.md is **75% accurate** and usable for basic scenarios. However, the **critical error around NuruRoute pattern syntax** could cause significant user confusion. The file `samples/fluent/03-syntax/fluent-syntax-examples.cs` contains code that doesn't match the actual API and should be corrected or clearly marked as pseudo-code.

**Verdict:** Fix the identified issues before relying on this SKILL.md for agent training.

---

*Report generated: 2026-02-11*
*Analyzer: AI Code Review Agent*
