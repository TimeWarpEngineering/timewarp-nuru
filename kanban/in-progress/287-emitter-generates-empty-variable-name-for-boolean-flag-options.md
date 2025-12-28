# Emitter generates empty variable name for boolean flag options

## Description

The code emitter generates an empty variable name for boolean flag options, causing a CS1001 "Identifier expected" compiler error.

## Symptom

```csharp
// Generated code with missing identifier:
bool  = Array.Exists(args, a => a == "--" || a == "-b");  // CS1001 - no name!
```

Error:
```
CS1001: Identifier expected
```

## Expected

```csharp
bool b = Array.Exists(args, a => a == "--b" || a == "-b");
```

## Root Cause

The flag parameter name is not being extracted or emitted correctly. For pattern `-b`, the parameter name should be `b`, but it's being emitted as empty.

## Checklist

- [ ] Locate the emitter code that generates boolean flag extraction
- [ ] Trace how flag parameter names are extracted from the route pattern
- [ ] Fix the extraction/emission to include the parameter name
- [ ] Add test case with boolean flag
- [ ] Verify bench-nuru-full compiles after fix

## Discovered In

`benchmarks/aot-benchmarks/bench-nuru-full/Program.cs` with pattern `--str {str} -i {i:int} -b`

## Files to Investigate

- `source/timewarp-nuru-analyzers/generators/emitters/` - emitter code
- Route pattern parser - how `-b` is parsed
