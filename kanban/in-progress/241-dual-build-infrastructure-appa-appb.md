# Dual-build infrastructure (AppA/AppB)

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Set up temporary infrastructure to build the same source code two ways:
- **AppA** - Uses current runtime path (reference implementation)
- **AppB** - Uses new source-generated path

This is a migration scaffold. Once AppB is proven correct via parity tests, AppA infrastructure gets deleted.

## Requirements

- Same source file(s) compile to both outputs
- Clear separation of which csproj produces which output
- Both outputs runnable for comparison testing
- Easy to delete AppA infrastructure later

## Checklist

- [x] Choose sample app for dual-build (calculator)
- [x] Create appA.csproj referencing current runtime packages
- [x] Create appB.csproj (stub, same as appA until generator implemented)
- [x] Verify both build successfully
- [x] Document build commands in README
- [ ] Add CI job to build both (optional, for parity test CI)

## Notes

### Project Structure (Updated)

```
samples/calculator/dual-build/
  shared/
    calc.cs           # Shared source
  appA/
    appA.csproj       # Runtime path (reference)
  appB/
    appB.csproj       # Generated path (stub for now)
  readme.md           # Build/run instructions
```

Separate directories avoid MSBuild conflicts that occur with multiple .csproj files in same directory.

### Build Commands

```bash
# Build
dotnet build samples/calculator/dual-build/appA/appA.csproj
dotnet build samples/calculator/dual-build/appB/appB.csproj

# Run (via dll for reliability)
dotnet samples/calculator/dual-build/appA/bin/Debug/net10.0/calc-appA.dll add 3 5
dotnet samples/calculator/dual-build/appB/bin/Debug/net10.0/calc-appB.dll add 3 5
```

### Parity Verified

Both apps produce identical output:
- `add 3 5` -> `3 + 5 = 8`
- `multiply 7 6` -> `7 * 6 = 42`
- `divide 10 0` -> `Error: Division by zero`

## Results

**Completed 2024-12-23**

Created dual-build infrastructure in `samples/calculator/dual-build/`:
- `shared/calc.cs` - Calculator with add, subtract, multiply, divide commands
- `appA/appA.csproj` - Current runtime Nuru (reference implementation)
- `appB/appB.csproj` - Stub (identical to appA until new generator ready)

Both build and run with identical output, ready for parity testing.
