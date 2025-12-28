# #293-006: Delete Dead Code

## Parent

#293 Make DSL Builder Methods No-Ops

## Dependencies

- #293-001, #293-002, #293-003, #293-004, #293-005 (all no-op conversions complete)

## Description

After converting all DSL builder methods to no-ops, significant runtime infrastructure becomes dead code. This task removes it to reduce binary size and simplify the codebase.

**IMPORTANT:** Only delete code after verifying it's not used by:
1. The source generator at compile time
2. Other runtime paths (REPL, help generation, etc.)

## Checklist

### Classes to Evaluate for Deletion

- [ ] `EndpointCollection` class (`source/timewarp-nuru-core/endpoints/endpoint-collection.cs`)
  - `Add()` is already no-op
  - Verify `Endpoints` property not read anywhere
  - Verify `Sort()`, `Count`, enumerator not used

- [ ] `Endpoint` class (`source/timewarp-nuru-core/endpoints/endpoint.cs`)
  - Was used to store route configuration
  - Verify not used by help generation or REPL

- [ ] `DefaultEndpointCollectionBuilder` class (`source/timewarp-nuru-core/endpoints/default-endpoint-collection-builder.cs`)
  - If `Map()` is no-op, whole class may be removable

### Private Helper Methods to Delete

In `nuru-core-app-builder.routes.cs`:
- [ ] `MapPatternTyped()` - line ~145-160
- [ ] `MapInternalTyped()` - line ~165-190
- [ ] `MapMediatorTyped()` - line ~195-215
- [ ] `MapNestedTyped()` - line ~220-235

### Fields/Properties to Remove

- [ ] `TypeConverterRegistry` field (if `AddTypeConverter` is no-op)
- [ ] `EndpointCollection` property (if class deleted)
- [ ] Constructor parameters that were only used for deleted functionality

### Temporary Backward-Compatible Constructors

Remove any temporary constructors added for incremental work:
- [ ] `EndpointBuilder(TBuilder, Endpoint?)` - remove, keep only `EndpointBuilder(TBuilder)`
- [ ] `GroupEndpointBuilder(GroupBuilder<T>, Endpoint?)` - similar
- [ ] `GroupBuilder(TParent, string, Action<Endpoint>, ILoggerFactory?)` - similar

### Verify Before Deleting

For each item, verify:
1. Not referenced by source generator (check `timewarp-nuru-analyzers`)
2. Not referenced by tests (update tests if needed)
3. Not referenced by samples (update samples if needed)
4. Not referenced by REPL (moved to reference-only in #293-007)

## Process

1. Run `grep -r "ClassName" source/` to find all usages
2. Run `grep -r "ClassName" tests/` to find test dependencies
3. If only references are in files being deleted, safe to remove
4. If references exist elsewhere, investigate before deleting

## Notes

- This is the riskiest task in the epic - be careful
- Prefer commenting out first, then deleting after tests pass
- Keep `PatternParser` and parsing infrastructure - used by source generator
- Keep `CompiledRoute`, matchers, etc. - used by generated code at runtime
- Consider keeping `Endpoint` if help system needs it (investigate first)
