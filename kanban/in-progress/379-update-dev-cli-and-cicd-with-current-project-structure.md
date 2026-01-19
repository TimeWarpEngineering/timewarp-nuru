# Update dev-cli and CI/CD with current project structure

## Description

The `tools/dev-cli/` commands and `.github/workflows/ci-cd.yml` reference outdated project structures. Several commands hardcode project paths and NuGet package names that no longer match the current repository structure.

### Current Project Structure (Jan 2026)

**Actual source projects:**
- `source/timewarp-nuru/timewarp-nuru.csproj` - Main library
- `source/timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj` - Roslyn analyzers
- `source/timewarp-nuru-mcp/timewarp-nuru-mcp.csproj` - MCP server
- `source/timewarp-nuru-parsing/timewarp-nuru-parsing.csproj` - Parsing library
- `source/timewarp-nuru-build/timewarp-nuru-build.csproj` - Build targets
- `source/timewarp-nuru-repl-reference-only/timewarp-nuru-repl.csproj` - REPL reference

**Samples:**
- `samples/03-attributed-routes/attributed-routes.csproj`
- `samples/05-aot-example/aot-example.csproj`
- `samples/99-timewarp-nuru-sample/timewarp-nuru-sample.csproj`

**Tests:**
- `tests/test-apps/timewarp-nuru-testapp-delegates/timewarp-nuru-testapp-delegates.csproj`

**Solution file (`timewarp-nuru.slnx`):** Only includes timewarp-nuru, timewarp-nuru-analyzers, timewarp-nuru-mcp, timewarp-nuru-parsing, and the test app.

### Files with Outdated References

1. **`tools/dev-cli/commands/build-command.cs`** (lines 71-82)
   - `projectsToBuild` array references: timewarp-nuru-core, timewarp-nuru-logging, timewarp-nuru-mcp, timewarp-nuru, timewarp-nuru-completion
   - Missing: timewarp-nuru-parsing, timewarp-nuru-analyzers
   - Commented out: timewarp-nuru-repl

2. **`tools/dev-cli/commands/ci-command.cs`**
   - `PackProjectsAsync()` (lines 191-201): References non-existent projects
   - `PushPackagesAsync()` (lines 237-247): References packages not in current structure

3. **`tools/dev-cli/commands/check-version-command.cs`** (lines 58-68)
   - `packages` array references non-existent NuGet packages

4. **`.github/workflows/ci-cd.yml`**
   - Paths look correct but may need verification

## Checklist

- [ ] Audit actual source project structure against dev-cli commands
- [ ] Update `build-command.cs` `projectsToBuild` array with current projects
- [ ] Update `ci-command.cs` `projectsToPack` array with packable projects
- [ ] Update `ci-command.cs` NuGet `packages` array for push operations
- [ ] Update `check-version-command.cs` `packages` array
- [ ] Verify `ci-cd.yml` paths match actual structure
- [ ] Test `dev ci --mode pr` locally
- [ ] Test `dev build` command locally
- [ ] Test `dev verify-samples` command locally
- [ ] Test `dev check-version` command locally (dry-run)

## Notes

- The solution file shows several projects commented out - determine which are active vs deprecated
- Verify what packages are actually published to NuGet.org (TimeWarp.Nuru.Core, etc.)
- The version in `source/Directory.Build.props` is 3.0.0-beta.24
- Consider if project consolidation occurred (multiple projects merged into fewer packages)
