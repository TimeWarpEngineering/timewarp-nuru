# Create timewarp-nuru-search companion tool

## Description

Create a standalone .NET tool that provides keyword search across all Nuru-based CLIs. Uses SQLite FTS5 (built-in full-text search) for fast text matching with on-demand indexing.

**Key Design Decision:** No embedding model needed. The agent (LLM) already has semantic understanding and formulates appropriate keyword queries. The search tool is a fast keyword matcher, not a semantic engine.

## Checklist

- [ ] Create timewarp-nuru-search project structure
- [ ] Implement SQLite schema with FTS5 virtual table
- [ ] Implement `nuru-search search` command
- [ ] Implement on-demand indexing (run `cli --capabilities`, parse, store)
- [ ] Implement version drift detection (re-index if version changed)
- [ ] Add `nuru-search index list` command (show indexed CLIs)
- [ ] Add `nuru-search index rebuild` command (force re-index)
- [ ] Publish as .NET tool to NuGet

## Notes

### CLI Surface

```bash
# Search (main use case)
nuru-search search --cli ganda --version 1.2.3 --query "commit"
nuru-search search --cli ganda --version 1.2.3 --query "push" --group "git"

# Index management
nuru-search index list                    # Show indexed CLIs and versions
nuru-search index rebuild --cli ganda     # Force re-index specific CLI
nuru-search index rebuild --all           # Re-index everything
nuru-search index clear                   # Clear entire index
```

### SQLite Schema

```sql
-- CLI metadata
CREATE TABLE clis (
  name TEXT PRIMARY KEY,
  version TEXT NOT NULL,
  indexed_at TEXT NOT NULL,
  capabilities_json TEXT NOT NULL
);

-- Endpoint data (for retrieval)
CREATE TABLE endpoints (
  id INTEGER PRIMARY KEY,
  cli_name TEXT NOT NULL,
  pattern TEXT NOT NULL,
  description TEXT,
  group_path TEXT,  -- JSON array
  FOREIGN KEY (cli_name) REFERENCES clis(name)
);

CREATE INDEX idx_endpoints_cli ON endpoints(cli_name);

-- FTS5 virtual table for full-text search
CREATE VIRTUAL TABLE endpoints_fts USING fts5(
  pattern,
  description,
  content='endpoints',
  content_rowid='id'
);

-- Triggers to keep FTS in sync
CREATE TRIGGER endpoints_ai AFTER INSERT ON endpoints BEGIN
  INSERT INTO endpoints_fts(rowid, pattern, description)
  VALUES (new.id, new.pattern, new.description);
END;

CREATE TRIGGER endpoints_ad AFTER DELETE ON endpoints BEGIN
  INSERT INTO endpoints_fts(endpoints_fts, rowid, pattern, description)
  VALUES ('delete', old.id, old.pattern, old.description);
END;
```

### Why FTS5 Instead of Semantic Search

**The agent is the semantic layer.**

When a user says "I want to save my work", the agent (LLM) doesn't pass that raw query to search. It translates:

```
User: "I want to save my work"
         ↓
Agent: "They probably mean git commit"
         ↓
Search query: "commit"
         ↓
Results: ganda git commit, ganda git commit --amend
```

The agent already has semantic understanding. The search tool just needs to be a **fast, reliable keyword matcher**.

**Benefits:**
- No embedding model (simpler, lighter, faster)
- No ML dependencies (AOT-friendly)
- Built into SQLite (no extensions needed)
- Instant indexing (no embedding generation time)

### On-Demand Indexing Flow

```
nuru-search receives: --cli ganda --version 1.2.3 --query "commit"
         │
         ▼
┌──────────────────────────────┐
│ Check if ganda v1.2.3       │
│ exists in index             │
└──────────────────────────────┘
         │
    YES  │  NO
         ▼         ▼
┌──────────┐ ┌──────────────────────┐
│ Use      │ │ Run: ganda          │
│ existing │ │ --capabilities      │
│ index    │ │ Parse JSON          │
└──────────┘ │ Insert into SQLite  │
             │ Update FTS5 index   │
             └──────────────────────┘
         │
         ▼
┌──────────────────────────────┐
│ FTS5 keyword search         │
│ Return matching endpoints   │
└──────────────────────────────┘
```

### Search Query Examples

```sql
-- Simple keyword search
SELECT e.* FROM endpoints e
JOIN endpoints_fts fts ON e.id = fts.rowid
WHERE endpoints_fts MATCH 'commit'
ORDER BY rank;

-- With group filter
SELECT e.* FROM endpoints e
JOIN endpoints_fts fts ON e.id = fts.rowid
WHERE endpoints_fts MATCH 'push'
  AND e.group_path LIKE '%"git"%'
ORDER BY rank;

-- Multiple keywords (AND)
WHERE endpoints_fts MATCH 'git push'

-- Multiple keywords (OR)
WHERE endpoints_fts MATCH 'git OR push'
```

### Return Format

Same as `--capabilities` output with filter metadata:

```json
{
  "cli": "ganda",
  "version": "1.2.3",
  "query": "commit",
  "group": "git",
  "endpoints": [...]
}
```

### Dependencies

```
timewarp-nuru-search
├── Microsoft.Data.Sqlite  (built into .NET, AOT-friendly)
├── TimeWarp.Amuru         (run `cli --capabilities`)
└── SQLite FTS5            (built into SQLite, no extension needed)
```

**No embedding model. No ML libraries. No semantic kernel.**

### Installation

```bash
dotnet tool install --global TimeWarp.Nuru.Search
```

## Implementation Plan

### Project Structure

```
source/timewarp-nuru-search/
├── timewarp-nuru-search.csproj
├── Directory.Build.props
├── global-usings.cs
├── program.cs                    # Entry point
├── endpoints/
│   ├── search-group.cs           # [NuruRouteGroup("search")]
│   ├── search-query.cs           # search --cli --version --query
│   ├── index-group.cs            # [NuruRouteGroup("index")]
│   ├── index-list-query.cs       # index list
│   ├── index-rebuild-command.cs  # index rebuild --cli/--all
│   └── index-clear-command.cs    # index clear
└── services/
    ├── search-index.cs           # SQLite FTS5 operations
    ├── capabilities-client.cs    # Run CLI --capabilities via Amuru
    └── database-path.cs          # ~/.nuru/index.db path resolution
```

### Phase 1: Project Setup

1. Create `source/timewarp-nuru-search/timewarp-nuru-search.csproj`
2. Create `source/timewarp-nuru-search/Directory.Build.props`
3. Create `source/timewarp-nuru-search/global-usings.cs`
4. Add to `timewarp-nuru.slnx`
5. Add `Microsoft.Data.Sqlite` to `Directory.Packages.props`

### Phase 2: Core Services

1. `services/database-path.cs` - Resolve `~/.nuru/index.db` path
2. `services/search-index.cs` - SQLite FTS5 operations
3. `services/capabilities-client.cs` - Run CLI --capabilities via Amuru

### Phase 3: SQLite Schema

Tables:
- `clis` - CLI metadata (name, version, indexed_at, capabilities_json)
- `endpoints` - Endpoint data (id, cli_name, pattern, description, group_path, endpoint_json)
- `endpoints_fts` - FTS5 virtual table with porter stemmer

Triggers for FTS sync (insert, delete, update)

### Phase 4: Endpoints

1. `search-group.cs` - Route group
2. `search-query.cs` - Main search endpoint
3. `index-group.cs` - Route group
4. `index-list-query.cs` - List indexed CLIs
5. `index-rebuild-command.cs` - Force re-index
6. `index-clear-command.cs` - Clear index

### Phase 5: Entry Point

`program.cs` - NuruApp with DI for services

### Phase 6: Testing

Unit tests for search-index, capabilities-client, FTS query sanitization

### Phase 7: NuGet Packaging

Package metadata, `dotnet pack`, publish to NuGet

### Key Design Decisions

1. **`~/.nuru/index.db`** - Consistent with existing Nuru history location
2. **AOT compilation** - Fast startup, single binary
3. **Explicit `--cli` argument** - Simple, predictable
4. **Porter stemmer** - FTS5 tokenizer for better word matching
5. **On-demand indexing** - No background services, index when needed

### Parent Task

#445 - Add --search and --group-filter options to --capabilities
