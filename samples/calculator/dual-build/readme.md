# Dual-Build Infrastructure (Task #241)

Part of Epic #239: Compile-time endpoint generation.

## Structure

```
dual-build/
  shared/
    calc.cs       # Shared calculator source
  appA/
    appA.csproj   # Runtime path (current Nuru)
  appB/
    appB.csproj   # Generated path (stub, future)
```

## Build & Run

```bash
# AppA (runtime path - reference implementation)
dotnet build appA/appA.csproj
dotnet run --project appA/appA.csproj -- add 3 5

# AppB (generated path - stub for now)
dotnet build appB/appB.csproj
dotnet run --project appB/appB.csproj -- add 3 5
```

## Parity Testing

Both apps compile the same `shared/calc.cs` source.
Run the same commands against both and compare output.

```bash
# Should produce identical output
dotnet run --project appA/appA.csproj -- add 10 20
dotnet run --project appB/appB.csproj -- add 10 20
```

## Status

- **appA**: Uses current runtime Nuru (reference implementation)
- **appB**: Currently identical to appA (stub). Will be updated to use new source generator once implemented.
