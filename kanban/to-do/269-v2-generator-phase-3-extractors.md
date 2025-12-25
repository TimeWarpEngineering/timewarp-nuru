# V2 Generator Phase 3: Extractors

## Description

Create extractor classes that parse located syntax elements into model objects. Extractors use locators to find syntax nodes and builders to construct the IR.

## Parent

#265 Epic: V2 Source Generator Implementation

## Checklist

### Commit 3.1: Create core extractors (5 files)
- [ ] `app-extractor.cs` - Orchestrates extraction, builds `AppModel`
- [ ] `fluent-chain-extractor.cs` - Walk builder chain, extract routes
- [ ] `pattern-string-extractor.cs` - Parse mini-language via `PatternParser`
- [ ] `handler-extractor.cs` - Extract handler lambda/method info
- [ ] `intercept-site-extractor.cs` - Extract file/line/column for interceptor

### Commit 3.2: Create remaining extractors (2 files)
- [ ] `attributed-route-extractor.cs` - Extract from `[NuruRoute]` classes
- [ ] `service-extractor.cs` - Extract from `ConfigureServices()`
- [ ] Verify build succeeds

## Notes

### AppExtractor Role
The `AppExtractor` is the main orchestrator:
1. Uses `RunAsyncLocator` to find entry point
2. Traces back to find builder chain
3. Uses `FluentChainExtractor` to extract fluent routes
4. Uses `AttributedRouteExtractor` for `[NuruRoute]` classes
5. Merges routes, checks for conflicts
6. Returns complete `AppModel`

### FluentChainExtractor Flow
```
RunAsync() call site
       │
       ▼
   Trace back to 'app' variable
       │
       ▼
   Find assignment from .Build()
       │
       ▼
   Walk builder chain to CreateBuilder()
       │
       ▼
   For each DSL call, use corresponding locator
       │
       ▼
   Build RouteDefinition using RouteDefinitionBuilder
```

### PatternStringExtractor
Integrates with existing `timewarp-nuru-parsing` project:
- Takes pattern string from `.Map("pattern")`
- Calls `PatternParser.Parse()`
- Converts `Syntax` nodes to `SegmentDefinition`

### Reference: Existing Extractors
See `reference-only/extractors/` for previous implementations:
- `fluent-chain-extractor.cs` - Basic chain walking
- `mediator-route-extractor.cs` - `Map<T>()` handling
- `attributed-route-extractor.cs` - Attribute extraction
