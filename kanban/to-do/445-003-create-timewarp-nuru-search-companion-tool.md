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

### Parent Task

#445 - Add --search and --group-filter options to --capabilities
