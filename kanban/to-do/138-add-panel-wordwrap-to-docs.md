# Add Panel WordWrap Property to Documentation

## Description

Document the new `WordWrap` property on the Panel widget added in v3.0.0-beta.19.

## Requirements

- Add to Panel Properties table in features/widgets.md
- Add to PanelBuilder Methods table
- Include example showing long text wrapping

## Checklist

- [ ] Add WordWrap to Panel Properties table (default: true)
- [ ] Add .WordWrap(bool) to PanelBuilder Methods table
- [ ] Add example showing automatic text wrapping
- [ ] Mention ANSI-aware wrapping preserves color codes

## Notes

Source: `source/timewarp-nuru-core/io/widgets/panel-widget.cs:79`

```csharp
/// Gets or sets whether to wrap long text at word boundaries.
/// Defaults to true.
public bool WordWrap { get; set; } = true;
```
