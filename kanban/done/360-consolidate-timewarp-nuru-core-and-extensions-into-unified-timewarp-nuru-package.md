# Consolidate timewarp-nuru-core and extensions into unified timewarp-nuru package

## Description

With CreateSlimBuilder/CreateEmptyBuilder removed and source generation handling everything at compile time, there was no longer a need for separate "core" and "full" packages. This task merged all related packages into a single unified package.

## Rationale

- CreateSlimBuilder/CreateEmptyBuilder are gone - only stale docs referenced them
- Source generator handles everything at compile time - no runtime "slim" vs "full" distinction
- Simpler mental model: one package instead of five
- Performance difference is negligible with AOT compilation

## Checklist

- [x] Move all source files from timewarp-nuru-core to timewarp-nuru
- [x] Merge global-usings.cs files
- [x] Merge .csproj settings (package refs, MSBuild properties, InternalsVisibleTo)
- [x] Merge timewarp-nuru-logging into timewarp-nuru
- [x] Merge timewarp-nuru-telemetry into timewarp-nuru
- [x] Merge timewarp-nuru-completion into timewarp-nuru (27 files + templates)
- [x] Update timewarp-nuru-mcp to reference timewarp-nuru
- [x] Update timewarp-nuru-repl to reference timewarp-nuru
- [x] Update solution file (timewarp-nuru.slnx) to remove merged projects
- [x] Delete source/timewarp-nuru-core/
- [x] Delete source/timewarp-nuru-logging/
- [x] Delete source/timewarp-nuru-telemetry/
- [x] Delete source/timewarp-nuru-completion/
- [x] Rename tests/timewarp-nuru-core-tests/ → tests/timewarp-nuru-tests/
- [x] Merge tests/timewarp-nuru-completion-tests/ → tests/timewarp-nuru-tests/completion/
- [x] Update tests/ci-tests/Directory.Build.props paths
- [x] Remove stale logging project reference from tests/Directory.Build.props
- [x] Delete tests/timewarp-nuru-completion-tests/
- [x] Verify build succeeds
- [x] Verify all 533 tests pass

## Notes

### Projects Merged
- timewarp-nuru-core → timewarp-nuru
- timewarp-nuru-logging → timewarp-nuru
- timewarp-nuru-telemetry → timewarp-nuru
- timewarp-nuru-completion → timewarp-nuru

### Projects Updated (reference changes)
- timewarp-nuru-mcp (now references timewarp-nuru directly)
- timewarp-nuru-repl (now references timewarp-nuru directly)

### Test Folders Consolidated
- timewarp-nuru-core-tests/ → timewarp-nuru-tests/
- timewarp-nuru-completion-tests/ → timewarp-nuru-tests/completion/

### Verification
- `dotnet build timewarp-nuru.slnx -c Release` - Build succeeded
- 533 tests pass (capabilities tests now pass with merged code)
- Samples work correctly
