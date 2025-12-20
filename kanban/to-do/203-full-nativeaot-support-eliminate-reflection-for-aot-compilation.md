# Full NativeAOT support - eliminate reflection for AOT compilation

## Description

Enable full NativeAOT compilation by eliminating all reflection-based code paths. This would eliminate JIT overhead entirely, reducing cold-start from ~11ms to ~400us (matching warm-run performance).

## Checklist

### Analysis
- [ ] Audit all `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]` attributes
- [ ] Identify all `Activator.CreateInstance` usages
- [ ] Identify all `Convert.ChangeType` usages
- [ ] Identify reflection-based parameter binding in `DelegateExecutor`

### DelegateExecutor (Parameter Binding)
- [ ] Source-generate parameter binders for delegate signatures
- [ ] Eliminate `method.GetParameters()` reflection
- [ ] Eliminate `param.ParameterType` reflection in `BindParameters`

### MediatorExecutor (Command Creation)
- [ ] Source-generate command factories
- [ ] Replace `Activator.CreateInstance(commandType)` with generated factories
- [ ] Source-generate property setters for command population

### Type Conversion
- [ ] Source-generate type converters for all supported types
- [ ] Replace `Convert.ChangeType` with generated converters
- [ ] Source-generate enum parsers

### JSON Serialization
- [ ] Create `JsonSerializerContext` for all response types
- [ ] Ensure all serializable types are included in source generation

### Validation
- [ ] Create AOT test project with `<PublishAot>true</PublishAot>`
- [ ] Verify no AOT warnings during publish
- [ ] Benchmark AOT build vs JIT build

## Notes

### Current AOT Blockers

| Component           | Current                    | Required for AOT                    |
| ------------------- | -------------------------- | ----------------------------------- |
| Delegate invocation | Generated invokers ✓       | Already fixed                       |
| Parameter binding   | Reflection                 | Source-generate parameter binders   |
| Command creation    | `Activator.CreateInstance` | Source-generate command factories   |
| Type conversion     | `Convert.ChangeType`       | Source-generate type converters     |
| JSON serialization  | Dynamic                    | `JsonSerializerContext` for all types |

### Files with AOT Warnings

- `source/timewarp-nuru-core/execution/delegate-executor.cs` (lines 16-17)
- `source/timewarp-nuru-core/execution/mediator-executor.cs` (lines 26-27)
- `source/timewarp-nuru-core/io/response-display.cs` (lines 18-19)
- `source/timewarp-nuru-core/type-conversion/default-type-converters.cs`

### Expected Performance Improvement

- **Current cold-start:** ~11 ms (Empty builder)
- **Current warm-run:** ~400 us
- **Expected AOT cold-start:** ~400 us (no JIT needed)

### Reference

See performance analysis report: `.agent/workspace/2024-12-20T03-30-00_runasync-performance-analysis.md`
