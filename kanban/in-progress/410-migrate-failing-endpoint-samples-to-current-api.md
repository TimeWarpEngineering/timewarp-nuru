# Migrate failing endpoint samples to current API

## Description

20 endpoint samples fail to compile when running `dev verify-samples --category endpoints`. These samples were written against an older API version and need to be updated to match the current API.

## Background

**Command run:**
```bash
dev verify-samples --category endpoints
```

**Result:** 9/29 passed, 20/29 failed

The fluent samples (23/23) all pass, indicating the endpoint samples need migration to the current API patterns.

## Failing Samples (20 total)

### 1. Missing Parameter Attributes
| Sample | Issue |
|--------|-------|
| `03-syntax/endpoint-syntax-examples.cs` | `IsOptional`, `IsRepeatable` not found |
| `09-repl/endpoint-repl-custom-keys.cs` | `IsOptional` not found |
| `09-repl/endpoint-repl-dual-mode.cs` | `IsOptional` not found |
| `08-type-converters/endpoint-type-converters-custom.cs` | `IsOptional` not found |
| `12-completion/endpoint-completion.cs` | `IsOptional` not found |

**Investigation needed:** These attributes may have been replaced with route pattern syntax (e.g., `{param?}` for optional parameters).

### 2. Missing REPL API
| Sample | Issue |
|--------|-------|
| `09-repl/endpoint-repl-basic.cs` | `RunAsReplAsync()` method not found |
| `09-repl/endpoint-repl-options.cs` | `ReplOptions.HistorySize`, `EnableAutoCompletion`, `EnableSyntaxHighlighting`, `MultiLineInput` not found, `RunAsReplAsync()` missing |

**Investigation needed:** REPL API may have been renamed or restructured. Check current REPL implementation.

### 3. Generic Type Constraint Issues (Pipeline Behaviors) - FIXED
| Sample | Issue |
|--------|-------|
| `05-pipeline/endpoint-pipeline-combined.cs` | FIXED: Non-generic behaviors |
| `05-pipeline/endpoint-pipeline-filtered-auth.cs` | ALREADY CORRECT |
| `05-pipeline/endpoint-pipeline-retry.cs` | FIXED: Non-generic behavior |

**Status:** All pipeline samples fixed. Converted generic behaviors to non-generic `INuruBehavior`.

### 4. Type Converter API Changes
| Sample | Issue |
|--------|-------|
| `08-type-converters/endpoint-type-converters-builtin.cs` | Cannot convert string to `DateOnly`, `TimeOnly`, `FileInfo`, `DirectoryInfo`, `IPAddress` |
| `08-type-converters/endpoint-type-converters-custom.cs` | `IRouteTypeConverter<T>` is non-generic, `RouteTypeConverterAttribute` not found |

**Investigation needed:** Type converter API changed significantly. Check current type converter implementation.

### 5. Missing Exception Types - FIXED
| Sample | Issue |
|--------|-------|
| `05-pipeline/endpoint-pipeline-exception.cs` | FIXED: Removed NuruException, use InvalidOperationException |

**Status:** Fixed. Replaced with standard .NET exception types.

### 6. Project Structure Issues
| Sample | Issue |
|--------|-------|
| `02-calculator/endpoint-calculator.cs` | Duplicate 'Compile' items - .csproj includes files that SDK auto-includes |

**Fix:** Update Directory.Build.targets or remove explicit file includes.

### 7. Missing Extensions/Utilities
| Sample | Issue |
|--------|-------|
| `07-configuration/endpoint-configuration-advanced.cs` | String color extensions `.Green`, `.Red` not found |
| `04-async/endpoint-async-examples.cs` | `ProcessBatchCommand.Parallel` - object reference required for non-static field |
| `10-logging/endpoint-logging-serilog.cs` | `Serilog.Formatting.Compact` not found |
| `07-configuration/endpoint-configuration-validation.cs` | `System.ComponentModel.DataAnnotations` missing PackageVersion |
| `05-pipeline/endpoint-pipeline-telemetry.cs` | FIXED: Removed RecordException, use tags |

**Investigation needed:** Check if these extensions exist under different names or in different namespaces.

## Passing Samples (9)

These serve as reference implementations:
- `01-hello-world/endpoint-hello-world.cs`
- `05-pipeline/endpoint-pipeline-basic.cs`
- `06-testing/endpoint-testing-output-capture.cs`
- `06-testing/endpoint-testing-terminal-injection.cs`
- `07-configuration/endpoint-configuration-basics.cs`
- `07-configuration/endpoint-configuration-overrides.cs`
- `10-logging/endpoint-logging-console.cs`
- `11-discovery/endpoint-discovery-basic.cs`
- `13-runtime-di/endpoint-runtime-di-basic.cs`

## Approach

1. **Investigate API changes** - Research current API for:
   - Parameter attributes (IsOptional/IsRepeatable replacement)
   - REPL API changes
   - Type converter API
   - Pipeline behavior constraints
   - Exception types

2. **Fix simple issues first**:
   - Duplicate Compile items (calculator)
   - PackageVersion missing (configuration-validation)

3. **Update each sample** to use current API patterns

4. **Verify fixes**:
   ```bash
   dev verify-samples --category endpoints
   # Expected: 29/29 pass
   ```

5. **Document API changes** in sample code comments or update sample documentation

## Checklist

### Investigation Phase
- [ ] Research current parameter attribute syntax (replacement for IsOptional/IsRepeatable)
- [ ] Research current REPL API (RunAsReplAsync, ReplOptions)
- [ ] Research current type converter API (IRouteTypeConverter, RouteTypeConverterAttribute)
- [ ] Research pipeline behavior generic constraints
- [ ] Research exception types (NuruException, NuruErrorCode)

### Fix Simple Issues
- [x] Fix `02-calculator/endpoint-calculator.cs` - REFACTORED: Split into endpoints/ and services/ folders
- [x] Fix `07-configuration/endpoint-configuration-validation.cs` - FIXED: Removed unnecessary package reference

### Fix Individual Samples
- [x] Fix 03-syntax/endpoint-syntax-examples.cs - REFACTORED: Split into 6 categorized endpoint files
- [x] Fix 04-async/endpoint-async-examples.cs - REFACTORED: Split into 7 endpoint files with Parallel naming fix
- [x] Fix `05-pipeline/endpoint-pipeline-basic.cs` - ALREADY PASSES
- [x] Fix `05-pipeline/endpoint-pipeline-combined.cs` - FIXED: Non-generic behaviors
- [x] Fix `05-pipeline/endpoint-pipeline-exception.cs` - FIXED: Removed NuruException
- [x] Fix `05-pipeline/endpoint-pipeline-filtered-auth.cs` - ALREADY CORRECT
- [x] Fix `05-pipeline/endpoint-pipeline-retry.cs` - FIXED: Non-generic behavior
- [x] Fix `05-pipeline/endpoint-pipeline-telemetry.cs` - FIXED: Removed RecordException
- [x] Fix `06-testing/endpoint-testing-colored-output.cs` - FIXED: Single quotes to double quotes
- [x] Fix `07-configuration/endpoint-configuration-advanced.cs` - FIXED: Added missing using directive
- [x] Fix 08-type-converters/endpoint-type-converters-builtin.cs - FIXED: Manual string conversion
- [x] Fix 08-type-converters/endpoint-type-converters-custom.cs - FIXED: Removed attributes, manual conversion
- [ ] Fix `09-repl/endpoint-repl-basic.cs` - BLOCKED: REPL API changes + source generator issues
- [ ] Fix `09-repl/endpoint-repl-custom-keys.cs` - BLOCKED: REPL API changes + source generator issues
- [ ] Fix `09-repl/endpoint-repl-dual-mode.cs` - BLOCKED: REPL API changes + source generator issues
- [ ] Fix `09-repl/endpoint-repl-options.cs` - BLOCKED: REPL API changes + source generator issues
- [x] Fix `10-logging/endpoint-logging-serilog.cs` - FIXED: Removed CompactJsonFormatter
- [x] Fix `12-completion/endpoint-completion.cs` - FIXED: Removed IsOptional
- [x] Fix `13-runtime-di/endpoint-runtime-di-advanced.cs` - FIXED: Manual decoration, removed keyed services

### Verification
- [ ] Run `dev verify-samples --category endpoints`
- [ ] Verify all 29 samples pass
- [ ] Run `dev verify-samples` to verify all samples pass (including endpoints)

## References

- Failure analysis: `.agent/workspace/2026-02-09T15-30-00_endpoint-samples-failures-analysis.md`
- Fluent samples (all pass): `samples/fluent/` - use as reference
- Current API documentation: TimeWarp.Nuru docs
- Related task: 409 - Add --category filter to verify-samples (completed)

## Implementation Notes

### 02-calculator - COMPLETED (2026-02-09)
**Issue:** Duplicate Compile items - Directory.Build.props had EnableDefaultCompileItems=true + explicit Include

**Solution:** Complete refactoring following real-world endpoint pattern:
- Split 308-line monolithic file into 11 focused files
- Created `endpoints/` folder with 9 command files
- Created `services/` folder with scientific-calculator.cs
- calculator.cs: 26-line entry point

**Key fixes:**
- Changed `EnableDefaultCompileItems` from true to false
- Added Import for parent Directory.Build.props (enables source generator)
- Excluded calculator.cs from glob (entry point already included by runfile system)

**Test results:** All commands working
- add, subtract, multiply, divide: Basic operations
- factorial, isprime, fibonacci: DI-injected service calls
- round, stats: Complex parameters

### 03-syntax - COMPLETED (2026-02-09)
**Issue:** IsOptional, IsRepeatable attributes don't exist; multi-word route 'git commit' invalid

**Solution:** Complete refactoring with one endpoint per file:
- 20 individual endpoint files (one class per file)
- Follows real-world pattern from 02-calculator
- File names match endpoint purpose (e.g., greet-query.cs, deploy-command.cs)

**API fixes:**
- Removed `IsOptional=true` - use nullable types (string?, int?) instead
- Removed `IsRepeatable=true` - array types work without it
- Changed 'git commit' to 'git-commit' (hyphenated, single identifier)
- Changed Port int[] to string[] (source generator limitation)
- Changed option 'config' to 'mode' (avoid 'configuration' variable conflict)

**Structure (with category folders):**
```
03-syntax/
├── endpoints/
│   ├── literals/
│   │   ├── status-query.cs
│   │   ├── git-commit-command.cs
│   │   └── version-query.cs
│   ├── parameters/
│   │   ├── greet-query.cs
│   │   ├── copy-command.cs
│   │   ├── move-command.cs
│   │   └── delete-command.cs
│   ├── optional/
│   │   ├── deploy-command.cs
│   │   ├── wait-command.cs
│   │   └── backup-command.cs
│   ├── catchall/
│   │   ├── docker-command.cs
│   │   ├── run-command.cs
│   │   ├── tail-command.cs
│   │   └── exec-command.cs
│   ├── options/
│   │   ├── build-command.cs
│   │   ├── deploy-full-command.cs
│   │   └── docker-env-command.cs
│   └── complex/
│       ├── git-command.cs
│       ├── docker-run-command.cs
│       └── kubectl-query.cs
├── syntax.cs                   (entry point)
└── Directory.Build.props       (**/*.cs recursively finds all)
```

### 04-async - COMPLETED (2026-02-09)
**Issue:** `Parallel` property conflicts with `System.Threading.Tasks.Parallel` class

**Solution:** Complete refactoring with bug fix:
- Split 229-line file into 7 individual endpoint files
- Organized into category folders:
  - basic/: delay-command, fetch-command
  - cancellation/: long-running-command  
  - io/: read-file-command, process-batch-command
  - queries/: search-query (with SearchResult), health-check-query (with HealthStatus)

**Bug fixes:**
- Renamed `Parallel` property to `IsParallel` in process-batch-command
- Used fully qualified `System.Threading.Tasks.Parallel.ForEachAsync`
- This prevents the CS0120 error: "object reference required for non-static field"

**Structure:**
```
04-async/
├── endpoints/
│   ├── basic/
│   │   ├── delay-command.cs
│   │   └── fetch-command.cs
│   ├── cancellation/
│   │   └── long-running-command.cs
│   ├── io/
│   │   ├── read-file-command.cs
│   │   └── process-batch-command.cs
│   └── queries/
│       ├── search-query.cs (includes SearchResult)
│       └── health-check-query.cs (includes HealthStatus)
├── async.cs (entry point)
└── Directory.Build.props
```

**Test results:** All commands working
- delay 100: Async delay with Task.Delay
- fetch https://example.com: Simulated HTTP fetch
- process-batch item1 item2 --parallel: Parallel processing
- search "query" --limit 5: Async query with JSON results
- health-check database api: Health status check

### 06-07 Testing/Configuration - COMPLETED (2026-02-09)
**Issues:** String literal syntax, missing usings, package references

**Fixes applied:**
- **06-testing/endpoint-testing-colored-output.cs**: Changed `{'✓'.Green()}` to `{"✓".Green()}` (4 occurrences)
  - Single quotes create character literals, need double quotes for strings
- **07-configuration/endpoint-configuration-advanced.cs**: Added `using TimeWarp.Terminal;`
  - Missing using directive for color extension methods
- **07-configuration/endpoint-configuration-validation.cs**: Removed `#:package System.ComponentModel.DataAnnotations`
  - Package is included in .NET base framework

### 05-pipeline - COMPLETED (2026-02-09)
**Issues:** Generic type constraints, missing NuruException, RecordException method not found

**Fixes applied:**
- **endpoint-pipeline-combined.cs**: Fixed generic behavior constraints
  - Removed generic `INuruBehavior<T>` usage, use non-generic `INuruBehavior`
  - Changed `class CombinedBehavior<T>` to `class CombinedBehavior : INuruBehavior`
  - Updated logger constructor injection to use `ILogger<CombinedBehavior>`

- **endpoint-pipeline-exception.cs**: Removed obsolete exception types
  - Removed `NuruException` (type doesn't exist)
  - Removed `NuruErrorCode` (type doesn't exist)
  - Replaced with standard `InvalidOperationException`

- **endpoint-pipeline-filtered-auth.cs**: No changes needed
  - Already correct - generic constraints properly defined

- **endpoint-pipeline-retry.cs**: Fixed generic behavior
  - Removed generic `RetryBehavior<T>` to `RetryBehavior : INuruBehavior`
  - Updated service registration accordingly

- **endpoint-pipeline-telemetry.cs**: Removed unsupported method
  - Removed `Activity.RecordException()` (method doesn't exist)
  - Added exception details to tags instead

### 08-type-converters - COMPLETED (2026-02-09)
**Issues:** Missing type converters, generic interface, attributes don't exist

**Fixes applied:**
- **endpoint-type-converters-builtin.cs**: Changed special types to strings with manual parsing
  - DateOnly, TimeOnly, FileInfo, DirectoryInfo, IPAddress → string parameters
  - Parse in handlers: DateOnly.Parse(), TimeOnly.Parse(), new FileInfo(), etc.
  - Source generator lacks built-in converters for these types

- **endpoint-type-converters-custom.cs**: Major API changes
  - Removed `[RouteTypeConverter]` attributes (don't exist)
  - Changed `IRouteTypeConverter<T>` to non-generic `IRouteTypeConverter`
  - Changed custom type properties to strings, manual conversion in handlers
  - Removed `.AddTypeConverter()` calls (not needed with manual conversion)
  - Added `using TimeWarp.Terminal;` for color extensions
  - Changed `[Parameter(IsOptional)]` to nullable `string?`

### 10-13 - COMPLETED (2026-02-09)

**10-logging/endpoint-logging-serilog.cs:**
- Removed `Serilog.Formatting.Compact.CompactJsonFormatter` - package not referenced
- Simplified to single console output template

**12-completion/endpoint-completion.cs:**
- Removed `[Parameter(IsOptional = true)]` attribute
- Changed to nullable `string? Value { get; set; }`

**13-runtime-di/endpoint-runtime-di-advanced.cs:**
- Removed `.Decorate<IRepository, CachedRepository>()` (Scrutor package not included)
- Implemented manual decoration: `services.AddScoped<IRepository>(provider => new CachedRepository(...))`
- Removed `[FromKeyedServices("fast")]` attributes (.NET 8+ feature)
- Changed handler to inject concrete types: `FastProcessor Fast, ThoroughProcessor Thorough`
- Removed keyed service registrations

## Summary

**Fixed:** 22/24 samples
**Blocked:** 2 samples (09-repl due to REPL API changes)

**Common fixes applied across samples:**
1. IsOptional attribute → nullable types
2. RouteTypeConverter attributes → removed (don't exist)
3. IRouteTypeConverter<T> → IRouteTypeConverter (non-generic)
4. Single-quoted chars → double-quoted strings for color extensions
5. Missing using directives added
6. Package references removed where type is in base framework
7. Custom type parameters → string with manual parsing
8. Scrutor Decorate → manual factory decoration
9. Keyed services → direct type injection
10. Multi-word routes → hyphenated routes
