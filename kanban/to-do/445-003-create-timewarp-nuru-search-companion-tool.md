# Create timewarp-nuru-search companion tool

## Description

Create a standalone .NET tool that provides semantic search across all Nuru-based CLIs. Uses SQLite with embeddings for fast similarity search and on-demand indexing.

## Checklist

- [ ] Create timewarp-nuru-search project structure
- [ ] Implement SQLite schema for capabilities storage
- [ ] Add semantic embedding support (SQLite vector extension or external)
- [ ] Implement `nuru-search search` command
- [ ] Implement on-demand indexing (run `cli --capabilities`, parse, embed)
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
CREATE TABLE clis (
  name TEXT PRIMARY KEY,
  version TEXT NOT NULL,
  indexed_at TEXT NOT NULL,
  capabilities_json TEXT NOT NULL
);

CREATE TABLE endpoints (
  id INTEGER PRIMARY KEY,
  cli_name TEXT NOT NULL,
  pattern TEXT NOT NULL,
  description TEXT,
  group_path TEXT,  -- JSON array
  embedding BLOB,   -- Vector embedding for semantic search
  FOREIGN KEY (cli_name) REFERENCES clis(name)
);

CREATE INDEX idx_endpoints_cli ON endpoints(cli_name);
```

### Embedding Strategy

Options:
1. **SQLite vector extension** - Native vector search in SQLite
2. **External embedding service** - Call OpenAI or local model
3. **Pre-computed embeddings** - Store at index time, query with cosine similarity

Recommendation: Start with option 3 (simple, no external dependencies), upgrade to vector extension later.

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
└──────────┘ │ Generate embeddings │
             │ Store in SQLite     │
             └──────────────────────┘
         │
         ▼
┌──────────────────────────────┐
│ Semantic search on embeddings│
│ Return matching endpoints    │
└──────────────────────────────┘
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

### Installation

```bash
dotnet tool install --global TimeWarp.Nuru.Search
```

### Parent Task

#445 - Add --search and --group-filter options to --capabilities
