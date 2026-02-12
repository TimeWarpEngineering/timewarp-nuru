# Implement group-based endpoint filtering for subset CLI editions

## Summary

Enable subset publishing of CLI applications by filtering endpoints based on their route group type. This allows creating specialized editions (e.g., `kanban.exe`, `git.exe`) from a unified codebase.

## Description

The ganda CLI use case requires the ability to publish multiple specialized executables from a single codebase:
- Full edition: `ganda.exe` with all commands (`ganda kanban add`, `ganda git worktree create`)
- Kanban edition: `kanban.exe` with only kanban commands (`kanban add`, `kanban list`)
- Git edition: `git.exe` with only git commands (`git worktree create`, `git worktree list`)

This is achieved by filtering endpoints based on their `[NuruRouteGroup]` inheritance and stripping parent group prefixes.

## API Design

```csharp
// Kanban-only edition (kanban.csproj)
NuruApp.CreateBuilder(args)
    .DiscoverEndpoints(typeof(KanbanGroupBase))
    .Build();

// Git-only edition (git.csproj)
NuruApp.CreateBuilder(args)
    .DiscoverEndpoints(typeof(GitGroupBase))
    .Build();

// Full edition (ganda.csproj) - unchanged behavior
NuruApp.CreateBuilder(args)
    .DiscoverEndpoints()  // All endpoints
    .Build();
```

## Behavior

- Include endpoint if it inherits (directly or indirectly) from **any** specified group type
- Strip all group prefixes **above** the matched type
- Ungrouped endpoints (no `[NuruRouteGroup]` base) are **excluded** when filter is active
- Type matching is case-insensitive on full type names
- Empty filter result produces CLI with no commands (help shows nothing)

## Checklist

### 1. Runtime API Changes
- [ ] Add `DiscoverEndpoints(params Type[] groupTypes)` overload to `NuruAppBuilder`
- [ ] Add XML documentation with examples
- [ ] Ensure backward compatibility (no-args overload unchanged)

### 2. IR Model Changes
- [ ] Add `FilterGroupTypeNames: ImmutableArray<string>` to `AppModel` record
- [ ] Add `GroupTypeHierarchy: ImmutableArray<string>` to `RouteDefinition` record
- [ ] Update record constructors and `with` expressions

### 3. DSL Interpreter Changes
- [ ] Modify `DispatchDiscoverEndpoints` to extract type arguments from `typeof()` expressions
- [ ] Store fully qualified type names in `IrAppBuilder.DiscoverEndpointGroupTypes`
- [ ] Handle both no-args and params overloads

### 4. Endpoint Extractor Changes
- [ ] Refactor `ExtractGroupPrefix` → `ExtractGroupHierarchy`
- [ ] Return full type hierarchy for each endpoint
- [ ] Implement prefix stripping logic based on matched filter type
- [ ] Exclude endpoints that don't match any filter type

### 5. Generator Model Changes
- [ ] Modify `FilterEndpointsForApp` to handle group type filtering
- [ ] Implement type matching with case-insensitive comparison
- [ ] Ensure filtered endpoints have correct stripped prefixes

### 6. Testing
- [ ] Create `generator-XX-group-filtering.cs` test file
- [ ] Test: Filter by single type includes descendants
- [ ] Test: Filter by multiple types uses OR logic
- [ ] Test: Ungrouped endpoints excluded when filter active
- [ ] Test: Full hierarchy preserved when no filter
- [ ] Test: Case-insensitive type matching
- [ ] Test: Empty filter result produces valid but empty CLI

### 7. Sample Implementation
- [ ] Create `samples/editions/01-group-filtering/` structure
- [ ] Create shared group base classes
- [ ] Create multiple `.csproj` files demonstrating editions
- [ ] Create shared `Program.cs` entry point
- [ ] Add README explaining the pattern

### 8. Documentation
- [ ] Create `docs/advanced/subset-publishing.md`
- [ ] Document use case and motivation
- [ ] Provide API reference
- [ ] Explain prefix stripping behavior
- [ ] Show MSBuild multi-project setup
- [ ] Add best practices section

## Notes

### Design Decisions

**Type-based vs String-based**: Chose type-based (`typeof(KanbanGroupBase)`) over string-based (`"kanban"`) for:
- Type safety and refactoring support
- No risk of typos
- Clear compile-time errors

**Prefix Stripping**: When filtering by `typeof(KanbanGroupBase)`:
```
Before: ganda kanban sub add
After:  kanban sub add   ("ganda" stripped)
```

**Multiple Editions Strategy**: Each edition is a separate `.csproj`:
- `kanban.csproj`: `<ProjectReference Include="..\shared\shared.csproj" />` + `Program.cs` with filter
- `git.csproj`: Same shared code, different filter
- `ganda.csproj`: No filter (full edition)

### Architecture Impact

Changes span:
1. `source/timewarp-nuru/builders/` - Runtime API
2. `source/timewarp-nuru-analyzers/generators/models/` - IR models
3. `source/timewarp-nuru-analyzers/generators/interpreter/` - DSL interpreter
4. `source/timewarp-nuru-analyzers/generators/extractors/` - Endpoint extraction
5. `source/timewarp-nuru-analyzers/generators/` - Generator logic

### Edge Cases

1. **Filter type has no descendants**: CLI will have no commands
2. **Partial overlap**: `groups: [typeof(TopGroup), typeof(SubGroup)]` - SubGroup commands included once
3. **Deep nesting**: `A:B:C:D` filtered by `B` → `B C D` (A stripped)
4. **Multiple inheritance**: Not supported in C#, so not a concern

### Related Files

- `source/timewarp-nuru/builders/nuru-app-builder/nuru-app-builder.routes.cs`
- `source/timewarp-nuru-analyzers/generators/nuru-generator.cs`
- `source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs`
- `source/timewarp-nuru-analyzers/generators/ir-builders/ir-app-builder.cs`
- `source/timewarp-nuru-analyzers/generators/models/app-model.cs`
- `source/timewarp-nuru-analyzers/generators/models/route-definition.cs`
- `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs`

### Future Enhancements (Out of Scope)

- `ExcludeGroups()` for negative filtering
- `WithPrefixOverride(string)` to customize root command name
- `DiscoverEndpoints(predicate)` for complex filtering logic
