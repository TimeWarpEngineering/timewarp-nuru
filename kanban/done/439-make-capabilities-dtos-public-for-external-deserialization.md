# Make capabilities DTOs public for external deserialization

## Description

Make the `--capabilities` JSON output DTOs public so external consumers can deserialize directly without duplicating model classes.

## Checklist

- [x] Change `CapabilitiesResponse` from `internal` to `public`
- [x] Change `GroupCapability` from `internal` to `public`
- [x] Change `CommandCapability` from `internal` to `public`
- [x] Change `ParameterCapability` from `internal` to `public`
- [x] Change `OptionCapability` from `internal` to `public`
- [x] Change `CapabilitiesJsonSerializerContext` from `internal` to `public`
- [x] Verify build succeeds

## Notes

These types were `internal sealed` originally. Users wanting to deserialize `--capabilities` JSON had to either:
1. Copy model classes into their own project
2. Use `JsonDocument`/`Dictionary` and navigate dynamically

Neither was ideal. Making them public enables consumers to:

```csharp
string json = await RunProcess("mytool", "--capabilities");
CapabilitiesResponse capabilities = JsonSerializer.Deserialize(
    json,
    CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse
);
```

## Results

All 6 types now `public`:

- `CapabilitiesResponse`
- `GroupCapability`
- `CommandCapability`
- `ParameterCapability`
- `OptionCapability`
- `CapabilitiesJsonSerializerContext`

Build succeeds with 0 warnings, 0 errors.
