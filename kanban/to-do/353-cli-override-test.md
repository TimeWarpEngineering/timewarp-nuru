# Create CLI configuration override test in tests/timewarp-nuru-core-tests/configuration/configuration-02-cli-overrides.cs

## Description

Create an automated test that verifies CLI configuration overrides work correctly, including IConfiguration injection. This will complement the existing sample `02-command-line-overrides.cs` by providing automated verification instead of manual demonstration.

## Checklist

- [ ] Create test file `tests/timewarp-nuru-core-tests/configuration/configuration-02-cli-overrides.cs`
- [ ] Create settings file `tests/timewarp-nuru-core-tests/configuration/configuration-02-test.settings.json`
- [ ] Implement test methods:
  - [ ] `Should_override_flat_key_from_cli()` - Test `Key=value` format
  - [ ] `Should_override_hierarchical_key_from_cli()` - Test `--Section:Key=value` format
  - [ ] `Should_inject_configuration_with_cli_overrides()` - Test IConfiguration injection
- [ ] Add proper JARIBU_MULTI support and test registration
- [ ] Run tests to verify they pass
- [ ] Add test to CI if needed

## Notes

The test will verify runtime behavior of CLI configuration overrides, fixing the issue with the failing `generator-13-ioptions-parameter-injection.cs` test. Unlike the existing sample which demonstrates the feature, this will be automated testing with assertions.

Test will use multiple CLI formats:
- Flat keys: `Key=value` (no prefix)
- Hierarchical keys: `--Section:Key=value` (with colon)
- Ensure proper filtering from route matching

Settings file will provide baseline values that CLI args override.
