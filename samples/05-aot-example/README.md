# AOT Example

Demonstrates Native AOT compilation with TimeWarp.Nuru - zero configuration required.

## Run It

```bash
# Run as interpreted
dotnet run samples/05-aot-example/aot-example.cs -- hello
dotnet run samples/05-aot-example/aot-example.cs -- greet World

# Publish as native AOT binary
dotnet publish samples/05-aot-example/aot-example.cs -c Release -r linux-x64
dotnet publish samples/05-aot-example/aot-example.cs -c Release -r osx-arm64
dotnet publish samples/05-aot-example/aot-example.cs -c Release -r win-x64
```

## What's Demonstrated

- Full AOT compatibility with no IL2XXX/IL3XXX warnings
- Typed parameters work in AOT
- Optional parameters work in AOT
- Boolean options work in AOT
- Async handlers work in AOT
- Result: ~10 MB native binary with instant startup

## Related Documentation

- [Overview](../../documentation/user/features/overview.md)
