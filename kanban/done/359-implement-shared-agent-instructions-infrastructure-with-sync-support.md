# Implement shared agent instructions infrastructure with sync support

## Description

Create infrastructure for sharing agent instructions across repos and tools (Claude Code, OpenCode) with single source of truth in timewarp-nuru repo.

## Goals

- DRY: Don't repeat instructions across repos/tools
- Single source of truth in one canonical repo
- Support both Claude Code (CLAUDE.md) and OpenCode (AGENTS.md)
- Efficient: plain file reads, no MCP/web fetches for basic usage

## Structure

```
timewarp-nuru/
  .agents/
    shared/                       # Source of truth for shared instructions
      agent-context-regions.md
      dotnet-runfiles.md
      git-guidelines.md
    local/                        # Repo-specific instructions
      nuru-specific.md
    CLAUDE.md                     # Includes from shared/ and local/
    AGENTS.md                     # Includes from shared/ and local/
```

## Sync Convention

Files synced from canonical source include header marker:
```markdown
<!-- @sync-source: timewarp-nuru/.agents/shared/agent-context-regions.md -->
```

- **With marker**: Synced file, overwrite from source
- **Without marker**: Local file, never touched by sync

Path resolves against worktree root:
`timewarp-nuru/.agents/shared/file.md` → `~/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/.agents/shared/file.md`

## Checklist

- [ ] Create `.agents/shared/` folder in timewarp-nuru
- [ ] Create `agent-context-regions.md` (first shared instruction)
- [ ] Create `dotnet-runfiles.md` (runfile best practices)
- [ ] Create `git-guidelines.md` (git conventions)
- [ ] Create `.agents/local/` for repo-specific instructions
- [ ] Create `.agents/CLAUDE.md` entry point
- [ ] Create `.agents/AGENTS.md` entry point
- [ ] Add sync support to existing sync tool (scan for `@sync-source:` markers)

## Notes

- Submodules and symlinks rejected (too painful)
- MCP/web fetches rejected (slow, caching uncertain)
- Out-of-repo file refs trigger permission prompts in tools
- Checked-in copies with sync tooling is the pragmatic solution
