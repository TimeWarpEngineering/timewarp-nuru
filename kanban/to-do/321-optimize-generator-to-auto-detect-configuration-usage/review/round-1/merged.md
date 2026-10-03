# Round 1 — merged findings
**Date:** 2026-10-03
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/locators/add-configuration-locator.cs:ParameterUsesConfiguration
- Description: Only parameters on a nested `Handler` with a Nuru handler interface counted as use. Registered services (through `__fw_IConfiguration = configuration`), behaviors (`ResolveServiceForBehavior`), and mediator-resolved types (`__mediatorConfiguration`) that take configuration in their constructors silently got a blank root.
- Suggestion: Count any constructor parameter of a configuration type as use.
- Source: general
- Disposition notes: Fixed. Any constructor parameter of type IConfiguration, IConfigurationRoot, or IOptions<T> now counts, including primary constructors (semantic path: MethodKind.Constructor; syntax fallback: ConstructorDeclarationSyntax or TypeDeclarationSyntax). The `Handle` method rule still requires a Nuru handler type. Tests: added `RegisteredServiceConstructorIConfiguration_Should_EmitFullConfiguration`, and replaced `HandlerTypeWithoutNuruInterface` (which encoded the bug) with `HandleMethodWithoutNuruInterface_Should_EmitMinimalConfiguration`.

### M2 — Severity: nit — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/locators/add-configuration-locator.cs:using aliases
- Description: Duplicate `SyntaxNode` / `RoslynSyntaxNode` aliases.
- Suggestion: Use a single alias.
- Source: general
- Disposition notes: Fixed. The `SyntaxNode` alias is removed and `RoslynSyntaxNode` is used.

## Duplicates / conflicts

- None (single reviewer).
