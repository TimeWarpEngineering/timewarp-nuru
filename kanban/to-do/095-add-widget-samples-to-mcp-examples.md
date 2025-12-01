# Add Widget Samples to MCP Examples

## Description

Update the MCP server's example discovery system to include the new terminal widget samples (Rule, Panel, Table). This ensures AI assistants using the MCP server can discover and reference the widget examples.

**Related Issues**: [#89](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/89), [#90](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/90), [#91](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/91)

**Depends On**: Tasks #091, #092, #093 (all completed)

## Requirements

- Add widget sample entries to `samples/examples.json`
- Ensure MCP `get_example` tool can retrieve widget examples
- Use appropriate tags for discoverability

## Checklist

### Update examples.json
- [ ] Add `rule-widget` example entry
- [ ] Add `panel-widget` example entry
- [ ] Add `table-widget` example entry
- [ ] Use consistent tags: `widgets`, `terminal`, `output`

### Verify MCP Discovery
- [ ] Test `get_example` tool retrieves rule-widget-demo
- [ ] Test `get_example` tool retrieves panel-widget-demo
- [ ] Test `get_example` tool retrieves table-widget-demo
- [ ] Verify examples appear in `list_examples` output

## Notes

### New Example Entries

```json
{
  "id": "rule-widget",
  "name": "Rule Widget Demo",
  "description": "Horizontal divider lines with optional centered text - lightweight Spectre.Console Rule alternative",
  "path": "samples/rule-widget-demo/rule-widget-demo.cs",
  "tags": ["widgets", "terminal", "rule", "divider", "output", "spectre-alternative"],
  "difficulty": "beginner"
},
{
  "id": "panel-widget",
  "name": "Panel Widget Demo",
  "description": "Bordered boxes with headers and styled content - lightweight Spectre.Console Panel alternative",
  "path": "samples/panel-widget-demo/panel-widget-demo.cs",
  "tags": ["widgets", "terminal", "panel", "box", "border", "output", "spectre-alternative"],
  "difficulty": "beginner"
},
{
  "id": "table-widget",
  "name": "Table Widget Demo",
  "description": "Columnar data tables with alignment, borders, and styling - lightweight Spectre.Console Table alternative",
  "path": "samples/table-widget-demo/table-widget-demo.cs",
  "tags": ["widgets", "terminal", "table", "columns", "output", "spectre-alternative"],
  "difficulty": "intermediate"
}
```

### Tags Strategy

- `widgets` - Primary category for all widget samples
- `terminal` - Links to terminal output features
- `output` - General output formatting
- `spectre-alternative` - Highlights as Spectre.Console replacement
- Widget-specific: `rule`, `panel`, `table`, `divider`, `box`, `border`, `columns`

### File Location

`samples/examples.json` - Central manifest for MCP example discovery
