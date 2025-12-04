# Pluggable Parsing Schemas for CLI Interception

## Description

Implement a pluggable parsing schema architecture to cleanly support Nuru's two distinct use cases:

1. **Greenfield CLI Applications** - Nuru's native POSIX-style syntax (`--option value`)
2. **CLI Interception/Gradual Rewrite** - Match existing tool syntax (MSBuild, npm, tar, etc.)

The current single-parser approach cannot cleanly support alternative option syntaxes (equals separators, colon separators, concatenated short options) without creating conflicts with configuration override filtering and accumulating ambiguity.

## Background

This task supersedes Task 023 ("Support Alternative Option-Value Separators"). Analysis revealed that incrementally adding separators to the existing parser creates fundamental conflicts:

- Colon separator (`--option:value`) conflicts with config overrides (`--Section:Key=value`)
- Concatenated options (`-xvf`) require knowing which options are boolean
- Each separator adds edge cases and testing complexity

The architecturally correct solution is schema-based parsing where each schema encapsulates its own lexer, option matcher, and rules.

## Proposed Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      NuruAppBuilder                          │
├─────────────────────────────────────────────────────────────┤
│  .UseParsingSchema(ParsingSchema.Nuru)      // Default      │
│  .UseParsingSchema(ParsingSchema.MSBuild)   // -p:Key=Value │
│  .UseParsingSchema(ParsingSchema.Posix)     // -xvf, --x=v  │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    IParsingSchema                            │
├─────────────────────────────────────────────────────────────┤
│  ILexer CreateLexer(string pattern)                         │
│  IArgumentNormalizer CreateNormalizer()                     │
│  IOptionMatcher CreateOptionMatcher(OptionSyntax syntax)    │
│  bool SupportsConfigOverrides { get; }                      │
└─────────────────────────────────────────────────────────────┘
```

## Target Schemas

### NuruSchema (Default)
- Options: `--option value`, `-x value`
- Config Overrides: Supported (`--Section:Key=value`)
- Use case: Greenfield Nuru applications

### PosixSchema
- Options: `--option=value`, `-x=value`, `-xvf` grouped
- Config Overrides: Supported (no colon conflict)
- Use case: Unix tool interception (tar, curl, docker, npm)

### MSBuildSchema
- Options: `-p:Property=Value`, `/p:Property=Value`
- Config Overrides: Disabled (syntax conflict)
- Use case: dotnet CLI interception

## Design Questions to Resolve

1. **Schema Selection Granularity**: Global per-app or per-route?
   ```csharp
   // Global
   .UseParsingSchema(ParsingSchema.MSBuild)
   
   // Per-route
   .Map("build {*args}", handler, schema: ParsingSchema.MSBuild)
   ```

2. **Schema Composition**: Can schemas be combined or extended?

3. **Default Schema**: Explicit or implicit?

## Requirements

- Existing Nuru applications continue working unchanged (backward compatible)
- Each schema is self-contained with its own lexer/parser
- Clear documentation: "Use X schema to wrap Y tools"
- No conflicts between schemas (isolated by design)

## Checklist

### Design
- [ ] Define `IParsingSchema` interface
- [ ] Design schema registration API in builder
- [ ] Decide on per-app vs per-route schema selection
- [ ] Document schema capabilities matrix

### Implementation
- [ ] Extract current parsing as `NuruSchema` (refactor, no behavior change)
- [ ] Implement `PosixSchema` (equals separator, grouped short options)
- [ ] Implement `MSBuildSchema` (colon separator, forward slash options)
- [ ] Add schema parameter to relevant builder methods

### Documentation
- [ ] Update user guide with schema selection guidance
- [ ] Create "CLI Interception" documentation section
- [ ] Document trade-offs (e.g., MSBuild schema disables config overrides)

## Related

- Supersedes: `kanban/to-do/023-support-alternative-option-value-separators.md`
- Analysis: `.agent/workspace/2025-12-04T14-45-00_task-023-revised-architecture-recommendation.md`
- Config Override Task: `kanban/done/022-support-command-line-configuration-overrides/`

## Notes

This is a significant architectural change. Consider implementing in phases:
1. Define interface and refactor current code as `NuruSchema`
2. Add `PosixSchema` (highest demand for npm/docker interception)
3. Add `MSBuildSchema` if demand exists
