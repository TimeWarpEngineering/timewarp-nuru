# Short-only options parsed incorrectly as empty long option

## Status: Fixed by #287

This issue was fixed as part of task #287. The root cause was the same: `OptionDefinition.LongForm` being non-nullable caused null values (for short-only options) to be coerced to empty strings.

## Description

When a route pattern uses short-only options like `-i {i:int}`, the parser/emitter incorrectly generates `"--"` as the long option name instead of recognizing it as a short-only option.

## Symptom

```csharp
// Generated code with wrong option names:
if (args[i] == "--" || args[i] == "-i")  // Should be just "-i"
```

## Solution

Making `OptionDefinition.LongForm` nullable (`string?`) and updating all consumers to properly handle null values. Short-only options now correctly generate:

```csharp
if (args[__idx] == "-i")  // Only short form check
```

## Discovered In

`benchmarks/aot-benchmarks/bench-nuru-full/Program.cs` with pattern `--str {str} -i {i:int} -b`
