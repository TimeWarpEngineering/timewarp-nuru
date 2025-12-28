# Short-only options parsed incorrectly as empty long option

## Description

When a route pattern uses short-only options like `-i {i:int}`, the parser/emitter incorrectly generates `"--"` as the long option name instead of recognizing it as a short-only option.

## Symptom

```csharp
// Generated code with wrong option names:
if (args[i] == "--" || args[i] == "-i")  // Should be just "-i" or "--i" 
```

And in PrintCapabilities:
```json
{
  "long": "--",      // Wrong - should be null or "-i" expanded
  "short": "-i",
  "required": true,
  "isFlag": false
}
```

## Expected

For `-i {i:int}`:
- Either recognize as short-only option (no long form)
- Or expand to `--i` as the long form

## Root Cause

The option parser is treating `-i` as having an empty long form `"--"` rather than:
1. Recognizing it has no long form, or
2. Deriving `--i` from the short form

## Checklist

- [ ] Investigate how option patterns are parsed
- [ ] Determine if short-only options should have a derived long form
- [ ] Fix the parser/model to handle short-only options correctly
- [ ] Fix the emitter to generate correct comparison code
- [ ] Add test cases for short-only options
- [ ] Verify bench-nuru-full compiles after fix

## Discovered In

`benchmarks/aot-benchmarks/bench-nuru-full/Program.cs` with pattern `--str {str} -i {i:int} -b`

## Files to Investigate

- Route pattern parser/lexer
- Option definition model
- `source/timewarp-nuru-analyzers/generators/emitters/` - emitter code
