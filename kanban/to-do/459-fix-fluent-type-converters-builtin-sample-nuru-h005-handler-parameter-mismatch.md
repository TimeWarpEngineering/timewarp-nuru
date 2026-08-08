# Fix fluent-type-converters-builtin sample: NURU_H005 handler parameter mismatch

## Description

`samples/fluent/08-type-converters/fluent-type-converters-builtin.cs` fails to
build (found during the 458-010 audit remediation, 2026-08-08; confirmed
PRE-EXISTING — reproduces identically with the original shebang, so unrelated
to the shebang normalization):

- line 76: `error NURU_H005: Handler parameter 'file' doesn't match any route
  segment; available segments: read, {path}`
- line 105: same for parameter 'dir' vs `list, {path}`
- cascade of CS errors in the generated interceptor follows.

The sample's handler parameter names drifted from its route patterns (routes
say `{path}`, handlers say `file`/`dir`) — either the sample predates an
analyzer tightening or was edited inconsistently. `dev verify-samples`
currently reports exactly this one failure.

## Checklist

- [ ] Align handler parameter names with route segments (or rename the route
      parameters — pick whichever reads better for a type-converter demo)
- [ ] `dotnet build samples/fluent/08-type-converters/fluent-type-converters-builtin.cs` clean
- [ ] `dev verify-samples` → 0 failures

## Notes

Found by: 458 orchestration session. Not blocking 458-010 rollout (the audit
itself passes; verify-samples is a separate pipeline step).
