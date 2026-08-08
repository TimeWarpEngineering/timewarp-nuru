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

- [x] Route params renamed to match handlers: `read {file:FileInfo}`,
      `list {dir:DirectoryInfo}` (reads better for a type-converter demo)
- [x] Second failure layer exposed and worked around: `{event}`/`{when}`
      params make the GENERATOR emit unescaped C# keywords (CS0065/CS0246) —
      renamed to `{name}`/`{at}`; generator bug filed as **task 460**
- [x] Sample builds clean
- [x] `dev verify-samples` → **64/64**

## Results

Fixed in commit `70793702` (dev). Two layers: NURU_H005 param-name mismatch
(fixed by route rename) and an unescaped-keyword generator bug underneath
(worked around by param rename; tracked as task 460 with exact repro).

### How to validate

Smoke: `dotnet build samples/fluent/08-type-converters/fluent-type-converters-builtin.cs`
→ clean. Automated: `dev verify-samples` → 64/64.

## Notes

Found by: 458 orchestration session. Not blocking 458-010 rollout (the audit
itself passes; verify-samples is a separate pipeline step).
