# Rename RouteBuilder to CompiledRouteBuilder

## Description

Task 164 renamed `CompiledRouteBuilder` to `RouteBuilder`, but this violates naming convention.

**Naming convention:** `{ThingItBuilds}Builder`

- `RouteBuilder` builds `CompiledRoute` → WRONG (no class called `Route`)
- `CompiledRouteBuilder` builds `CompiledRoute` → CORRECT

Rename to follow convention and update all task files to use correct name.

## Checklist

### Code Changes
- [x] Rename `RouteBuilder` class back to `CompiledRouteBuilder`
- [x] Rename `route-builder.cs` file to `compiled-route-builder.cs`
- [x] Update all references in source code
- [x] Update all references in tests
- [x] Update all references in samples

### Task File Cleanup
- [x] Update task 160 (epic) to use `CompiledRouteBuilder`
- [x] Update task 161 to use `CompiledRouteBuilder`
- [x] Update task 162 if it references RouteBuilder (N/A - no references)
- [x] Update task 163 if it references RouteBuilder (N/A - no references)
- [x] Update task 148 hierarchy/design docs
- [x] Search all kanban tasks for "RouteBuilder" and fix

### Verify
- [x] Build succeeds
- [x] Tests pass

## Notes

### Builder Naming Convention

| Builder | Builds | Correct? |
|---------|--------|----------|
| `CompiledRouteBuilder` | `CompiledRoute` | YES |
| `EndpointBuilder` | `Endpoint` | YES |
| `TableBuilder` | `Table` | YES |
| `PanelBuilder` | `Panel` | YES |
| `RuleBuilder` | `Rule` | YES |
