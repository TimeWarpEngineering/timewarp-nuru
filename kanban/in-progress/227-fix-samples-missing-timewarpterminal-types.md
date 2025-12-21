# Fix samples missing TimeWarp.Terminal types

## Description

Terminal widget samples are missing references to TimeWarp.Terminal types: `ITerminal`, `NuruTerminal`, `Table`, `Panel`, `Rule`, `LineStyle`, `BorderStyle`, `Alignment`, `AnsiColors`, and color extension methods (`.Cyan()`, `.Green()`, `.Yellow()`, `.Gray()`, `.Link()`).

Errors:
- `CS0246: The type or namespace name 'ITerminal' could not be found`
- `CS0246: The type or namespace name 'Table' could not be found`
- `CS0103: The name 'NuruTerminal' does not exist`
- `CS0103: The name 'LineStyle' does not exist`
- `CS1061: 'string' does not contain a definition for 'Cyan'`

## Checklist

- [ ] samples/terminal/hyperlink-widget.cs
- [ ] samples/terminal/panel-widget.cs
- [ ] samples/terminal/rule-widget.cs
- [ ] samples/terminal/table-widget.cs

## Notes

- Discovered by `runfiles/verify-samples.cs` (task 221)
- May need to add `#:project ../../source/timewarp-terminal/timewarp-terminal.csproj`
- May also need `using TimeWarp.Terminal;` or similar
