# Diagnosis: Issue #208 repro in tools/dev-cli

## Symptom

`dotnet run tools/dev-cli/dev.cs -- --help` fails with CS8801 in generated code:

- `NuruGenerated.g.cs(47,182): error CS8801`
- `NuruGenerated.g.cs(50,203): error CS8801`

Error text: cannot use local variable `app` declared in top-level statement in this context.

## Root Cause

The generator emits `app.Terminal` into **static** `Lazy<T>` field initializers for singleton services with built-in constructor dependencies (`ITerminal`), but `app` only exists in method scope (`ExecuteRouteAsync(NuruApp app, ...)`).

This is a scope mismatch caused by reusing context-agnostic dependency-expression emitters in both static and method contexts.

## Evidence Chain

1. `InterceptorEmitter.EmitServiceFields()` emits static service fields and calls:
   - `ServiceResolverEmitter.ResolveConstructorArguments(...)` (`interceptor-emitter.cs:355`)

2. `ResolveConstructorArguments` delegates built-in deps to `ResolveBuiltInType`.

3. `ResolveBuiltInType` hardcodes:
   - `ITerminal -> "app.Terminal"` (`service-resolver-emitter.cs:387`)
   - `IConfiguration -> "configuration"` (`:383`)
   - `NuruApp -> "app"` (`:391`)

4. Generated output proves invalid static emission (`artifacts/generated/dev/.../NuruGenerated.g.cs`):
   - line 47: `new RepoCleanService(app.Terminal)` in static lazy initializer
   - line 50: `new RepoCheckVersionService(app.Terminal, ...)` in static lazy initializer

5. `app` is only declared as a parameter in method scope:
   - `ExecuteRouteAsync(NuruApp app, string[] args)` (`NuruGenerated.g.cs:52-56`)

## Affected Scope

Any singleton/scoped service emitted as static lazy field whose constructor includes built-ins requiring runtime context:

- `ITerminal`
- `IConfiguration` / `IConfigurationRoot`
- `NuruApp`

`ILogger<T>` does not trigger this specific failure because its expression is static-factory based.

## Reproduction Steps

1. Compile `tools/dev-cli/dev.cs` with shared dev-cli endpoints included.
2. Ensure services include `RepoCleanService` / `RepoCheckVersionService` registrations.
3. Run `dotnet run tools/dev-cli/dev.cs -- --help`.
4. Observe CS8801 at generated lines 47 and 50.

## Contributing Factors

- Phase 3 introduced constructor-arg emission into static singleton/scoped fields.
- Dependency expression generation does not carry an emission-context parameter (static vs method scope).
- Integration test coverage did not include singleton service constructors requiring `ITerminal` in static field path.

## Related History

- Commit introducing behavior: `91e92888` (Phase 3 constructor dependency resolution).
- Existing tracked issue: #208.
