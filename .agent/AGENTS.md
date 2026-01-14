# Agent Instructions Index

This file is the entry point for AI agents (OpenCode, Claude Code). Instructions are organized into separate files for lazy loading - read the relevant file when the context matches.

## When to Load Each File

Format: **File:** link to read | **When:** context that triggers loading

### Shared Instructions (cross-repo patterns)

**File:** [jaribu-testing-guide.md](shared/jaribu-testing-guide.md)
**When:** Writing, debugging, or reviewing Jaribu tests. Understanding test patterns, attributes ([Input], [Skip], [TestTag]), Setup/CleanUp, or multi-mode compatibility.

**File:** [dotnet-runfiles.md](shared/dotnet-runfiles.md)
**When:** Working with .cs file-based apps (runfiles). Using directives (#:package, #:project), shebang syntax, or discussing dotnet-script (which is obsolete).

**File:** [git-guidelines.md](shared/git-guidelines.md)
**When:** Making commits, PRs, merges. Understanding merge strategy (no squash, no rebase), worktree limitations, or commit message format.

**File:** [agent-context-regions.md](shared/agent-context-regions.md)
**When:** Adding #region Purpose or #region Design blocks to source files. Understanding the pattern for embedding agent-useful context in code.

### Local Instructions (this repo only)

**File:** [nuru-specific.md](local/nuru-specific.md)
**When:** Working on TimeWarp.Nuru source code. Understanding the source generator architecture, fluent API patterns, or TestTerminal testing pattern.

## Quick Reference (always available)

### Build Commands
```bash
dotnet build timewarp-nuru.slnx -c Release  # Full build
dotnet runfiles/build.cs                      # Runfile build with format/analyze
dotnet runfiles/clean-and-build.cs            # Clean & rebuild
```

### Test Commands
```bash
dotnet run tests/ci-tests/run-ci-tests.cs                              # CI tests (~500 tests)
dotnet run tests/timewarp-nuru-core-tests/routing/routing-01-basic.cs  # Single test file
```

### Key Paths
- Generated files: `artifacts/generated/{ProjectName}/`
- Kanban tasks: `kanban/{to-do,in-progress,done,backlog}/`

### Kanban Rules
- **NEVER add**: Status, Priority, Category fields (folder = status)
- **Use**: Description, Checklist, Notes
