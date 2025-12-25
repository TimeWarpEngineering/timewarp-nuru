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
- [ ] `nuru-route-attribute-locator.cs` - Find `[NuruRoute]` classes
- [ ] `nuru-route-group-attribute-locator.cs` - Find `[NuruRouteGroup]` base classes
- [ ] `parameter-attribute-locator.cs` - Find `[Parameter]` properties
- [ ] `option-attribute-locator.cs` - Find `[Option]` properties
- [ ] Verify build succeeds

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
