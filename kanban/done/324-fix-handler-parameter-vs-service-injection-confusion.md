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

- [x] Find where handler parameters are analyzed for service injection
- [x] Add check: if parameter name matches a route parameter, it's NOT a service
- [x] Ensure built-in types (`IPAddress`, `FileInfo`, etc.) are recognized as route-bindable
- [x] Test with various built-in types in handlers
- [x] Verify `01-builtin-types.cs` sample progresses

## Key Files Modified

- `source/timewarp-nuru-analyzers/generators/extractors/handler-extractor.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs`

## Results

### Problem 1: Built-in types treated as services
**Root cause:** `IsServiceType()` checked if type name starts with `I` + uppercase (to detect interfaces like `ILogger`), but this matched `IPAddress` which is a CLASS.

**Fix:** Added `IsBuiltInRouteBindableType()` helper that returns `true` for all 21 built-in types (`IPAddress`, `FileInfo`, `DirectoryInfo`, etc.). `IsServiceType()` now checks this first and returns `false` for built-in types.

### Problem 2: Handler parameter names vs route segment names
**Root cause:** When route pattern uses `{path:FileInfo}` but handler uses `(FileInfo file)`, the generator created a variable `path` but tried to pass `file` to the local function.

**Fix:** Added `BuildArgumentListFromRoute()` that matches handler parameters to route parameters by **position**, then uses the **route segment name** (which is the generated variable name) when calling the local function. The local function signature uses the **handler parameter name** so the lambda body works correctly.

### Test Results
All commands in `01-builtin-types.cs` now work:
- `delay 100` ✅
- `ping 192.168.1.1` ✅ (IPAddress)
- `connect 10.0.0.1 8080` ✅ (IPAddress + int)
- `read /etc/passwd` ✅ (FileInfo with name mismatch path→file)
- `list /tmp` ✅ (DirectoryInfo with name mismatch path→dir)

### Remaining Issues (not in scope)
- NURU_H002 false positives - see task #307
