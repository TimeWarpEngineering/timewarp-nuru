# Syntax Examples

Comprehensive reference for all route pattern syntax supported by TimeWarp.Nuru.

## Run It

```bash
dotnet run samples/04-syntax-examples/syntax-examples.cs
dotnet run samples/04-syntax-examples/syntax-examples.cs -- --help
```

## What's Demonstrated

- **Literals**: `status`, `git commit`
- **Parameters**: `{name}`, `{source} {destination}`
- **Typed parameters**: `{ms:int}`, `{amount:double}`, `{date:DateTime}`
- **Optional parameters**: `{tag?}`, `{seconds:int?}`
- **Catch-all**: `{*args}`, `{*params}`
- **Options**: `--verbose`, `--config {mode}`, `-c {mode}`
- **Option aliases**: `--config,-c {mode}`
- **Repeated options**: `--env {var}*`
- **Descriptions**: `{env|Target environment}`

## Related Documentation

- [Routing](../../documentation/user/features/routing.md)
