# TimeWarp.Nuru Samples DSL-First Reorganization Proposal

**Executive Summary:**
Reorganize samples into DSL-first folders: `samples/fluent/`, `samples/endpoints/`, and `samples/hybrid/`. This mirrors the ASP.NET paradigm where developers choose Minimal APIs OR Controllers, not both. The hybrid folder becomes the exception for edge cases (migration scenarios, unified pipeline demonstrations). This eliminates AI confusion by making DSL choice explicit in the path structure.

---

## Philosophy: DSL as Primary Axis of Organization

### The ASP.NET Analogy

Just as ASP.NET offers:
- **Minimal APIs** (inline, lambda-based, quick scripts)
- **Controllers/Endpoints** (class-based, testable, production apps)

TimeWarp.Nuru offers:
- **Fluent DSL** (`.Map().WithHandler()`) - Quick scripting, performance
- **Endpoint DSL** (`[NuruRoute]`) - Production apps, testability, DI

**Key Insight:** Developers don't build apps that arbitrarily mix Minimal APIs and Controllers. They pick a paradigm and commit to it.

### Why DSL-First Organization?

| Current (Feature-First) | Proposed (DSL-First) |
|------------------------|----------------------|
| `01-hello-world/` has mixed DSLs | `fluent/01-hello-world/` is pure Fluent |
| AI must parse file contents | AI knows DSL from path |
| "Which sample should I follow?" | "I use Endpoint DSL, I'll check `endpoints/`" |
| Confusing side-by-side in same folder | Clear separation of concerns |

---

## Proposed Structure

```
samples/
├── fluent/                           # Complete Fluent DSL experience
│   ├── 01-hello-world/
│   │   ├── 01-lambda.cs
│   │   └── 02-method-reference.cs
│   │
│   ├── 02-calculator/
│   │   └── calculator.cs
│   │
│   ├── 03-syntax/
│   │   └── syntax-examples.cs
│   │
│   ├── 04-async/
│   │   └── async-examples.cs
│   │
│   ├── 05-pipeline/
│   │   ├── 01-basic.cs
│   │   ├── 02-exception.cs
│   │   ├── 03-telemetry.cs
│   │   ├── 04-filtered-auth.cs
│   │   ├── 05-retry.cs
│   │   └── 06-combined.cs
│   │
│   ├── 06-testing/
│   │   ├── 01-output-capture.cs
│   │   ├── 02-colored-output.cs
│   │   └── 03-terminal-injection.cs
│   │
│   ├── 07-configuration/
│   │   ├── 01-basics.cs
│   │   ├── 02-command-line-overrides.cs
│   │   ├── 03-validation.cs
│   │   └── 04-user-secrets.cs
│   │
│   ├── 08-type-converters/
│   │   ├── 01-builtin-types.cs
│   │   └── 02-custom-types.cs
│   │
│   ├── 09-repl/
│   │   ├── 01-dual-mode.cs
│   │   ├── 02-custom-keys.cs
│   │   ├── 03-options.cs
│   │   └── 04-complete.cs
│   │
│   ├── 10-logging/
│   │   ├── 01-console-logging.cs
│   │   └── 02-serilog.cs
│   │
│   ├── 11-completion/
│   │   └── completion.cs
│   │
│   └── 12-runtime-di/
│       ├── 01-basic.cs
│       └── 02-logging.cs
│
├── endpoints/                        # Complete Endpoint DSL experience
│   ├── 01-hello-world/
│   │   └── hello-world.cs
│   │
│   ├── 02-calculator/
│   │   └── calculator.cs
│   │
│   ├── 03-syntax/
│   │   └── syntax-examples.cs
│   │
│   ├── 04-async/
│   │   └── async-examples.cs
│   │
│   ├── 05-pipeline/
│   │   ├── 01-basic.cs
│   │   ├── 02-exception.cs
│   │   ├── 03-telemetry.cs
│   │   ├── 04-filtered-auth.cs
│   │   ├── 05-retry.cs
│   │   └── 06-combined.cs
│   │
│   ├── 06-testing/
│   │   ├── 01-output-capture.cs
│   │   ├── 02-colored-output.cs
│   │   └── 03-terminal-injection.cs
│   │
│   ├── 07-configuration/
│   │   ├── 01-basics.cs
│   │   ├── 02-command-line-overrides.cs
│   │   ├── 03-validation.cs
│   │   └── 04-user-secrets.cs
│   │
│   ├── 08-type-converters/
│   │   ├── 01-builtin-types.cs
│   │   └── 02-custom-types.cs
│   │
│   ├── 09-repl/
│   │   ├── 01-dual-mode.cs
│   │   ├── 02-custom-keys.cs
│   │   ├── 03-options.cs
│   │   └── 04-complete.cs
│   │
│   ├── 10-logging/
│   │   ├── 01-console-logging.cs
│   │   └── 02-serilog.cs
│   │
│   ├── 11-discovery/                 # Endpoint-specific feature
│   │   ├── 01-basic-discovery.cs
│   │   ├── 02-route-groups.cs
│   │   ├── 03-aliases.cs
│   │   └── 04-nested-groups.cs
│   │
│   ├── 12-completion/
│   │   └── completion.cs
│   │
│   └── 13-runtime-di/
│       ├── 01-basic.cs
│       └── 02-logging.cs
│
└── hybrid/                           # Edge cases - mixing DSLs (rare)
    ├── 01-migration/
    │   ├── 01-start-fluent.cs
    │   ├── 02-add-endpoint.cs
    │   └── 03-complete-conversion.cs
    │
    ├── 02-unified-pipeline/
    │   └── unified-middleware.cs
    │
    └── 03-when-to-mix/
        └── decision-guide.md
```

---

## Why This Structure Works

### 1. Clear Developer Journey

```
Developer: "I'm building a CLI tool, should I use Fluent or Endpoint?"

Option A: Quick script, performance critical
→ samples/fluent/01-hello-world/01-lambda.cs

Option B: Production app, needs testing, complex DI
→ samples/endpoints/01-hello-world/hello-world.cs

Option C: Existing Fluent app, gradually migrating
→ samples/hybrid/01-migration/02-add-endpoint.cs
```

### 2. AI Agent Clarity

**Before (Current):**
```python
# AI has to parse file to know DSL
if "[NuruRoute]" in file_content:
    dsl = "endpoint"
elif ".Map(" in file_content:
    dsl = "fluent"
```

**After (Proposed):**
```python
# AI knows DSL from path
if "samples/endpoints/" in file_path:
    dsl = "endpoint"
elif "samples/fluent/" in file_path:
    dsl = "fluent"
```

### 3. Parallel Learning Paths

Each DSL has a **complete, standalone learning path**:

| Step | Fluent Path | Endpoint Path |
|------|-------------|---------------|
| 1. Hello World | `fluent/01-hello-world/` | `endpoints/01-hello-world/` |
| 2. Real App | `fluent/02-calculator/` | `endpoints/02-calculator/` |
| 3. All Syntax | `fluent/03-syntax/` | `endpoints/03-syntax/` |
| 4. Async | `fluent/04-async/` | `endpoints/04-async/` |
| 5. Middleware | `fluent/05-pipeline/` | `endpoints/05-pipeline/` |
| 6. Testing | `fluent/06-testing/` | `endpoints/06-testing/` |
| 7. Config | `fluent/07-configuration/` | `endpoints/07-configuration/` |

### 4. Hybrid as Exception

The `hybrid/` folder is intentionally small because mixing DSLs should be rare:

```
hybrid/
├── 01-migration/              # Moving from Fluent → Endpoint
├── 02-unified-pipeline/       # Technical demo (same pipeline for both)
└── 03-when-to-mix/            # Decision guide (when is it justified?)
```

---

## Migration from Current Structure

### Current → Proposed Mapping

| Current File | Proposed Path | DSL |
|--------------|---------------|-----|
| `01-hello-world/01-hello-world-lambda.cs` | `fluent/01-hello-world/01-lambda.cs` | fluent |
| `01-hello-world/02-hello-world-method.cs` | `fluent/01-hello-world/02-method-reference.cs` | fluent |
| `01-hello-world/03-hello-world-endpoint.cs` | `endpoints/01-hello-world/hello-world.cs` | endpoint |
| `02-calculator/01-calc-delegate.cs` | `fluent/02-calculator/calculator.cs` | fluent |
| `02-calculator/02-calc-commands.cs` | `endpoints/02-calculator/calculator.cs` | endpoint |
| `02-calculator/03-calc-mixed.cs` | `hybrid/01-migration/02-add-endpoint.cs` | hybrid |
| `03-endpoints/endpoints.cs` | `endpoints/11-discovery/01-basic-discovery.cs` | endpoint |
| `04-syntax-examples/syntax-examples.cs` | `fluent/03-syntax/syntax-examples.cs` | fluent |
| `06-async-examples/async-examples.cs` | `fluent/04-async/async-examples.cs` | fluent |
| `07-pipeline-middleware/01-pipeline-middleware-basic.cs` | `fluent/05-pipeline/01-basic.cs` | fluent |
| `11-unified-middleware/unified-middleware.cs` | `hybrid/02-unified-pipeline/unified-middleware.cs` | hybrid |

### Phase 1: Create New Structure (Week 1)

```bash
# Create folder structure
mkdir -p samples/fluent/{01-hello-world,02-calculator,03-syntax,04-async,05-pipeline,06-testing,07-configuration,08-type-converters,09-repl,10-logging,11-completion,12-runtime-di}
mkdir -p samples/endpoints/{01-hello-world,02-calculator,03-syntax,04-async,05-pipeline,06-testing,07-configuration,08-type-converters,09-repl,10-logging,11-discovery,12-completion,13-runtime-di}
mkdir -p samples/hybrid/{01-migration,02-unified-pipeline,03-when-to-mix}

# Move existing Fluent samples
mv samples/01-hello-world/01-hello-world-lambda.cs samples/fluent/01-hello-world/01-lambda.cs
mv samples/01-hello-world/02-hello-world-method.cs samples/fluent/01-hello-world/02-method-reference.cs
mv samples/02-calculator/01-calc-delegate.cs samples/fluent/02-calculator/calculator.cs
# ... (all Fluent samples)

# Move existing Endpoint samples
mv samples/01-hello-world/03-hello-world-endpoint.cs samples/endpoints/01-hello-world/hello-world.cs
mv samples/02-calculator/02-calc-commands.cs samples/endpoints/02-calculator/calculator.cs
mv samples/03-endpoints/endpoints.cs samples/endpoints/11-discovery/01-basic-discovery.cs
mv samples/03-endpoints/messages/ samples/endpoints/11-discovery/messages/
# ... (all Endpoint samples)

# Move hybrid samples
mv samples/02-calculator/03-calc-mixed.cs samples/hybrid/01-migration/02-add-endpoint.cs
mv samples/11-unified-middleware/unified-middleware.cs samples/hybrid/02-unified-pipeline/
```

### Phase 2: Create Missing Endpoint Mirrors (Week 2-3)

Create these new samples to complete the Endpoint DSL experience:

```
samples/endpoints/
├── 03-syntax/syntax-examples.cs              # NEW
├── 04-async/async-examples.cs                # NEW
├── 05-pipeline/01-basic.cs                     # NEW
├── 05-pipeline/02-exception.cs                 # NEW
├── 05-pipeline/03-telemetry.cs               # NEW
├── 05-pipeline/04-filtered-auth.cs             # NEW
├── 05-pipeline/05-retry.cs                   # NEW
├── 05-pipeline/06-combined.cs                  # NEW
├── 06-testing/01-output-capture.cs             # NEW
├── 06-testing/02-colored-output.cs           # NEW
├── 06-testing/03-terminal-injection.cs       # NEW
├── 07-configuration/01-basics.cs             # NEW
├── 07-configuration/02-command-line-overrides.cs  # NEW
├── 07-configuration/03-validation.cs         # NEW
├── 07-configuration/04-user-secrets.cs         # NEW
├── 08-type-converters/01-builtin-types.cs      # NEW
├── 08-type-converters/02-custom-types.cs       # NEW
├── 09-repl/01-dual-mode.cs                   # NEW
├── 09-repl/02-custom-keys.cs                 # NEW
├── 09-repl/03-options.cs                     # NEW
├── 09-repl/04-complete.cs                    # NEW
├── 10-logging/01-console-logging.cs            # NEW
├── 10-logging/02-serilog.cs                  # NEW
├── 12-completion/completion.cs               # NEW
└── 13-runtime-di/
    ├── 01-basic.cs                           # NEW
    └── 02-logging.cs                         # NEW
```

### Phase 3: Documentation Updates (Week 4)

1. **Root README.md** - DSL choice guide at top level
2. **Individual READMEs** - Each subfolder explains the DSL choice
3. **examples.json** - Updated with DSL classification
4. **Cross-references** - "See also: endpoints/03-syntax/ for Endpoint DSL version"

---

## examples.json Update

```json
{
  "version": "3.0",
  "description": "TimeWarp.Nuru samples organized by DSL (fluent, endpoints, hybrid)",
  "structure": {
    "fluent": {
      "description": "Delegate-based Fluent DSL - quick scripts, performance",
      "path": "samples/fluent/"
    },
    "endpoints": {
      "description": "Class-based Endpoint DSL - production apps, testability",
      "path": "samples/endpoints/"
    },
    "hybrid": {
      "description": "Edge cases - migration, unified pipeline",
      "path": "samples/hybrid/"
    }
  },
  "examples": [
    {
      "id": "hello-world-lambda",
      "dsl": "fluent",
      "category": "hello-world",
      "path": "samples/fluent/01-hello-world/01-lambda.cs",
      "description": "Simplest Fluent DSL example with inline lambda",
      "tags": ["basics", "lambda", "getting-started"],
      "difficulty": "beginner",
      "endpointEquivalent": "samples/endpoints/01-hello-world/hello-world.cs"
    },
    {
      "id": "hello-world-endpoint",
      "dsl": "endpoints",
      "category": "hello-world",
      "path": "samples/endpoints/01-hello-world/hello-world.cs",
      "description": "Simplest Endpoint DSL example with [NuruRoute]",
      "tags": ["basics", "nuru-route", "getting-started", "recommended"],
      "difficulty": "beginner",
      "fluentEquivalent": "samples/fluent/01-hello-world/01-lambda.cs"
    },
    {
      "id": "migration-add-endpoint",
      "dsl": "hybrid",
      "category": "migration",
      "path": "samples/hybrid/01-migration/02-add-endpoint.cs",
      "description": "Adding first Endpoint to existing Fluent DSL app",
      "tags": ["migration", "mixed", "advanced"],
      "difficulty": "advanced",
      "prerequisites": ["hello-world-lambda", "hello-world-endpoint"]
    },
    {
      "id": "unified-pipeline",
      "dsl": "hybrid",
      "category": "pipeline",
      "path": "samples/hybrid/02-unified-pipeline/unified-middleware.cs",
      "description": "Behaviors apply to both Fluent and Endpoint routes",
      "tags": ["pipeline", "behaviors", "inuru-behavior", "mixed"],
      "difficulty": "intermediate"
    }
  ]
}
```

---

## Benefits Summary

| Benefit | Before | After |
|---------|--------|-------|
| **AI Clarity** | Parse file contents | Path-based identification |
| **Developer Clarity** | Mixed DSLs in folder | One DSL per folder |
| **Learning Path** | Unclear progression | Complete parallel paths |
| **Discoverability** | "Which sample?" | "I'm using X DSL" |
| **Documentation** | Hard to compare | Clear separation |
| **Maintenance** | Mixed concerns | Clean separation |

---

## Decision Guide for Users

Add to root `samples/README.md`:

```markdown
# TimeWarp.Nuru Samples

Choose your developer experience:

## 🚀 Quick Scripts → [fluent/](fluent/)

Use Fluent DSL when:
- Building quick CLI scripts
- Performance is critical (minimal overhead)
- No need for unit testing
- No complex dependency injection
- One-off tools and utilities

**Start here:** [fluent/01-hello-world/01-lambda.cs](fluent/01-hello-world/01-lambda.cs)

## 🏭 Production Apps → [endpoints/](endpoints/) ⭐ RECOMMENDED

Use Endpoint DSL when:
- Building maintainable production applications
- Need unit-testable handlers
- Complex dependency injection requirements
- Large command sets (auto-discovery)
- Team development with separation of concerns

**Start here:** [endpoints/01-hello-world/hello-world.cs](endpoints/01-hello-world/hello-world.cs)

## 🔀 Edge Cases → [hybrid/](hybrid/)

Consider hybrid when:
- Migrating existing Fluent app to Endpoint DSL
- Need to demonstrate unified pipeline behavior
- Rare cases requiring both patterns

**Note:** Most applications should NOT use hybrid. Pick one DSL and commit to it.

---

## Feature Comparison

| Feature | Fluent | Endpoint |
|---------|--------|----------|
| Quick prototyping | ✅ | ⚠️ |
| Performance | ✅ Maximum | ✅ Good |
| Unit testing | ⚠️ Limited | ✅ Full |
| Dependency injection | ⚠️ Basic | ✅ Advanced |
| Auto-discovery | ❌ | ✅ |
| Route groups | ⚠️ Manual | ✅ Declarative |
| AOT compatibility | ✅ | ✅ |
```

---

## Conclusion

**DSL-first organization** (`fluent/`, `endpoints/`, `hybrid/`) provides:

1. **Immediate clarity** - Path unambiguously indicates DSL
2. **Complete experiences** - Each folder has full learning path
3. **AI-friendly** - No content parsing required
4. **Human-friendly** - Clear guidance at entry point
5. **Correct mental model** - Developers pick one DSL, not both

The `hybrid/` folder remains small by design - mixing DSLs should be the exception, not the rule.

---

*Proposal Date: 2026-02-09*
*Status: Ready for Implementation*
