# Strip commit hash from version output

## Description

The `--version` route currently displays redundant commit hash information. The `InformationalVersion` attribute contains the full version with commit hash suffix (e.g., `1.0.0+9c5645b...`), and then the commit hash is printed again as a separate field.

**Current output:**
```
1.0.0+9c5645b72d7078981643c5d1968a9ebf76f60117
Commit: 9c5645b72d7078981643c5d1968a9ebf76f60117
Date: 2025-12-09T13:12:36
```

**Expected output:**
```
1.0.0
Commit: 9c5645b72d7078981643c5d1968a9ebf76f60117
Date: 2025-12-09T13:12:36
```

## Requirements

1. Strip the `+<hash>` suffix from the version string when displaying
2. Keep the separate `Commit:` line for the full commit hash
3. Maintain backward compatibility for versions without the `+` suffix

## Checklist

### Implementation
- [x] Modify `DisplayVersion()` in `nuru-app-builder-extensions.cs` to strip `+<hash>` suffix from version
- [x] Handle edge cases (no `+` in version, empty hash, etc.)

### Testing
- [x] Verify version output no longer shows redundant commit hash

## Notes

- Location: `source/timewarp-nuru/nuru-app-builder-extensions.cs` lines 97-101
- The `+` suffix follows SemVer 2.0 build metadata convention
- Simple string manipulation: split on `+` and take the first part

## Results

- Modified `DisplayVersion()` to strip build metadata suffix using `version.Split('+')[0]`
- Edge case handled: if no `+` exists, the full string is returned unchanged
- Verified output now shows clean version without redundant commit hash
