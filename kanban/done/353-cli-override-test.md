# Create CLI configuration override test in tests/timewarp-nuru-core-tests/configuration/configuration-02-cli-overrides.cs

## Description

Create an automated test that verifies CLI configuration overrides work correctly, including IConfiguration injection. This will complement the existing sample `02-command-line-overrides.cs` by providing automated verification instead of manual demonstration.

## Checklist

- [x] Create test file `tests/timewarp-nuru-core-tests/configuration/configuration-02-cli-overrides.cs`
- [x] Create settings file `tests/timewarp-nuru-core-tests/configuration/configuration-02-cli-overrides.settings.json`
- [x] Implement test methods:
  - [x] `Should_override_flat_key_from_cli()` - Test `--Key=value` format (FAILING - exposes bug #354)
  - [x] `Should_override_hierarchical_key_from_cli()` - Test `--Section:Key=value` format
  - [x] `Should_inject_configuration_with_cli_overrides()` - Test IConfiguration injection
  - [x] `Should_use_defaults_when_no_cli_override()` - Test default values
- [x] Add proper JARIBU_MULTI support and test registration
- [x] Add test to CI
- [x] Run tests - 3 pass, 1 fails (intentionally exposes bug)

## Completion Notes

Tests created and added to CI. One test (`Should_override_flat_key_from_cli`) intentionally fails to expose bug #354: flat CLI config keys (`--Key=value`) are not filtered from route matching, only hierarchical keys (`--Section:Key=value`) are filtered.

Bug tracked in: `kanban/to-do/354-bug-flat-cli-config-keys--keyvalue-not-filtered-from-route-matching.md`

## Files Created

- `tests/timewarp-nuru-core-tests/configuration/configuration-02-cli-overrides.cs`
- `tests/timewarp-nuru-core-tests/configuration/configuration-02-cli-overrides.settings.json`

## Files Modified

- `tests/ci-tests/Directory.Build.props` - Added test to CI, added `Microsoft.Extensions.Configuration` using
