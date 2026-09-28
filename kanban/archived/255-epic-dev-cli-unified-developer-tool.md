# Epic: Dev CLI - Unified Developer Tool

## Description

Design and implement a unified `dev` CLI tool that consolidates all developer tooling into a single AOT-compiled binary. This tool will provide both CI/CD orchestration capabilities for GitHub Actions and streamlined development workflows for developers using the kanban task management system.

## Current State Analysis

### ✅ **Prerequisites Complete**
- **Task 150 (Endpoints)**: Production-ready with auto-registration, grouped commands, aliases
- **TimeWarp.Nuru 3.0.0-beta.22**: Full AOT support and endpoints capabilities
- **Existing Runfiles**: 10 C# scripts providing solid foundation and patterns

### 🔍 **Investigation Findings**

#### Current Runfiles Structure
```
runfiles/
├── build.cs              # Build all projects
├── clean.cs              # Clean artifacts
├── test.cs               # CI test suite (~12s)
├── verify-samples.cs     # Verify sample compilation
├── format.cs             # Code formatting
├── analyze.cs            # Roslynator analysis
├── check-version.cs      # NuGet version validation
└── [4 maintenance scripts]
```

#### Current GitHub Actions
```
.github/workflows/
├── ci-cd.yml           # Main CI/CD pipeline (118 lines)
├── builder-publish.yml  # TimeWarp.Builder releases
└── terminal-publish.yml # TimeWarp.Terminal releases
```

#### Current Kanban System
- 5-state workflow: backlog/ → to-do/ → in-progress/ → done/ → archived/
- Well-defined task templates and naming conventions
- Manual Git operations for task movement

## Proposed Architecture

### **Dual Purpose Design**
1. **CI/CD Orchestration**: Clean up GitHub Actions workflows
2. **Development Workflows**: Streamline kanban and Git operations

### **Command Structure**
```bash
# CI/CD Orchestration (for GitHub Actions)
dev ci                    # Run full CI/CD pipeline
dev build                 # Build projects  
dev test ci              # Run CI tests
dev release publish      # Publish release

# Development Workflows (for developers)
dev task start 184       # Start kanban task
dev pr create           # Create pull request
dev release prepare 3.0.1 # Prepare release locally
```

### **Directory Structure**
```
runfiles/dev/
├── dev.csproj           # AOT-enabled project
├── Program.cs           # Minimal entry point
├── bootstrap.cs         # Build dev binary for current platform
├── Commands/
│   ├── CI/             # CI/CD orchestration commands
│   ├── Development/    # Development workflow commands
│   └── Core/           # Migrated runfile commands
└── Docs/               # Extended help markdown files
```

## Implementation Phases

### Phase 1: Foundation & CI/CD Commands
- Create dev CLI project structure
- Implement CI/CD orchestration commands using endpoints
- Refactor GitHub Actions workflows to use dev CLI
- Test dual-mode execution (standalone + AOT binary)

### Phase 2: Core Command Migration  
- Convert existing runfiles to endpoint pattern
- Implement grouped commands for test suite
- Add `--capabilities` for AI discoverability
- Set up AOT bootstrap process

### Phase 3: Development Workflow Commands
- Implement kanban task management (`dev task` group)
- Add PR workflow automation (`dev pr` group)  
- Implement release preparation (`dev release` group)
- Integrate with existing kanban system and Git operations

## Key Benefits

### **GitHub Actions Benefits**
- **Centralized Logic**: All build/test/publish logic in dev CLI
- **Cleaner Workflows**: 80% reduction in YAML complexity
- **Local Parity**: Developers can run exact same commands locally
- **Maintainability**: Single source of truth for CI/CD processes

### **Development Benefits**
- **Streamlined Workflows**: One command replaces multiple manual steps
- **Consistency**: Standardized process across all developers  
- **Quality Gates**: Automated pre-commit and pre-PR checks
- **Task Integration**: Seamless kanban board integration

### **Technical Benefits**
- **Performance**: AOT binary provides instant startup vs script compilation
- **Discoverability**: AI tools can use `--capabilities` for automation
- **Flexibility**: Endpoints allow easy command reorganization
- **Extensibility**: Easy to add new commands and features

## Open Questions

1. **Command Naming**: Final preference for command groups and aliases
2. **Migration Priority**: CI/CD cleanup first or development workflows?
3. **Error Handling**: Failed workflow steps - fail fast or continue?
4. **Authentication**: NuGet API keys in local dev CLI usage
5. **Integration**: Best way to integrate with existing kanban task files

## Technical Requirements

- **Framework**: TimeWarp.Nuru with endpoints (Task 150)
- **Compilation**: AOT-enabled for performance
- **Platform**: Cross-platform via TimeWarp.Amuru patterns
- **AI Discovery**: `--capabilities` flag for automation
- **Documentation**: Extended help via embedded markdown resources

## Dependencies

- ✅ **Task 150**: Endpoints (Complete)
- 🔄 **Task 157**: `--capabilities` Flag (In Progress)  
- ⏳ **Implementation Tasks**: To be created after investigation

## Checklist

- [x] Create Phase 1: Foundation & CI/CD Commands task
- [x] Create Phase 2: Core Command Migration task  
- [x] Create Phase 3: Development Workflow Commands task
- [x] Finalize Epic scope and requirements
- [x] Archive investigation task (184) with findings

## Notes

This Epic leverages the mature TimeWarp.Nuru endpoints infrastructure (Task 150) and existing runfile patterns to create a unified developer tool that serves both CI/CD orchestration and development workflow automation needs. The endpoints approach provides maximum flexibility for command organization and future evolution based on user feedback.

## Closed 2026-09-28 (triage 477)

Epic checklist complete. Delivery happened as **448** (shared DevCli endpoints) and the **458** series (versioning, release gate, `dev release`); the Phase 3 workflow vision (kanban, PR automation) is owned by ganda. Archived.
