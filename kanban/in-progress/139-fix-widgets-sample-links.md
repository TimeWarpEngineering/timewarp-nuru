# Fix Broken Sample Links in Widgets Documentation

## Description

The widgets.md documentation references non-existent sample folders. Update links to point to actual locations.

## Requirements

- Fix broken links at bottom of features/widgets.md
- Verify all sample references are valid

## Checklist

- [ ] Update Rule Widget Demo link
- [ ] Update Panel Widget Demo link  
- [ ] Update Table Widget Demo link
- [ ] Verify links work (samples/terminal/ folder)

## Notes

Current broken links (lines 529-531):
```markdown
- [Rule Widget Demo](../../../samples/rule-widget-demo/)
- [Panel Widget Demo](../../../samples/panel-widget-demo/)
- [Table Widget Demo](../../../samples/table-widget-demo/)
```

Actual samples are in `samples/terminal/`:
- `samples/terminal/rule-widget.cs`
- `samples/terminal/panel-widget.cs`
- `samples/terminal/table-widget.cs`
