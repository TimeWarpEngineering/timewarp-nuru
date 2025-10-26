# Investigate How Web SDK Changes Working Directory for File-Based Apps

## Problem

When using `#:sdk Microsoft.NET.Sdk.Web` in a runfile, the current working directory (CWD) is automatically set to the script's directory, allowing configuration files like `{appname}.settings.json` to be found regardless of where the script is executed from.

Without the Web SDK, CWD remains where the user executed the script from, causing configuration files to not be found.

## Current Behavior

**With `#:sdk Microsoft.NET.Sdk.Web`:**
```bash
cd /anywhere
./Samples/Configuration/script.cs
# CWD inside script: /path/to/Samples/Configuration/ ✓
```

**Without Web SDK:**
```bash
cd /anywhere  
./Samples/Configuration/script.cs
# CWD inside script: /anywhere ✗
```

## Investigation Results

We've confirmed:
- NOT an assembly attribute
- NOT an AppContext data value (`ENTRY_POINT_FILE_PATH` is empty)
- NOT MSBuild properties
- Happens at **runtime**, not build time
- No obvious source code in ASP.NET DefaultBuilder

## Possible Causes

1. **Dotnet host behavior** - The native dotnet executable may check SDK type and change CWD
2. **Hidden module initializer** - Web SDK may inject code that runs before Main
3. **Runtime configuration** - Some `.runtimeconfig.json` setting we haven't found
4. **Application host model** - ASP.NET uses different hosting that changes directory

## Next Steps

Waiting for response from Damien Edwards (@DamianEdwards) on Twitter: https://x.com/StevenTCramer/status/1982309927361327452

Once we understand the mechanism, we can either:
1. Replicate it in TimeWarp.Nuru's build process
2. Add a lightweight package that provides just this behavior
3. Make `AddConfiguration()` smarter about finding settings files

## Workaround

For now, NuruApp configuration samples must be run from their containing directory, or use `#:sdk Microsoft.NET.Sdk.Web` (which pulls in all of ASP.NET).

## References

- Twitter thread: https://x.com/StevenTCramer/status/1982309927361327452
- .NET file-based apps spec: https://github.com/dotnet/sdk/blob/main/documentation/general/dotnet-run-file.md
