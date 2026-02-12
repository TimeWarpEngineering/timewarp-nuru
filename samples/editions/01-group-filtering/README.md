# Group Filtering Sample

This sample demonstrates how to use route groups in TimeWarp.Nuru to create different "editions" of your CLI from a shared codebase.

## Overview

The sample contains a shared library with commands for two groups: `kanban` and `git`. Three executables demonstrate different group filtering strategies:

- **ganda**: Full edition with all commands
- **kanban**: Kanban-only edition
- **git**: Git-only edition

## Project Structure

```
01-group-filtering/
├── shared/
│   ├── Groups.cs          # Route group definitions
│   ├── KanbanCommands.cs  # Kanban commands
│   ├── GitCommands.cs     # Git commands
│   └── shared.csproj
├── ganda/
│   ├── Program.cs         # Discovers all endpoints
│   └── ganda.csproj
├── kanban/
│   ├── Program.cs         # Discovers only kanban endpoints
│   └── kanban.csproj
├── git/
│   ├── Program.cs         # Discovers only git endpoints
│   └── git.csproj
└── README.md
```

## Group Hierarchy

The groups form an inheritance hierarchy:

```
GandaGroupBase (root)
├── KanbanGroupBase
└── GitGroupBase
```

The `[NuruRouteGroup("name")]` attribute marks classes as group bases.

## Building

### Build the Full Edition (ganda)

```bash
cd ganda
dotnet build
```

### Build the Kanban Edition

```bash
cd kanban
dotnet build
```

### Build the Git Edition

```bash
cd git
dotnet build
```

## Running

### Ganda Edition

```bash
dotnet run --project ganda -- kanban add
# Output: Kanban: Adding a new task

dotnet run --project ganda -- kanban list
# Output: Kanban: Listing all tasks

dotnet run --project ganda -- git commit
# Output: Git: Committing changes

dotnet run --project ganda -- git status
# Output: Git: Showing repository status
```

### Kanban Edition

```bash
dotnet run --project kanban -- add
# Output: Kanban: Adding a new task

dotnet run --project kanban -- list
# Output: Kanban: Listing all tasks

# Git commands are NOT available:
dotnet run --project kanban -- commit
# Output: No matching command found
```

### Git Edition

```bash
dotnet run --project git -- commit
# Output: Git: Committing changes

dotnet run --project git -- status
# Output: Git: Showing repository status

# Kanban commands are NOT available:
dotnet run --project git -- add
# Output: No matching command found
```

## Key Concepts

### `DiscoverEndpoints()` Behavior

1. **No arguments**: Discovers ALL endpoints from referenced assemblies
   ```csharp
   builder.DiscoverEndpoints()
   ```

2. **With group type**: Discovers only endpoints in that group and its subgroups
   ```csharp
   builder.DiscoverEndpoints(typeof(KanbanGroupBase))  // kanban + subgroups
   builder.DiscoverEndpoints(typeof(GitGroupBase))     // git + subgroups
   ```

### Use Cases

- **Plugin architectures**: Ship different feature sets based on license
- **Modular deployments**: Build lightweight tools for specific workflows
- **Development vs production**: Include debug commands only in dev builds
- **Multi-tenant CLIs**: Same codebase, different command sets per customer

## Source Generator Output

The TimeWarp.Nuru source generator emits different route tables based on the `DiscoverEndpoints()` call. Each edition gets only the routes it needs, ensuring minimal binary size and no unused code paths.
