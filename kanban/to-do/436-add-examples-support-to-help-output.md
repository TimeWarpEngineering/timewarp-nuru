# Add Examples support to help output

## Description

Add per-route and app-level Examples sections to CLI help output, matching and exceeding Spectre.Console.Cli's examples support. Examples should appear at the bottom of help output (after Options/Commands), following standard CLI conventions (`dotnet`, `git`, `docker`, `kubectl`, `gh`).

## Competitive Analysis

### What Spectre.Console.Cli Offers

- Per-command examples via `.WithExample("add", "todo.txt")` — each arg is a separate string
- Multiple examples per command — chain multiple `.WithExample()` calls
- Runtime validation via `config.ValidateExamples()` (DEBUG only, catches typos at startup)
- No app-level examples, no per-example descriptions, no attribute-based API

### Nuru Advantages (Proposed)

| Feature | Spectre | Nuru (proposed) |
|---|---|---|
| Per-command examples | Yes | Yes |
| Multiple examples | Yes | Yes |
| Example descriptions | No | **Yes** |
| Attribute-based (Endpoint DSL) | No (fluent only) | **Yes** |
| Validation | Runtime (DEBUG) | **Compile-time analyzer** |
| App-level examples section | No | **Yes** (aggregate in main help) |

Two big differentiators: **per-example descriptions** and **compile-time validation via analyzer diagnostics**.

## Design

### New Model

Add to `RouteDefinition` record:
```csharp
ImmutableArray<ExampleDefinition> Examples
```

New record:
```csharp
record ExampleDefinition(string Args, string? Description);
```

### Fluent DSL API

```csharp
.Map("deploy {env} --force")
  .WithHandler(...)
  .WithDescription("Deploy to an environment")
  .WithExample("deploy prod", "Deploy to production")
  .WithExample("deploy staging --force", "Force deploy to staging")
  .AsCommand().Done()
```

### Endpoint DSL API

```csharp
[NuruRoute("deploy", Description = "Deploy to an environment")]
[Example("deploy prod", Description = "Deploy to production")]
[Example("deploy staging --force", Description = "Force deploy to staging")]
public sealed class DeployCommand : ICommand<Unit> { ... }
```

### Help Output (per-route)

```
deploy {env} [--force,-f]
  Deploy to an environment

Parameters:
  ...

Options:
  ...

Examples:
  deploy prod              Deploy to production
  deploy staging --force   Force deploy to staging
```

### Help Output (app-level)

Examples section appears after Commands section, showing a curated selection or all route examples.

### Compile-Time Validation

Since the source generator already parses route patterns and knows parameters/options, it can validate that each example string actually matches the route pattern and report a diagnostic if it doesn't. This is strictly better than Spectre's runtime `ValidateExamples()`.

## Files to Modify

| Component | File | Change |
|---|---|---|
| Route Model | `source/timewarp-nuru-analyzers/generators/models/route-definition.cs` | Add `Examples` property |
| New Model | `source/timewarp-nuru-analyzers/generators/models/` | Add `ExampleDefinition` record |
| Fluent Builder | `source/timewarp-nuru/builders/endpoint-builder.cs` | Add `WithExample()` method |
| Endpoint Attribute | `source/timewarp-nuru/attributes/` | Add `[Example]` attribute |
| Fluent Locator | `source/timewarp-nuru-analyzers/generators/locators/` | New `with-example-locator.cs` |
| Attribute Locator | `source/timewarp-nuru-analyzers/generators/locators/` | Discover `[Example]` attributes |
| IR Builder | `source/timewarp-nuru-analyzers/generators/ir-builders/ir-app-builder.cs` | Accumulate examples |
| App Help Emitter | `source/timewarp-nuru-analyzers/generators/emitters/help-emitter.cs` | Emit app-level Examples section |
| Route Help Emitter | `source/timewarp-nuru-analyzers/generators/emitters/route-help-emitter.cs` | Emit per-route Examples section |
| Analyzer (optional) | `source/timewarp-nuru-analyzers/` | Compile-time example validation diagnostic |

## Checklist

- [ ] Add `ExampleDefinition` record model
- [ ] Add `Examples` to `RouteDefinition`
- [ ] Add `WithExample()` to `EndpointBuilder` and `GroupEndpointBuilder` (Fluent DSL)
- [ ] Add `[Example]` attribute (Endpoint DSL)
- [ ] Create `with-example-locator.cs` for Fluent DSL
- [ ] Update attribute locator for `[Example]` attribute
- [ ] Update IR builder to accumulate examples
- [ ] Update `route-help-emitter.cs` to emit per-route Examples section
- [ ] Update `help-emitter.cs` to emit app-level Examples section
- [ ] Add compile-time validation analyzer for examples against route patterns
- [ ] Add tests for Fluent DSL examples
- [ ] Add tests for Endpoint DSL examples
- [ ] Add tests for help output with examples
- [ ] Verify CI passes

## Notes

- Examples appear at the bottom of help, after all other sections — standard CLI convention
- `ExampleDefinition.Description` is optional — supports both `WithExample("deploy prod")` and `WithExample("deploy prod", "Deploy to production")`
- Compile-time validation can be a separate analyzer diagnostic (e.g., `NURU_H006`) added as a follow-up
- Related to #434 (review help-model.cs) — the `HelpModel` dead code should be cleaned up as part of broader help improvements
