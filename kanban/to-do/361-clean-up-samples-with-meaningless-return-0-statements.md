# Clean up samples with meaningless return 0 statements

## Description

Several sample files have handlers that `return 0` for no semantic reason. These appear to be leftover from testing the "handler returns int" feature. Handlers should only return values when the return value is meaningful (e.g., exit codes, counts, results).

## Checklist

- [ ] Review `samples/10-type-converters/02-custom-type-converters.cs` - handlers like `send-email`, `set-theme`, `release`, `notify`, `deploy` all return 0 meaninglessly
- [ ] Check other samples in `samples/` for similar patterns
- [ ] Remove `return 0` where it has no semantic meaning (convert to void handlers)
- [ ] Keep `return 0/1` only where it represents a meaningful exit code or result

## Notes

Example of bad code (current):
```csharp
.WithHandler((EmailAddress to, string subject) =>
{
  Console.WriteLine($"Sending Email to {to}");
  return 0;  // meaningless - not used for anything
})
```

Should be:
```csharp
.WithHandler((EmailAddress to, string subject) =>
{
  Console.WriteLine($"Sending Email to {to}");
})
```

Files identified with this issue:
- `samples/10-type-converters/02-custom-type-converters.cs` (lines 52, 64, 80, 91, 103)
