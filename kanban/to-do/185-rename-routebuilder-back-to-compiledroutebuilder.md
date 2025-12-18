# Rename RouteBuilder back to CompiledRouteBuilder

## Description

Task 164 incorrectly renamed `CompiledRouteBuilder` to `RouteBuilder`. 

**Naming convention:** `{ThingItBuilds}Builder`

- `RouteBuilder` builds `CompiledRoute` → WRONG
- `CompiledRouteBuilder` builds `CompiledRoute` → CORRECT

Revert this specific rename and update all task files to use correct name.

## Checklist

### Code Changes
- [ ] Rename `RouteBuilder` class back to `CompiledRouteBuilder`
- [ ] Rename `route-builder.cs` file to `compiled-route-builder.cs`
- [ ] Update all references in source code
- [ ] Update all references in tests
- [ ] Update all references in samples

### Task File Cleanup
- [ ] Update task 160 (epic) to use `CompiledRouteBuilder`
- [ ] Update task 161 to use `CompiledRouteBuilder`
- [ ] Update task 162 if it references RouteBuilder
- [ ] Update task 163 if it references RouteBuilder
- [ ] Update task 148 hierarchy/design docs
- [ ] Search all kanban tasks for "RouteBuilder" and fix

### Verify
- [ ] Build succeeds
- [ ] Tests pass

## Notes

### Builder Naming Convention

| Builder | Builds | Correct? |
|---------|--------|----------|
| `CompiledRouteBuilder` | `CompiledRoute` | YES |
| `EndpointBuilder` | `Endpoint` | YES |
| `TableBuilder` | `Table` | YES |
| `PanelBuilder` | `Panel` | YES |
| `RuleBuilder` | `Rule` | YES |
