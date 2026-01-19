# V2 Generator Phase 2: Locators

## Description

Create all locator classes that find specific syntax elements in consumer code. Each locator is responsible for identifying one type of DSL element. All locators start as placeholder implementations with `IsPotentialMatch` and `Extract` stubs.

## Parent

#265 Epic: V2 Source Generator Implementation

## Checklist

### Commit 2.1: Create fluent DSL locators (21 files)
- [x] `run-async-locator.cs` - Find `app.RunAsync(...)` call sites
- [x] `create-builder-locator.cs` - Find `NuruApp.CreateBuilder(...)`
- [x] `build-locator.cs` - Find `.Build()` calls
- [x] `map-locator.cs` - Find `.Map("pattern")` calls
- [x] `with-handler-locator.cs` - Find `.WithHandler(...)` calls
- [x] `with-description-locator.cs` - Find `.WithDescription(...)` calls
- [x] `with-option-locator.cs` - Find `.WithOption(...)` calls
- [x] `with-alias-locator.cs` - Find `.WithAlias(...)` calls
- [x] `with-group-prefix-locator.cs` - Find `.WithGroupPrefix(...)` calls
- [x] `as-query-locator.cs` - Find `.AsQuery()` calls
- [x] `as-command-locator.cs` - Find `.AsCommand()` calls
- [x] `as-idempotent-command-locator.cs` - Find `.AsIdempotentCommand()` calls
- [x] `add-help-locator.cs` - Find `.AddHelp(...)` calls
- [x] `add-repl-locator.cs` - Find `.AddRepl(...)` calls
- [x] `add-behavior-locator.cs` - Find `.AddBehavior(...)` calls
- [x] `add-configuration-locator.cs` - Find `.AddConfiguration()` calls
- [x] `configure-services-locator.cs` - Find `.ConfigureServices(...)` calls
- [x] `use-terminal-locator.cs` - Find `.UseTerminal(...)` calls
- [x] `with-name-locator.cs` - Find `.WithName(...)` calls
- [x] `with-ai-prompt-locator.cs` - Find `.WithAiPrompt(...)` calls
- [x] `done-locator.cs` - Find `.Done()` calls

### Commit 2.2: Create attributed route locators (4 files)
- [x] `nuru-route-attribute-locator.cs` - Find `[NuruRoute]` classes
- [x] `nuru-route-group-attribute-locator.cs` - Find `[NuruRouteGroup]` base classes
- [x] `parameter-attribute-locator.cs` - Find `[Parameter]` properties
- [x] `option-attribute-locator.cs` - Find `[Option]` properties
- [x] Verify build succeeds

## Results

Phase 2 completed successfully with 2 commits:

1. **Commit 2.1:** Created 21 fluent DSL locators covering all builder methods
2. **Commit 2.2:** Created 4 attributed route locators for class/property attributes

### Locator Summary
```
generators/locators/ (25 files)
├── Fluent DSL Locators (21)
│   ├── run-async-locator.cs         # Entry point for interception
│   ├── create-builder-locator.cs    # Builder chain start
│   ├── build-locator.cs             # Builder chain end
│   ├── map-locator.cs               # Route definitions
│   ├── with-handler-locator.cs      # Handler lambdas
│   ├── with-description-locator.cs  # Help text
│   ├── with-option-locator.cs       # Command options
│   ├── with-alias-locator.cs        # Route aliases
│   ├── with-group-prefix-locator.cs # Route groups
│   ├── as-query-locator.cs          # Query marker
│   ├── as-command-locator.cs        # Command marker
│   ├── as-idempotent-command-locator.cs # Idempotent marker
│   ├── add-help-locator.cs          # Help feature
│   ├── add-repl-locator.cs          # REPL feature
│   ├── add-behavior-locator.cs      # Pipeline behaviors
│   ├── add-configuration-locator.cs # Configuration
│   ├── configure-services-locator.cs # DI services
│   ├── use-terminal-locator.cs      # Terminal config
│   ├── with-name-locator.cs         # App name
│   ├── with-ai-prompt-locator.cs    # AI prompt
│   └── done-locator.cs              # Scope terminator
└── Attributed Route Locators (4)
    ├── nuru-route-attribute-locator.cs       # [NuruRoute] classes
    ├── nuru-route-group-attribute-locator.cs # [NuruRouteGroup] bases
    ├── parameter-attribute-locator.cs        # [Parameter] properties
    └── option-attribute-locator.cs           # [Option] properties
```

### Technical Note
Due to namespace conflict with `TimeWarp.Nuru.SyntaxNode` (from parsing project), all locators use `RoslynSyntaxNode` alias for `Microsoft.CodeAnalysis.SyntaxNode`.

### Build Status
- Analyzer project builds with 0 warnings, 0 errors

## Notes

### Locator Pattern
Each locator follows this structure:
```csharp
namespace TimeWarp.Nuru.Generators;

internal static class RunAsyncLocator
{
    public static bool IsPotentialMatch(SyntaxNode node)
    {
        // Fast syntactic check
        // TODO: Implement
        return false;
    }
    
    public static TResult? Extract(GeneratorSyntaxContext context, CancellationToken ct)
    {
        // Semantic analysis and extraction
        // TODO: Implement
        return default;
    }
}
```

### Key Locators for Phase 1 Functionality
The following locators are essential for minimal end-to-end:
- `run-async-locator.cs` - Entry point for interception
- `map-locator.cs` - Find route definitions
- `with-handler-locator.cs` - Find handler lambdas

Other locators can remain as stubs until needed.
