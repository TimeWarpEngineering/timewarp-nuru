# TimeWarp.Nuru Samples DSL Analysis

**Executive Summary:**
The TimeWarp.Nuru samples currently favor the Fluent DSL (delegate-style `.Map()` calls) with 24 Fluent-only samples vs only 4 Endpoint-only samples. This imbalance confuses AI agents trying to understand the framework. The samples lack mirrored examples showing equivalent functionality in both DSL styles, and only 2 samples demonstrate mixing both approaches. Recommend restructuring to provide balanced DSL coverage with clear guidance toward the Endpoint DSL as the preferred pattern for production applications.

---

## Scope

This analysis covers all 50+ sample files in `/samples/` to:
1. Categorize each sample by DSL style (Fluent, Endpoint, or Mixed)
2. Identify gaps where mirrored examples are missing
3. Document which samples demonstrate mixing capabilities
4. Provide recommendations for sample restructuring

---

## Methodology

**Approach:**
- Read all `.cs` files in the `/samples/` directory tree
- Analyzed code patterns to identify DSL usage:
  - **Fluent DSL**: Uses `.Map("pattern").WithHandler(...).Done()` chain
  - **Endpoint DSL**: Uses `[NuruRoute]` attributes on classes with nested Handler classes
  - **Mixed**: Contains both `.Map()` calls AND `[NuruRoute]` classes
- Cross-referenced with `examples.json` manifest
- Examined README files for stated intentions

**DSL Identification Criteria:**
| Pattern | Fluent DSL | Endpoint DSL |
|---------|------------|--------------|
| Route Definition | `.Map("pattern")` | `[NuruRoute("pattern")]` |
| Handler | `.WithHandler(lambda)` | Nested `Handler` class |
| Discovery | Explicit builder calls | `.DiscoverEndpoints()` |
| Parameters | Route pattern syntax | `[Parameter]`, `[Option]` attributes |

---

## Current Sample Inventory

### Fluent DSL-Only Samples (24 files)

| Sample | Path | Description |
|--------|------|-------------|
| hello-world-lambda | `01-hello-world/01-hello-world-lambda.cs` | Basic lambda handler |
| hello-world-method | `01-hello-world/02-hello-world-method.cs` | Method reference handler |
| calc-delegate | `02-calculator/01-calc-delegate.cs` | Calculator with delegates |
| syntax-examples | `04-syntax-examples/syntax-examples.cs` | Route pattern syntax showcase |
| async-examples | `06-async-examples/async-examples.cs` | Async/await patterns |
| pipeline-middleware-basic | `07-pipeline-middleware/01-pipeline-middleware-basic.cs` | Basic pipeline behaviors |
| pipeline-middleware-exception | `07-pipeline-middleware/02-pipeline-middleware-exception.cs` | Exception handling |
| pipeline-middleware-telemetry | `07-pipeline-middleware/03-pipeline-middleware-telemetry.cs` | OpenTelemetry |
| pipeline-middleware-filtered-auth | `07-pipeline-middleware/04-pipeline-middleware-filtered-auth.cs` | Filtered behaviors |
| pipeline-middleware-retry | `07-pipeline-middleware/05-pipeline-middleware-retry.cs` | Retry patterns |
| pipeline-middleware-combined | `07-pipeline-middleware/06-pipeline-middleware-combined.cs` | Complete pipeline |
| test-output-capture | `08-testing/01-output-capture.cs` | Testing with TestTerminal |
| test-colored-output | `08-testing/02-colored-output.cs` | Colored output testing |
| test-terminal-injection | `08-testing/03-terminal-injection.cs` | ITerminal DI testing |
| configuration-basics | `09-configuration/01-configuration-basics.cs` | Configuration patterns |
| command-line-overrides | `09-configuration/02-command-line-overrides.cs` | CLI config overrides |
| configuration-validation | `09-configuration/03-configuration-validation.cs` | Validation patterns |
| user-secrets-property | `09-configuration/04-user-secrets-property.cs` | User secrets |
| builtin-types | `10-type-converters/01-builtin-types.cs` | Built-in type converters |
| custom-type-converters | `10-type-converters/02-custom-type-converters.cs` | Custom converters |
| repl-cli-dual-mode | `13-repl/01-repl-cli-dual-mode.cs` | CLI + REPL dual mode |
| repl-custom-keys | `13-repl/02-repl-custom-keys.cs` | Custom key bindings |
| repl-options | `13-repl/03-repl-options.cs` | REPL configuration |
| repl-complete | `13-repl/04-repl-complete.cs` | Complete REPL demo |
| completion-example | `15-completion/completion-example.cs` | Shell completion |
| runtime-di-basic | `16-runtime-di/01-runtime-di-basic.cs` | Runtime DI |
| runtime-di-logging | `16-runtime-di/02-runtime-di-logging.cs` | DI with logging |

**Total: 27 Fluent DSL-only samples**

### Endpoint DSL-Only Samples (4 files)

| Sample | Path | Description |
|--------|------|-------------|
| hello-world-endpoint | `01-hello-world/03-hello-world-endpoint.cs` | Basic endpoint pattern |
| endpoints | `03-endpoints/endpoints.cs` | Endpoint discovery showcase |
| goodbye-command | `03-endpoints/messages/commands/goodbye-command.cs` | Command example |
| set-config-command | `03-endpoints/messages/idempotent/set-config-command.cs` | Idempotent command |
| docker-tag-command | `03-endpoints/messages/docker/idempotent/docker-tag-command.cs` | Docker tag |
| docker-group-base | `03-endpoints/messages/docker/docker-group-base.cs` | Route groups |
| docker-ps-query | `03-endpoints/messages/docker/queries/docker-ps-query.cs` | Docker ps query |
| docker-run-command | `03-endpoints/messages/docker/commands/docker-run-command.cs` | Docker run |
| docker-build-command | `03-endpoints/messages/docker/commands/docker-build-command.cs` | Docker build |
| exec-command | `03-endpoints/messages/commands/exec-command.cs` | Exec command |
| deploy-command | `03-endpoints/messages/commands/deploy-command.cs` | Deploy command |
| default-query | `03-endpoints/messages/queries/default-query.cs` | Default query |
| greet-query | `03-endpoints/messages/queries/greet-query.cs` | Greet query |
| get-config-query | `03-endpoints/messages/queries/get-config-query.cs` | Get config |
| ping-request | `03-endpoints/messages/unspecified/ping-request.cs` | Unspecified type |
| nested-group-example | `03-endpoints/messages/nested-groups/nested-group-example.cs` | Nested groups |
| config-group-base | `03-endpoints/messages/config/config-group-base.cs` | Config group |

**Note:** While there are 17 files in `03-endpoints/`, only `endpoints.cs` is a runnable sample. The rest are message classes that support it.

**Total: 1 true Endpoint DSL-only sample + 16 supporting classes**

### Mixed DSL Samples (2 files)

| Sample | Path | Description |
|--------|------|-------------|
| calc-mixed | `02-calculator/03-calc-mixed.cs` | Mixes delegates + endpoints |
| unified-middleware | `11-unified-middleware/unified-middleware.cs` | Unified pipeline demo |

**Total: 2 Mixed samples**

---

## Gap Analysis

### Missing Mirrored Examples

The following Fluent DSL samples lack Endpoint DSL equivalents:

| Fluent Sample | Missing Endpoint Equivalent | Priority |
|---------------|----------------------------|----------|
| `01-hello-world-lambda.cs` | `01-hello-world-endpoint.cs` | ✅ EXISTS |
| `04-syntax-examples/syntax-examples.cs` | `04-syntax-examples/syntax-examples-endpoints.cs` | **HIGH** |
| `06-async-examples/async-examples.cs` | `06-async-examples/async-endpoints.cs` | **HIGH** |
| `07-pipeline-middleware/01-pipeline-middleware-basic.cs` | `07-pipeline-middleware/01-pipeline-middleware-endpoints.cs` | **HIGH** |
| `08-testing/01-output-capture.cs` | `08-testing/01-output-capture-endpoints.cs` | **MEDIUM** |
| `09-configuration/01-configuration-basics.cs` | `09-configuration/01-configuration-basics-endpoints.cs` | **MEDIUM** |
| `10-type-converters/01-builtin-types.cs` | `10-type-converters/01-builtin-types-endpoints.cs` | **MEDIUM** |
| `10-type-converters/02-custom-type-converters.cs` | `10-type-converters/02-custom-type-converters-endpoints.cs` | **MEDIUM** |
| `13-repl/01-repl-cli-dual-mode.cs` | `13-repl/01-repl-cli-dual-mode-endpoints.cs` | **MEDIUM** |
| `12-logging/console-logging.cs` | `12-logging/console-logging-endpoints.cs` | **LOW** |

### Missing Mixed Examples

Only 2 mixed examples exist. Recommended additions:

| Suggested Sample | Description |
|-----------------|-------------|
| `02-calculator/04-calc-advanced-mixed.cs` | Complex mixing patterns |
| `05-mixed-patterns/migration-guide.cs` | Shows migration path from Fluent to Endpoint |
| `05-mixed-patterns/dsl-chooser.cs` | Decision framework for choosing DSL |

---

## AI Agent Confusion Factors

### 1. Sample Volume Imbalance
- **27 Fluent samples** vs **1 Endpoint sample** (plus supporting classes)
- AI agents statistically favor the more prevalent pattern
- Creates impression that Fluent is the "primary" or "recommended" approach

### 2. Documentation Hierarchy
- `01-hello-world` starts with Fluent (lambdas, then methods, THEN endpoint)
- Sequential numbering implies progression: Fluent → Endpoint
- But Endpoint is actually the more robust pattern

### 3. `examples.json` Metadata
```json
{
  "id": "hello-world",
  "path": "samples/01-hello-world/01-hello-world-lambda.cs",  // Fluent first
  "tags": ["basics", "getting-started", "minimal"]
}
```
The `hello-world` ID points to the Fluent version, not the Endpoint version.

### 4. Feature Showcase Asymmetry
- **Syntax examples**: Only Fluent (`04-syntax-examples/syntax-examples.cs`)
- **Type converters**: Only Fluent
- **Configuration**: Only Fluent
- **Testing**: Only Fluent
- **Pipeline middleware**: Only Fluent (except unified-middleware mixed)

---

## Recommendations

### Immediate Actions (High Priority)

1. **Create Endpoint DSL equivalents for core samples:**
   ```
   samples/01-hello-world/04-hello-world-endpoint-advanced.cs
   samples/04-syntax-examples/syntax-examples-endpoints.cs
   samples/06-async-examples/async-endpoints.cs
   samples/07-pipeline-middleware/01-pipeline-middleware-endpoints.cs
   ```

2. **Restructure sample numbering to prioritize Endpoint:**
   ```
   01-hello-world/
     01-hello-world-endpoint.cs        # Endpoint FIRST (recommended)
     02-hello-world-lambda.cs          # Fluent alternative
     03-hello-world-method.cs          # Fluent method reference
   ```

3. **Update `examples.json` to favor Endpoint:**
   ```json
   {
     "id": "hello-world",
     "path": "samples/01-hello-world/01-hello-world-endpoint.cs",  // Endpoint
     "tags": ["basics", "getting-started", "recommended"]
   }
   ```

### Medium Priority

4. **Create side-by-side comparison samples:**
   ```
   samples/00-comparison/
     fluent-vs-endpoint-hello-world.md
     fluent-vs-endpoint-calculator.md
     when-to-use-each.md
   ```

5. **Add "Migration Path" sample:**
   ```
   samples/05-migration/
     01-starting-fluent.cs
     02-adding-first-endpoint.cs
     03-full-endpoint-conversion.cs
   ```

6. **Expand mixed examples:**
   ```
   samples/02-calculator/04-calc-why-mixed.cs  # Explains decision criteria
   samples/05-mixed-patterns/
     simple-plus-complex.cs    # Simple = Fluent, Complex = Endpoint
     performance-critical.cs   # When to keep Fluent
     testable-boundaries.cs  # Testing considerations
   ```

### Documentation Updates

7. **README.md updates:**
   - Add DSL choice guidance at top of each sample README
   - Include "Prefer Endpoint DSL for..." callouts
   - Show both patterns side-by-side where applicable

8. **Sample headers:**
   Add DSL identification comments:
   ```csharp
   // DSL STYLE: Endpoint DSL (Recommended for production)
   // DSL STYLE: Fluent DSL (Good for prototyping)
   // DSL STYLE: Mixed (Demonstrates interoperability)
   ```

---

## Sample Dependency Graph

```
samples/
├── 01-hello-world/                          [FLUENT-HEAVY → Balance needed]
│   ├── 01-hello-world-lambda.cs             [FLUENT]
│   ├── 02-hello-world-method.cs             [FLUENT]
│   └── 03-hello-world-endpoint.cs            [ENDPOINT ✓]
│
├── 02-calculator/                           [GOOD MIX ✓]
│   ├── 01-calc-delegate.cs                  [FLUENT]
│   ├── 02-calc-commands.cs                  [ENDPOINT ✓]
│   └── 03-calc-mixed.cs                     [MIXED ✓]
│
├── 03-endpoints/                            [ENDPOINT ONLY ✓]
│   ├── endpoints.cs                         [ENDPOINT]
│   └── messages/                            [Supporting classes]
│
├── 04-syntax-examples/                      [NEEDS ENDPOINT MIRROR]
│   └── syntax-examples.cs                   [FLUENT ONLY]
│
├── 06-async-examples/                       [NEEDS ENDPOINT MIRROR]
│   └── async-examples.cs                    [FLUENT ONLY]
│
├── 07-pipeline-middleware/                  [NEEDS ENDPOINT MIRRORS]
│   ├── 01-pipeline-middleware-basic.cs      [FLUENT]
│   ├── 02-pipeline-middleware-exception.cs  [FLUENT]
│   ├── 03-pipeline-middleware-telemetry.cs  [FLUENT]
│   ├── 04-pipeline-middleware-filtered-auth.cs [FLUENT]
│   ├── 05-pipeline-middleware-retry.cs      [FLUENT]
│   └── 06-pipeline-middleware-combined.cs   [FLUENT]
│
├── 08-testing/                              [NEEDS ENDPOINT MIRRORS]
│   ├── 01-output-capture.cs                   [FLUENT]
│   ├── 02-colored-output.cs                 [FLUENT]
│   └── 03-terminal-injection.cs             [FLUENT]
│
├── 09-configuration/                        [NEEDS ENDPOINT MIRRORS]
│   ├── 01-configuration-basics.cs             [FLUENT]
│   ├── 02-command-line-overrides.cs         [FLUENT]
│   ├── 03-configuration-validation.cs         [FLUENT]
│   └── 04-user-secrets-property.cs          [FLUENT]
│
├── 10-type-converters/                      [NEEDS ENDPOINT MIRRORS]
│   ├── 01-builtin-types.cs                    [FLUENT]
│   └── 02-custom-type-converters.cs         [FLUENT]
│
├── 11-unified-middleware/                   [MIXED ✓]
│   └── unified-middleware.cs                  [MIXED]
│
├── 12-logging/                              [NEEDS ENDPOINT MIRROR]
│   ├── console-logging.cs                     [FLUENT]
│   └── serilog-logging.cs                   [FLUENT]
│
├── 13-repl/                                 [NEEDS ENDPOINT MIRRORS]
│   ├── 01-repl-cli-dual-mode.cs               [FLUENT]
│   ├── 02-repl-custom-keys.cs               [FLUENT]
│   ├── 03-repl-options.cs                   [FLUENT]
│   └── 04-repl-complete.cs                  [FLUENT]
│
└── 16-runtime-di/                           [NEEDS ENDPOINT MIRRORS]
    ├── 01-runtime-di-basic.cs                 [FLUENT]
    └── 02-runtime-di-logging.cs             [FLUENT]
```

---

## Conclusion

The current sample structure inadvertently guides AI agents (and developers) toward the Fluent DSL by sheer volume (27:1 ratio). To properly guide users toward the Endpoint DSL as the preferred production pattern:

1. **Create 15+ Endpoint DSL mirrors** of existing Fluent samples
2. **Restructure numbering** to prioritize Endpoint samples
3. **Add 5+ new mixed examples** showing when/how to combine approaches
4. **Update metadata** in `examples.json` to favor Endpoint
5. **Add DSL choice guidance** to all README files

The unified middleware sample (`11-unified-middleware/unified-middleware.cs`) is an excellent model showing how both DSLs work together—it should be referenced as the canonical "mixed" pattern.

---

## References

- **Fluent DSL Pattern:** `.Map("pattern").WithHandler(...).AsCommand().Done()`
- **Endpoint DSL Pattern:** `[NuruRoute("pattern")]` with nested Handler classes
- **Mixed Pattern:** Both patterns in same application
- **Key Files:**
  - `samples/03-endpoints/endpoints.cs` - Endpoint showcase
  - `samples/02-calculator/03-calc-mixed.cs` - Mixed example
  - `samples/11-unified-middleware/unified-middleware.cs` - Unified pipeline demo
- **Documentation:**
  - `samples/01-hello-world/README.md`
  - `samples/02-calculator/README.md`
  - `samples/03-endpoints/README.md`
  - `samples/examples.json`

---

*Analysis Date: 2026-02-09*
*Analyst: AI Code Analysis Agent*
