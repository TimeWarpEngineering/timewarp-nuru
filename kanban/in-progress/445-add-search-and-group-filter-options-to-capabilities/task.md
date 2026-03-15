# Add --search and --group-filter options to --capabilities

## Description

Extend the `--capabilities` built-in flag to support filtering options for AI agent token efficiency. This enables agents to discover relevant endpoints without receiving the full capabilities catalog.

Includes three components:
1. **Nuru changes** - Add Amuru dependency, implement `--group-filter` (local) and `--search` (subprocess) options
2. **nuru-search tool** - Companion CLI for keyword search across all Nuru-based CLIs using SQLite FTS5

## Checklist

- [ ] Add TimeWarp.Amuru dependency and migrate clipboard code (#445-001)
- [ ] Implement --search and --group-filter options (#445-002)
- [ ] Create timewarp-nuru-search companion tool (#445-003)

## Notes

### Design Decisions

**CLI Surface:**
```bash
ganda --capabilities                                    # Full
ganda --capabilities --group-filter "kanban"            # Local filter
ganda --capabilities --search "commit"                  # nuru-search
ganda --capabilities --group-filter "git" --search "push"  # Both → nuru-search
```

**Behavior:**
- `--group-filter` → Local filtering (exact match on GroupPath prefix)
- `--search` → Requires `nuru-search` tool installed, keyword search across all indexed CLIs
- Both options → Pass both to nuru-search, let it handle combined filtering

**Error Handling:**
- `--search` without nuru-search → upsell message, no fallback
- `--group-filter` alone → local filtering, no nuru-search needed

**Output Format (when filtered):**
```json
{
  "name": "ganda",
  "version": "1.2.3",
  "filter": {
    "group": "kanban",      // present if --group-filter used
    "search": "commit"      // present if --search used
  },
  "endpoints": [...]
}
```

### nuru-search Contract

```bash
nuru-search search --cli ganda --version 1.2.3 --query "push" --group "git"
```

Returns same CapabilitiesResponse format with filter metadata.

### nuru-search Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Nuru Search Index                    │
│  (SQLite with FTS5 full-text search)                   │
│  Location: ~/.nuru/search-index.db                      │
│                                                         │
│  ganda --capabilities → ─┐                              │
│  nuru --capabilities  → ─┼─→ indexed for FTS5          │
│  other-cli caps       → ─┘                              │
└─────────────────────────────────────────────────────────┘
```

**Key Insight: No Embedding Model Needed**

The agent (LLM) already has semantic understanding. When a user says "I want to save my work", the agent formulates the search query "commit" - it doesn't pass the raw user request to search. The search tool is a fast keyword matcher, not a semantic engine.

```
User: "I want to save my work"
         ↓
Agent (LLM): Translates to search query "commit"
         ↓
nuru-search (FTS5): Returns ganda git commit, etc.
         ↓
Agent: Presents options, executes chosen command
```

**Indexing Strategy:**
- On-demand: When `--search` is used, nuru-search checks if CLI+version is indexed
- If not indexed or version mismatch: Run `cli --capabilities`, index results into FTS5
- No embedding generation - just text indexing

**Search Flow:**
```
ganda --capabilities --search "commit"
         │
         ▼
┌──────────────────────────────┐
│ Call nuru-search subprocess  │
│ (cli: ganda, version: 1.2.3, │
│  query: "commit")            │
└──────────────────────────────┘
         │
         ▼
┌──────────────────────────────┐
│ nuru-search checks index:    │
│ ganda v1.2.3 indexed?        │
└──────────────────────────────┘
         │
    YES  │  NO
         ▼         ▼
┌──────────┐ ┌──────────────────┐
│ Return   │ │ Run ganda       │
│ results  │ │ --capabilities  │
└──────────┘ │ index into FTS5 │
             └──────────────────┘
```

### Related

- Design discussion: 2026-03-12
- Precedent: Cloudflare MCP discover/call pattern
