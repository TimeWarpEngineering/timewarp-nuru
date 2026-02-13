# Group Filtering Sample

This sample demonstrates how to create different "editions" of a CLI from shared endpoint files using `DiscoverEndpoints(typeof(...))`.

## Overview

All commands live in `ganda/endpoints/`. Three runfile entry points demonstrate different group filtering:

- **ganda.cs**: Full edition with all commands
- **kanban.cs**: Kanban-only edition (parent "ganda" prefix stripped)
- **git.cs**: Git-only edition (parent "ganda" prefix stripped)

## Project Structure

```
01-group-filtering/
├── Directory.Build.props       # Includes ganda/endpoints/** for all editions
├── ganda/
│   ├── ganda.cs                # Full edition entry point
│   └── endpoints/
│       ├── ganda-group.cs      # [NuruRouteGroup("ganda")] root group
│       ├── kanban-group.cs     # [NuruRouteGroup("kanban")] : GandaGroup
│       ├── git-group.cs        # [NuruRouteGroup("git")] : GandaGroup
│       ├── kanban-add-command.cs
│       ├── kanban-list-command.cs
│       ├── git-commit-command.cs
│       └── git-status-command.cs
├── kanban/
│   └── kanban.cs               # Kanban-only entry point
├── git/
│   └── git.cs                  # Git-only entry point
└── README.md
```

## Group Hierarchy

```
GandaGroup          [NuruRouteGroup("ganda")]
├── KanbanGroup     [NuruRouteGroup("kanban")]
│   ├── KanbanAddCommand
│   └── KanbanListCommand
└── GitGroup        [NuruRouteGroup("git")]
    ├── GitCommitCommand
    └── GitStatusCommand
```

## How It Works

The `Directory.Build.props` in the sample root includes `ganda/endpoints/**/*.cs` for all runfiles. Each entry point differs only in its `DiscoverEndpoints()` call:

```csharp
// ganda.cs - full edition
.DiscoverEndpoints()

// kanban.cs - kanban only, "ganda" prefix stripped
.DiscoverEndpoints(typeof(KanbanGroup))

// git.cs - git only, "ganda" prefix stripped
.DiscoverEndpoints(typeof(GitGroup))
```

## Running

### Ganda (Full Edition)

```bash
dotnet run ganda/ganda.cs -- --help
# Shows: ganda kanban add, ganda kanban list, ganda git commit, ganda git status

dotnet run ganda/ganda.cs -- ganda kanban add "Task 1"
# [KANBAN] Added task: Task 1

dotnet run ganda/ganda.cs -- ganda git commit -m "message"
# [GIT] Committed: message
```

### Kanban Edition

```bash
dotnet run kanban/kanban.cs -- --help
# Shows: kanban add, kanban list (no git commands, no "ganda" prefix)

dotnet run kanban/kanban.cs -- kanban add "Task 1"
# [KANBAN] Added task: Task 1
```

### Git Edition

```bash
dotnet run git/git.cs -- --help
# Shows: git commit, git status (no kanban commands, no "ganda" prefix)

dotnet run git/git.cs -- git commit -m "message"
# [GIT] Committed: message
```

## Key Concepts

### Shared Endpoints

All command classes live in `ganda/endpoints/` and are compiled into every edition via `Directory.Build.props`. The source generator filters which routes are active based on the `DiscoverEndpoints()` call.

### Prefix Stripping

When filtering by `typeof(KanbanGroup)`, the parent "ganda" prefix is stripped. Users type `kanban add` instead of `ganda kanban add`.

### Type-Based Filtering

Filtering uses `typeof()` for type safety. Refactoring a group class name automatically updates all references.
