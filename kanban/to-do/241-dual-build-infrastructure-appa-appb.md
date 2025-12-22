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

- [ ] Choose sample app for dual-build (calculator recommended)
- [ ] Create `calc-appA.csproj` referencing current runtime packages
- [ ] Create `calc-appB.csproj` referencing new source generator
- [ ] Verify both build successfully
- [ ] Document build commands in README or notes
- [ ] Add CI job to build both (optional, for parity test CI)

## Notes

### Project Structure

```
samples/calculator/
  calc.cs                    # Shared source
  calc-appA.csproj           # Runtime path
  calc-appB.csproj           # Generated path
```

### Build Commands

```bash
dotnet build calc-appA.csproj    # Builds with runtime Nuru
dotnet build calc-appB.csproj    # Builds with generated code
```

### Alternative: Single csproj with flag

```xml
<PropertyGroup Condition="'$(NuruGenMode)' == 'true'">
  <DefineConstants>NURU_GENERATED</DefineConstants>
</PropertyGroup>
```

```bash
dotnet build                           # AppA
dotnet build -p:NuruGenMode=true       # AppB
```

Choose whichever approach is simpler to maintain.
