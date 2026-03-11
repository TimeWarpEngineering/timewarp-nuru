# Add --search and --group-filter options to --capabilities

## Description

Extend the `--capabilities` built-in flag to support filtering options for AI agent token efficiency. This enables agents to discover relevant endpoints without receiving the full capabilities catalog.

## Checklist

- [ ] Add TimeWarp.Amuru dependency and migrate clipboard code (#445-001)
- [ ] Implement --search and --group-filter options (#445-002)
- [ ] Create timewarp-nuru-search companion tool (separate epic)

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
- `--search` → Requires `nuru-search` tool installed, semantic search across all indexed CLIs
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

### Related

- Design discussion: 2026-03-12
- Precedent: Cloudflare MCP discover/call pattern
