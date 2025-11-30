# Kebab-Case Migration Analysis

**Date**: 2025-11-30
**Target Reference**: `/home/steventcramer/worktrees/github.com/TimeWarpEngineering/timewarp-state/Cramer-2025-08-12-032migrate-from-mediatr-to-timewarp-mediator`

## Executive Summary

This analysis examines the effort required to migrate the TimeWarp.Nuru repository from its current PascalCase/mixed naming conventions to the all-lowercase kebab-case file and directory naming standard used in the timewarp-state repository.

## Target Convention (timewarp-state)

The reference repository demonstrates these conventions:

### Directories
- All lowercase with hyphens: `samples/`, `tests/`, `features/`, `properties/`
- Numbered prefixes for ordering: `00-state-action-handler/`, `01-redux-dev-tools/`
- Hierarchical organization: `samples/00-state-action-handler/auto/sample-00-auto/`

### Files
- C# files: lowercase kebab-case: `program.cs`, `counter-state.cs`, `counter-state.increment-count.cs`
- Config files: lowercase: `appsettings.json`, `appsettings.Development.json`
- Markdown files: lowercase: `overview.md`, `readme.md`
- Project files: lowercase: `sample-00-wasm.csproj`
- Solution files: lowercase: `timewarp-state.slnx`
- Special files: lowercase: `.editorconfig`, `.gitignore`, `unlicense.txt`

### Exceptions (Preserve Case)
- Razor components: `App.razor`, `MainLayout.razor`, `Counter.razor` (required by Blazor framework)
- MSBuild standard files: `Directory.Build.props` (MSBuild convention)
- _imports files: `_imports.razor`, `_Imports.cs` (framework convention)
- `Properties/` folder with `launchSettings.json` (dotnet convention)

## Current State (timewarp-nuru)

### Directories Requiring Rename

| Current | Target | Impact |
|---------|--------|--------|
| `Kanban/` | `kanban/` | Low - markdown files only |
| `Kanban/ToDo/` | `kanban/to-do/` | Low |
| `Kanban/InProgress/` | `kanban/in-progress/` | Low |
| `Kanban/Done/` | `kanban/done/` | Low |
| `Kanban/Backlog/` | `kanban/backlog/` | Low |
| `Source/` | `source/` | High - project references |
| `Source/TimeWarp.Nuru/` | `source/timewarp-nuru/` | High |
| `Source/TimeWarp.Nuru.Analyzers/` | `source/timewarp-nuru-analyzers/` | High |
| `Source/TimeWarp.Nuru.Completion/` | `source/timewarp-nuru-completion/` | High |
| `Source/TimeWarp.Nuru.Core/` | `source/timewarp-nuru-core/` | High |
| `Source/TimeWarp.Nuru.Logging/` | `source/timewarp-nuru-logging/` | High |
| `Source/TimeWarp.Nuru.Mcp/` | `source/timewarp-nuru-mcp/` | High |
| `Source/TimeWarp.Nuru.Parsing/` | `source/timewarp-nuru-parsing/` | High |
| `Source/TimeWarp.Nuru.Repl/` | `source/timewarp-nuru-repl/` | High |
| `Source/TimeWarp.Nuru.Telemetry/` | `source/timewarp-nuru-telemetry/` | High |
| `Tests/` | `tests/` | High - project references |
| `Tests/TestApps/` | `tests/test-apps/` | Medium |
| `Tests/TimeWarp.Nuru.Tests/` | `tests/timewarp-nuru-tests/` | High |
| `Tests/TimeWarp.Nuru.Analyzers.Tests/` | `tests/timewarp-nuru-analyzers-tests/` | High |
| `Tests/TimeWarp.Nuru.Completion.Tests/` | `tests/timewarp-nuru-completion-tests/` | High |
| `Tests/TimeWarp.Nuru.Mcp.Tests/` | `tests/timewarp-nuru-mcp-tests/` | High |
| `Tests/TimeWarp.Nuru.Repl.Tests/` | `tests/timewarp-nuru-repl-tests/` | High |
| `Samples/` | `samples/` | Medium |
| `Samples/AspireHostOtel/` | `samples/aspire-host-otel/` | Medium |
| `Samples/AspireTelemetry/` | `samples/aspire-telemetry/` | Medium |
| `Samples/AsyncExamples/` | `samples/async-examples/` | Medium |
| `Samples/Calculator/` | `samples/calculator/` | Low |
| `Samples/CoconaComparison/` | `samples/cocona-comparison/` | Medium |
| `Samples/Configuration/` | `samples/configuration/` | Low |
| `Samples/DynamicCompletionExample/` | `samples/dynamic-completion-example/` | Medium |
| `Samples/HelloWorld/` | `samples/hello-world/` | Low |
| `Samples/Logging/` | `samples/logging/` | Low |
| `Samples/PipelineMiddleware/` | `samples/pipeline-middleware/` | Low |
| `Samples/ReplDemo/` | `samples/repl-demo/` | Low |
| `Samples/ShellCompletionExample/` | `samples/shell-completion-example/` | Medium |
| `Samples/Testing/` | `samples/testing/` | Low |
| `Samples/UnifiedMiddleware/` | `samples/unified-middleware/` | Low |
| `Benchmarks/` | `benchmarks/` | Medium |
| `Benchmarks/TimeWarp.Nuru.Benchmarks/` | `benchmarks/timewarp-nuru-benchmarks/` | Medium |
| `Assets/` | `assets/` | Low |
| `Scripts/` | `scripts/` | Low |
| `documentation/` | Already lowercase | None |
| `.agent/` | Already lowercase | None |
| `msbuild/` | Already lowercase | None |

### Files Requiring Rename

#### Root Level Files
| Current | Target | Impact |
|---------|--------|--------|
| `TimeWarp.Nuru.slnx` | `timewarp-nuru.slnx` | High - all project refs |
| `CLAUDE.md` | `claude.md` | Low |
| `Agents.md` | `agents.md` | Low |
| `OPTIMIZATION-RESULTS.md` | `optimization-results.md` | Low |
| `README.md` | `readme.md` | Low |
| `LICENSE` | `license` | Low |

#### Source C# Files (PascalCase → kebab-case)
All `.cs` files in `Source/` directories need renaming:
- `NuruApp.cs` → `nuru-app.cs`
- `NuruAppBuilder.cs` → `nuru-app-builder.cs`
- `GlobalUsings.cs` → `global-usings.cs`
- `CompletionProvider.cs` → `completion-provider.cs`
- etc.

**Estimated count**: ~150+ C# files

#### Test Files
Many test files already use kebab-case (e.g., `lexer-01-basic-token-types.cs`)
Some still use PascalCase:
- `TestSamples.cs` → `test-samples.cs`
- `LexerTestHelper.cs` → `lexer-test-helper.cs`

#### Project Files (.csproj)
| Current | Target |
|---------|--------|
| `TimeWarp.Nuru.csproj` | `timewarp-nuru.csproj` |
| `TimeWarp.Nuru.Analyzers.csproj` | `timewarp-nuru-analyzers.csproj` |
| `TimeWarp.Nuru.Completion.csproj` | `timewarp-nuru-completion.csproj` |
| `TimeWarp.Nuru.Logging.csproj` | `timewarp-nuru-logging.csproj` |
| `TimeWarp.Nuru.Mcp.csproj` | `timewarp-nuru-mcp.csproj` |
| `TimeWarp.Nuru.Parsing.csproj` | `timewarp-nuru-parsing.csproj` |
| `TimeWarp.Nuru.Repl.csproj` | `timewarp-nuru-repl.csproj` |
| `TimeWarp.Nuru.Telemetry.csproj` | `timewarp-nuru-telemetry.csproj` |
| `TimeWarp.Nuru.Benchmarks.csproj` | `timewarp-nuru-benchmarks.csproj` |

**Note**: Assembly names and root namespaces can remain PascalCase inside .csproj

#### Markdown/Documentation Files
| Current | Target |
|---------|--------|
| `README.md` | `readme.md` |
| `Overview.md` | `overview.md` |
| `Task-Template.md` | `task-template.md` |
| `Workflow.md` | `workflow.md` |

### Kanban Task Files
Current format uses underscores: `001_Task-Name.md`
Target format should use hyphens: `001-task-name.md`

**Estimated count**: 90+ task files in Done, 50+ in ToDo, plus backlog

## High-Risk Areas

### 1. Solution File Updates (Critical)
The `.slnx` file contains all project references with paths. After renaming:
- All project paths must be updated
- Build will fail if not updated correctly

### 2. Project Reference Updates (Critical)
Each `.csproj` file contains `<ProjectReference>` elements:
```xml
<ProjectReference Include="..\TimeWarp.Nuru.Core\TimeWarp.Nuru.Core.csproj" />
```
Must become:
```xml
<ProjectReference Include="..\timewarp-nuru-core\timewarp-nuru-core.csproj" />
```

### 3. CI/CD Pipeline (.github/workflows/ci-cd.yml)
May contain hardcoded paths that need updating.

### 4. NuGet Package Names
- Assembly names in .csproj should remain `TimeWarp.Nuru.*` (PascalCase is standard for NuGet)
- Only file/folder names change, not package identifiers

### 5. InternalsVisibleTo Attributes
Files like `InternalsVisibleTo.g.cs` reference assembly names which should remain PascalCase.

### 6. Documentation Cross-References
Internal links in markdown files may break if not updated.

## Migration Strategy

### Phase 1: Preparation
1. Create backup branch
2. Document all project references
3. Document all internal links in markdown
4. Update CI/CD to handle migration

### Phase 2: Directory Structure
1. Rename directories bottom-up (deepest first)
2. Use `git mv` to preserve history
3. Commit after each major section

### Phase 3: File Renames
1. Rename .csproj files
2. Rename .cs files
3. Rename .md files
4. Rename other files

### Phase 4: Reference Updates
1. Update .slnx with new paths
2. Update all ProjectReference elements
3. Update all internal markdown links
4. Update CI/CD pipeline paths

### Phase 5: Validation
1. Run `dotnet build` on solution
2. Run all tests
3. Verify documentation links
4. Test CI/CD pipeline

## Estimated Effort

| Category | Item Count | Complexity |
|----------|------------|------------|
| Directory renames | ~50 | Medium |
| C# file renames | ~150 | Low |
| Project file renames | ~15 | Medium |
| Markdown file renames | ~200+ | Low |
| Kanban task renames | ~140+ | Low |
| Solution file updates | 1 | High |
| Project reference updates | ~30 | High |
| CI/CD updates | 1 | Medium |
| Documentation link fixes | Unknown | Medium |

## Recommendations

### Do First (Low Risk)
1. Kanban directory and file renames
2. Documentation directory standardization
3. Root-level markdown files

### Do Second (Medium Risk)
1. Samples directory restructuring
2. Scripts directory
3. Assets directory

### Do Last (High Risk)
1. Source directory restructuring
2. Tests directory restructuring
3. Benchmarks directory restructuring
4. Solution and project reference updates

### Automation Script
Consider creating a migration script that:
1. Generates all `git mv` commands
2. Updates all project references
3. Updates solution file
4. Updates markdown links

## Files to Preserve Case

Per .NET/Blazor conventions, preserve case for:
- `Directory.Build.props` (MSBuild standard)
- `Directory.Packages.props` (MSBuild standard)
- `_Imports.cs` / `_imports.razor` (framework convention)
- `Properties/launchSettings.json` (dotnet convention)
- Razor component files `*.razor` (Blazor convention)

## Conclusion

The migration is feasible but requires careful execution due to:
1. Complex project reference graph
2. Large number of files (~500+)
3. CI/CD pipeline dependencies
4. NuGet package naming considerations

Recommend breaking into multiple focused tasks in the Kanban system, with the high-risk items blocked until low-risk items are validated.
