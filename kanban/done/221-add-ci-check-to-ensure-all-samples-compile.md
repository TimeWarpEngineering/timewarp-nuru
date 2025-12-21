# Add CI check to ensure all samples compile

## Description

During work on task 157 (--capabilities flag), we discovered that samples were already broken with invoker registration errors. The calculator sample fails with:

```
No source-generated invoker found for signature '_Returns_Int'. Ensure the NuruInvokerGenerator source generator is running and the delegate signature is supported.
```

We need a CI check that at minimum verifies all samples in the `samples/` directory compile successfully. This will catch regressions early.

**Priority:** high

## Checklist

- [x] Add a CI workflow step or script that builds all samples
- [x] Samples should at least compile (runtime testing can be a separate task)
- [x] Failures should block the CI pipeline
- [x] Test the CI change locally before pushing

## Implementation

This task was already implemented:

1. **Runfile script:** `runfiles/verify-samples.cs`
   - Discovers all runfile samples (42 `.cs` files with shebang)
   - Discovers all project samples (4 `.csproj` files)
   - Builds each in Release configuration
   - Returns exit code 1 on any failure

2. **CI integration:** `.github/workflows/ci-cd.yml` lines 57-61
   ```yaml
   - name: Verify Samples
     if: github.event_name != 'release'
     run: dotnet ${{ github.workspace }}/runfiles/verify-samples.cs
   ```

3. **Local test:** All 46/46 samples build successfully

## Notes

- Discovered during task 157 work - samples were broken but we didn't know until manually testing
- The invoker registration errors suggest source generator issues that should be caught at compile time
- Used `verify-samples.cs` runfile for consistency with other build tasks
