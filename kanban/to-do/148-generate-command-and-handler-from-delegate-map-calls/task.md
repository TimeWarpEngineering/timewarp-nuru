# Generate Command and Handler from Delegate Map Calls

## Description

Extend the existing `NuruInvokerGenerator` (or create a sibling generator) to emit `IRequest` Command classes and `IRequestHandler<T>` Handler classes from delegate-based `Map()` calls. This unifies the execution model so that delegates become pure syntactic sugar — all routes flow through the same Command/Handler pipeline.

**3.0 is a breaking release** — no backward compatibility constraints. Design the optimal API and implementation.

## Goals

- **Unified execution model** — All routes become Command/Handler pairs
- **Remove `DelegateExecutor`** — Single execution path, no tech debt
- **AOT-first** — No reflection, no `DynamicInvoke`

## Supporting Documents

- `fluent-route-builder-design.md` — **Main design doc** with phased implementation plan
- `api-design.md` — Consumer-facing route registration API
- `source-gen-design.md` — Source generator implementation details

## Phased Implementation Plan

| Phase | Name | Description | Releasable? |
|-------|------|-------------|-------------|
| **0** | Foundation | `CompiledRouteBuilder` (internal) + tests | No |
| **1** | Attributed Routes | `[Route]`, `[RouteGroup]` → auto-registration | **Yes** |
| **2** | Delegate Generation | String pattern + delegate → Command/Handler gen | **Yes** |
| **3** | Unified Pipeline | Remove `DelegateExecutor`, single code path | **Yes** |
| **4** | Fluent Builder API | Public `CompiledRouteBuilder`, `MapGroup()` | **Yes** |
| **5** | Relaxed Constraints | Data flow analysis for `MapGroup()` | **Yes** |

## Checklist

### Phase 0: Foundation (NEXT)
- [ ] Create `CompiledRouteBuilder` class in `timewarp-nuru-parsing`
- [ ] Keep visibility `internal` (public in Phase 4)
- [ ] Add `[InternalsVisibleTo]` for test project
- [ ] Implement builder methods: `WithLiteral`, `WithParameter`, `WithOptionalParameter`, `WithOption`, `WithCatchAll`, `Build`
- [ ] Use same specificity constants as existing `Compiler`
- [ ] Write tests comparing `PatternParser.Parse()` results with builder `.Build()` results
- [ ] Validate builder produces identical `CompiledRoute` instances

### Phase 1: Attributed Routes
- [ ] Design and implement `[Route]`, `[RouteAlias]`, `[RouteGroup]`, `[Parameter]`, `[Option]`, `[GroupOption]` attributes
- [ ] Source generator reads attributes from Command classes
- [ ] Generator emits `CompiledRouteBuilder` calls for each attributed Command
- [ ] Auto-registration via `[ModuleInitializer]`

### Phase 2: Delegate Generation
- [ ] Extend `NuruInvokerGenerator` or create sibling generator
- [ ] Implement Command class generation from delegate signature
- [ ] Implement Handler class generation from delegate body
- [ ] Implement parameter rewriting (`x` → `command.X`)
- [ ] DI parameter detection (parameters not in route → constructor injection)
- [ ] Handle async delegates and return values

### Phase 3: Unified Pipeline
- [ ] Remove `DelegateExecutor`
- [ ] Refactor `Compiler` to use `CompiledRouteBuilder` internally
- [ ] Single code path for `CompiledRoute` construction

### Phase 4: Fluent Builder API
- [ ] Make `CompiledRouteBuilder` public
- [ ] Add `Map(Action<CompiledRouteBuilder>, Delegate)` overload
- [ ] Add `MapGroup()` API with fluent chain constraint
- [ ] Source generator walks fluent builder syntax tree

### Phase 5: Relaxed Constraints
- [ ] Data flow analysis within method scope for `MapGroup()` variable tracking
- [ ] Diagnostic warnings for unresolvable cases

### Final Cleanup
- [ ] Update all samples
- [ ] Update documentation

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
