---
name: cli-search
description: Search for CLI commands and tools installed on your system. Use when you need to find specific functionality, discover available commands, or explore what CLI tools are available.
---

# nuru - CLI Search Tool

Search and discover commands across Nuru-based CLI applications installed on your system.

**Package:** `TimeWarp.Nuru.Search`
**Repository:** https://github.com/TimeWarpEngineering/timewarp-nuru

## Installation

```bash
dotnet tool install --global TimeWarp.Nuru.Search
```

## Commands

### Search for Commands

```bash
nuru search --query "commit"
nuru search --query "push" --group git
nuru search --query "build" --cli ganda
```

**Options:**
- `--query` / `-q` - Search terms (keyword matching)
- `--group` / `-g` - Filter by group path prefix
- `--cli` / `-c` - Limit results to a specific CLI
- `--limit` / `-l` - Maximum results (default: 50)

### Index Management

```bash
nuru index list                    # Show indexed CLIs
nuru index rebuild --cli /path/to/cli  # Index a specific CLI
nuru index rebuild --all           # Rebuild all indexes
nuru index clear                   # Clear the entire index
```

## How It Works

1. **On-demand indexing** - When you search, the tool checks if the CLI is indexed
2. **Auto-index** - If not indexed or version changed, runs `cli --capabilities` and indexes results
3. **SQLite FTS5** - Full-text search with Porter stemmer for fast keyword matching
4. **Database location** - `~/.nuru/index.db`

## Search Behavior

- **Keyword matching** - Searches across pattern, description, and group path
- **Case-insensitive** - "Commit" and "commit" return same results
- **Group filtering** - Use `--group` to narrow to a specific command group
- **CLI filtering** - Use `--cli` to search within a specific application

## Output Format

Returns JSON compatible with `--capabilities` output:

```json
{
  "cli": "ganda",
  "version": "1.0.0",
  "query": "commit",
  "group": "git",
  "endpoints": [...]
}
```

## Integration with Nuru CLIs

Any CLI built with TimeWarp.Nuru supports `--capabilities` output, which this tool indexes:

```bash
# Index any Nuru-based CLI
nuru index rebuild --cli /path/to/my-cli

# Then search across all indexed CLIs
nuru search --query "deploy"
```

## Examples

```bash
# Find all commit-related commands
nuru search --query "commit"

# Find push commands in the git group
nuru search --query "push" --group git

# Search only within ganda CLI
nuru search --query "kanban" --cli ganda

# List what's indexed
nuru index list

# Rebuild index for a specific CLI after update
nuru index rebuild --cli /usr/local/bin/ganda
```
