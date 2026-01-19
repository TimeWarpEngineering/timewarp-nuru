# Optimize Generator to Auto-Detect Configuration Usage

## Description

Currently the generator always emits configuration code (`HasConfiguration=true`). This optimization task will make the `AddConfigurationLocator` smart - only emitting configuration code when actually needed.

## Goal

Reduce generated code size by only emitting configuration setup when the app actually uses configuration.

## Detection Logic

Set `HasConfiguration=true` if ANY of:
- `.AddConfiguration()` is called on the builder
- Any handler has `IConfiguration` parameter
- Any handler has `IConfigurationRoot` parameter
- Any handler has `IOptions<T>` parameter

## Files Involved

- `source/timewarp-nuru-analyzers/generators/locators/add-configuration-locator.cs` - Expand detection logic
- `source/timewarp-nuru-analyzers/generators/nuru-generator.cs` - Wire up locator result (currently hardcoded to `true`)

## Checklist

- [ ] Update `AddConfigurationLocator` to scan handler parameters
- [ ] Detect `IConfiguration` usage
- [ ] Detect `IConfigurationRoot` usage
- [ ] Detect `IOptions<T>` usage
- [ ] Keep detection of `.AddConfiguration()` calls
- [ ] Wire locator into generator pipeline
- [ ] Update `nuru-generator.cs` to use locator result instead of hardcoded `true`
- [ ] Add tests for detection logic

## Notes

- Low priority optimization - generated code works either way
- `CreateBuilder()` always has configuration at runtime regardless of whether we emit setup code
- This is a code size optimization, not a correctness fix
