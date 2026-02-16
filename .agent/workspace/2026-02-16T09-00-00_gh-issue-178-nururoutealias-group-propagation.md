# GitHub Issue #178: NuruRouteAlias Attribute Does Not Propagate to Subcommands

## Executive Summary

GitHub Issue #178 reports that `[NuruRouteAlias]` attributes defined on `[NuruRouteGroup]` base classes do not propagate to subcommands. Investigation confirms this is a **feature gap** - the current implementation has no mechanism to extract or apply aliases from group base classes.

## Scope

- Analyze the reported issue: `[NuruRouteAlias("ws")]` on `[NuruRouteGroup("workspace")]` base class does not work for nested subcommands
- Investigate how `[NuruRouteAlias]` is currently processed in the source generator
- Identify the gap in group alias propagation
- **Create test case to validate failure before implementing fix**

## Methodology

- Reviewed the GitHub issue description
- Searched for `NuruRouteAlias` attribute definition and usage
- Analyzed `endpoint-extractor.cs` for attribute extraction logic
- Analyzed `route-matcher-emitter.cs` and `capabilities-emitter.cs` for alias handling
- Examined `route-definition.cs` and `route-definition-builder.cs` for data models
- Reviewed existing sample code using `[NuruRouteAlias]`

## Findings

### 1. Current `[NuruRouteAlias]` Implementation

**Location:** `/source/timewarp-nuru/attributes/nuru-route-alias-attribute.cs`

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class NuruRouteAliasAttribute : Attribute
{
  public string[] Aliases { get; }
  public NuruRouteAliasAttribute(params string[] aliases)
  {
    Aliases = aliases ?? [];
  }
}
```

**Key observations:**
- Supports multiple aliases via `params string[]` - e.g., `[NuruRouteAlias("ws", "work")]`
- `Inherited = false` prevents automatic attribute inheritance to derived classes

### 2. Endpoint Extractor Does Not Extract `[NuruRouteAlias]`

**Location:** `/source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs`

The `Extract` method in `EndpointExtractor`:
1. Extracts `[NuruRoute]` pattern and description (lines 40-143)
2. Extracts `[NuruRouteGroup]` prefix from base class hierarchy (lines 156-242)
3. Does **NOT** extract `[NuruRouteAlias]` from the command class or base classes

### 3. Group Info Extraction - No Alias Collection

**Location:** `/source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs` (lines 156-242)

The `ExtractGroupInfo` method walks the inheritance chain collecting:
- Type hierarchy (list of base class full names)
- Group prefixes from `[NuruRouteGroup]` attributes

**What it does NOT collect:**
- Aliases from `[NuruRouteAlias]` on any class in the hierarchy

### 4. Route Definition Model Supports Aliases

**Location:** `/source/timewarp-nuru-analyzers/generators/models/route-definition.cs`

The `RouteDefinition` record already has an `Aliases` property, but it's only populated for fluent API routes (via `.WithAlias()` calls), not for attributed endpoints.

### 5. Fluent API `.WithAlias()` Works Correctly

**Location:** `/source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs` (line 586)

For fluent routes, `.WithAlias()` is processed correctly. This is a separate code path that works.

### 6. Route Matcher Does Not Handle Aliases

**Location:** `/source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`

There is no code to generate alternative route matching for alias patterns. Even if aliases were extracted, they wouldn't work because the matcher doesn't generate alternative match branches.

### 7. Sample Code May Not Work as Expected

**Location:** `/samples/endpoints/13-discovery/endpoints/commands/goodbye-command.cs`

```csharp
[NuruRoute("goodbye", Description = "Say goodbye and exit")]
[NuruRouteAlias("bye", "cya")]
public sealed class GoodbyeCommand : ICommand<Unit>
```

**This sample may not be working as expected.** The `EndpointExtractor` does not read `[NuruRouteAlias]` from the command class. This needs verification via test.

## Root Cause Analysis

The issue has **two parts**:

1. **Endpoint Extraction Gap** - `EndpointExtractor.Extract()` does not read `[NuruRouteAlias]` from the command class.
2. **Group Alias Propagation Gap** - `ExtractGroupInfo()` walks the hierarchy but only looks for `[NuruRouteGroup]`, not `[NuruRouteAlias]`.

## Alias Semantics

**Simple substitution:** An alias substitutes for the group segment it's defined on.

```
[NuruRouteGroup("workspace")]
[NuruRouteAlias("ws", "work")]
public abstract class WorkspaceGroupBase;
```

- `workspace repo info` → works
- `ws repo info` → should work (alias for `workspace`)
- `work repo info` → should work (another alias for `workspace`)

The alias replaces the group prefix segment where it's defined. Multiple aliases are supported.

## Expected Behavior

For the user's example:

```csharp
[NuruRouteGroup("workspace")]
[NuruRouteAlias("ws")]
public abstract class WorkspaceGroupBase;

[NuruRouteGroup("repo")]
public abstract class WorkspaceRepoGroupBase : WorkspaceGroupBase;

[NuruRoute("info")]
public sealed class WorkspaceRepoInfoCommand : WorkspaceRepoGroupBase, ICommand<Unit>
```

Expected:
- `workspace repo info` works ✓
- `ws repo info` should work (alias on `WorkspaceGroupBase`)

## Implementation Plan

### Step 1: Create Test Case (Do First)

**Create a failing test to validate the bug before fixing.**

Suggested test file: `tests/timewarp-nuru-tests/routing/routing-XX-group-alias.cs`

```csharp
// Test case structure (Jaribu framework):

[NuruRouteGroup("workspace")]
[NuruRouteAlias("ws")]
public abstract class WorkspaceGroupBase;

[NuruRoute("info")]
public sealed class WorkspaceInfoCommand : WorkspaceGroupBase, ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<WorkspaceInfoCommand, Unit>
  {
    public ValueTask<Unit> Handle(WorkspaceInfoCommand command, CancellationToken ct)
    {
      // handler logic
      return Unit.ValueTask;
    }
  }
}

// Test 1: workspace info should work
// Test 2: ws info should work (alias)
```

**Additionally, test that direct alias on command class works:**

```csharp
[NuruRoute("goodbye")]
[NuruRouteAlias("bye", "cya")]
public sealed class GoodbyeCommand : ICommand<Unit>
{
  // ...
}

// Test 1: goodbye should work
// Test 2: bye should work
// Test 3: cya should work
```

### Step 2: Implement Fix

**2a. Extract `[NuruRouteAlias]` from Command Class**

**File:** `endpoint-extractor.cs`

Add method to extract aliases from the command class:

```csharp
private static ImmutableArray<string> ExtractNuruRouteAliasAttribute(
  ClassDeclarationSyntax classDeclaration,
  SemanticModel semanticModel)
{
  // Walk attribute lists looking for NuruRouteAlias
  // Extract all string arguments (handles params string[])
}
```

**2b. Extract Group Aliases from Base Class Hierarchy**

**File:** `endpoint-extractor.cs`

Modify `ExtractGroupInfo` to collect aliases:

```csharp
private readonly record struct GroupInfo
(
  ImmutableArray<string> TypeHierarchy,
  string? FullPrefix,
  ImmutableArray<string> GroupAliases  // NEW
);
```

**2c. Generate Alias Route Match Branches**

**File:** `route-matcher-emitter.cs`

For each alias from group hierarchy, generate an alternative match pattern:

```
// For route: workspace repo info
// With alias: ws (on workspace group)
// Generate match branch for: ws repo info
```

### Step 3: Verify Tests Pass

Run tests to confirm fix works:

```bash
dotnet run tests/timewarp-nuru-tests/routing/routing-XX-group-alias.cs
```

## Checklist for Implementation

- [ ] **Step 1:** Create test case for `[NuruRouteAlias]` on command class (verify current state)
- [ ] **Step 1:** Create test case for `[NuruRouteAlias]` on group base class (verify failure)
- [ ] **Step 1:** Run tests to confirm failure
- [ ] **Step 2a:** Add `ExtractNuruRouteAliasAttribute()` method
- [ ] **Step 2b:** Modify `ExtractGroupInfo()` to collect aliases
- [ ] **Step 2c:** Modify `route-matcher-emitter.cs` for alias match branches
- [ ] **Step 3:** Verify tests pass
- [ ] **Step 3:** Update capabilities emitter if needed
- [ ] **Step 4:** Update documentation and samples

## Files Affected

| File | Changes |
|------|---------|
| `tests/timewarp-nuru-tests/routing/routing-XX-group-alias.cs` | **NEW** - Test case to validate bug |
| `source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs` | Add alias extraction |
| `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs` | Generate alias match branches |
| `source/timewarp-nuru-analyzers/generators/emitters/capabilities-emitter.cs` | May need alias expansion |

## References

- GitHub Issue: https://github.com/TimeWarpEngineering/timewarp-nuru/issues/178
- `[NuruRouteAlias]` attribute: `source/timewarp-nuru/attributes/nuru-route-alias-attribute.cs`
- `[NuruRouteGroup]` attribute: `source/timewarp-nuru/attributes/nuru-route-group-attribute.cs`
- Endpoint extractor: `source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs`
- Route definition: `source/timewarp-nuru-analyzers/generators/models/route-definition.cs`