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

- [x] Update `AddConfigurationLocator` to scan handler parameters
- [x] Detect `IConfiguration` usage
- [x] Detect `IConfigurationRoot` usage
- [x] Detect `IOptions<T>` usage
- [x] Keep detection of `.AddConfiguration()` calls
- [x] Wire locator into generator pipeline
- [x] Update `nuru-generator.cs` to use locator result instead of hardcoded `true`
- [x] Add tests for detection logic

## Results

`HasConfiguration` follows real use. The generator emits appsettings, environment, user-secrets, and command-line sources when the compilation calls `.AddConfiguration()` or a handler takes `IConfiguration`, `IConfigurationRoot`, or `IOptions<T>`. Other apps get the blank `ConfigurationBuilder` root.

Handler forms covered: `WithHandler` lambdas, anonymous methods, and method groups, plus a nested `Handler` constructor or `Handle` method on `ICommandHandler`, `IQueryHandler`, or `IIdempotentCommandHandler`. `IOptionsSnapshot<T>` and `IOptionsMonitor<T>` do not turn sources on. A helper that merely mentions `IConfiguration` does not either. The flag is compilation-scoped because endpoint handlers are not nested in one `Build()` chain.

### How to validate

Smoke: `dotnet run tests/timewarp-nuru-tests/generator/generator-52-configuration-detection.cs`

Expect: 15 passed. Cases with `AddConfiguration()`, `IConfiguration`, `IConfigurationRoot`, or `IOptions<T>` on a handler contain `CONFIGURATION (from AddConfiguration())`. Apps with no configuration use, an `IOptionsSnapshot<T>` handler, or a non-handler `IConfiguration` parameter contain `Minimal configuration for service initialization` and do not contain the full configuration banner.

Smoke: `dotnet run tests/timewarp-nuru-tests/generator/generator-14-options-validation.cs`

Expect: passed. `IOptions<T>` method-group handlers still bind `--Test:Port=0` without calling `AddConfiguration()`, and invalid values still throw `OptionsValidationException`.

## Session

- Implementation: grok task-work (2026-10-03)

## Notes

- Low priority optimization - generated code works either way
- `CreateBuilder()` always has configuration at runtime regardless of whether we emit setup code
- This is a code size optimization, not a correctness fix
