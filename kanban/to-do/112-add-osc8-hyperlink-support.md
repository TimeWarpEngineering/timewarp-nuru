# Add OSC 8 Hyperlink Support

## Description

Add support for OSC 8 hyperlinks to enable clickable URLs in supported terminals (Windows Terminal, iTerm2, VS Code terminal, Hyper, Konsole, GNOME Terminal 3.26+).

GitHub Issue: #95

## Requirements

- String extension method `Link(string url)` for inline usage
- Terminal extension method `WriteLink()` for direct output
- Chainable with existing ANSI color extensions
- Graceful degradation on unsupported terminals (text displays without link)

## Checklist

### Implementation
- [ ] Create `ansi-hyperlink-extensions.cs` with string extension `Link()` method
- [ ] Create `terminal-hyperlink-extensions.cs` with `WriteLink()` and `WriteLinkLine()` methods
- [ ] Add `SupportsHyperlinks` property to `ITerminal` interface
- [ ] Implement `SupportsHyperlinks` in `NuruTerminal`
- [ ] Implement `SupportsHyperlinks` in `TestTerminal`

### Testing
- [ ] Create unit tests for hyperlink string extension
- [ ] Create unit tests for terminal hyperlink methods
- [ ] Test chaining with existing color extensions

### Documentation
- [ ] Create sample `samples/terminal/hyperlink-widget.cs`
- [ ] Add user documentation

## Notes

OSC 8 hyperlink format:
```
\e]8;;URL\e\\DISPLAY_TEXT\e]8;;\e\\
```

Proposed API:
```csharp
// String extension (chainable)
terminal.WriteLine($"Visit {"Ardalis.com".Link("https://ardalis.com").Cyan().Bold()}");

// Terminal method
terminal.WriteLink("https://ardalis.com", "Ardalis.com");
```

Analysis report: `.agent/workspace/2025-12-02T10-00-00_issue-95-osc8-hyperlink-analysis.md`

Terminal detection can use environment variables:
- `WT_SESSION` - Windows Terminal
- `TERM_PROGRAM=vscode` - VS Code
- `TERM_PROGRAM=iTerm.app` - iTerm2
- `KONSOLE_VERSION` - Konsole
- `VTE_VERSION` - GNOME Terminal
