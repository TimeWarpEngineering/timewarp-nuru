# Add MapGroup with WithGroupOptions support

## Status

**SUPERSEDED** by Task 153 (Implement Fluent Builder API - Phase 4)

Task 153 fully covers MapGroup functionality as part of the unified fluent builder design, including:
- `IRouteGroupBuilder` interface
- `MapGroup()`, `WithDescription()`, `WithGroupOptions()`
- Nested groups with prefix and options accumulation
- Help generation for group structure
- Shell completion for group options
- Source generator support with fluent chain constraint

See: `kanban/to-do/153-implement-fluent-builder-api-phase-4.md`

---

## Original Description

Add ASP.NET Minimal API-style `MapGroup()` to Nuru, enabling hierarchical command organization with shared options that propagate to child routes. This provides Docker-style "global options" scoped to command groups.

## Results

- Superseded during design phase
- Concepts incorporated into Phase 4 of the unified fluent builder design
- No implementation work was done on this task
