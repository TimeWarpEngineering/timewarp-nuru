# Update LangVersion to 'latest' After .NET 10 Release

## Context

After .NET 10 is released (November 2025), C# 14 will become the "latest" language version. We're using `<LangVersion>preview</LangVersion>` to access C# 14 extension block syntax.

## Task

Update `Directory.Build.props` line 29:

```xml
<!-- FROM -->
<LangVersion>preview</LangVersion>

<!-- TO -->
<LangVersion>latest</LangVersion>
```

## When

After .NET 10 RTM release (expected November 2025).

## Notes

- .NET 10 SDK 10.0.100 is now released
- C# 14 is the "latest" version, enabling the change from `preview` to `latest`

## Why

- Using `preview` after release is unnecessary
- `latest` is the standard convention for released language versions
- Reduces confusion about why preview features are enabled

## Files to Update

- `/Directory.Build.props` (line 29)

## Verification

After update, build the solution to ensure all C# 14 features still work:

```bash
dotnet build timewarp-nuru.slnx -c Release
```

Should build with 0 errors and 0 warnings.

## Results

- Changed `<LangVersion>preview</LangVersion>` to `<LangVersion>latest</LangVersion>` in Directory.Build.props line 20
- Build verification passed: `dotnet build timewarp-nuru.slnx -c Release` completed with 0 warnings and 0 errors
- .NET 10 SDK 10.0.100 is released, C# 14 is now the "latest" version
- All C# 14 features continue to work with `latest` instead of `preview`
