# Refactor NuruAppBuilder Into Partial Classes

## Description

Reorganize `NuruAppBuilder.cs` (773 lines) into partial classes with logical groupings. Extract help generation logic into a dedicated `HelpRouteGenerator` class. No regions.

## Requirements

- Split NuruAppBuilder into partial classes by concern
- Extract HelpRouteGenerator as a separate class
- Maintain consistent method ordering: public methods first (alphabetical), private methods after (alphabetical)
- All existing tests must pass
- No behavioral changes

## Checklist

### Implementation
- [x] Create `NuruAppBuilder.cs` - Core (fields, properties, Build, WithMetadata, AddAutoHelp)
- [x] Create `NuruAppBuilder.Routes.cs` - Route registration methods
- [x] Create `NuruAppBuilder.Configuration.cs` - DI, Config, Services, Logging, Terminal
- [x] Create `HelpRouteGenerator.cs` - Extract help generation logic
- [x] Remove original monolithic file
- [x] Verify all builds succeed
- [x] Verify all tests pass

## Notes

### Proposed File Structure

```
Source/TimeWarp.Nuru/
├── NuruAppBuilder.cs              # Core: fields, properties, Build()
├── NuruAppBuilder.Routes.cs       # Route registration (Add*Route methods)
├── NuruAppBuilder.Configuration.cs # DI, Config, Services, Logging
├── Help/
│   └── HelpRouteGenerator.cs      # Extracted from NuruAppBuilder
```

### NuruAppBuilder.cs (~150 lines) - Core
- Private fields (TypeConverterRegistry, ServiceCollection, AutoHelpEnabled, etc.)
- Public properties (EndpointCollection, Services)
- `Build()` method
- `WithMetadata()`
- `AddAutoHelp()`

### NuruAppBuilder.Routes.cs (~180 lines) - Route Registration
- `AddRoute()` (all overloads)
- `AddRoutes()` (all overloads)
- `AddDefaultRoute()`
- `AddRouteInternal()` (private)
- `AddMediatorRoute()` (private)
- `AddTypeConverter()`
- `AddReplOptions()`

### NuruAppBuilder.Configuration.cs (~140 lines) - DI & Configuration
- `AddConfiguration()`
- `AddDependencyInjection()`
- `ConfigureServices()` (both overloads)
- `UseLogging()`
- `UseTerminal()`
- `DetermineConfigurationBasePath()` (private)
- `GetSanitizedApplicationName()` (private static)

### HelpRouteGenerator.cs (~170 lines) - Extracted Class
- `GenerateHelpRoutes()` - called from Build() when AutoHelpEnabled
- `GetCommandPrefix()` (private static)
- `GetCommandGroupHelpText()` (private static)

### Method Ordering Convention
Within each partial class file:
1. Public methods (alphabetical)
2. Private methods (alphabetical)
