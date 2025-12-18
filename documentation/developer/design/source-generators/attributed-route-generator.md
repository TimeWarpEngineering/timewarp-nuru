# Attributed Route Source Generator

Design documentation for the `NuruAttributedRouteGenerator` source generator that enables auto-registration of routes from `[NuruRoute]` attributes.

## Overview

The attributed route generator scans for classes decorated with `[NuruRoute]` and generates:
1. `CompiledRouteBuilder` calls for each attributed request
2. `[ModuleInitializer]` registration code
3. Pattern strings for help display

This enables zero-ceremony route registration - users decorate request classes and routes are automatically discovered.

## Motivation

Without attributed routes, users must explicitly call `Map()` for each route:

```csharp
var app = NuruApp.CreateBuilder(args)
  .Map("deploy {env}", (DeployRequest req) => mediator.Send(req))
  .Build();
```

With attributed routes, registration is automatic:

```csharp
[NuruRoute("deploy")]
public sealed class DeployRequest : IRequest
{
  [Parameter]
  public string Env { get; set; } = string.Empty;
}

// No Map() call needed - auto-registered via [ModuleInitializer]
var app = NuruApp.CreateBuilder(args).Build();
```

## Attributes

### `[NuruRoute]`

Applied to request classes. Specifies the route pattern (literals only).

```csharp
[NuruRoute("deploy", Description = "Deploy to an environment")]
public sealed class DeployRequest : IRequest { }
```

**Properties:**
- `Pattern` (constructor) - Route literals, space-separated. Use `""` for default route.
- `Description` - Help text for the route.

### `[NuruRouteAlias]`

Registers additional patterns for the same request type.

```csharp
[NuruRoute("goodbye")]
[NuruRouteAlias("bye", "cya")]
public sealed class GoodbyeRequest : IRequest { }
```

Generates three routes: `goodbye`, `bye`, `cya` - all mapping to `GoodbyeRequest`.

### `[NuruRouteGroup]`

Applied to base classes. Provides shared prefix and options for derived requests.

```csharp
[NuruRouteGroup("docker")]
public abstract class DockerRequestBase
{
  [GroupOption("debug", "D")]
  public bool Debug { get; set; }
}

[NuruRoute("run")]
public sealed class DockerRunRequest : DockerRequestBase, IRequest { }
// Generated pattern: "docker run --debug,-D"
```

### `[Parameter]`

Marks a property as a positional parameter.

```csharp
[Parameter(Description = "Target environment")]
public string Env { get; set; } = string.Empty;

[Parameter(IsCatchAll = true)]
public string[] Args { get; set; } = [];
```

**Properties:**
- `Name` - Override parameter name (defaults to property name in camelCase)
- `Description` - Help text
- `IsCatchAll` - Captures all remaining arguments

**Optionality:** Inferred from nullability. `string?` = optional, `string` = required.

### `[Option]`

Marks a property as a command-line option.

```csharp
[Option("force", "f", Description = "Skip confirmation")]
public bool Force { get; set; }

[Option("config", "c")]
public string? ConfigFile { get; set; }

[Option("replicas", "r")]
public int Replicas { get; set; } = 1;
```

**Constructor:**
- `longForm` - Long option name (without `--`)
- `shortForm` - Short option name (without `-`), optional

**Properties:**
- `Description` - Help text
- `IsRepeated` - Option can be specified multiple times

**Type inference:**
- `bool` → Flag (no value expected)
- Other types → Valued option (`expectsValue: true`)
- Nullable types → Optional value (`parameterIsOptional: true`)

### `[GroupOption]`

Same as `[Option]` but defined on a group base class. Inherited by all derived requests.

```csharp
[NuruRouteGroup("docker")]
public abstract class DockerRequestBase
{
  [GroupOption("debug", "D")]
  public bool Debug { get; set; }
}
```

## Generated Code Structure

### Simple Route

For a request like:

```csharp
[NuruRoute("deploy", Description = "Deploy to an environment")]
public sealed class DeployRequest : IRequest
{
  [Parameter]
  public string Env { get; set; } = string.Empty;
  
  [Option("force", "f")]
  public bool Force { get; set; }
}
```

The generator produces:

```csharp
// GeneratedAttributedRoutes.g.cs
namespace TimeWarp.Nuru.Generated;

internal static class GeneratedAttributedRoutes
{
  internal static readonly CompiledRoute __Route_DeployRequest = 
    new CompiledRouteBuilder()
      .WithLiteral("deploy")
      .WithParameter("env")
      .WithOption("force", shortForm: "f", isOptionalFlag: true)
      .Build();
      
  internal const string __Pattern_DeployRequest = "deploy {env} --force,-f";
}

internal static class GeneratedAttributedRouteRegistration
{
  [ModuleInitializer]
  internal static void Register()
  {
    NuruRouteRegistry.Register(
      typeof(DeployRequest),
      GeneratedAttributedRoutes.__Route_DeployRequest,
      GeneratedAttributedRoutes.__Pattern_DeployRequest,
      "Deploy to an environment");
  }
}
```

### Grouped Route

For a grouped request like:

```csharp
[NuruRouteGroup("docker")]
public abstract class DockerRequestBase
{
  [GroupOption("debug", "D", Description = "Enable debug mode")]
  public bool Debug { get; set; }
}

[NuruRoute("run", Description = "Run a container")]
public sealed class DockerRunRequest : DockerRequestBase, IRequest
{
  [Parameter(Description = "Image name")]
  public string Image { get; set; } = string.Empty;
  
  [Option("detach", "d")]
  public bool Detach { get; set; }
}
```

The generator produces:

```csharp
// GeneratedAttributedRoutes.g.cs
namespace TimeWarp.Nuru.Generated;

internal static class GeneratedAttributedRoutes
{
  internal static readonly CompiledRoute __Route_DockerRunRequest = 
    new CompiledRouteBuilder()
      .WithLiteral("docker")          // Group prefix
      .WithLiteral("run")             // Route pattern
      .WithParameter("image", type: "string", description: "Image name")
      .WithOption("debug", shortForm: "D", description: "Enable debug mode", isOptionalFlag: true)  // From group
      .WithOption("detach", shortForm: "d", isOptionalFlag: true)
      .Build();
      
  internal const string __Pattern_DockerRunRequest = "docker run {image} --debug,-D --detach,-d";
}

internal static class GeneratedAttributedRouteRegistration
{
  [ModuleInitializer]
  internal static void Register()
  {
    NuruRouteRegistry.Register(
      typeof(DockerRunRequest),
      GeneratedAttributedRoutes.__Route_DockerRunRequest,
      GeneratedAttributedRoutes.__Pattern_DockerRunRequest,
      "Run a container");
  }
}
```

Note how the group prefix `"docker"` and group option `--debug,-D` are inherited from `DockerRequestBase`.

## Registration Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                        COMPILE TIME                                  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  1. Source Generator scans for [NuruRoute] classes                  │
│                     ↓                                                │
│  2. Extracts attributes from class and properties                   │
│                     ↓                                                │
│  3. Walks inheritance chain for [NuruRouteGroup]                    │
│                     ↓                                                │
│  4. Generates CompiledRouteBuilder calls                            │
│                     ↓                                                │
│  5. Generates [ModuleInitializer] registration code                 │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                         RUNTIME                                      │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  1. [ModuleInitializer] runs before Main()                          │
│                     ↓                                                │
│  2. Routes registered in NuruRouteRegistry                          │
│                     ↓                                                │
│  3. NuruApp.Build() pulls routes from registry                      │
│                     ↓                                                │
│  4. Routes added to endpoint collection                             │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

## Design Decisions

### 1. Attributes on Properties, Not Constructor Parameters

**Decision:** Use properties with attributes, not primary constructors or record parameters.

**Rationale:**
- Primary constructors don't allow per-parameter attributes easily
- Properties are clearer for binding semantics
- Matches MediatR/Mediator pattern of request classes

### 2. No Dashes in Attribute Parameters

**Decision:** `[Option("force", "f")]` not `[Option("--force", "-f")]`

**Rationale:**
- The generator adds `--` and `-` prefixes when building the route
- Cleaner syntax - users don't type dashes that would just be stripped

### 3. Nullability-Based Optionality

**Decision:** `string?` = optional, `string` = required

**Rationale:**
- Consistent with C# nullability semantics
- No extra `IsOptional` property needed
- Compiler enforces handling of optional values

### 4. Inheritance-Based Grouping

**Decision:** `[NuruRouteGroup]` on base class, not a separate grouping construct

**Rationale:**
- Natural C# pattern - inheritance already conveys "is-a" relationship
- Group options inherited via normal property inheritance
- No special container classes needed

### 5. Separate Route Constants for Aliases

**Decision:** Generate `__Route_X_Alias_Y` for each alias, not reuse main route

**Rationale:**
- Aliases might have different pattern strings in help
- Cleaner separation in generated code
- No runtime overhead (constants)

## Implementation Details

### Source File

`source/timewarp-nuru-analyzers/analyzers/nuru-attributed-route-generator.cs`

### Key Methods

- `IsClassWithNuruRouteAttribute()` - Syntax predicate for incremental generator
- `ExtractRouteInfo()` - Semantic analysis to extract attribute data
- `GenerateRegistrationCode()` - Emits the C# source

### Data Types

```csharp
record AttributedRouteInfo(
  string FullTypeName,
  string TypeName,
  string Pattern,
  string? Description,
  List<string> Aliases,
  string? GroupPrefix,
  List<GroupOptionInfo> GroupOptions,
  List<ParameterInfo> Parameters,
  List<OptionInfo> Options
);
```

## Sample Application

A complete working example is in [samples/attributed-routes/attributed-routes.cs](../../../../samples/attributed-routes/attributed-routes.cs).

Features demonstrated:
- Simple routes with parameters and options (`GreetRequest`, `DeployRequest`)
- Default route with empty pattern (`DefaultRequest`)
- Route aliases (`GoodbyeRequest` with `bye`, `cya` aliases)
- Grouped routes (`DockerRunRequest`, `DockerBuildRequest` inheriting from `DockerRequestBase`)
- Catch-all parameters (`ExecRequest`)

Run the sample:
```bash
dotnet run --project samples/attributed-routes -- greet Alice
dotnet run --project samples/attributed-routes -- deploy prod --force --replicas 3
dotnet run --project samples/attributed-routes -- docker run nginx --debug
dotnet run --project samples/attributed-routes -- exec echo hello world
```

## Testing

Tests are in `tests/timewarp-nuru-analyzers-tests/auto/`:

- `attributed-route-generator-01-basic.cs` - Integration tests (routes registered correctly)
- `attributed-route-generator-02-source.cs` - Source verification tests (generated code correct)

Shared utilities in `attributed-route-test-helpers.cs`.

## Related Documentation

- [Route Pattern Anatomy](../parser/route-pattern-anatomy.md)
- [Syntax Rules](../parser/syntax-rules.md)
- [Error Handling](../cross-cutting/error-handling.md)
