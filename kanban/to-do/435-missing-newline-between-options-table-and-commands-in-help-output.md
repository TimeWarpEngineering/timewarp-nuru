# Missing newline between Options table and Commands in help output

## Description

When running `ganda --help`, there is no blank line between the Options table and the "Commands:" heading. The output looks cramped:

```
Options:
┌────────────────┬────────────────────────────────┐
│ --help, -h     │ Show this help message         │
│ --version      │ Show version information       │
│ --capabilities │ Show capabilities for AI tools │
└────────────────┴────────────────────────────────┘
Commands:
```

Expected — a blank line between the table and "Commands:":

```
Options:
┌────────────────┬────────────────────────────────┐
│ --help, -h     │ Show this help message         │
│ --version      │ Show version information       │
│ --capabilities │ Show capabilities for AI tools │
└────────────────┴────────────────────────────────┘

Commands:
```

## Checklist

- [ ] Locate the help emitter code that renders the Options and Commands sections
- [ ] Add a newline after the Options table before the Commands heading
- [ ] Verify fix with `ganda --help`

## Notes

- Observed in ganda v1.0.0-beta.20
- Likely in the generated `PrintHelp()` method from `help-emitter.cs`
