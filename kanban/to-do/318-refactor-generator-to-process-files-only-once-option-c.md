# Refactor Generator to Process Files Only Once (Option C)

## Description

The current generator architecture processes the source file **once per RunAsync call**, leading to N² work for N calls. This causes duplicate intercept sites that require deduplication (currently implemented via `DistinctBy`).

### Current Flow (Inefficient)
1. Generator finds N `RunAsync` calls in source file
2. For **each** `RunAsync` call, calls `AppExtractor.Extract`
3. **Each** `Extract` creates a new `DslInterpreter` and processes the **entire** file
4. Each `AppModel` contains intercept sites for **all** N `RunAsync` calls
5. `CombineModels` receives N AppModels with N intercept sites each (N² total)
6. Deduplication removes duplicates, leaving N unique sites

### Desired Flow (Efficient)
1. Generator processes the source file **once**
2. Extracts all routes, apps, and intercept sites in a single pass
3. Produces one `AppModel` with N unique intercept sites directly

### Key Files
- `source/timewarp-nuru-analyzers/generators/nuru-generator.cs` - Main generator pipeline
- `source/timewarp-nuru-analyzers/generators/extractors/app-extractor.cs` - Per-RunAsync extraction
- `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs` - DSL interpretation

## Checklist

- [ ] Analyze generator pipeline to identify refactoring approach
- [ ] Design single-pass extraction architecture
- [ ] Implement file-level extraction (not per-RunAsync)
- [ ] Update `CombineModels` or replace with simpler merging
- [ ] Remove deduplication workaround (or keep as safety net)
- [ ] Test with multiple RunAsync calls in various patterns
- [ ] Verify performance improvement

## Notes

- Related to intercept site deduplication fix in `CombineModels`
- See also task #319 for related bug with multiple apps in same block
- Current deduplication: `allInterceptSites.DistinctBy(site => site.GetAttributeSyntax())`
- This is a performance/architectural improvement, not a bug fix
