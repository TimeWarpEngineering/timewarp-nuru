# V2 Generator Phase 3: Extractors

## Description

Create extractor classes that parse located syntax elements into model objects. Extractors use locators to find syntax nodes and builders to construct the IR (Intermediate Representation).

## Parent

#265 Epic: V2 Source Generator Implementation

## Key References

**IMPORTANT: Read these before starting:**

1. **Architecture Document:**
   `.agent/workspace/2024-12-25T14-00-00_v2-source-generator-architecture.md`
   - Full pipeline design (Locate → Extract → Emit)
   - Three DSL support (Fluent, Mini-Language, Attributed)
   - Extraction flow diagrams
   - AppModel structure

2. **Fluent DSL Design:**
   `.agent/workspace/2024-12-25T12-00-00_v2-fluent-dsl-design.md`
   - DSL specification
   - Method chain patterns
   - Handler patterns

3. **DSL Reference Implementation:**
   `tests/timewarp-nuru-core-tests/routing/dsl-example.cs`
   - Working example of fluent DSL usage

4. **Existing Pattern Parser:**
   `source/timewarp-nuru-parsing/parsing/`
   - `parser/parser.cs` - PatternParser class
   - `syntax/` - Syntax node types (LiteralSyntax, ParameterSyntax, OptionSyntax)
   - Use this to parse mini-language pattern strings

5. **Reference Extractors (old implementation):**
   `source/timewarp-nuru-analyzers/reference-only/extractors/`
   - `fluent-chain-extractor.cs` - Basic chain walking pattern
   - `attributed-route-extractor.cs` - Attribute extraction pattern
   - `delegate-analyzer.cs` - Handler lambda analysis

6. **Models (created in Phase 1):**
   `source/timewarp-nuru-analyzers/generators/models/`
   - `app-model.cs` - Top-level IR
   - `route-definition.cs` - Route IR
   - `handler-definition.cs` - Handler IR
   - `segment-definition.cs` - Pattern segment IR

7. **Builders (created in Phase 1):**
   `source/timewarp-nuru-analyzers/generators/extractors/builders/`
   - `app-model-builder.cs` - Build AppModel
   - `route-definition-builder.cs` - Build RouteDefinition
   - `handler-definition-builder.cs` - Build HandlerDefinition

8. **Locators (created in Phase 2):**
   `source/timewarp-nuru-analyzers/generators/locators/`
   - 25 locator files for finding syntax elements

## Technical Notes

### Namespace Conflict
Due to `TimeWarp.Nuru.SyntaxNode` shadowing `Microsoft.CodeAnalysis.SyntaxNode`, use:
```csharp
using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;
```

### Coding Standards
Follow `documentation/developer/standards/csharp-coding.md`:
- PascalCase for private fields (no underscore prefix)
- 2-space indentation
- Allman bracket style
- No `var` keyword

## Checklist

### Commit 3.1: Create core extractors (5 files)
- [x] `generators/extractors/app-extractor.cs` - Main orchestrator, builds `AppModel`
- [x] `generators/extractors/fluent-chain-extractor.cs` - Walk builder chain, extract routes
- [x] `generators/extractors/pattern-string-extractor.cs` - Parse mini-language via `PatternParser`
- [x] `generators/extractors/handler-extractor.cs` - Extract handler lambda/method info
- [x] `generators/extractors/intercept-site-extractor.cs` - Extract file/line/column for interceptor

### Commit 3.2: Create remaining extractors (2 files)
- [x] `generators/extractors/attributed-route-extractor.cs` - Extract from `[NuruRoute]` classes
- [x] `generators/extractors/service-extractor.cs` - Extract from `ConfigureServices()`
- [x] Verify build succeeds

## Detailed Design

### AppExtractor Role
The `AppExtractor` is the main orchestrator that:
1. Receives `GeneratorSyntaxContext` from the generator
2. Uses `RunAsyncLocator` to find entry point and get `InterceptSiteModel`
3. Traces back through syntax tree to find builder chain
4. Uses `FluentChainExtractor` to extract fluent routes
5. Uses `AttributedRouteExtractor` for `[NuruRoute]` classes (if any)
6. Merges routes from all sources, checks for conflicts
7. Returns complete `AppModel` via `AppModelBuilder`

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
   Walk builder chain upward to CreateBuilder()
       │
       ▼
   For each .Map() call:
       ├── Extract pattern string
       ├── Use PatternStringExtractor if mini-language detected
       ├── Find .WithHandler() → use HandlerExtractor
       ├── Find .WithDescription() → extract description
       ├── Find .WithOption() → extract options
       ├── Find .AsQuery()/.AsCommand() → set message type
       ├── Track .WithGroupPrefix() scope
       └── Build RouteDefinition via RouteDefinitionBuilder
       │
       ▼
   Collect app-level settings:
       ├── .WithName() → app name
       ├── .WithDescription() → app description
       ├── .WithAiPrompt() → AI prompt
       ├── .AddHelp() → help options
       ├── .AddRepl() → REPL options
       ├── .AddBehavior() → behaviors
       └── .ConfigureServices() → services
```

### PatternStringExtractor
Integrates with existing `timewarp-nuru-parsing` project:
```csharp
// Pattern: "deploy {env|Description} --force,-f|Skip confirmation"
// 1. Call PatternParser.Parse(pattern)
// 2. Get Syntax with segments:
//    - LiteralSyntax("deploy")
//    - ParameterSyntax(name: "env", description: "Description")
//    - OptionSyntax(long: "force", short: "f", description: "Skip confirmation")
// 3. Convert each to SegmentDefinition subtype
```

### HandlerExtractor
Analyzes handler expressions:
```csharp
// Lambda: (string env, bool force) => Deploy(env, force)
// Method group: HandleDeploy
// Returns: HandlerDefinition with parameters, return type, async info
```

### AttributedRouteExtractor
For `[NuruRoute]` classes:
```csharp
// 1. Read pattern from [NuruRoute("pattern")]
// 2. Check base class for [NuruRouteGroup] → prefix
// 3. Check interface for message type (IQuery<T>, ICommand<T>)
// 4. Find [Parameter] and [Option] properties
// 5. Find nested Handler class
// 6. Build RouteDefinition
```

## Output Structure

After Phase 3, extractors produce:
```
AppModel
├── Name, Description, AiPrompt
├── HasHelp, HelpOptions
├── HasRepl, ReplOptions
├── HasConfiguration
├── Routes[] ← from FluentChainExtractor + AttributedRouteExtractor
│   └── RouteDefinition
│       ├── OriginalPattern
│       ├── Segments[]
│       ├── Handler
│       ├── Description
│       ├── MessageType
│       └── Aliases[]
├── Behaviors[]
├── Services[]
└── InterceptSite
```

## Testing Strategy

After extractors are created, verify with:
1. Unit tests for each extractor (Phase 6)
2. Manual inspection of extracted models
3. Build verification

## Notes from Previous Implementation

The reference-only extractors show patterns for:
- Walking invocation chains (`ExtractFromMapChain`)
- Handling method group expressions
- Extracting attribute arguments
- Building route definitions incrementally

Key learning: Start simple with literal patterns, then add mini-language support.

## Results

**Completed:** 2024-12-25

### Files Created (7 extractors)

1. `generators/extractors/app-extractor.cs` (176 lines)
   - Main orchestrator coordinating all extraction
   - Uses RunAsyncLocator to find entry point
   - Traces back through syntax tree to find builder chain
   - Coordinates FluentChainExtractor and AttributedRouteExtractor

2. `generators/extractors/fluent-chain-extractor.cs` (273 lines)
   - Walks builder chain from Build() to CreateBuilder()
   - Extracts routes from .Map() calls
   - Handles app-level settings (name, description, help, repl, etc.)
   - Tracks WithGroupPrefix scope for route prefixes

3. `generators/extractors/pattern-string-extractor.cs` (224 lines)
   - Integrates with existing PatternParser from timewarp-nuru-parsing
   - Converts Syntax nodes to SegmentDefinition subtypes
   - Resolves type constraints to CLR type names
   - Builds parameter bindings from segments

4. `generators/extractors/handler-extractor.cs` (393 lines)
   - Analyzes lambda expressions and method references
   - Extracts parameter types and bindings
   - Determines return type and async status
   - Detects service injection and CancellationToken

5. `generators/extractors/intercept-site-extractor.cs` (139 lines)
   - Extracts file/line/column for [InterceptsLocation]
   - Gets method name location from invocation
   - Formats output for code generation

6. `generators/extractors/attributed-route-extractor.cs` (492 lines)
   - Extracts from [NuruRoute] decorated classes
   - Gets group prefix from [NuruRouteGroup] on base class
   - Infers message type from IQuery/ICommand interfaces
   - Extracts [Parameter] and [Option] properties
   - Creates Mediator handler definitions

7. `generators/extractors/service-extractor.cs` (246 lines)
   - Extracts from ConfigureServices() calls
   - Handles AddTransient, AddScoped, AddSingleton
   - Supports generic and typeof() syntax
   - Analyzes lambda and block bodies

### Also Modified

- `generators/locators/build-locator.cs` - Added IsConfirmedBuildCall() method

### Key Design Decisions

1. **Type alias pattern** - Used `RoslynParameterSyntax` alias to avoid namespace conflict with `TimeWarp.Nuru.ParameterSyntax`

2. **Fallback to delegates** - When semantic model can't resolve method groups, create delegate handlers instead of methods to avoid null type names

3. **Pattern parser reuse** - Successfully integrated existing PatternParser as a source-only dependency within the analyzer project

### Next Steps

- Phase 4: Emitters (generate C# code from AppModel)
- Phase 6: Unit tests for extractors
