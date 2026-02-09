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

### Phase 1: Create folder structure and migrate Fluent samples ✅ COMPLETE
- [x] Create `fluent/` folder with subfolders (01-hello-world through 12-runtime-di)
- [x] Migrate existing Fluent samples to new locations with DSL suffixes (24 files total)

### Phase 2: Create Endpoint DSL mirrors ✅ COMPLETE
- [x] Create `endpoints/` folder with identical subfolder structure
- [x] Create Endpoint versions of all Fluent samples (24+ new files)
- [x] Migrate existing Endpoint-specific content:
  - [x] `11-discovery/endpoint-discovery-basic.cs` (from `03-endpoints/endpoints.cs`)
  - [x] Move `messages/` folder to `endpoints/11-discovery/`

### Phase 3: Create hybrid folder (edge cases) ✅ COMPLETE
- [x] Create `hybrid/` folder with migration and unified-pipeline samples
- [x] Create 3 migration steps (start-fluent, add-endpoint, complete)
- [x] Create unified-pipeline demonstration
- [x] Create decision guide documentation

### Phase 4: Documentation updates ✅ COMPLETE
- [x] Create root `samples/README.md` with DSL choice guide
- [x] Update `examples.json` with `dsl` field for programmatic filtering
- [x] Create `endpoints/README.md` with Endpoint DSL index
- [x] Create `hybrid/README.md` with warning

### Phase 5: Cleanup (DOCUMENTED - DO NOT DELETE YET)

**Old folders to be removed after verification:**

```
samples/01-hello-world/       → Moved to fluent/01-hello-world/
samples/02-calculator/        → Moved to fluent/02-calculator/ + endpoints/02-calculator/
samples/03-endpoints/         → Moved to endpoints/11-discovery/
samples/04-syntax-examples/  → Moved to fluent/03-syntax/ + endpoints/03-syntax/
samples/06-async-examples/    → Moved to fluent/04-async/ + endpoints/04-async/
samples/07-pipeline-middleware/ → Moved to fluent/05-pipeline/ + endpoints/05-pipeline/
samples/08-testing/           → Moved to fluent/06-testing/ + endpoints/06-testing/
samples/09-configuration/     → Moved to fluent/07-configuration/ + endpoints/07-configuration/
samples/10-type-converters/   → Moved to fluent/08-type-converters/ + endpoints/08-type-converters/
samples/11-unified-middleware/ → Moved to hybrid/02-unified-pipeline/
samples/12-logging/           → Moved to fluent/09-logging/ + endpoints/10-logging/
samples/13-repl/              → Moved to fluent/10-repl/ + endpoints/09-repl/
samples/15-completion/        → Moved to fluent/11-completion/ + endpoints/12-completion/
samples/16-runtime-di/        → Moved to fluent/12-runtime-di/ + endpoints/13-runtime-di/
```

**⚠️ IMPORTANT:** Do not delete these folders yet. Verify all new samples work correctly before cleanup.

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
