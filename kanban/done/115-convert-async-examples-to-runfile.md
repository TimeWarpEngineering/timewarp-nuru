# Convert async-examples to Runfile

## Description

Convert the `samples/async-examples` sample from a traditional csproj+program.cs structure to a single-file runfile pattern, matching the convention used by other samples in the repository.

## Requirements

- Remove `samples/async-examples/async-examples.csproj`
- Convert `samples/async-examples/program.cs` to `samples/async-examples/async-examples.cs`
- Add shebang `#!/usr/bin/dotnet --`
- Add `#:project` directive referencing the appropriate timewarp-nuru project
- Verify the runfile executes correctly

## Checklist

### Implementation
- [ ] Rename `program.cs` to `async-examples.cs`
- [ ] Add shebang and project directive
- [ ] Remove `async-examples.csproj`
- [ ] Verify runfile runs with `./async-examples.cs`
- [ ] Update `samples/examples.json` if needed

## Notes

Reference pattern from other samples:
```csharp
#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;
// ... rest of code
```
