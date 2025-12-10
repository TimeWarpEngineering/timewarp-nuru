# Fix Panel Widget Text Wrapping

## Description

The Panel widget does not wrap long text that exceeds the panel width, causing content to overflow past the right border. This was observed comparing Sceptre 1.18.0 (which wraps correctly) to the current Nuru implementation.

## Problem

When content text is longer than the available `contentAreaWidth`, the Panel renders the full text on a single line, extending past the panel's right border character (`│`).

**Example command to reproduce:**
```bash
dnx ardalis courses
```

Long course descriptions overflow the panel border instead of wrapping to multiple lines.

## Root Cause

Located in `source/timewarp-nuru-core/io/widgets/panel-widget.cs`:

1. `RenderWithBorder()` (lines 117-119) splits content by `\n` only - no wrapping performed
2. `RenderContentRow()` calls `AnsiStringUtils.PadRightVisible()`
3. `PadRightVisible()` returns text unchanged when it exceeds width (no truncation or wrapping)

## Requirements

- Panel must wrap long content text at word boundaries to fit within `contentAreaWidth`
- Word wrapping must preserve ANSI color/style codes across line breaks
- Word wrapping must handle OSC 8 hyperlink sequences correctly
- Consider adding a `WordWrap` property (default: true) to allow disabling wrapping

## Checklist

### Implementation
- [x] Add `WrapText()` method to `AnsiStringUtils` that wraps text at word boundaries while preserving ANSI codes
- [x] Update `Panel.RenderWithBorder()` to wrap each content line before rendering
- [x] Add `WordWrap` property to `Panel` class (default: true)
- [x] Update `PanelBuilder` with `WordWrap()` method

### Testing
- [x] Add test: `Should_wrap_long_content_within_panel_width`
- [x] Add test: `Should_wrap_content_preserving_ansi_codes`
- [x] Add test: `Should_not_wrap_when_WordWrap_is_false`
- [x] Add test: `Should_wrap_content_with_hyperlinks`
- [ ] Verify with `dnx ardalis courses` command

### Documentation
- [ ] Update Panel widget documentation if needed

## Notes

**Test case to reproduce:**
```csharp
public static async Task Should_wrap_long_content_within_panel_width()
{
    // Arrange
    var longText = "Learn how to get started with building web products with ASP.NET Core. This course covers the fundamentals of ASP.NET Core web development.";
    Panel panel = new() { Content = longText, Width = 80 };

    // Act
    string[] lines = panel.Render(80);

    // Assert - All content lines should fit within the panel width
    foreach (string line in lines)
    {
        int visibleLength = AnsiStringUtils.GetVisibleLength(line);
        visibleLength.ShouldBeLessThanOrEqualTo(80);
    }
    
    // Should have multiple content lines due to wrapping
    lines.Length.ShouldBeGreaterThan(3); // top + at least 2 content lines + bottom
}
```

**Comparison:**
| Feature | Sceptre (1.18.0) | Nuru (current) |
|---------|------------------|----------------|
| Text wrapping | ✅ Wraps at word boundaries | ❌ No wrapping |
| Long lines | Multi-line display | Single line, overflows border |
