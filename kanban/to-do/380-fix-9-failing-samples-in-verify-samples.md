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

### Source generator errors (CS0103 - variable not in scope):
6. `samples/10-type-converters/01-builtin-types.cs` - Generated code has undefined variables (target, source, dest, file, url, path)
7. `samples/10-type-converters/02-custom-type-converters.cs` - Generated code has undefined variables (recipient, color, version, to, primary, secondary)

### Route analyzer error (NURU_R003):
8. `samples/08-testing/runfile-test-harness/real-app.cs` - Route 'deploy {env}' marked as unreachable due to 'deploy {env} --dry-run' having higher specificity

### Build failure:
9. `samples/03-attributed-routes/attributed-routes.csproj` - Build failed (needs investigation)

## Immediate Action
Exclude underscore-prefixed samples from verify-samples command to unblock CI.

## Checklist

- [ ] Exclude underscore-prefixed samples from verify-samples configuration
- [ ] Fix package version mismatch in samples/09-configuration/
- [ ] Fix source generator issues in samples/10-type-converters/
- [ ] Fix route analyzer error in samples/08-testing/runfile-test-harness/
- [ ] Investigate and fix build failure in samples/03-attributed-routes/
- [ ] Verify all samples pass after fixes

## Notes

Priority: high
