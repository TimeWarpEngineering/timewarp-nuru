# V2 Generator Phase 0: Reorganization

## Description

Reorganize the `timewarp-nuru-analyzers` project to establish the new folder structure for V2 generator implementation. Move existing code to appropriate locations and update namespaces.

## Parent

#265 Epic: V2 Source Generator Implementation

## Checklist

### Commit 0.1: Create folder structure
- [x] Create `generators/locators/`
- [x] Create `generators/extractors/builders/`
- [x] Create `generators/emitters/`
- [x] Create `generators/models/`
- [x] Create `reference-only/extractors/`
- [x] Create `reference-only/emitters/`
- [x] Create `reference-only/generators/`

### Commit 0.2: Move models to generators/models/
- [x] Move `models/route-definition.cs`
- [x] Move `models/segment-definition.cs`
- [x] Move `models/handler-definition.cs`
- [x] Move `models/parameter-binding.cs`
- [x] Move `models/pipeline-definition.cs`
- [x] Move `models/delegate-signature.cs`
- [x] Update namespace to `TimeWarp.Nuru.Generators`

### Commit 0.3: Move builders to generators/extractors/builders/
- [x] Move `analyzers/builders/handler-definition-builder.cs`
- [x] Move `analyzers/builders/route-definition-builder.cs`
- [x] Update namespace to `TimeWarp.Nuru.Generators`
- [x] Remove empty `analyzers/builders/` folder

### Commit 0.4: Move extractors to reference-only/
- [ ] Move `analyzers/extractors/fluent-chain-extractor.cs`
- [ ] Move `analyzers/extractors/fluent-route-builder-extractor.cs`
- [ ] Move `analyzers/extractors/mediator-route-extractor.cs`
- [ ] Move `analyzers/extractors/attributed-route-extractor.cs`
- [ ] Move `analyzers/extractors/delegate-analyzer.cs`
- [ ] Remove empty `analyzers/extractors/` folder

### Commit 0.5: Move emitters to reference-only/
- [ ] Move `analyzers/emitters/runtime-code-emitter.cs`
- [ ] Remove empty `analyzers/emitters/` folder

### Commit 0.6: Move old generators to reference-only/
- [ ] Move `analyzers/nuru-v2-generator.cs`
- [ ] Move `analyzers/nuru-attributed-route-generator/` folder
- [ ] Move `analyzers/nuru-delegate-command-generator/` folder
- [ ] Move `analyzers/nuru-invoker-generator/` folder
- [ ] Move `analyzers/generator-helpers.cs`

### Commit 0.7: Update global-usings and verify build
- [ ] Update `global-usings.cs` for new namespace
- [ ] Run `dotnet build` to verify no breakage
- [ ] Fix any compilation errors

## Notes

### Files to Keep in `analyzers/`
- `diagnostics/` folder
- `mediator-dependency-analyzer.cs`
- `nuru-handler-analyzer.cs`
- `nuru-route-analyzer.cs`

### Folder Structure After Completion
```
source/timewarp-nuru-analyzers/
├── analyzers/
│   ├── diagnostics/
│   └── *.cs (analyzers only)
├── generators/
│   ├── locators/
│   ├── extractors/builders/
│   ├── emitters/
│   └── models/
└── reference-only/
    ├── extractors/
    ├── emitters/
    └── generators/
```

### Namespace Change
- Old: `TimeWarp.Nuru.SourceGen`
- New: `TimeWarp.Nuru.Generators`
