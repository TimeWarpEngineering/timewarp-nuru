# Round 1 — general
**Date:** 2026-10-03
**Scope reviewed:** branch vs master: add-configuration-locator.cs, nuru-generator.cs, interceptor-emitter.cs, generator-52 test, ci-tests wiring. Call sites checked: service-resolver-emitter.cs, framework-services.cs, behavior-emitter.cs, interceptor-emitter.cs (EnsureServicesInitialized, EmitSourceGenMediator).

## Summary

The locator replaces the hardcoded `HasConfiguration = true` with compilation-wide detection of `.AddConfiguration()` calls and of configuration-typed parameters on handlers. Pipeline wiring and the emitter fallback are correct. The detection is too narrow, though. The generated `configuration` root reaches more than handler parameters: registered services, behaviors, and the generated mediator's DI container all get it through constructors. When none of those consumers is detected, they now get a blank root and silently lose appsettings, environment, and command-line values.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/locators/add-configuration-locator.cs:ParameterUsesConfiguration
- Description: Only constructor or `Handle` parameters on a nested `Handler` type with a Nuru handler interface count as use. These consumers are missed: a service registered in `ConfigureServices` whose constructor takes `IConfiguration` or `IConfigurationRoot` (resolved through `__fw_IConfiguration = configuration` in `FrameworkServices.GetInitExpression`); a pipeline behavior constructor taking `IConfiguration` (`BehaviorEmitter.ResolveServiceForBehavior` → `configuration`); and services or handlers resolved through the generated TimeWarp.Mediator container (`__mediatorConfiguration = configuration`). With no `AddConfiguration()` call and no handler-level parameter, those consumers used to read appsettings, environment, user-secrets, and command-line values. Now they get an empty `ConfigurationBuilder().Build()`. That is a silent behavior regression in a change that is meant to be size-only (task Notes: "This is a code size optimization, not a correctness fix").
- Suggestion: Treat any constructor parameter of a configuration type (any class or record, including primary constructors) as a use, and keep the `Handle`-method / delegate rules for handlers. Update `HandlerTypeWithoutNuruInterface_Should_EmitMinimalConfiguration` to match, because that class is exactly the kind a DI registration constructs. Add a test for a `ConfigureServices`-registered service with an `IConfiguration` constructor.
- Status: open

### Issue 2 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/locators/add-configuration-locator.cs:using aliases
- Description: `RoslynSyntaxNode` and `SyntaxNode` aliases both name `Microsoft.CodeAnalysis.SyntaxNode`.
- Suggestion: Use a single alias.
- Status: open
