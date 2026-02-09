# Reorganize samples into DSL-first structure (fluent/, endpoints/, hybrid/)

## Description

Reorganize `/samples/` from feature-based folders to DSL-first folders to eliminate AI confusion about which DSL pattern samples use. The new structure mirrors ASP.NET's Minimal APIs vs Controllers paradigm where developers choose one paradigm and commit to it.

**New Structure:**
```
samples/
├── fluent/          # Complete Fluent DSL experience (~25 samples)
├── endpoints/       # Complete Endpoint DSL experience (~25 samples)
└── hybrid/          # Edge cases: migration, unified pipeline (~3-5 samples)
```

## Requirements

- AI agents must identify DSL from path alone (no content parsing)
- Both `fluent/` and `endpoints/` must have complete learning paths
- `hybrid/` remains small by design (mixing DSLs is rare)
- All existing samples must be migrated with DSL suffixes in filenames

## Checklist

### Phase 1: Create folder structure and migrate Fluent samples
- [x] Create `fluent/` folder with subfolders:
  - [x] `01-hello-world/`
  - [x] `02-calculator/`
  - [x] `03-syntax/`
  - [x] `04-async/`
  - [x] `05-pipeline/`
  - [x] `06-testing/`
  - [x] `07-configuration/`
  - [x] `08-type-converters/`
  - [x] `09-repl/`
  - [x] `10-logging/`
  - [x] `11-completion/`
  - [x] `12-runtime-di/`
- [x] Migrate existing Fluent samples to new locations with DSL suffixes:
  - [x] `01-hello-world/01-lambda.cs` (from `01-hello-world-lambda.cs`)
  - [x] `01-hello-world/02-method-reference.cs` (from `02-hello-world-method.cs`)
  - [x] `02-calculator/calculator.cs` (from `01-calc-delegate.cs`)
  - [x] `03-syntax/syntax-examples.cs` (from `syntax-examples.cs`)
  - [x] `04-async/async-examples.cs` (from `async-examples.cs`)
  - [ ] All pipeline middleware samples (6 files)
  - [ ] All testing samples (3 files)
  - [ ] All configuration samples (4 files)
  - [ ] All type converter samples (2 files)
  - [ ] All REPL samples (4 files)
  - [ ] All logging samples (2 files)
  - [ ] Completion sample
  - [ ] Runtime DI samples (2 files)

### Phase 2: Create Endpoint DSL mirrors
- [ ] Create `endpoints/` folder with identical subfolder structure
- [ ] Create Endpoint versions of all Fluent samples:
  - [x] `01-hello-world/hello-world.cs`
  - [x] `02-calculator/calculator.cs`
  - [x] `03-syntax/syntax-examples.cs`
  - [x] `04-async/async-examples.cs`
  - [ ] All pipeline middleware samples (6 files)
  - [ ] All testing samples (3 files)
  - [ ] All configuration samples (4 files)
  - [ ] All type converter samples (2 files)
  - [ ] All REPL samples (4 files)
  - [ ] All logging samples (2 files)
  - [ ] Completion sample
  - [ ] Runtime DI samples (2 files)
- [ ] Migrate existing Endpoint-specific content:
  - [ ] `11-discovery/01-basic-discovery.cs` (from `03-endpoints/endpoints.cs`)
  - [ ] `11-discovery/02-route-groups.cs` (from message files)
  - [ ] `11-discovery/03-aliases.cs`
  - [ ] `11-discovery/04-nested-groups.cs`
  - [ ] Move `messages/` folder to `endpoints/11-discovery/`

### Phase 3: Create hybrid folder (edge cases)
- [ ] Create `hybrid/` folder:
  - [ ] `01-migration/` - Fluent to Endpoint migration path
  - [ ] `02-unified-pipeline/` - Unified behavior demo
  - [ ] `03-when-to-mix/` - Decision guide
- [ ] Migrate existing mixed samples:
  - [ ] `02-unified-pipeline/unified-middleware.cs` (from `11-unified-middleware/`)
  - [ ] `01-migration/02-add-endpoint.cs` (from `02-calculator/03-calc-mixed.cs`)
- [ ] Create migration guide samples:
  - [ ] `01-migration/01-start-fluent.cs`
  - [ ] `01-migration/02-add-endpoint.cs`
  - [ ] `01-migration/03-complete-conversion.cs`

### Phase 4: Documentation updates
- [ ] Create root `samples/README.md` with DSL choice guide
- [ ] Update `examples.json` with `dsl` field for programmatic filtering
- [ ] Update all folder READMEs with DSL badges
- [ ] Add cross-reference links (e.g., "See also: endpoints/03-syntax/ for Endpoint version")
- [ ] Update `.agent/workspace/` documentation

### Phase 5: Cleanup
- [ ] Remove old `samples/01-hello-world/` folder (contents moved)
- [ ] Remove old `samples/02-calculator/` folder (contents moved)
- [ ] Remove old `samples/03-endpoints/` folder (contents moved)
- [ ] Remove old `samples/04-syntax-examples/` folder (contents moved)
- [ ] Remove old `samples/06-async-examples/` folder (contents moved)
- [ ] Remove old `samples/07-pipeline-middleware/` folder (contents moved)
- [ ] Remove old `samples/08-testing/` folder (contents moved)
- [ ] Remove old `samples/09-configuration/` folder (contents moved)
- [ ] Remove old `samples/10-type-converters/` folder (contents moved)
- [ ] Remove old `samples/11-unified-middleware/` folder (contents moved)
- [ ] Remove old `samples/12-logging/` folder (contents moved)
- [ ] Remove old `samples/13-repl/` folder (contents moved)
- [ ] Remove old `samples/15-completion/` folder (contents moved)
- [ ] Remove old `samples/16-runtime-di/` folder (contents moved)

## Notes

### Philosophy
Developers typically pick ONE DSL paradigm and commit to it, similar to ASP.NET Minimal APIs vs Controllers. Mixing should be rare (migration scenarios, edge cases).

### Files to Reference
- **Proposal Document:** `.agent/workspace/2026-02-09T00-00-00_samples-dsl-first-reorganization.md`
- **Current Structure Analysis:** `.agent/workspace/2026-02-09T00-00-00_samples-dsl-analysis.md`
- **Root README Template:** See proposal document Section "Decision Guide for Users"
- **examples.json Template:** See proposal document Section "examples.json Update"

### Naming Convention
- Fluent samples: `-fluent-` suffix in filename (e.g., `hello-world-fluent.cs`)
- Endpoint samples: `-endpoint` suffix in filename (e.g., `hello-world-endpoint.cs`)
- Hybrid samples: `-hybrid` suffix in filename (e.g., `migration-hybrid.cs`)

### Backward Compatibility
- Consider keeping symlinks or redirects for old paths
- Update any external links/documentation references

## Related Documentation

- **ASP.NET Analogy:** ASP.NET Minimal APIs (fluent) vs Controllers (endpoints)
- **Sample Philosophy:** Clean separation improves AI understanding
- **Previous Analysis:** Samples DSL analysis document

## Implementation Plan

### Naming Conventions
- **Fluent samples:** `fluent-{feature}-{variant}.cs` (e.g., `fluent-hello-world-lambda.cs`)
- **Endpoint samples:** `endpoint-{feature}-{variant}.cs` (e.g., `endpoint-hello-world.cs`)
- **Hybrid samples:** `hybrid-{feature}-{variant}.cs` (e.g., `hybrid-migration-add-endpoint.cs`)

### Final Structure
```
samples/
├── fluent/
│   ├── 01-hello-world/
│   ├── 02-calculator/
│   ├── 03-syntax/
│   ├── 04-async/
│   ├── 05-pipeline/
│   ├── 06-testing/
│   ├── 07-configuration/
│   ├── 08-type-converters/
│   ├── 09-repl/
│   ├── 10-logging/
│   ├── 11-completion/
│   ├── 12-runtime-di/
│   └── 13-aot-example/
├── endpoints/
│   ├── 01-hello-world/
│   ├── 02-calculator/           # Complex: has Directory.Build.props + messages/
│   ├── 03-syntax/
│   ├── 04-async/
│   ├── 05-pipeline/
│   ├── 06-testing/
│   ├── 07-configuration/
│   ├── 08-type-converters/
│   ├── 09-repl/
│   ├── 10-logging/
│   ├── 11-discovery/
│   ├── 12-completion/
│   ├── 13-runtime-di/
│   ├── 14-aspire-otel/
│   └── 99-endpoint-sample/      # csproj example
└── hybrid/
    ├── 01-migration/
    ├── 02-unified-pipeline/
    └── 03-when-to-mix/
```

### Phase Breakdown (6 Commits)

**Commit 1:** Create `fluent/` structure and migrate all Fluent samples (27 files)
**Commit 2:** Create `endpoints/` structure and migrate existing endpoint content
**Commit 3:** Create Endpoint mirrors for all Fluent samples that don't exist (15+ new files)
**Commit 4:** Create `hybrid/` structure and migrate existing hybrid samples
**Commit 5:** Documentation updates (READMEs, examples.json)
**Commit 6:** Cleanup old folders and verify

### Key Decisions
1. **Single PR with multiple commits** (not multiple PRs)
2. **Keep numbered subfolders** (01-hello-world/ pattern)
3. **DSL prefix in filenames** (fluent-, endpoint-, hybrid-)
4. **Directory.Build.props for complex endpoint samples** (02-calculator/, 99-endpoint-sample/)
5. **Preserve existing functionality** - all samples must still build and run

### README Templates

**fluent/README.md:** DSL badge "Fluent DSL", "When to Use", sample table
**endpoints/README.md:** DSL badge "Endpoint DSL ⭐ RECOMMENDED", "When to Use", complexity indicators
**hybrid/README.md:** DSL badge "Hybrid ⚠️", warning about mixing, edge case descriptions

### Testing Strategy
```bash
# Validate all fluent samples
for f in samples/fluent/**/fluent-*.cs; do dotnet run "$f" --help; done

# Validate endpoint samples
cd samples/endpoints/02-calculator && dotnet run -- --help
cd samples/endpoints/99-endpoint-sample && dotnet run -- --help
```
