# Add CI check to ensure all samples compile

## Description

During work on task 157 (--capabilities flag), we discovered that samples were already broken with invoker registration errors. The calculator sample fails with:

```
No source-generated invoker found for signature '_Returns_Int'. Ensure the NuruInvokerGenerator source generator is running and the delegate signature is supported.
```

We need a CI check that at minimum verifies all samples in the `samples/` directory compile successfully. This will catch regressions early.

**Priority:** high

## Checklist

- [ ] Add a CI workflow step or script that builds all samples
- [ ] Samples should at least compile (runtime testing can be a separate task)
- [ ] Failures should block the CI pipeline
- [ ] Test the CI change locally before pushing

## Notes

- Discovered during task 157 work - samples were broken but we didn't know until manually testing
- The invoker registration errors suggest source generator issues that should be caught at compile time
- Consider using a dedicated runfile script (e.g., `build-samples.cs`) for consistency with other build tasks
