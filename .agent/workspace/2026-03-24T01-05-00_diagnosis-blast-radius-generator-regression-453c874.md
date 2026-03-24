# Symptom

`dev verify-samples` fails broadly after commit `453c874` with:

- `CS0103: The name 'EnsureServicesInitialized' does not exist in the current context`
- `CS1061: 'Lazy<TBehavior>' does not contain a definition for 'HandleAsync'`

# Blast Radius Summary

Scanned generated files: `artifacts/generated/**/NuruGenerated.g.cs` (58 files)

| Category | Count | % |
|---|---:|---:|
| A-only (`EnsureServicesInitialized` call without method definition) | 40 | 69% |
| B-only (`Lazy<T>.HandleAsync`) | 0 | 0% |
| A+B | 14 | 24% |
| Unaffected | 4 | 7% |

Total affected: **54 / 58 (93%)**

# Root Cause Categories

## A) Missing `EnsureServicesInitialized` definition

- Generated dispatch emits unconditional call: `EnsureServicesInitialized(app, configuration)`.
- In affected files, no `private static void EnsureServicesInitialized(...)` is emitted.

Representative affected artifacts:

- `artifacts/generated/endpoint-hello-world/.../NuruGenerated.g.cs`
- `artifacts/generated/async/.../NuruGenerated.g.cs`
- `artifacts/generated/ganda/.../NuruGenerated.g.cs`
- `artifacts/generated/fluent-hello-world-lambda/.../NuruGenerated.g.cs`

## B) `Lazy<TBehavior>.HandleAsync` misuse

- Behavior fields are generated as `Lazy<TBehavior>`.
- Pipeline invokes `__behavior_...HandleAsync(...)` directly on the `Lazy<T>`, not on its wrapped value.

Representative affected artifacts:

- `artifacts/generated/nuru-client/.../NuruGenerated.g.cs`
- `artifacts/generated/combined/.../NuruGenerated.g.cs`
- `artifacts/generated/fluent-pipeline-basic/.../NuruGenerated.g.cs`
- `artifacts/generated/hybrid-unified-pipeline/.../NuruGenerated.g.cs`

# Category Breakdown

## A-only (40)

Examples include:

`advanced`, `async`, `basic`, `basics`, `builtin`, `colored-output`, `completion`, `console`, `custom`, `custom-keys`, `dual-mode`, `endpoint-discovery-basic`, `endpoint-hello-world`, `endpoint-httpclient-openmeteo`, `fluent-async-examples`, `fluent-calculator-delegate`, `fluent-completion`, `fluent-configuration-basics`, `fluent-configuration-overrides`, `fluent-configuration-validation`, `fluent-hello-world-lambda`, `fluent-hello-world-method`, `fluent-repl-basic`, `fluent-runtime-di`, `fluent-syntax-examples`, `fluent-testing-output-capture`, `fluent-type-converters-custom`, `ganda`, `git`, `group-options`, `hybrid-migration-start-fluent`, `kanban`, `options`, `output-capture`, `overrides`, `structured-logging`, `syntax`, `terminal-injection`, `test-filter`, `validation`.

## A+B (14)

`combined`, `exception`, `filtered-auth`, `fluent-logging-console`, `fluent-pipeline-basic`, `fluent-pipeline-combined`, `fluent-pipeline-exception`, `fluent-pipeline-filtered-auth`, `fluent-pipeline-retry`, `fluent-pipeline-telemetry`, `hybrid-unified-pipeline`, `nuru-client`, `retry`, `telemetry`.

## Unaffected (4)

- `calculator`
- `dev`
- `hybrid-migration-add-endpoint`
- `hybrid-migration-complete`

Observed commonality: unaffected files include generated service fields (e.g., `__svc_*`), so method emission path is present; they also do not hit behavior-lazy invocation mismatch.

# Evidence Chain

1. CI and local reproduction both fail in `verify-samples` with the two errors above.
2. Generated files show unconditional call sites for `EnsureServicesInitialized` and no matching method in most outputs.
3. Generated behavior fields are `Lazy<T>` while pipeline code calls `.HandleAsync` on the lazy field.
4. Blame analysis attributes relevant emitter lines to commit `453c874` (with line ownership concentrated in `interceptor-emitter.cs` call/emission regions and `behavior-emitter.cs` callsite edits).

# Affected Scope

- Most sample runfiles and endpoint/fluent/hybrid examples fail to compile.
- CI workflow fails at sample verification stage.
- Runtime/interceptor path reliability is compromised for nearly all generated apps.
