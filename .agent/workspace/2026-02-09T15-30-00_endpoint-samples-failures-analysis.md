# Endpoint Samples Compilation Failures Analysis

**Date:** 2026-02-09  
**Command:** `dev verify-samples --category endpoints`  
**Result:** 9/29 passed, 20/29 failed

## Summary of Failures by Category

### 1. **Missing API/Types** (8 samples)
Types that don't exist or were renamed in the current API:

| Sample | Missing Type/Feature | Error Count |
|--------|-------------------|-------------|
| endpoint-syntax-examples.cs | `IsOptional`, `IsRepeatable` attributes | 5 |
| endpoint-repl-custom-keys.cs | `IsOptional` attribute | 1 |
| endpoint-repl-dual-mode.cs | `IsOptional` attribute | 1 |
| endpoint-repl-options.cs | `ReplOptions.HistorySize`, `EnableAutoCompletion`, `EnableSyntaxHighlighting`, `MultiLineInput`, `RunAsReplAsync` | 5 |
| endpoint-repl-basic.cs | `RunAsReplAsync` method | 1 |
| endpoint-pipeline-exception.cs | `NuruException`, `NuruErrorCode` | 4 |
| endpoint-completion.cs | `IsOptional` attribute | 1 |
| endpoint-runtime-di-advanced.cs | `Decorate` extension method | 2 |

### 2. **Generic Type Constraint Issues** (3 samples)
Pipeline behaviors with generic constraints:

| Sample | Error |
|--------|-------|
| endpoint-pipeline-combined.cs | `The type 'T' must be a reference type` for `INuruBehavior<TFilter>` |
| endpoint-pipeline-filtered-auth.cs | Same constraint error + `readonly` modifier invalid |
| endpoint-pipeline-retry.cs | Same constraint error + `readonly` modifier invalid |

### 3. **Type Converter Issues** (2 samples)

| Sample | Error |
|--------|-------|
| endpoint-type-converters-builtin.cs | Cannot convert string to `DateOnly`, `TimeOnly`, `FileInfo`, `DirectoryInfo`, `IPAddress` |
| endpoint-type-converters-custom.cs | `IRouteTypeConverter<T>` is non-generic, `RouteTypeConverterAttribute` not found, `IsOptional` not found |

### 4. **Missing Package References** (2 samples)

| Sample | Error |
|--------|-------|
| endpoint-configuration-validation.cs | `System.ComponentModel.DataAnnotations` missing PackageVersion |
| endpoint-logging-serilog.cs | `Serilog.Formatting.Compact` not found |

### 5. **Project Structure Issues** (2 samples)

| Sample | Error |
|--------|-------|
| endpoint-calculator.cs | Duplicate 'Compile' items - .csproj includes files that SDK auto-includes |
| endpoint-configuration-advanced.cs | String extensions `.Green`, `.Red` not found |

### 6. **Other Issues** (2 samples)

| Sample | Error |
|--------|-------|
| endpoint-async-examples.cs | `ProcessBatchCommand.Parallel` - object reference required for non-static field |
| endpoint-testing-colored-output.cs | `CS1012: Too many characters in character literal` |
| endpoint-pipeline-telemetry.cs | `Activity.RecordException` not found |
| endpoint-syntax-examples.cs | Invalid route pattern 'git commit' - must be single identifier |

## Complete Failed Samples List

1. ❌ `samples/endpoints/02-calculator/endpoint-calculator.cs` - NETSDK1022: Duplicate Compile items
2. ❌ `samples/endpoints/03-syntax/endpoint-syntax-examples.cs` - Multiple: IsOptional, IsRepeatable, invalid route pattern
3. ❌ `samples/endpoints/04-async/endpoint-async-examples.cs` - CS0120: Static field reference
4. ❌ `samples/endpoints/05-pipeline/endpoint-pipeline-combined.cs` - Generic constraints + readonly errors
5. ❌ `samples/endpoints/05-pipeline/endpoint-pipeline-exception.cs` - NuruException, NuruErrorCode not found
6. ❌ `samples/endpoints/05-pipeline/endpoint-pipeline-filtered-auth.cs` - Generic constraints
7. ❌ `samples/endpoints/05-pipeline/endpoint-pipeline-retry.cs` - Generic constraints
8. ❌ `samples/endpoints/05-pipeline/endpoint-pipeline-telemetry.cs` - Activity.RecordException
9. ❌ `samples/endpoints/06-testing/endpoint-testing-colored-output.cs` - Character literal too long
10. ❌ `samples/endpoints/07-configuration/endpoint-configuration-advanced.cs` - String color extensions
11. ❌ `samples/endpoints/07-configuration/endpoint-configuration-validation.cs` - PackageVersion missing
12. ❌ `samples/endpoints/08-type-converters/endpoint-type-converters-builtin.cs` - String->Type conversions
13. ❌ `samples/endpoints/08-type-converters/endpoint-type-converters-custom.cs` - IRouteTypeConverter generic
14. ❌ `samples/endpoints/09-repl/endpoint-repl-basic.cs` - RunAsReplAsync not found
15. ❌ `samples/endpoints/09-repl/endpoint-repl-custom-keys.cs` - IsOptional not found
16. ❌ `samples/endpoints/09-repl/endpoint-repl-dual-mode.cs` - IsOptional not found
17. ❌ `samples/endpoints/09-repl/endpoint-repl-options.cs` - ReplOptions properties, RunAsReplAsync
18. ❌ `samples/endpoints/10-logging/endpoint-logging-serilog.cs` - Serilog.Formatting.Compact
19. ❌ `samples/endpoints/12-completion/endpoint-completion.cs` - IsOptional not found
20. ❌ `samples/endpoints/13-runtime-di/endpoint-runtime-di-advanced.cs` - Decorate extension

## Passed Samples (9)

1. ✅ `samples/endpoints/01-hello-world/endpoint-hello-world.cs`
2. ✅ `samples/endpoints/05-pipeline/endpoint-pipeline-basic.cs`
3. ✅ `samples/endpoints/06-testing/endpoint-testing-output-capture.cs`
4. ✅ `samples/endpoints/06-testing/endpoint-testing-terminal-injection.cs`
5. ✅ `samples/endpoints/07-configuration/endpoint-configuration-basics.cs`
6. ✅ `samples/endpoints/07-configuration/endpoint-configuration-overrides.cs`
7. ✅ `samples/endpoints/10-logging/endpoint-logging-console.cs` (with warning)
8. ✅ `samples/endpoints/11-discovery/endpoint-discovery-basic.cs`
9. ✅ `samples/endpoints/13-runtime-di/endpoint-runtime-di-basic.cs`

## Recommendations

### High Priority
1. **Fix missing API types**: `IsOptional`, `IsRepeatable`, `RunAsReplAsync`
2. **Fix generic constraint issues** in pipeline behaviors
3. **Fix or remove** endpoint-calculator.cs project file issue

### Medium Priority
4. **Update type converters** - IRouteTypeConverter appears to be non-generic now
5. **Fix package references** for Serilog and DataAnnotations
6. **Fix NuruException/NuruErrorCode** - may have been renamed

### Lower Priority
7. Fix string color extensions
8. Fix REPL options API
9. Fix Activity.RecordException (needs newer OpenTelemetry?)

## Pattern Analysis

Most failures indicate the **endpoint samples were written against a different API version** than what's currently in the codebase. Key discrepancies:

- Parameter attributes (`IsOptional`, `IsRepeatable`) don't exist
- REPL API has changed significantly
- Type converter API changed from generic to non-generic
- Some extension methods removed (Decorate, string colors)
- Some types renamed or removed (NuruException, NuruErrorCode)

These samples likely need to be **updated to match current API** or **marked as outdated**.
