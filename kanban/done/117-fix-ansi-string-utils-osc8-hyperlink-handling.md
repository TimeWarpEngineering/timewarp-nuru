# Fix AnsiStringUtils OSC 8 Hyperlink Handling

## Description

The Table widget incorrectly calculates column widths when cell content contains OSC 8 hyperlink escape sequences. The `AnsiStringUtils.GetVisibleLength()` method only strips CSI color codes (`\x1b[...m`) but ignores OSC 8 hyperlink sequences (`\x1b]8;;URL\x1b\`), causing invisible URL bytes to be counted as visible characters. This results in massively oversized columns when tables contain hyperlinks.

## Requirements

- `AnsiStringUtils.StripAnsiCodes()` must strip OSC 8 hyperlink sequences
- `AnsiStringUtils.GetVisibleLength()` must return correct visible length for hyperlinked text
- Tables with hyperlinked cells must render with correct column widths

## Checklist

### Implementation
- [x] Update `AnsiPattern` regex to include OSC 8 sequences (also supports BEL terminator)
- [x] Add test: `Should_strip_osc8_hyperlink_sequences`
- [x] Add test: `Should_get_visible_length_with_hyperlinks`
- [x] Add test: `Should_handle_styled_hyperlinks`
- [x] Add test: `Should_strip_osc8_hyperlink_with_bel_terminator`
- [x] Add test: `Should_handle_multiple_hyperlinks_in_text`
- [ ] Verify table rendering with `dnx ardalis dotnetconf-score`

## Notes

**Root Cause**: The regex pattern in `ansi-string-utils.cs` line 12 only matches CSI sequences:
```csharp
private const string AnsiPattern = @"\x1b\[[0-9;]*m";
```

This pattern does not match OSC 8 hyperlink format:
```
\x1b]8;;{URL}\x1b\{displayText}\x1b]8;;\x1b\
```

**Example Impact**: A title "Clean Architecture with ASP.NET Core 10" (39 visible chars) with a 50+ char YouTube URL causes `GetVisibleLength()` to return 100+ instead of 39.

**Files to Modify**:
- `source/timewarp-nuru-core/io/widgets/ansi-string-utils.cs`
- `tests/timewarp-nuru-core-tests/ansi-string-utils-01-basic.cs`

**Analysis Document**: `.agent/workspace/2025-12-06T14-30-00_table-widget-osc8-hyperlink-bug.md`
