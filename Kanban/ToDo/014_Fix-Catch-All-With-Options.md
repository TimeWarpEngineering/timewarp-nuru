# Fix Catch-All With Options

## Problem
The parser incorrectly rejects patterns with options after catch-all parameters, even though the documentation explicitly states this should work.

## Current Behavior
```
Error at position 12: Catch-all parameter 'resources' must be the last segment in the route
```

## Expected Behavior
According to `/Documentation/Developer/Design/design/parser/syntax-rules.md`, this pattern should work:
```csharp
// ✅ WORKS: Catch-all for multiple resources
.AddRoute("kubectl get {*resources} --namespace? {ns?} --output? {format?}",
    (string[] resources, string? ns, string? format) => ...)
```

## Test Case
`/Tests/SingleFileTests/test-matrix/test-catch-all-with-options.cs` demonstrates this issue

## Rationale
Options with `--` prefix are distinguishable from positional arguments, so they should be allowed after catch-all parameters. This is essential for intercepting real-world CLI commands like:
- `kubectl get pods svc --namespace prod`
- `git add file1 file2 --force --dry-run`
- `docker run ubuntu bash -c "echo hello" --detach`

## Implementation Notes
The parser needs to be updated to allow option segments after catch-all parameters, since options are processed separately from positional arguments.

## Acceptance Criteria
- [ ] Pattern `command {*args} --option {value}` is accepted by parser
- [ ] Options after catch-all are correctly parsed and bound
- [ ] test-catch-all-with-options.cs passes