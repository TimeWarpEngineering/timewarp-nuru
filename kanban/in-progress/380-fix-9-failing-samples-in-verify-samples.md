# Fix 9 failing samples in verify-samples

## Description

Nine samples are failing during the verify-samples command. Fix these issues to unblock CI.

## Failing Samples

### Missing project references (need migration - DO NOT DELETE):
1. `samples/_aspire-telemetry/aspire-telemetry.cs` - references missing `timewarp-nuru-telemetry` project
2. `samples/_dynamic-completion-example/dynamic-completion-example.cs` - references missing `timewarp-nuru-completion` project
3. `samples/_shell-completion-example/shell-completion-example.cs` - references missing `timewarp-nuru-completion` project

**Note:** These underscore-prefixed samples should be EXCLUDED from verify-samples for now (not deleted). They need to be migrated later.

### Package version mismatch (NU1605):
4. `samples/09-configuration/01-configuration-basics.cs` - Microsoft.Extensions.Options.ConfigurationExtensions version conflict (10.0.1 vs 10.0.2)
5. `samples/09-configuration/02-command-line-overrides.cs` - Same package version conflict

### Route analyzer error (NURU_R003):
6. `samples/08-testing/runfile-test-harness/real-app.cs` - Route 'deploy {env}' marked as unreachable due to 'deploy {env} --dry-run' having higher specificity **(FIXED)**

### Route overlap errors (NURU_R003):
7. `samples/05-aot-example/aot-example.cs` - Three route overlap errors:
   - Line 61: Route `build --debug` unreachable (equal specificity to `build --release`)
   - Line 66: Route `build` unreachable (lower specificity than `build --release`)
   - Line 89: Route `{*args}` unreachable (lower specificity than `--help`)

### Type converter samples (REQUIRE TASK 383 - Source Generator Bug):
- `samples/10-type-converters/01-builtin-types.cs` - **(FIXED in task 381)** Uri/FileInfo/DirectoryInfo now work correctly
- `samples/10-type-converters/02-custom-type-converters.cs` - **REQUIRES TASK 383** - Source generator bug: only detects `ConfigureServices` converters, not `builder.AddTypeConverter()` calls

**Root cause:** The source generator bug is NOT a sample issue. The sample uses a valid API pattern (`builder.Services.AddConverter()`) that the generator should support but currently doesn't. Proper fix is in task 383.

## Immediate Action

Fix route overlap errors in samples/05-aot-example/aot-example.cs to unblock CI.

## Checklist

- [x] Exclude underscore-prefixed samples from verify-samples configuration
- [x] Fix package version mismatch in samples/09-configuration/ - RESOLVED (package versions aligned)
- [x] Fix route analyzer error in samples/08-testing/runfile-test-harness/
- [x] Fix samples/05-aot-example/aot-example.cs - was missing shebang/project directive (no route overlap errors)
- [x] Fix 01-builtin-types.cs (Uri/FileInfo/DirectoryInfo) - COMPLETED in task 381
- [x] Task 383 completed - `builder.AddTypeConverter()` now supported, `02-custom-type-converters.cs` works
- [ ] Verify all samples pass after fixes

## Notes

Priority: high

## Results

### Item 1: Exclude underscore-prefixed samples (COMPLETED)
- Modified: tools/dev-cli/commands/verify-samples-command.cs
- Added filtering logic to skip directories starting with underscore
- Underscore-prefixed samples are now excluded from verify-samples command
- Previously: 39 runfile samples
- After fix: 36 runfile samples (3 underscore-prefixed samples excluded)

### Item 2: Package version mismatch (COMPLETED)
- Status: RESOLVED - Package versions now aligned, no more NU1605 errors
- Both samples run successfully: `01-configuration-basics.cs`, `02-command-line-overrides.cs`

### Item 3: 01-builtin-types.cs (COMPLETED in task 381)
- Status: Uri/FileInfo/DirectoryInfo now work correctly
- Fixed as part of task 381

### Item 4: real-app.cs route analyzer error (COMPLETED)
- **File modified:** samples/08-testing/runfile-test-harness/real-app.cs
- **Change:** Removed unreachable `deploy {env}` route (lines 27-31) and unused `Deploy` handler
- **Reason:** The `--dry-run` flag handling belongs in the handler, not in route specificity
- **Result:** Sample now builds without NURU_R003 error

### Item 5: aot-example.cs (COMPLETED)
- **Status:** FIXED - Was missing shebang and `#:project` directive
- **Issue:** Not route overlaps - file simply couldn't build as a runfile
- **Fix:** Added `#!/usr/bin/dotnet --` and `#:project ../../source/timewarp-nuru/timewarp-nuru.csproj`

### Item 6: 02-custom-type-converters.cs (COMPLETED - Task 383 done)
- **Status:** FIXED - `builder.AddTypeConverter()` now supported by source generator
- **Verified:** Sample runs correctly, custom EmailAddress type converter works

### attributed-routes.csproj (REMOVED - Was never broken)
- **Status:** This sample was actually PASSING all along
- **Issue:** Build order problem during verification (not a sample issue)
- **Action:** Removed from failing samples list - no code changes needed
