# Fix Aspire Host OpenTelemetry sample issues

## Summary

Fix documentation and configuration issues in the Aspire Host OpenTelemetry sample project. The sample has broken path references, a broken package link, unclear documentation about IHostApplicationBuilder extension methods, and lacks explanation for a warning suppression pragma.

## Todo List

- [ ] Fix path references in overview.md (samples/aspire-host-otel → samples/_aspire-host-otel)
- [ ] Fix or remove broken link to TimeWarp.Nuru.Telemetry package
- [ ] Clarify IHostApplicationBuilder documentation - either implement AddNuruClientDefaults() or update docs
- [ ] Add explanatory comment for ASPIRECSHARPAPPS001 warning suppression
- [ ] Optionally add troubleshooting section

## Notes

The Aspire Host OpenTelemetry sample has several issues that need attention:

### Path Reference Issue
The overview.md file references `samples/aspire-host-otel` but the actual folder is `samples/_aspire-host-otel` (with leading underscore). This causes broken links when navigating the documentation.

### Broken Package Link
The TimeWarp.Nuru.Telemetry package link in the sample documentation is broken. Need to either find the correct package reference or remove the outdated link.

### IHostApplicationBuilder Documentation
The documentation mentions using IHostApplicationBuilder but doesn't clearly explain how to set up Nuru client defaults. Two options:
1. Implement `AddNuruClientDefaults()` extension method on IHostApplicationBuilder
2. Update documentation to show correct configuration approach

### Warning Suppression
The code uses `#pragma warning disable ASPIRECSHARPAPPS001` without explanation. Need to add a comment explaining why this warning is suppressed.

### Troubleshooting Section (Optional)
Consider adding a troubleshooting section to help users common issues like:
- Configuration problems
- Telemetry export issues
- Missing dependencies

## Results

[TBD - to be filled after completion]
