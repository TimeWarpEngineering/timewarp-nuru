# Type Converters

Demonstrates all 15 built-in type converters and custom type registration.

## Run It

```bash
dotnet run samples/10-type-converters/01-builtin-types.cs -- delay 1000
dotnet run samples/10-type-converters/01-builtin-types.cs -- fetch https://example.com
dotnet run samples/10-type-converters/01-builtin-types.cs -- ping 192.168.1.1
dotnet run samples/10-type-converters/01-builtin-types.cs -- report 2024-12-25
dotnet run samples/10-type-converters/02-custom-type-converters.cs -- color red
```

## What's Demonstrated

- **01-builtin-types**: All 15 built-in converters:
  - Original: `int`, `long`, `double`, `decimal`, `bool`, `DateTime`, `Guid`, `TimeSpan`
  - New in v2.1: `Uri`, `FileInfo`, `DirectoryInfo`, `IPAddress`, `DateOnly`, `TimeOnly`
- **02-custom-type-converters**: Registering custom `IRouteTypeConverter`

## Related Documentation

- [Routing](../../documentation/user/features/routing.md)
