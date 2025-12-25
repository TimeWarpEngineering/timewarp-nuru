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
- [x] Move `analyzers/extractors/fluent-chain-extractor.cs`
- [x] Move `analyzers/extractors/fluent-route-builder-extractor.cs`
- [x] Move `analyzers/extractors/mediator-route-extractor.cs`
- [x] Move `analyzers/extractors/attributed-route-extractor.cs`
- [x] Move `analyzers/extractors/delegate-analyzer.cs`
- [x] Remove empty `analyzers/extractors/` folder

### Commit 0.5: Move emitters to reference-only/
- [x] Move `analyzers/emitters/runtime-code-emitter.cs`
- [x] Remove empty `analyzers/emitters/` folder

### Commit 0.6: Move old generators to reference-only/
- [x] Move `analyzers/nuru-v2-generator.cs`
- [x] Move `analyzers/nuru-attributed-route-generator/` folder
- [x] Move `analyzers/nuru-delegate-command-generator/` folder
- [x] Move `analyzers/nuru-invoker-generator/` folder
- [x] Move `analyzers/generator-helpers.cs`

### Commit 0.7: Update global-usings and verify build
- [x] Update `global-usings.cs` for new namespace
- [x] Run `dotnet build` to verify no breakage
- [x] Fix any compilation errors (none needed)

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

## Results

Phase 0 reorganization completed successfully with 7 commits:

1. **Commit 0.1:** Created new folder structure (`generators/` and `reference-only/`)
2. **Commit 0.2:** Moved 6 model files to `generators/models/`, updated namespace to `TimeWarp.Nuru.Generators`
3. **Commit 0.3:** Moved 2 builder files to `generators/extractors/builders/`, updated namespace
4. **Commit 0.4:** Moved 5 extractor files to `reference-only/extractors/`
5. **Commit 0.5:** Moved 1 emitter file to `reference-only/emitters/`
6. **Commit 0.6:** Moved all old generators to `reference-only/generators/` (including 3 generator folders)
7. **Commit 0.7:** Updated `global-usings.cs` with new namespace, verified build succeeds

### Final Structure
```
source/timewarp-nuru-analyzers/
├── analyzers/
│   ├── diagnostics/        # 6 files
│   └── *.cs                 # 3 analyzer files
├── generators/
│   ├── locators/           # Empty (Phase 2)
│   ├── extractors/builders/# 2 files
│   ├── emitters/           # Empty (Phase 4)
│   └── models/             # 6 files
└── reference-only/
    ├── extractors/         # 5 files
    ├── emitters/           # 1 file
    └── generators/         # 2 files + 3 folders
```

### Build Status
- Full solution builds with 0 warnings, 0 errors
- All packages created successfully
