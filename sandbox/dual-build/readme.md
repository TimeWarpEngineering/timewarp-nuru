# Dual-Build Infrastructure (Task #241)

Part of Epic #239: Compile-time endpoint generation.

**Location**: `sandbox/` - experimental work, not a sample yet.

## Structure

```
sandbox/dual-build/
  shared/
    calc.cs       # Shared calculator source
  appA/
    appA.csproj   # Runtime path (current Nuru)
  appB/
    appB.csproj   # Generated path (stub, future)
```

## Build & Run

```bash
# Build both
dotnet build sandbox/dual-build/appA/appA.csproj
dotnet build sandbox/dual-build/appB/appB.csproj

# Run via dll (faster than dotnet run)
dotnet sandbox/dual-build/appA/bin/Debug/net10.0/calc-appA.dll add 3 5
dotnet sandbox/dual-build/appB/bin/Debug/net10.0/calc-appB.dll add 3 5
```

## Parity Testing

Both apps compile the same `shared/calc.cs` source.
Run the same commands against both and compare output.

```bash
# Should produce identical output
dotnet sandbox/dual-build/appA/bin/Debug/net10.0/calc-appA.dll add 10 20
dotnet sandbox/dual-build/appB/bin/Debug/net10.0/calc-appB.dll add 10 20
```

## Status

- **appA**: Uses current runtime Nuru (reference implementation)
- **appB**: Currently identical to appA (stub). Will be updated to use new source generator once implemented.
