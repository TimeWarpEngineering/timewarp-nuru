# Implement Static Factory Builder API

## Description

Redesign `NuruAppBuilder` API to mirror ASP.NET Core's `WebApplication` pattern with static factory methods. This provides a familiar "aha moment" for .NET developers and establishes a clear feature hierarchy.

## API Design

### Static Factory Methods on `NuruApp`

```csharp
// Full featured - requires args for Configuration
public static NuruAppBuilder CreateBuilder(string[] args, NuruApplicationOptions? options = null);

// Lightweight - no Configuration, args optional
public static NuruAppBuilder CreateSlimBuilder(string[]? args = null, NuruApplicationOptions? options = null);

// Bare minimum - total control
public static NuruAppBuilder CreateEmptyBuilder(string[]? args = null, NuruApplicationOptions? options = null);
```

### NuruApplicationOptions

```csharp
public class NuruApplicationOptions
{
    public string[]? Args { get; set; }
    public string? ApplicationName { get; set; }
    public string? EnvironmentName { get; set; }
    public string? ContentRootPath { get; set; }
}
```

### Feature Matrix

| Feature | CreateBuilder | CreateSlimBuilder | CreateEmptyBuilder |
|---------|:-------------:|:-----------------:|:------------------:|
| Args stored | ✅ | ✅ | ✅ |
| Type converters | ✅ | ✅ | ✅ |
| Configuration | ✅ | ✅ | ❌ |
| Auto-help | ✅ | ✅ | ❌ |
| Logging infra | ✅ | ✅ | ❌ |
| DI Container | ✅ | ❌ | ❌ |
| Mediator | ✅ | ❌ | ❌ |
| REPL ready | ✅ | ❌ | ❌ |
| Completion ready | ✅ | ❌ | ❌ |
| OpenTelemetry ready | ✅ | ❌ | ❌ |
| AOT-friendly | ⚠️ Partial | ✅ | ✅ |

## Usage Examples

### CreateBuilder (Full featured)
```csharp
var builder = NuruApp.CreateBuilder(args);
builder.Map("status", () => "OK");
builder.Map<DeployCommand>("deploy {env}");
// DI, REPL, Completion, Telemetry all ready
await builder.Build().RunAsync(args);
```

### CreateSlimBuilder (Lightweight)
```csharp
var builder = NuruApp.CreateSlimBuilder();
builder.Map("greet {name}", (string name) => $"Hello, {name}!");
await builder.Build().RunAsync(args);
```

### CreateEmptyBuilder (Bare minimum)
```csharp
var builder = NuruApp.CreateEmptyBuilder();
builder.AddTypeConverter(new MyCustomConverter());
builder.Map("cmd", () => "minimal");
await builder.Build().RunAsync(args);
```

## Requirements

- `CreateBuilder` requires `args` parameter (Configuration needs them for command-line overrides)
- `CreateSlimBuilder` and `CreateEmptyBuilder` have optional `args` parameter
- Existing `new NuruAppBuilder()` API should continue to work (backward compatibility)
- `Map()` method as alias for `AddRoute()` for ASP.NET familiarity

## Checklist

### Design
- [x] Define `NuruApplicationOptions` class
- [x] Document feature inclusion for each builder type

### Implementation
- [x] Add static factory methods to `NuruApp` class
- [x] Implement `CreateBuilder` with full feature set
- [x] Implement `CreateSlimBuilder` with lightweight feature set
- [x] Implement `CreateEmptyBuilder` with minimal feature set
- [x] Add `Map()` method alias for `AddRoute()`
- [x] Ensure backward compatibility with existing `new NuruAppBuilder()` API

### Testing
- [x] Add tests for each factory method
- [x] Verify feature inclusion/exclusion for each builder type
- [x] Test `RunAsync(args)` override behavior

### Documentation
- [x] Update README with new API examples
- [x] Update samples to demonstrate new patterns
- [ ] Document migration path from existing API

## Notes

- Mirrors ASP.NET Core's `WebApplication.CreateBuilder()`, `CreateSlimBuilder()`, `CreateEmptyBuilder()` pattern
- Future consideration: `AddOpenTelemetry()` for Aspire integration (included in CreateBuilder)
- `CreateBuilder` includes Configuration which requires args for command-line config provider

## Results

- Successfully implemented static factory methods (`CreateBuilder`, `CreateSlimBuilder`, `CreateEmptyBuilder`) on `NuruApp` class
- Added `NuruApplicationOptions` for configuration
- Implemented `Map()` method alias for ASP.NET Core familiarity
- All factory method tests passing
- Samples updated to demonstrate new patterns
- Backward compatibility maintained with existing `new NuruAppBuilder()` API
- Migration path documentation deferred to separate task
