# Support Alternative Option-Value Separators

## Superseded

> **This task has been superseded by [Task 116: Pluggable Parsing Schemas for CLI Interception](../backlog/116-pluggable-parsing-schemas-for-cli-interception.md).**
>
> Analysis revealed that incrementally adding separators to the existing parser creates fundamental conflicts with configuration override filtering (Issue #77). The architecturally correct solution is pluggable parsing schemas where each schema (Nuru, POSIX, MSBuild) encapsulates its own lexer, option matcher, and rules.
>
> See analysis: `.agent/workspace/2025-12-04T14-45-00_task-023-revised-architecture-recommendation.md`

---

## Original Goal

Extend TimeWarp.Nuru's option parsing to support alternative syntaxes commonly used by existing CLI applications, enabling better CLI interception capabilities for wrapping/extending third-party command-line tools.

## Why This Approach Was Rejected

Nuru serves two distinct purposes:

1. **Greenfield CLI Applications** - New apps using Nuru's native syntax
2. **CLI Interception** - Wrapping existing tools (MSBuild, npm, tar, etc.)

Trying to support all separator styles in one parser creates:

- **Conflicts**: Colon separator (`--option:value`) is indistinguishable from config overrides (`--Section:Key=value`)
- **Ambiguity**: Each separator adds edge cases
- **Complexity**: Testing and documentation become unwieldy

## Recommended Path Forward

Use Task 116's schema-based approach:

```csharp
// Greenfield app (default)
NuruApp.CreateBuilder(args)
  .Map("deploy --env {env}", handler)  // Uses --env value syntax
  .Build();

// Intercept dotnet CLI
NuruApp.CreateBuilder(args)
  .UseParsingSchema(ParsingSchema.MSBuild)
  .Map("build -p {properties}*", handler)  // Accepts -p:Configuration=Release
  .Build();

// Intercept npm/docker
NuruApp.CreateBuilder(args)
  .UseParsingSchema(ParsingSchema.Posix)
  .Map("install --registry {url}", handler)  // Accepts --registry=https://...
  .Build();
```

---

## Original Analysis (Preserved for Reference)

### Separators Considered

1. **Equals syntax** (`--option=value`) - npm, docker, kubectl
2. **Colon syntax** (`-p:Property=Value`) - MSBuild, dotnet CLI
3. **Concatenated short options** (`-xvf`) - tar, curl, Unix tools
4. **Windows-style** (`/option:value`) - MSBuild, Visual Studio

### Critical Conflict Identified

Configuration override filtering (Issue #77 fix) uses this heuristic:

```csharp
// Line 169, nuru-core-app.cs
string[] routeArgs = [.. args.Where(arg => 
  !(arg.StartsWith("--", StringComparison.Ordinal) && 
    arg.Contains(':', StringComparison.Ordinal)))];
```

This filters ALL `--*:*` patterns before routing. Supporting `--option:value` for routes would break this filtering, causing config overrides to fail.

## Related

- Superseded by: [Task 116](../backlog/116-pluggable-parsing-schemas-for-cli-interception.md)
- Issue #77: Arguments with colons should not be filtered
- Task #022: Configuration override implementation
