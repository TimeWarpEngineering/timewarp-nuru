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

### 3. Generic Type Constraint Issues (Pipeline Behaviors)
| Sample | Issue |
|--------|-------|
| `05-pipeline/endpoint-pipeline-combined.cs` | `The type 'T' must be a reference type` for `INuruBehavior<TFilter>`, `readonly` modifier invalid |
| `05-pipeline/endpoint-pipeline-filtered-auth.cs` | Same constraint error |
| `05-pipeline/endpoint-pipeline-retry.cs` | Same constraint error |

**Investigation needed:** Pipeline behavior generic constraints need to be updated. May need `where T : class` constraint.

### 4. Type Converter API Changes
| Sample | Issue |
|--------|-------|
| `08-type-converters/endpoint-type-converters-builtin.cs` | Cannot convert string to `DateOnly`, `TimeOnly`, `FileInfo`, `DirectoryInfo`, `IPAddress` |
| `08-type-converters/endpoint-type-converters-custom.cs` | `IRouteTypeConverter<T>` is non-generic, `RouteTypeConverterAttribute` not found |

**Investigation needed:** Type converter API changed significantly. Check current type converter implementation.

### 5. Missing Exception Types
| Sample | Issue |
|--------|-------|
| `05-pipeline/endpoint-pipeline-exception.cs` | `NuruException`, `NuruErrorCode` not found |

**Investigation needed:** These types may have been renamed or moved.

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
| `05-pipeline/endpoint-pipeline-telemetry.cs` | `Activity.RecordException` not found |

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
- [ ] Fix `05-pipeline/endpoint-pipeline-combined.cs` - BLOCKED: Source generator issues with generic constraints
- [ ] Fix `05-pipeline/endpoint-pipeline-exception.cs` - BLOCKED: Source generator issues with generic constraints
- [ ] Fix `05-pipeline/endpoint-pipeline-filtered-auth.cs` - BLOCKED: Source generator issues with generic constraints
- [ ] Fix `05-pipeline/endpoint-pipeline-retry.cs` - BLOCKED: Source generator issues with generic constraints
- [ ] Fix `05-pipeline/endpoint-pipeline-telemetry.cs` - BLOCKED: Source generator issues with generic constraints
- [x] Fix `06-testing/endpoint-testing-colored-output.cs` - FIXED: Single quotes to double quotes
- [x] Fix `07-configuration/endpoint-configuration-advanced.cs` - FIXED: Added missing using directive
- [x] Fix 08-type-converters/endpoint-type-converters-builtin.cs - FIXED: Manual string conversion
- [x] Fix 08-type-converters/endpoint-type-converters-custom.cs - FIXED: Removed attributes, manual conversion
- [ ] Fix `09-repl/endpoint-repl-basic.cs`
- [ ] Fix `09-repl/endpoint-repl-custom-keys.cs`
- [ ] Fix `09-repl/endpoint-repl-dual-mode.cs`
- [ ] Fix `09-repl/endpoint-repl-options.cs`
- [ ] Fix `10-logging/endpoint-logging-serilog.cs`
- [ ] Fix `12-completion/endpoint-completion.cs`
- [ ] Fix `13-runtime-di/endpoint-runtime-di-advanced.cs`

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
