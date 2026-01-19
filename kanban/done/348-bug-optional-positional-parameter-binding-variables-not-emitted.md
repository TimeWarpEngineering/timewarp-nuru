# Bug: Tests use unsupported closure pattern - need to use TestTerminal

## Description

**This is NOT a generator bug.** The routing tests use closures to capture handler parameter values, but closures are explicitly not supported (NURU_H002 analyzer warns against them).

The tests need to be rewritten to use TestTerminal output capture instead of closures.

## The Problem

Tests use this pattern:
```csharp
string? boundSource = null;  // Variable in test method
NuruCoreApp app = NuruApp.CreateBuilder([])
  .Map("backup {source} --compress --output {dest}")
  .WithHandler((string source, bool compress, string dest) =>
  {
    boundSource = source;  // CLOSURE - not supported!
  })
  .Build();

await app.RunAsync([...]);
boundSource.ShouldBe("something");  // Assert captured value
```

The generator extracts the lambda body and emits it as a local function, but `boundSource` doesn't exist in that scope.

## The Solution

Rewrite tests to use TestTerminal output capture:
```csharp
using TestTerminal terminal = new();
NuruCoreApp app = NuruApp.CreateBuilder([])
  .UseTerminal(terminal)
  .Map("backup {source} --compress --output {dest}")
  .WithHandler((string source, bool compress, string dest) =>
    $"source={source}|compress={compress}|dest={dest}")  // Return value, no closure
  .AsQuery().Done()
  .Build();

await app.RunAsync([...]);
terminal.OutputContains("source=something").ShouldBeTrue();
terminal.OutputContains("compress=True").ShouldBeTrue();
terminal.OutputContains("dest=mydest").ShouldBeTrue();
```

## Files to Fix

- `routing-14-option-order-independence.cs` - uses `boundSource`, `boundDest`, `boundCompress`, `boundAlpha`, `boundBeta`, `boundGamma`

## Checklist

- [ ] Rewrite routing-14 tests to use TestTerminal output capture
- [ ] Verify tests compile and pass
- [ ] Check for any other routing tests using closure pattern

## Notes

- NURU_H002 analyzer explicitly warns: "Handler lambda captures external variable(s). Lambdas with closures are not supported."
- The generator was working correctly - it just can't support closures
- TestTerminal output capture is the supported pattern for testing handler parameter binding
