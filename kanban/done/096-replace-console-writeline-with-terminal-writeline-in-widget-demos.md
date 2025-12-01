# Replace Console.WriteLine with terminal.WriteLine in widget demos

## Description

The widget demo samples (panel-widget-demo, rule-widget-demo, table-widget-demo) inconsistently use Console.WriteLine() for descriptive text while using terminal.WritePanel()/WriteRule()/WriteTable() for the actual widget output. Since the demos already get an ITerminal instance via NuruTerminal.Default, they should consistently use terminal.WriteLine() throughout to:

1. Demonstrate proper usage of the ITerminal abstraction
2. Show that ITerminal is the unified API for all terminal output
3. Make the demos testable by allowing injection of a test terminal

## Affected Files

- samples/panel-widget-demo/panel-widget-demo.cs
- samples/rule-widget-demo/rule-widget-demo.cs
- samples/table-widget-demo/table-widget-demo.cs

## Checklist

### Implementation
- [ ] All Console.WriteLine() calls replaced with terminal.WriteLine()
- [ ] All Console.Write() calls replaced with terminal.Write()
- [ ] Demos still run correctly and produce the same visual output
- [ ] Code is consistent in using ITerminal abstraction throughout
