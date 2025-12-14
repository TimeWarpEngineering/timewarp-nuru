# Generate Command and Handler from Delegate Map Calls

## Description

Extend the existing `NuruInvokerGenerator` (or create a sibling generator) to emit `IRequest` Command classes and `IRequestHandler<T>` Handler classes from delegate-based `Map()` calls. This unifies the execution model so that delegates become pure syntactic sugar — all routes flow through the same Command/Handler pipeline.

**3.0 is a breaking release** — no backward compatibility constraints. Design the optimal API and implementation.

## Goals

- **Unified execution model** — All routes become Command/Handler pairs
- **Remove `DelegateExecutor`** — Single execution path, no tech debt
- **AOT-first** — No reflection, no `DynamicInvoke`

## Supporting Documents

- `api-design.md` — Consumer-facing route registration API
- `source-gen-design.md` — Source generator implementation details

## Checklist

### API Design
- [ ] Finalize `Map()` API variants
- [ ] Finalize `MapMultiple()` API (consider rename to `MapAliases`?)
- [ ] Finalize `MapGroup()` fluent API and constraints
- [ ] Decide on return value semantics (exit codes, output)
- [ ] Decide on DI parameter injection in delegates

### Source Generator
- [ ] Extend `NuruInvokerGenerator` or create sibling generator
- [ ] Implement Command class generation from pattern
- [ ] Implement Handler class generation from delegate body
- [ ] Implement parameter rewriting (`x` → `request.X`)
- [ ] Handle `MapMultiple` array parsing
- [ ] Handle `MapGroup` fluent chain parsing
- [ ] Handle `MapGroup` variable tracking (same method scope)
- [ ] Handle async delegates
- [ ] Handle return values (`int`, `Task<T>`)
- [ ] Detect and handle closures
- [ ] Generate DI registration (module initializer)

### Cleanup
- [ ] Remove `DelegateExecutor`
- [ ] Remove old invoker-only generation (if superseded)
- [ ] Update all samples
- [ ] Update documentation

### Testing
- [ ] Unit tests for generated Command classes
- [ ] Unit tests for generated Handler classes
- [ ] Integration tests for full pipeline
- [ ] AOT compatibility verification

## Notes

### Existing Infrastructure

`NuruInvokerGenerator` already:
- Finds `Map()` and `MapDefault()` calls
- Extracts pattern strings
- Extracts delegate signatures (parameters, types, return)
- Detects async

This is 80% of what we need — extend to also emit Command/Handler classes.

### Key Decisions

1. **MapGroup API constraint** — Require fluent chains to enable codegen without complex data flow analysis
2. **Closure handling** — Emit diagnostic error or generate delegate-wrapping handler
3. **Naming convention** — `AddCommand` vs `Add_Generated_Command`

### Dependencies

- `Mediator.Abstractions` for `IRequest`, `IRequestHandler<T>`, `Unit`
- NO dependency on `Mediator.SourceGenerator`
