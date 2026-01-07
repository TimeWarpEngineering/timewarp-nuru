# Fix handler parameter vs service injection confusion

## Summary

The generator incorrectly treats handler parameters of certain types (like `IPAddress`) as service injection requests, even when the parameter name matches a route parameter.

## Example

**Route:** `connect {host:ipaddress} {port:int}`

**Handler:** `(IPAddress host, int port) => ...`

**Generated (broken):**
```csharp
// Route binding - CORRECT
global::System.Net.IPAddress host = global::System.Net.IPAddress.Parse(__host_6);

// Service injection - WRONG (duplicate, overwrites route value)
global::System.Net.IPAddress host = default!; // Service not registered
```

The generator emits BOTH a route parameter binding AND a service injection attempt for `host`, causing a duplicate variable declaration error.

## Root Cause

The generator's logic for determining "is this a service to inject?" vs "is this a route parameter to bind?" is flawed. It appears to treat any non-primitive type in handler parameters as a service.

## Affected Sample

- `samples/10-type-converters/01-builtin-types.cs`

## Blocks

- #313 - Fix generator type resolution for built-in types

## Checklist

- [ ] Find where handler parameters are analyzed for service injection
- [ ] Add check: if parameter name matches a route parameter, it's NOT a service
- [ ] Ensure built-in types (`IPAddress`, `FileInfo`, etc.) are recognized as route-bindable
- [ ] Test with various built-in types in handlers
- [ ] Verify `01-builtin-types.cs` sample progresses

## Key Files to Investigate

- `source/timewarp-nuru-analyzers/generators/extractors/handler-extractor.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/service-resolver-emitter.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs`

## Notes

The logic should be:
1. Match handler parameter names to route parameters/options
2. If matched → bind from route
3. If NOT matched → attempt service injection
4. Never do both for the same parameter
